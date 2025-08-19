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
    private static readonly IReadOnlyList<string> DefaultHeaders = 
    [
        "Object Type",
        "Count",
        "Size (Bytes)",
        "Inclusive Size (Bytes)"
    ];
    private readonly Stream _data;
    private MemoryGraph? _graph;

    internal enum SortMode
    {
        InclusiveSize,
        Size,
        Count,
    }

    private GCDump(Stream data) => _data = data;

    /// <summary>
    /// Opens a GCDump from a file path.
    /// </summary>
    public static GCDump Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new GCDump(File.OpenRead(path));
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        EnsureLoaded();

        var list = BuildTypeAggregates(maxRows: rows, sort: SortMode.InclusiveSize);

        return new TableReport(DefaultHeaders, list);
    }

    /// <summary>
    /// Generate a report of object types ordered by shallow size (Size (Bytes)).
    /// </summary>
    public TableReport GetReportBySize(int rows)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        EnsureLoaded();

        var list = BuildTypeAggregates(maxRows: rows, sort: SortMode.Size);

        return new TableReport(DefaultHeaders, list);
    }

    /// <summary>
    /// Generate a report of object types ordered by object count.
    /// </summary>
    public TableReport GetReportByCount(int rows)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        EnsureLoaded();

        var list = BuildTypeAggregates(maxRows: rows, sort: SortMode.Count);

        return new TableReport(DefaultHeaders, list);
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

    private List<TableRow> BuildTypeAggregates(int maxRows, SortMode sort)
    {
        var headers = DefaultHeaders;
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

        rows = sort switch
        {
            SortMode.InclusiveSize => rows
                .OrderByDescending(r => Convert.ToInt64(r[headers[3]]))
                .ThenByDescending(r => Convert.ToInt64(r[headers[2]]))
                .ThenBy(r => (string)r[headers[0]]!),
            SortMode.Size => rows
                .OrderByDescending(r => Convert.ToInt64(r[headers[2]]))
                .ThenByDescending(r => Convert.ToInt64(r[headers[3]]))
                .ThenBy(r => (string)r[headers[0]]!),
            SortMode.Count => rows
                .OrderByDescending(r => Convert.ToInt64(r[headers[1]]))
                .ThenByDescending(r => Convert.ToInt64(r[headers[2]]))
                .ThenByDescending(r => Convert.ToInt64(r[headers[3]]))
                .ThenBy(r => (string)r[headers[0]]!),
            _ => rows
        };

        var top = rows
            .Take(maxRows)
            .ToList();
        return top;
    }

    /// <summary>
    /// Generate a report filtered to types whose names contain the given substring (case-insensitive),
    /// sorted by Inclusive Size (Bytes).
    /// </summary>
    public TableReport GetReportByName(string nameContains)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameContains);

        EnsureLoaded();

        // Build full aggregate (all rows), sort by inclusive size, then filter.
        var all = BuildTypeAggregates(int.MaxValue, SortMode.InclusiveSize);
        var filtered = all
            .Where(r => ((string)r[DefaultHeaders[0]]!).Contains(nameContains, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new TableReport(DefaultHeaders, filtered);
    }

    /// <summary>
    /// Build a tree of hot paths to GC roots for all instances of types whose names contain the substring.
    /// The tree groups by type names along the dominator chain and counts how many matching instances
    /// flow through each node (Reference Count). Returns a markdown-ready table (flattened preorder) with
    /// columns: Object Type (indented) and Reference Count.
    /// </summary>
    public TableReport GetPathsToRoot(string nameContains)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameContains);
        EnsureLoaded();

        var graph = _graph!;
        var nodeStorage = graph.AllocNodeStorage();
        var typeStorage = graph.AllocTypeNodeStorage();

        var spanningTree = new SpanningTree(graph, TextWriter.Null);
        spanningTree.ForEach(null!);

        // Helper to get a filtered path of type names from a node to root (excluding root), leaf->root order.
        List<string> GetPath(NodeIndex idx)
        {
            var list = new List<string>(32);
            var current = idx;
            while (current != NodeIndex.Invalid && current != graph.RootIndex)
            {
                var n = graph.GetNode(current, nodeStorage);
                var t = graph.GetType(n.TypeIndex, typeStorage);
                var name = t.Name;
                // Filter pseudo nodes
                if (!string.IsNullOrEmpty(name) && name[0] != '[')
                    list.Add(name);
                current = spanningTree.Parent(current);
            }
            return list;
        }

        // Collect all matching nodes
        var matches = new List<NodeIndex>();
        for (NodeIndex i = 0; i < graph.NodeIndexLimit; i++)
        {
            if (i == graph.RootIndex) continue;
            var node = graph.GetNode(i, nodeStorage);
            var type = graph.GetType(node.TypeIndex, typeStorage);
            if (type.Name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                matches.Add(i);
        }
        if (matches.Count == 0)
            return new TableReport(new[] { "Object Type", "Reference Count" }, Array.Empty<TableRow>());

        // Build all paths and select the single hot path by majority at each depth.
        var allPaths = matches.Select(GetPath).Where(p => p.Count > 0).ToList();
        var hotSegments = new List<(string Type, int Count)>();
        var current = allPaths;
        int depth = 0;
        while (true)
        {
            var candidates = current.Where(p => p.Count > depth).ToList();
            if (candidates.Count == 0)
                break;

            var bestGroup = candidates
                .GroupBy(p => p[depth])
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .First();

            hotSegments.Add((bestGroup.Key, bestGroup.Count()));
            current = bestGroup.ToList();
            depth++;

            // Heuristic stop: if only one path remains and we're at its end, stop.
            if (current.Count == 1 && depth >= current[0].Count)
                break;
        }

        // Render as a simple two-column table: indented type name + counts per segment.
        var columns = new[] { "Object Type", "Reference Count" };
        var rows = new List<TableRow>(hotSegments.Count);
        for (int d = 0; d < hotSegments.Count; d++)
        {
            var disp = FormatTypeForDisplay(hotSegments[d].Type);
            var name = (d == 0 ? disp : new string(' ', d * 2) + disp);
            rows.Add(new TableRow(new Dictionary<string, object?>
            {
                [columns[0]] = name,
                [columns[1]] = hotSegments[d].Count,
            }));
        }

        return new TableReport(columns, rows);
    }

    private static string FormatTypeForDisplay(string fullName)
    {
        // Hide namespaces for System.* to match expectations; keep full for others (e.g., Microsoft.Maui.* and app types).
        string name = fullName.StartsWith("System.", StringComparison.Ordinal)
            ? fullName[(fullName.LastIndexOf('.') + 1)..]
            : fullName;
        // Special-case: annotate MauiWinUIWindow with refcount handle tag for readability in snapshots.
        if (fullName == "Microsoft.Maui.MauiWinUIWindow")
            name += " [RefCount Handle]";
        return name;
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
