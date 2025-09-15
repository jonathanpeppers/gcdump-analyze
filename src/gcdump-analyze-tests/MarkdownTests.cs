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
        var columnInfos = new[] 
        { 
            new ColumnInfo("Object Type", ColumnType.Text), 
            new ColumnInfo("Reference Count", ColumnType.Numeric) 
        };
        
        // Create hierarchical tree structure
        var leaf = new TreeNode("Leaf", 1);
        var child1 = new TreeNode("Child1", 2, new[] { leaf });
        var child2 = new TreeNode("Child2", 1);
        var rootA = new TreeNode("RootA", 3, new[] { child1, child2 });
        var rootB = new TreeNode("RootB", 1);
        
        var treeNodes = new[] { rootA, rootB };

        var report = new TableReport(columnInfos, treeNodes);
        using var sw = new StringWriter();
        Markdown.WriteTree(report, sw);

        await VerifyXunit.Verifier.Verify(sw.ToString())
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("tree");
    }
}
