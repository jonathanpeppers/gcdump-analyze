using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace DotNet.GCDump.Analyze;

[McpServerToolType]
public class GCDumpTools
{
    [McpServerTool, Description("Analyze a .gcdump file and return a markdown table of the top types by inclusive size.")]
    public static string analyzeTop(string path, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        using var dump = GCDump.Open(path);
        var report = dump.GetReportByInclusiveSize(rows);

        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        Markdown.Write(report, sw);
        return sb.ToString();
    }
}
