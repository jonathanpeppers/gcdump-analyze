using System.CommandLine;
using DotNet.GCDump.Analyze;

var root = new RootCommand("Analyze .gcdump files and render markdown reports.");

// Shared options/arguments
var pathArg = new Argument<string>("path")
{
    Description = "Path to the .gcdump file."
};

var rowsOpt = new Option<int>("--rows", "-r")
{
    Description = "Number of rows to include.",
    DefaultValueFactory = _ => 10
};
var outputOpt = new Option<FileInfo?>("--output", "-o")
{
    Description = "Write output to file instead of stdout.",
};
var nameOpt = new Option<string>("--name", "-n")
{
    Description = "Case-insensitive substring to match in type names.",
};

// top
var top = new Command("top", "Show top types by Inclusive Size (retained).");
top.Arguments.Add(pathArg);
top.Options.Add(rowsOpt);
top.Options.Add(outputOpt);
top.SetAction(parseResult =>
{
    var path = parseResult.GetValue(pathArg) ?? "";
    var rows = parseResult.GetValue(rowsOpt);
    var output = parseResult.GetValue(outputOpt);
    using var dump = GCDump.Open(path);
    var report = dump.GetReportByInclusiveSize(rows);
    Markdown.Write(report, Console.Out);
});
root.Add(top);

// top-size
var topSize = new Command("top-size", "Show top types by shallow Size (Bytes).");
topSize.Arguments.Add(pathArg);
topSize.Options.Add(rowsOpt);
topSize.Options.Add(outputOpt);
topSize.SetAction(parseResult =>
{
    var path = parseResult.GetValue(pathArg) ?? "";
    var rows = parseResult.GetValue(rowsOpt);
    var output = parseResult.GetValue(outputOpt);
    using var dump = GCDump.Open(path);
    var report = dump.GetReportBySize(rows);
    Markdown.Write(report, Console.Out);
});
root.Add(topSize);

// top-count
var topCount = new Command("top-count", "Show top types by object Count.");
topCount.Arguments.Add(pathArg);
topCount.Options.Add(rowsOpt);
topCount.Options.Add(outputOpt);
topCount.SetAction(parseResult =>
{
    var path = parseResult.GetValue(pathArg) ?? "";
    var rows = parseResult.GetValue(rowsOpt);
    var output = parseResult.GetValue(outputOpt);
    using var dump = GCDump.Open(path);
    var report = dump.GetReportByCount(rows);
    Markdown.Write(report, Console.Out);
});
root.Add(topCount);

// filter
var filter = new Command("filter", "Show rows for types whose name contains a substring (sorted by Inclusive Size).");
filter.Arguments.Add(pathArg);
filter.Options.Add(nameOpt);
filter.Options.Add(rowsOpt);
filter.Options.Add(outputOpt);
filter.SetAction(parseResult =>
{
    var path = parseResult.GetValue(pathArg) ?? "";
    var name = parseResult.GetValue(nameOpt) ?? "";
    var rows = parseResult.GetValue(rowsOpt);
    using var dump = GCDump.Open(path);
    var report = dump.GetReportByName(name);
    if (rows > 0 && report.Rows.Count > rows)
        report = new TableReport(report.Columns, [.. report.Rows.Take(rows)]);
    Markdown.Write(report, Console.Out);
});
root.Add(filter);

// roots
var roots = new Command("roots", "Show hot path(s) to GC roots for matching types.");
roots.Arguments.Add(pathArg);
roots.Options.Add(nameOpt);
roots.Options.Add(outputOpt);
roots.SetAction(parseResult =>
{
    var path = parseResult.GetValue(pathArg) ?? "";
    var name = parseResult.GetValue(nameOpt) ?? "";
    var output = parseResult.GetValue(outputOpt);
    using var dump = GCDump.Open(path);
    var report = dump.GetPathsToRoot(name);
    // Always render roots as a tree view
    Markdown.WriteTree(report, Console.Out);
});
root.Add(roots);

var parseResult = root.Parse(args);
return await parseResult.InvokeAsync();
