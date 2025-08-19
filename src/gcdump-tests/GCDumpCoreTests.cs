using DotNet.GCDump.Analyze;
using VerifyTests;

namespace DotNet.GCDump.Analyze.Tests;

public class GCDumpCoreTests
{
    [Fact]
    public void CanOpenByPathAndStream()
    {
        using var dump1 = GCDump.Open(GetFilePath("test1.gcdump"));
        using var fs = File.OpenRead(GetFilePath("test2.gcdump"));
        using var dump2 = GCDump.Open(fs);

        var report1 = dump1.GetReportByInclusiveSize(5);
        var report2 = dump2.GetReportByInclusiveSize(3);

        Assert.NotNull(report1);
        Assert.NotNull(report2);
        Assert.Equal(5, report1.Rows.Count);
        Assert.Equal(3, report2.Rows.Count);
    }

    [Fact]
    public async Task EndToEnd_Markdown_Snapshot()
    {
        using var dump = GCDump.Open(GetFilePath("test1.gcdump"));
        var report = dump.GetReportByInclusiveSize(8);

        using var sw = new StringWriter();
        Markdown.Write(report, sw);

        await VerifyXunit.Verifier.Verify(sw.ToString())
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("markdown");
    }

    [Fact]
    public async Task EndToEnd_Markdown_BySize_Snapshot()
    {
        using var dump = GCDump.Open(GetFilePath("test1.gcdump"));
        var report = dump.GetReportBySize(8);

        using var sw = new StringWriter();
        Markdown.Write(report, sw);

        await VerifyXunit.Verifier.Verify(sw.ToString())
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("markdown-by-size");
    }

    [Fact]
    public async Task EndToEnd_Markdown_ByCount_Snapshot()
    {
        using var dump = GCDump.Open(GetFilePath("test1.gcdump"));
        var report = dump.GetReportByCount(8);

        using var sw = new StringWriter();
        Markdown.Write(report, sw);

        await VerifyXunit.Verifier.Verify(sw.ToString())
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("markdown-by-count");
    }

    [Fact]
    public async Task EndToEnd_Markdown_ByName_Snapshot()
    {
        using var dump = GCDump.Open(GetFilePath("test1.gcdump"));
        var report = dump.GetReportByName("LeakyPage");

        using var sw = new StringWriter();
        Markdown.Write(report, sw);

        await VerifyXunit.Verifier.Verify(sw.ToString())
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("markdown-by-name");
    }

    private static string GetFilePath(string fileName) =>
        Path.Combine(GetDataDir(), fileName);

    private static string GetDataDir()
    {
        // AppContext.BaseDirectory points to bin/<config>/<tfm>/ for the test assembly
        // Navigate to repo root from that.
        var baseDir = AppContext.BaseDirectory;
        // search upwards for the repo root containing 'data' directory
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 6 && dir != null; i++)
        {
            var dataPath = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(dataPath))
                return dataPath;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate 'data' directory relative to test base directory.");
    }

    private static string GetProjectDir()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++)
        {
            var csproj = Path.Combine(dir.FullName, "gcdump-tests.csproj");
            if (File.Exists(csproj))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate test project directory.");
    }
}
