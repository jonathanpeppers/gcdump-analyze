# gcdump-analyze MCP Server

This project exposes the core library via a Model Context Protocol (MCP) server with a single tool.

## Tool: gcdump.analyzeTop

- Description: Analyze a .gcdump file and return a markdown table of the top types by inclusive size.
- Parameters:
  - path (string, required): Path to the .gcdump file.
  - rows (int, required): Number of rows to include in the report.
- Returns: text/markdown containing the formatted table (same format as the CLI/test snapshot).

## Implementation

The server is implemented in `src/gcdump-analyze-mcp/Program.cs` using the MCP C# Core SDK (`ModelContextProtocol.Core`).

- Defines a single tool via `McpServerTool.Create((string path, int rows) => string)`.
- Opens the dump using `GCDump.Open(path)`, calls `GetReportByInclusiveSize(rows)`, and renders markdown via `Markdown.Write`.
- Runs over stdio using `StdioServerTransport`.

## Build

- Requires .NET 9 SDK.
- The project already references `ModelContextProtocol.Core` preview package and the core library.

## Run (stdio)

Typical MCP clients launch servers via stdio. You can test locally by starting the server and connecting a client.

For example, using the C# SDK client in another process:

- Transport: stdio launching this executable.
- Tool call: name `gcdump.analyzeTop` with params: `{ "path": "./data/test1.gcdump", "rows": 8 }`.

## Notes

- Inclusive size calculations are provided by the core library. The MCP server is a thin wrapper only.
- Errors:
  - If the file is missing: returns a FileNotFound error.
  - If rows <= 0: returns an argument error.
- Output mime type: `text/markdown`.
