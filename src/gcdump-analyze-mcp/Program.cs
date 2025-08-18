using DotNet.GCDump.Analyze;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.Text;

// MCP server exposing one tool: gcdump.analyzeTop
// Params: (string path, int rows)
// Returns: markdown string of the report

var serverOptions = new McpServerOptions
{
	ServerInfo = new() { Name = "gcdump-analyze-mcp", Version = "0.1" },
	Capabilities = new()
};
serverOptions.Capabilities.Tools = new()
{
	ListChanged = true,
	ToolCollection =
	[
		McpServerTool.Create((string path, int rows) =>
		{
			if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
			if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

			using var dump = GCDump.Open(path);
			var report = dump.GetReportByInclusiveSize(rows);

			using var sw = new StringWriter(new StringBuilder(capacity: 8 * 1024));
			Markdown.Write(report, sw);
			return sw.ToString();
		}, new()
		{
			Name = "gcdump.analyzeTop",
			Description = "Analyze a .gcdump file and return a markdown table of the top types by inclusive size."
		})
	]
};

await using var transport = new StdioServerTransport(serverOptions);
var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Information));
var server = McpServerFactory.Create(transport, serverOptions, loggerFactory, serviceProvider: null);
await server.RunAsync(CancellationToken.None);
