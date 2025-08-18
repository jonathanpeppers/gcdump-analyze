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

        foreach (var row in report.Rows)
        {
            var values = report.Columns.Select(h => row.TryGetValue(h, out var v) ? FormatValue(v) : string.Empty);
            writer.WriteLine(string.Join(" | ", values));
        }
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
