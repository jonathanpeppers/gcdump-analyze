---
applyTo: "**/*.csproj"
---

# Tech Stack

* Targets .NET 9 runtime & .NET 9 SDK.
* All implementation is contained in a `gcdump-analyze-core` class library, to be reused by multiple client programs.
* Command-line tools use System.CommandLine:
  * https://www.nuget.org/packages/System.CommandLine/2.0.0-beta7.25380.108

## Repo Structure

* `gcdump-analyze.sln`: main Visual Studio solution file
* `data`: test data, example `.gcdump` files
* `DotNet.GCDump.Analyze` is the default namespace
* Shared settings should all go in `Directory.Build.props`
* `src\gcdump-analyze-core\`: class library containing shared C# code
* `src\gcdump-analyze\`: .NET global tool, can be invoked by `gcdump-analyze foo.gcdump`
* `src\gcdump-analyze-tests\`: Xunit, unit test project
* `samples\hellomauileak`: an example .NET MAUI app to record `.gcdump` files from
