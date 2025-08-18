using System.Text;

namespace DotNet.GCDump.Analyze;

public static class Markdown
{
    /// <summary>
    /// Write a markdown table report to the provided TextWriter.
    /// Expects the input dictionary to have keys: "columns" (string[]), "rows" (List<Dictionary<string, object>>), and optional "source".
    /// </summary>
    public static void Write(Dictionary<string, object> report, TextWriter writer)
    {
        if (report is null) throw new ArgumentNullException(nameof(report));
        if (writer is null) throw new ArgumentNullException(nameof(writer));

        var columns = report.TryGetValue("columns", out var colsObj) && colsObj is string[] c ? c : throw new ArgumentException("Report missing 'columns'.");
        var rows = report.TryGetValue("rows", out var rowsObj) && rowsObj is List<Dictionary<string, object>> r ? r : throw new ArgumentException("Report missing 'rows'.");
        var source = report.TryGetValue("source", out var sourceObj) ? sourceObj as string : null;

        if (!string.IsNullOrEmpty(source))
        {
            writer.WriteLine($"Report for {source}");
            writer.WriteLine();
        }

        // Header
        writer.WriteLine(string.Join(" | ", columns));
        writer.WriteLine(string.Join("|", columns.Select(_ => "---")));

        foreach (var row in rows)
        {
            var values = columns.Select(h => row.TryGetValue(h, out var v) ? FormatValue(v) : string.Empty);
            writer.WriteLine(string.Join(" | ", values));
        }
    }

    private static string FormatValue(object value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable f when value is int or long => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
