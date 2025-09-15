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

        // Calculate column widths
        var columnWidths = new Dictionary<string, int>();
        
        // Initialize with header widths
        foreach (var columnInfo in report.ColumnInfos)
        {
            columnWidths[columnInfo.Name] = columnInfo.Name.Length;
        }
        
        // Check all row values to find maximum width for each column
        foreach (var row in report.Rows)
        {
            foreach (var columnInfo in report.ColumnInfos)
            {
                var value = row.TryGetValue(columnInfo.Name, out var v) ? FormatValue(v) : string.Empty;
                columnWidths[columnInfo.Name] = Math.Max(columnWidths[columnInfo.Name], value.Length);
            }
        }

        // Header with padding
        var headerValues = report.ColumnInfos.Select(col => 
            col.Type == ColumnType.Numeric 
                ? col.Name.PadLeft(columnWidths[col.Name])
                : col.Name.PadRight(columnWidths[col.Name]));
        writer.WriteLine(string.Join(" | ", headerValues));
        
        // Separator line with proper padding and alignment indicators
        var separators = report.ColumnInfos.Select(col =>
        {
            var width = columnWidths[col.Name];
            return col.Type == ColumnType.Numeric 
                ? new string('-', width - 1) + ":"  // Right-aligned: ----:
                : new string('-', width);           // Left-aligned: -----
        });
        writer.WriteLine(string.Join(" | ", separators));

        // Rows with padding
        foreach (var row in report.Rows)
        {
            var values = report.ColumnInfos.Select(col =>
            {
                var value = row.TryGetValue(col.Name, out var v) ? FormatValue(v) : string.Empty;
                return col.Type == ColumnType.Numeric
                    ? value.PadLeft(columnWidths[col.Name])
                    : value.PadRight(columnWidths[col.Name]);
            });
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

        // Use TreeNodes if available, otherwise fall back to parsing row data
        if (report.TreeNodes.Count > 0)
        {
            WriteTreeFromNodes(report.TreeNodes, writer);
            return;
        }

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
                var line = prefix + connector + child.Label + (child.Count.HasValue ? $" (Count: {child.Count.Value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)})" : string.Empty);
                writer.WriteLine(line);
                var nextPrefix = prefix + (isRoot ? "│   " : (last ? "    " : "│   "));
                if (child.Children.Count > 0)
                    Render(child.Children, nextPrefix);
            }
        }

        Render(roots, string.Empty);
    }

    /// <summary>
    /// Write a tree-style report using TreeNode objects to the provided TextWriter using box-drawing characters.
    /// </summary>
    public static void WriteTreeFromNodes(IReadOnlyList<TreeNode> nodes, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(writer);

        RenderTreeNodes(nodes, writer, string.Empty);
    }

    private static void RenderTreeNodes(IReadOnlyList<TreeNode> children, TextWriter writer, string prefix)
    {
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            bool last = i == children.Count - 1;
            bool isRoot = prefix.Length == 0;
            var connector = isRoot ? "├── " : (last ? "└── " : "├── ");
            var valueStr = child.Value != null ? $" (Count: {FormatValue(child.Value)})" : string.Empty;
            var line = prefix + connector + child.Label + valueStr;
            
            writer.WriteLine(line);
            
            if (child.Children.Count > 0)
            {
                var childPrefix = prefix + (isRoot ? "│   " : (last ? "    " : "│   "));
                RenderTreeNodes(child.Children, writer, childPrefix);
            }
        }
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
            IFormattable f when value is int || value is long => f.ToString("N0", System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
