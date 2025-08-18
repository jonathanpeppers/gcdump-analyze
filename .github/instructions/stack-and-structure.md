---
applyTo: "**/*.csproj"
---

# Tech Stack

* Targets .NET 9 runtime & .NET 9 SDK.
* All implementation is contained in a `gcdump-analyze-core` class library, to be reused by multiple client programs.
* Command-line tools use System.CommandLine:
  * https://www.nuget.org/packages/System.CommandLine/2.0.0-beta7.25380.108
* C# MCP servers use:
  * https://www.nuget.org/packages/ModelContextProtocol/0.3.0-preview.3
  * https://github.com/modelcontextprotocol/csharp-sdk

## Repo Structure

* `gcdump-analyze.sln`: main Visual Studio solution file
* `data`: test data, example `.gcdump` files
* `DotNet.GCDump.Analyze` is the default namespace
* Shared settings should all go in `Directory.Build.props`
* `src\gcdump-analyze-core\`: class library containing shared C# code
* `src\gcdump-analyze\`: .NET global tool, can be invoked by `gcdump-analyze foo.gcdump`
* `src\gcdump-analyze-mcp\`: exposes functionality from `gcdump-analyze-core` as an MCP server
* `src\gcdump-tests\`: Xunit, unit test project
