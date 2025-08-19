---
applyTo: "src/gcdump-analyze-mcp/**"
---

# gcdump-analyze MCP Server

This project exposes the core library via a Model Context Protocol (MCP) server. It provides multiple tools for analyzing `.gcdump` files and returns results as Markdown tables or a tree view for root paths.

## Tools exposed

Implemented in `src/gcdump-analyze-mcp/GCDumpTools.cs` using attribute-based registration (`[McpServerToolType]`, `[McpServerTool]`). Tool names are derived from method names:

- analyze_top_by_inclusive_size(path: string, rows: int) → text/markdown
- analyze_top_by_size(path: string, rows: int) → text/markdown
- analyze_top_by_count(path: string, rows: int) → text/markdown
- analyze_by_name(path: string, name: string) → text/markdown
- paths_to_root(path: string, name: string) → text/plain (box-drawing tree)

Notes:
- Table outputs match the CLI/test markdown format.
- Paths to root always returns a tree view (no table mode).

## Implementation

The server is implemented in `src/gcdump-analyze-mcp/Program.cs` using the MCP C# Core SDK (`ModelContextProtocol.Core`).

- Host builder registers the MCP server, stdio transport, and the tools class:
  - `builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<GCDumpTools>();`
- Each tool opens the dump via `GCDump.Open(path)` and formats the response:
  - Tables: `report.ToString()` (Markdown)
  - Roots: `table.ToTreeString()` (tree text)

## Using released package

- Requires .NET 9 SDK.

Install the tool globally (deployed to NuGet.org):

```pwsh
dotnet tool install --global gcdump-analyze-mcp
```

## Run (stdio)

Typical MCP clients launch servers via stdio. After installing the global tool, the server can be started via the `gcdump-analyze-mcp` command (no args).

### VS Code setup (MCP)

Configure VS Code to launch the server using the installed .NET global tool. In `.vscode/mcp.json`:

```jsonc
{
  "servers": {
    "gcdump-analyze-mcp": {
      "type": "stdio",
      "command": "gcdump-analyze-mcp",
      "args": []
    }
  }
}
```

This replaces the previous configuration that used `dotnet run --project src\\gcdump-analyze-mcp\\gcdump-analyze-mcp.csproj`.

## Using the tools

Examples (tool names as exposed by the server):

- analyze_top_by_inclusive_size: `{ "path": "./data/test1.gcdump", "rows": 8 }` → returns markdown table
- analyze_top_by_size: `{ "path": "./data/test1.gcdump", "rows": 8 }` → returns markdown table
- analyze_top_by_count: `{ "path": "./data/test1.gcdump", "rows": 8 }` → returns markdown table
- analyze_by_name: `{ "path": "./data/test1.gcdump", "name": "LeakyPage" }` → returns markdown table
- paths_to_root: `{ "path": "./data/leakypage.gcdump", "name": "LeakyPage" }` → returns a tree (plain text)

## Error handling

- Missing file: returns a file-not-found error.
- Invalid arguments (e.g., empty `name`, `rows <= 0`): returns an argument error.
- All analysis is performed by the core library; the MCP server is a thin wrapper.
