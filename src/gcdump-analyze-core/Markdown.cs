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
    /// Write a tree-style view using box-drawing characters from a TableReport.
    /// Uses TreeNodes and throws otherwise
    /// </summary>
    public static void WriteTree(TableReport report, TextWriter writer, string nameColumn = "Object Type", string? countColumn = "Reference Count")
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(writer);

        // Use TreeNodes if available
        if (report.TreeNodes.Count > 0)
        {
            WriteTreeFromNodes(report.TreeNodes, writer);
            return;
        }

        throw new InvalidOperationException("TableReport must contain TreeNodes for tree rendering. Use the TreeNode-based constructor.");
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
