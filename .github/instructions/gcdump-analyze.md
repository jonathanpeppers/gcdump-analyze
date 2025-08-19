---
applyTo: "src/gcdump-analyze/**"
---

gcdump-analyze CLI
==================

Command-line tool for analyzing .gcdump files using the APIs in `DotNet.GCDump.Analyze.GCDump`.

The CLI exposes each public capability of `GCDump` as a subcommand and renders results as Markdown tables by default (using `Markdown.Write`). For tree views (for root paths), a box-drawing tree can be emitted via `Markdown.WriteTree`.

Capabilities (maps to GCDump APIs)
----------------------------------

- Top types by Inclusive Size → `GetReportByInclusiveSize(int rows)`
- Top types by Size (shallow) → `GetReportBySize(int rows)`
- Top types by Count → `GetReportByCount(int rows)`
- Filter by name contains (sorted by Inclusive Size) → `GetReportByName(string nameContains)`
- Hot paths to GC roots for name contains → `GetPathsToRoot(string nameContains)`

Input is a single `.gcdump` file path. Output is Markdown to stdout by default; you can redirect to a file.

Usage and help
--------------

```text
gcdump-analyze [command] [options]

Commands:
	top            Show top types by Inclusive Size (retained).
	top-size       Show top types by shallow Size (Bytes).
	top-count      Show top types by object Count.
	filter         Show rows for types whose name contains a substring (sorted by Inclusive Size).
	roots          Show hot path(s) to GC roots for matching types.
	--version      Show version information.
	-?, -h, --help Show help and usage information.
```

Common options
--------------

- `-r, --rows <n>`: Number of rows to include (default: 10) — applies to `top`, `top-size`, and `top-count`. For `filter`, it limits the printed rows after filtering (the underlying API returns all matches). Not used by `roots`.
- `-o, --output <file>`: Write output to the specified file (Markdown text). Defaults to stdout.

All commands require a `PATH` argument to the `.gcdump` file unless otherwise noted.

Commands
--------

top — Top by Inclusive Size
---------------------------

Maps to: `GCDump.GetReportByInclusiveSize(int rows)`

```text
gcdump-analyze top [options] <PATH>

Options:
	-r, --rows <n>      Number of rows (default: 10). Must be > 0.
	-o, --output <file> Write Markdown to file instead of stdout.
	-?, -h, --help      Show help and usage information.
```

Output columns:

- Object Type | Count | Size (Bytes) | Inclusive Size (Bytes)

top-size — Top by shallow Size (Bytes)
--------------------------------------

Maps to: `GCDump.GetReportBySize(int rows)`

```text
gcdump-analyze top-size [options] <PATH>

Options:
	-r, --rows <n>      Number of rows (default: 10). Must be > 0.
	-o, --output <file> Write Markdown to file instead of stdout.
	-?, -h, --help      Show help and usage information.
```

Output columns:

- Object Type | Count | Size (Bytes) | Inclusive Size (Bytes)

top-count — Top by object Count
-------------------------------

Maps to: `GCDump.GetReportByCount(int rows)`

```text
gcdump-analyze top-count [options] <PATH>

Options:
	-r, --rows <n>      Number of rows (default: 10). Must be > 0.
	-o, --output <file> Write Markdown to file instead of stdout.
	-?, -h, --help      Show help and usage information.
```

Output columns:

- Object Type | Count | Size (Bytes) | Inclusive Size (Bytes)

filter — Rows for matching types (by name contains)
---------------------------------------------------

Maps to: `GCDump.GetReportByName(string nameContains)`

```text
gcdump-analyze filter [options] <PATH>

Options:
	-n, --name <substring>  Required. Case-insensitive substring to match in type names.
	-r, --rows <n>          Optional. Limit the number of printed rows (post-filter).
	-o, --output <file>     Write Markdown to file instead of stdout.
	-?, -h, --help          Show help and usage information.
```

Notes:

- The report is sorted by Inclusive Size before filtering (matching the API behavior).
- If no rows match, an empty table is printed.

Output columns:

- Object Type | Count | Size (Bytes) | Inclusive Size (Bytes)

roots — Hot paths to GC roots for matching types (tree)
------------------------------------------------

Maps to: `GCDump.GetPathsToRoot(string nameContains)`

```text
gcdump-analyze roots [options] <PATH>

Options:
	-n, --name <substring>  Required. Case-insensitive substring to match in type names.
	-o, --output <file>      Write output to file instead of stdout.
	-?, -h, --help           Show help and usage information.
```

Output: a tree view using box-drawing characters, such as:

```text
├── hellomauileak.LeakyPage (Count: 3)
│   └── Microsoft.Maui.Controls.Window (Count: 3)
│       └── hellomauileak.App (Count: 3)
```

Examples
--------

Show top 8 types by inclusive size:

```pwsh
gcdump-analyze top -r 8 .\data\test1.gcdump
```

Top by size (shallow), default 10 rows:

```pwsh
gcdump-analyze top-size .\data\test1.gcdump
```

Top by count, write to a file:

```pwsh
gcdump-analyze top-count -r 20 .\data\test1.gcdump --output .\out.md
```

Filter types whose names contain "LeakyPage":

```pwsh
gcdump-analyze filter -n LeakyPage .\data\leakypage.gcdump
```

Show hot root path for types containing "LeakyPage":

```pwsh
gcdump-analyze roots -n LeakyPage .\data\leakypage.gcdump
```

Errors and exit codes
---------------------

- Non-zero exit code on failure; error text to stderr.
- File not found or unreadable: print an error indicating the path.
- Invalid `--rows` (<= 0): print an argument error (matches API validation).
- Missing or empty `--name` where required: print an argument error.
- Parse errors: print a generic failure: "Failed to open or parse the .gcdump file." (surfaced from the core API).

Implementation notes (for maintainers)
-------------------------------------

- Use System.CommandLine to define subcommands and options.
- For each command: open with `using var dump = GCDump.Open(path);`, call the respective API, then render:
	- Tables: `Markdown.Write(report, Console.Out)` for top/top-size/top-count/filter
	- Roots (always tree): `Markdown.WriteTree(report, Console.Out)`
- Respect `--output` by opening a `StreamWriter` to the file instead of stdout.
- Keep column names and sort behavior exactly as returned by `TableReport` from the core library.

