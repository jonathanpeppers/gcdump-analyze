using System.Text;

namespace DotNet.GCDump.Analyze;

public static class Markdown
{
    /// <summary>
    /// Write a markdown table report to the provided TextWriter.
    /// </summary>
    public static void Write(TableReport report, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(writer);

        // Header
        writer.WriteLine(string.Join(" | ", report.Columns));
        writer.WriteLine(string.Join("|", report.Columns.Select(_ => "---")));

        // Rows
        foreach (var row in report.Rows)
        {
            var values = report.Columns.Select(h => row.TryGetValue(h, out var v) ? FormatValue(v) : string.Empty);
            writer.WriteLine(string.Join(" | ", values));
        }
    }

    /// <summary>
    /// Write a tree-style view using box-drawing characters from a TableReport that encodes hierarchy by
    /// leading spaces in a name column (default: "Object Type"). Optionally shows a numeric count in parentheses.
    /// </summary>
    public static void WriteTree(TableReport report, TextWriter writer, string nameColumn = "Object Type", string? countColumn = "Reference Count")
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(writer);

        // Build nodes with depth inferred from leading spaces (2 spaces per depth per our generator).
        var nodes = new List<Node>();
        foreach (var row in report.Rows)
        {
            if (!row.TryGetValue(nameColumn, out var nameObj))
                continue;
            var nameRaw = nameObj?.ToString() ?? string.Empty;
            int leadingSpaces = 0;
            while (leadingSpaces < nameRaw.Length && nameRaw[leadingSpaces] == ' ') leadingSpaces++;
            int depth = leadingSpaces / 2;
            var label = leadingSpaces > 0 ? nameRaw.Substring(leadingSpaces) : nameRaw;

            long? count = null;
            if (!string.IsNullOrEmpty(countColumn) && row.TryGetValue(countColumn!, out var cntObj) && cntObj is not null)
            {
                count = Convert.ToInt64(cntObj, System.Globalization.CultureInfo.InvariantCulture);
            }
            nodes.Add(new Node { Depth = depth, Label = label, Count = count });
        }

        // Build a tree structure using a depth stack.
        var roots = new List<Node>();
        var stack = new Stack<Node>();
        foreach (var n in nodes)
        {
            while (stack.Count > n.Depth) stack.Pop();
            if (stack.Count == 0)
            {
                roots.Add(n);
            }
            else
            {
                stack.Peek().Children.Add(n);
            }
            stack.Push(n);
        }

        // Render recursively with box-drawing characters.
        void Render(IReadOnlyList<Node> children, string prefix)
        {
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                bool last = i == children.Count - 1;
                bool isRoot = prefix.Length == 0;
                var connector = isRoot ? "├── " : (last ? "└── " : "├── ");
                var line = prefix + connector + child.Label + (child.Count.HasValue ? $" (Count: {child.Count.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)})" : string.Empty);
                writer.WriteLine(line);
                var nextPrefix = prefix + (isRoot ? "│   " : (last ? "    " : "│   "));
                if (child.Children.Count > 0)
                    Render(child.Children, nextPrefix);
            }
        }

        Render(roots, string.Empty);
    }

    private sealed class Node
    {
        public int Depth { get; set; }
        public string Label { get; set; } = string.Empty;
        public long? Count { get; set; }
        public List<Node> Children { get; } = new List<Node>();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable f when value is int || value is long => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
