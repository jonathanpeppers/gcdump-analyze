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
        
        // Create hierarchical tree structure using TableRow
        var leaf = new TableRow(new Dictionary<string, object?> 
        { 
            ["Object Type"] = "Leaf", 
            ["Reference Count"] = 1 
        });
        
        var child1 = new TableRow(new Dictionary<string, object?> 
        { 
            ["Object Type"] = "Child1", 
            ["Reference Count"] = 2 
        }, new[] { leaf });
        
        var child2 = new TableRow(new Dictionary<string, object?> 
        { 
            ["Object Type"] = "Child2", 
            ["Reference Count"] = 1 
        });
        
        var rootA = new TableRow(new Dictionary<string, object?> 
        { 
            ["Object Type"] = "RootA", 
            ["Reference Count"] = 3 
        }, new[] { child1, child2 });
        
        var rootB = new TableRow(new Dictionary<string, object?> 
        { 
            ["Object Type"] = "RootB", 
            ["Reference Count"] = 1 
        });
        
        var treeNodes = new[] { rootA, rootB };

        var report = TableReport.CreateTreeReport(columnInfos, treeNodes);
        using var sw = new StringWriter();
        Markdown.WriteTree(report, sw);

        await VerifyXunit.Verifier.Verify(sw.ToString())
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("tree");
    }
}
