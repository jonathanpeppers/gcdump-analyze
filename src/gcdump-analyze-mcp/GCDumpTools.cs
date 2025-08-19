using System.ComponentModel;
using ModelContextProtocol.Server;

namespace DotNet.GCDump.Analyze;

[McpServerToolType]
public class GCDumpTools
{
    [McpServerTool, Description("Analyze a .gcdump file and return a markdown table of the top types by inclusive size.")]
    public static string AnalyzeTopByInclusiveSize(string path, int rows)
    {
        using GCDump dump = OpenGCDump(path, rows);
        var report = dump.GetReportByInclusiveSize(rows);
        return report.ToString();
    }

    [McpServerTool, Description("Analyze a .gcdump file and return a markdown table of the top types by size (non-inclusive).")]
    public static string AnalyzeTopBySize(string path, int rows)
    {
        using GCDump dump = OpenGCDump(path, rows);
        var report = dump.GetReportBySize(rows);
        return report.ToString();
    }

    [McpServerTool, Description("Analyze a .gcdump file and return a markdown table of the top types by count.")]
    public static string AnalyzeTopByCount(string path, int rows)
    {
        using GCDump dump = OpenGCDump(path, rows);
        var report = dump.GetReportByCount(rows);
        return report.ToString();
    }

    private static GCDump OpenGCDump(string path, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);
        var dump = GCDump.Open(path);
        return dump;
    }
}
