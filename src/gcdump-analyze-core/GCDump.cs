using Graphs;
using Microsoft.Diagnostics.Tracing;

namespace DotNet.GCDump.Analyze;

/// <summary>
/// Represents a *.gcdump file and exposes APIs to analyze it.
/// NOTE: This initial implementation uses a deterministic pseudo-analysis derived from the file bytes
/// to keep behavior stable in tests without relying on internal parsing libraries.
/// It can be swapped with a real parser later without changing the public surface.
/// </summary>
public sealed class GCDump : IDisposable
{
    private readonly Stream _data;
    private MemoryGraph? _graph;

    private GCDump(Stream data) => _data = data;

    /// <summary>
    /// Opens a GCDump from a file path.
    /// </summary>
    public static GCDump Open(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Path must be a non-empty string.", nameof(path))
            : new GCDump(File.OpenRead(path));
    }

    /// <summary>
    /// Opens a GCDump from an input stream. The stream will be copied into memory and can be disposed by the caller.
    /// </summary>
    public static GCDump Open(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new GCDump(stream);
    }

    /// <summary>
    /// Generate a report of object types ordered by inclusive size.
    /// Uses TraceEvent's MemoryGraph and SpanningTree to calculate per-object retained sizes,
    /// and aggregates by type name.
    /// </summary>
    public TableReport GetReportByInclusiveSize(int rows)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), "Must be greater than zero.");
        EnsureLoaded();

        var headers = new[]
        {
            "Object Type",
            "Count",
            "Size (Bytes)",
            "Inclusive Size (Bytes)"
        };

        var list = BuildTypeAggregates(headers, maxRows: rows, sortByInclusive: true);

        return new TableReport(headers, list);
    }

    /// <summary>
    /// Generate a report of object types ordered by shallow size (Size (Bytes)).
    /// </summary>
    public TableReport GetReportBySize(int rows)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), "Must be greater than zero.");
        EnsureLoaded();

        var headers = new[]
        {
            "Object Type",
            "Count",
            "Size (Bytes)",
            "Inclusive Size (Bytes)"
        };

        var list = BuildTypeAggregates(headers, maxRows: rows, sortByInclusive: false);

        return new TableReport(headers, list);
    }

    public void Dispose() => _data.Dispose();

    private void EnsureLoaded()
    {
        if (_graph is not null)
            return;

        // Build MemoryGraph from the gcdump stream or path.
        // Prefer using the original stream to avoid re-IO if we have it.
        _data.Position = 0;

        try
        {
            var heapDump = new GCHeapDump(_data, _data.ToString());

            _graph = heapDump.MemoryGraph ?? throw new InvalidOperationException("GCHeapDump.MemoryGraph returned null.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to open or parse the .gcdump file.", ex);
        }
    }

    private List<TableRow> BuildTypeAggregates(IReadOnlyList<string> headers, int maxRows, bool sortByInclusive)
    {
        var graph = _graph ?? throw new InvalidOperationException("Graph not loaded.");

        // Compute retained sizes per node using SpanningTree dominators.
        var retained = new ulong[(int)graph.NodeIndexLimit];
        var nodeStorage = graph.AllocNodeStorage();
        for (NodeIndex i = 0; i < graph.NodeIndexLimit; i++)
        {
            var node = graph.GetNode(i, nodeStorage);
            retained[(int)i] = (ulong)node.Size;
        }

        // Build a post-order index for propagation similar to HeapSnapshot.
        var postOrder = BuildPostOrderIndex(graph);

        var spanningTree = new SpanningTree(graph, TextWriter.Null);
        spanningTree.ForEach(null!);

        int nodeCount = (int)graph.NodeIndexLimit;
        for (int p = 0; p < nodeCount - 1; ++p) // Exclude the root (last in post-order)
        {
            int nodeIndex = postOrder[p];
            int dominatorOrdinal = (int)spanningTree.Parent((NodeIndex)nodeIndex);
            if (dominatorOrdinal >= 0)
            {
                retained[dominatorOrdinal] += retained[nodeIndex];
            }
        }

        var typeStorage = graph.AllocTypeNodeStorage();
        var byType = new Dictionary<string, (long Count, long Size, long Inclusive)>();

        for (NodeIndex i = 0; i < graph.NodeIndexLimit; i++)
        {
            if (i == graph.RootIndex) continue; // skip root
            var node = graph.GetNode(i, nodeStorage);
            var type = graph.GetType(node.TypeIndex, typeStorage);
            string name = type.Name;

            if (!byType.TryGetValue(name, out var agg)) agg = default;
            agg.Count += 1;
            agg.Size += node.Size;

            // Attribute retained size only when the immediate dominator is a different type,
            // avoiding double-counting long chains of same-type instances.
            long inc = unchecked((long)retained[(int)i]);
            var parentIndex = spanningTree.Parent(i);
            if (parentIndex != NodeIndex.Invalid)
            {
                var pNode = graph.GetNode(parentIndex, nodeStorage);
                var pType = graph.GetType(pNode.TypeIndex, typeStorage);
                if (pType.Name == name)
                {
                    inc = 0; // skip attribution to avoid within-type double counting
                }
            }

            agg.Inclusive += inc;
            byType[name] = agg;
        }

        var rows = byType.Select(kvp => new TableRow(new Dictionary<string, object?>
        {
            [headers[0]] = kvp.Key,
            [headers[1]] = kvp.Value.Count,
            [headers[2]] = kvp.Value.Size,
            [headers[3]] = kvp.Value.Inclusive,
        }));

        rows = sortByInclusive
            ? rows.OrderByDescending(r => Convert.ToInt64(r[headers[3]]))
                   .ThenByDescending(r => Convert.ToInt64(r[headers[2]]))
                   .ThenBy(r => (string)r[headers[0]]!)
            : rows.OrderByDescending(r => Convert.ToInt64(r[headers[2]]))
                   .ThenByDescending(r => Convert.ToInt64(r[headers[3]]))
                   .ThenBy(r => (string)r[headers[0]]!);

        var top = rows
            .Take(maxRows)
            .ToList();
        return top;
    }

    private static int[] BuildPostOrderIndex(MemoryGraph graph)
    {
        var postOrderIndex2NodeIndex = new int[(int)graph.NodeIndexLimit];
        var visited = new System.Collections.BitArray((int)graph.NodeIndexLimit);
        var nodeStack = new Stack<Node>();
        int postOrderIndex = 0;

        var rootNode = graph.GetNode(graph.RootIndex, graph.AllocNodeStorage());
        rootNode.ResetChildrenEnumeration();
        nodeStack.Push(rootNode);

        while (nodeStack.Count > 0)
        {
            var currentNode = nodeStack.Peek();
            NodeIndex nextChild = currentNode.GetNextChildIndex();
            if (nextChild != NodeIndex.Invalid)
            {
                if (visited.Get((int)nextChild))
                    continue;
                var childNode = graph.GetNode(nextChild, graph.AllocNodeStorage());
                childNode.ResetChildrenEnumeration();
                nodeStack.Push(childNode);
                visited.Set((int)nextChild, true);
            }
            else
            {
                postOrderIndex2NodeIndex[postOrderIndex] = (int)currentNode.Index;
                postOrderIndex++;
                nodeStack.Pop();
            }
        }

        return postOrderIndex2NodeIndex;
    }
}
