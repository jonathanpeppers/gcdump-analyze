using DotNet.GCDump.Analyze;

namespace DotNet.GCDump.Analyze.Tests;

public class MarkdownTests : BaseTest
{
    [Fact]
    public async Task Markdown_WritesTable_Snapshot()
    {
        var columns = new[] { "Col1", "Col2", "Num" };
        var rows = new List<TableRow>
        {
            new(new Dictionary<string, object?>
            {
                ["Col1"] = "foo",
                ["Col2"] = "bar",
                ["Num"] = 42,
            }),
            new(new Dictionary<string, object?>
            {
                ["Col1"] = "baz",
                ["Col2"] = null,
                ["Num"] = 0,
            }),
        };

        var report = new TableReport(columns, rows);
        using var sw = new StringWriter();
        Markdown.Write(report, sw);

        await VerifyXunit.Verifier.Verify(sw.ToString())
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("table");
    }

    [Fact]
    public async Task Markdown_WriteTree_Snapshot()
    {
        var columns = new[] { "Object Type", "Reference Count" };
        var rows = new List<TableRow>
        {
            new(new Dictionary<string, object?> { [columns[0]] = "RootA", [columns[1]] = 3 }),
            new(new Dictionary<string, object?> { [columns[0]] = "  Child1", [columns[1]] = 2 }),
            new(new Dictionary<string, object?> { [columns[0]] = "    Leaf", [columns[1]] = 1 }),
            new(new Dictionary<string, object?> { [columns[0]] = "  Child2", [columns[1]] = 1 }),
            new(new Dictionary<string, object?> { [columns[0]] = "RootB", [columns[1]] = 1 }),
        };

        var report = new TableReport(columns, rows);
        using var sw = new StringWriter();
        Markdown.WriteTree(report, sw);

        await VerifyXunit.Verifier.Verify(sw.ToString())
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("tree");
    }
}
