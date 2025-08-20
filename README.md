# gcdump-analyze

Command-line tools for analyzing .gcdump files produced by `dotnet gcdump`.

This repository contains a core analysis library plus two clients:

- `gcdump-analyze` — a .NET global tool/CLI that prints Markdown reports and tree views.
- `gcdump-analyze-mcp` — an MCP (Model Context Protocol) server exposing the same functionality over stdio.

This README explains how to install and use the `gcdump-analyze` .NET global tool from NuGet.org and provides example invocations.

## Install (from NuGet)

The `gcdump-analyze` CLI is published as a .NET global tool package on NuGet.org. To install it globally for your user account:

```pwsh
dotnet tool install --global gcdump-analyze
```

To run the tool without installing globally (one-off):

```pwsh
dotnet tool run gcdump-analyze -- [command] [options]
```

## Usage

Run `gcdump-analyze --help` to show top-level help. The tool exposes subcommands that map to the core library APIs.

Common commands:

- `top` — Show top types by Inclusive Size (retained).
- `top-size` — Show top types by shallow Size (Bytes).
- `top-count` — Show top types by object Count.
- `filter` — Show rows for types whose name contains a substring (case-insensitive).
- `roots` — Show hot path(s) to GC roots for matching types (tree view).

Basic examples:

Show the top 10 types by retained (inclusive) size:

```pwsh
gcdump-analyze top path\to\heap.gcdump
```

Example output (top by inclusive size):

Object Type | Count | Size (Bytes) | Inclusive Size (Bytes)
---|---|---|---
[static vars] | 1 | 0 | 2350790
RuntimeTypeCache | 108 | 17280 | 667785
System.Reflection.RuntimeMethodInfo | 3409 | 354536 | 662698
System.Reflection.RuntimePropertyInfo | 800 | 83200 | 576762
System.String | 6913 | 518562 | 518562
MemberInfoCache<System.Reflection.RuntimeMethodInfo> | 83 | 4648 | 438171
System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.MemberInfo,System.ComponentModel.TypeConverter> | 1 | 32 | 384470
[static var System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.MemberInfo,System.ComponentModel.TypeConverter>.s_converterCache] | 1 | 0 | 384470

Show top 8 by inclusive size:

```pwsh
gcdump-analyze top -r 8 path\to\heap.gcdump
```

Show top by shallow size (default 10 rows):

```pwsh
gcdump-analyze top-size path\to\heap.gcdump
```

Example output (top by shallow size):

Object Type | Count | Size (Bytes) | Inclusive Size (Bytes)
---|---|---|---
System.String | 6913 | 518562 | 518562
System.Reflection.RuntimeMethodInfo | 3409 | 354536 | 662698
System.Object | 6375 | 153000 | 153000
WinRT.ObjectReference<WinRT.Interop.IUnknownVftbl> | 1540 | 98560 | 98560
System.Int32[] (Bytes > 10K) | 1 | 98352 | 98352
System.Reflection.RuntimeParameterInfo | 1101 | 96888 | 218654
System.RuntimeType | 2170 | 86800 | 86800
System.Reflection.RuntimePropertyInfo | 800 | 83200 | 576762

Show top by object count and write to a file:

```pwsh
gcdump-analyze top-count -r 20 path\to\heap.gcdump --output out.md
```

Filter types whose names contain "LeakyPage":

```pwsh
gcdump-analyze filter -n LeakyPage path\to\leakypage.gcdump
```

Example output (filter by name):

```text
Object Type | Count | Size (Bytes) | Inclusive Size (Bytes)
---|---|---|---
hellomauileak.LeakyPage | 3 | 2520 | 31800
```

Show hot paths to GC roots for types containing "LeakyPage":

```pwsh
gcdump-analyze roots -n LeakyPage path\to\leakypage.gcdump
```

Example output (paths to root):

```text
├── hellomauileak.LeakyPage (Count: 3)
│   └── System.ComponentModel.PropertyChangedEventHandler (Count: 3)
│       └── System.Delegate[] (Count: 3)
│           └── System.ComponentModel.PropertyChangedEventHandler (Count: 3)
│               └── Microsoft.Maui.Controls.Window (Count: 3)
│                   └── Microsoft.Maui.Controls.Element[] (Count: 3)
│                       └── System.Collections.Generic.List<Microsoft.Maui.Controls.Element> (Count: 3)
│                           └── hellomauileak.App (Count: 3)
```

## Options

- `-r, --rows <n>`: Number of rows to display (default: 10) for `top`, `top-size`, and `top-count`.
- `-n, --name <substring>`: Required for `filter` and `roots` to select matching type names.
- `-o, --output <file>`: Write output to a file instead of stdout.

All commands expect a single `.gcdump` file path argument.

## Output

By default the CLI prints Markdown tables for `top`, `top-size`, `top-count`, and `filter` commands. The `roots` command prints a box-drawing tree of hot paths to GC roots.

Examples of output formats are in the repository tests and snapshots.

## MCP server

The `gcdump-analyze-mcp` tool runs an MCP server exposing the same tools (useful for editor integrations). See `src/gcdump-analyze-mcp` for details and [`.github/instructions/gcdump-analyze-mcp.md`](.github/instructions/gcdump-analyze-mcp.md) for VS Code configuration.

## Contributing

See the repository for unit tests (xUnit + Verify) and instructions for running them. Run tests with:

```pwsh
dotnet test src/gcdump-analyze-tests/gcdump-analyze-tests.csproj
```

## Notes

- Requires .NET 9 SDK for building and running.
- The core library provides programmatic APIs useful for embedding in other tools or servers.
