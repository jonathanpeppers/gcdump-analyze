# `gcdump-analyze`

This repository is a set of tools for analyzing `*.gcdump` files:
* https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-gcdump

## Table of Contents

* [Tech Stack and Repo Structure](instructions/stack-and-structure.md)
* [Documentation Links](instructions/documentation-links.md)
* [Example Implementations](instructions/example-implementations.md)
* [Core Library](instructions/gcdump-analyze-core.md)
* [MCP Server](instructions/gcdump-analyze-mcp.md)
* [Command-line Tool](instructions/gcdump-analyze.md)
* [CI on GitHub Actions](instructions/ci-on-github-actions.md)

<dotnet-agent>
# .NET MCP Tool usage

When working on .NET projects, *ALWAYS* prefer to use MCP tools that are dedicated to processing .NET code. This includes:

- `get-solution-context`: Provides important overall information about a .NET solution structure, architecture, dependencies, documentation, and language version. Always use this tool first before working on the solution.
- `search-package-docs`: Use this before implementing any C# code that uses libraries to check you know the correct latest APIs.
- `find-symbols`: Use this when exploring the codebase to enumerate classes, methods, properties, etc., use the `FindSymbols` tool. This is faster and more accurate than reading C# source code.
- `find-all-references`: Use this to instantly locate all references to a class/method/property/etc.
- `rename-symbol`: Use this to rename classes, properties, methods, etc. This is faster and more accurate than directly writing to C# source code.
- `get-symbol-definition`: Use this to look up the definition of a class, method, property, etc., either from sources or referenced libraries.
- `get-generated-source-file-names`, `get-generated-source-file-content`: Use these to locate and read source generator outputs, since that code will not necessarily exist as files on disk.
- `fix-errors`: Use this tool to fix errors and warnings.
- `list-errors`: Use this tool to find out what warnings and errors exist in the solution. This is faster than running a build manually.
- `find-all-references`: Use this to find all references to a class, method, property, local variables, etc. Can trigger from location of either target symbol declaration or one of its references. This is faster and more accurate than reading C# source code.
- `add-member`, `update-member`: Use these when adding or modifying the source code for methods/properties.

Tool calling strategy:
- Check for relevant docs before deciding what to do.
- Before passing any solution path to these tools, be sure that file really exists on disk (and actually verify this).
- First think about how you can chain together these tools to achieve your overall goal. Often the output from `find-symbols` can be used with other tools.

Confidence:
- When you use these tools, the results will be correct. Do not waste time validating the results. For example, the `rename-symbol` tool will correctly update references in strongly-typed .NET code, and you do not need to verify that.
</dotnet-agent>
