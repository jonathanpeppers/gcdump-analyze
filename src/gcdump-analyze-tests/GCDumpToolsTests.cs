using DotNet.GCDump.Analyze;

namespace DotNet.GCDump.Analyze.Tests;

public class GCDumpToolsTests : BaseTest
{
    [Fact]
    public async Task AnalyzeTopByInclusiveSize_ReturnsMarkdown()
    {
        var path = GetFilePath("test1.gcdump");
        var result = GCDumpTools.AnalyzeTopByInclusiveSize(path, 8);

        await VerifyXunit.Verifier.Verify(result)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("inclusive");
    }

    [Fact]
    public async Task AnalyzeTopBySize_ReturnsMarkdown()
    {
        var path = GetFilePath("test1.gcdump");
        var result = GCDumpTools.AnalyzeTopBySize(path, 8);

        await VerifyXunit.Verifier.Verify(result)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("size");
    }

    [Fact]
    public async Task AnalyzeTopByCount_ReturnsMarkdown()
    {
        var path = GetFilePath("test1.gcdump");
        var result = GCDumpTools.AnalyzeTopByCount(path, 8);

        await VerifyXunit.Verifier.Verify(result)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("count");
    }

    [Fact]
    public async Task AnalyzeByName_ReturnsMarkdown()
    {
        var path = GetFilePath("leakypage.gcdump");
        var result = GCDumpTools.AnalyzeByName(path, "LeakyPage");

        await VerifyXunit.Verifier.Verify(result)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("byname");
    }

    [Fact]
    public async Task PathsToRoot_ReturnsTree()
    {
        var path = GetFilePath("leakypage.gcdump");
        var result = GCDumpTools.PathsToRoot(path, "LeakyPage");

        await VerifyXunit.Verifier.Verify(result)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("paths");
    }

    [Fact]
    public void Methods_ThrowOnMissingFile()
    {
        var missing = Path.Combine(GetDataDir(), "no-such-file.gcdump");

        Assert.Throws<FileNotFoundException>(() => GCDumpTools.AnalyzeTopByInclusiveSize(missing, 1));
        Assert.Throws<FileNotFoundException>(() => GCDumpTools.AnalyzeTopBySize(missing, 1));
        Assert.Throws<FileNotFoundException>(() => GCDumpTools.AnalyzeTopByCount(missing, 1));
        Assert.Throws<FileNotFoundException>(() => GCDumpTools.AnalyzeByName(missing, "x"));
        Assert.Throws<FileNotFoundException>(() => GCDumpTools.PathsToRoot(missing, "x"));
    }

    [Fact]
    public void AnalyzeByName_ThrowsOnEmptyName()
    {
        var path = GetFilePath("test1.gcdump");
        Assert.Throws<ArgumentException>(() => GCDumpTools.AnalyzeByName(path, ""));
        Assert.Throws<ArgumentException>(() => GCDumpTools.PathsToRoot(path, ""));
    }
}
