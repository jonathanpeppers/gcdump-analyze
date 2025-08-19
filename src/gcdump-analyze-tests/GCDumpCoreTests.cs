using DotNet.GCDump.Analyze;
using VerifyTests;

namespace DotNet.GCDump.Analyze.Tests;

public class GCDumpCoreTests : BaseTest
{
    [Fact]
    public void CanOpenByPathAndStream()
    {
        using var dump1 = GCDump.Open(GetFilePath("test1.gcdump"));
        using var fs = File.OpenRead(GetFilePath("leakypage.gcdump"));
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

    [Fact]
    public async Task PathsToRoot_LeakyPage_Snapshot()
    {
        using var dump = GCDump.Open(GetFilePath("test1.gcdump"));
        var table = dump.GetPathsToRoot("LeakyPage");

        using var sw = new StringWriter();
        Markdown.WriteTree(table, sw);

        await VerifyXunit.Verifier.Verify(sw.ToString())
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("paths-to-root");
    }
}
