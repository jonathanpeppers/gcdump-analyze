using System.Diagnostics;
using VerifyTests;

namespace DotNet.GCDump.Analyze.Tests;

public class CliTests : BaseTest
{
    private static string GetCliDll()
    {
        // Resolve path to src/gcdump-analyze/bin/<config>/net9.0/gcdump-analyze.dll
        var repoRoot = Path.GetFullPath(Path.Combine(GetProjectDir(), "..", ".."));
#if DEBUG
        const string cfg = "Debug";
#else
        const string cfg = "Release";
#endif
        var cliDll = Path.Combine(repoRoot, "src", "gcdump-analyze", "bin", cfg, "net9.0", "gcdump-analyze.dll");
        if (!File.Exists(cliDll))
            throw new FileNotFoundException($"CLI not found at {cliDll}. Build the solution before running tests.");
        return cliDll;
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunCliAsync(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{GetCliDll()}\" {string.Join(' ', args.Select(QuoteIfNeeded))}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi)!;
        string stdout = await proc.StandardOutput.ReadToEndAsync();
        string stderr = await proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        return (proc.ExitCode, stdout.Replace("\r\n", "\n"), stderr.Replace("\r\n", "\n"));
    }

    private static string QuoteIfNeeded(string s) => s.Contains(' ') ? $"\"{s}\"" : s;

    [Fact]
    public async Task CLI_Top_InclusiveSize_Snapshot()
    {
        var path = GetFilePath("test1.gcdump");
        var (code, stdout, stderr) = await RunCliAsync("top", "-r", "8", path);
        Assert.Equal(0, code);
        Assert.True(string.IsNullOrEmpty(stderr), stderr);

        await VerifyXunit.Verifier.Verify(stdout)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("cli-top");
    }

    [Fact]
    public async Task CLI_TopSize_Snapshot()
    {
        var path = GetFilePath("test1.gcdump");
        var (code, stdout, stderr) = await RunCliAsync("top-size", "-r", "8", path);
        Assert.Equal(0, code);
        Assert.True(string.IsNullOrEmpty(stderr), stderr);

        await VerifyXunit.Verifier.Verify(stdout)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("cli-top-size");
    }

    [Fact]
    public async Task CLI_TopCount_Snapshot()
    {
        var path = GetFilePath("test1.gcdump");
        var (code, stdout, stderr) = await RunCliAsync("top-count", "-r", "8", path);
        Assert.Equal(0, code);
        Assert.True(string.IsNullOrEmpty(stderr), stderr);

        await VerifyXunit.Verifier.Verify(stdout)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("cli-top-count");
    }

    [Fact]
    public async Task CLI_Filter_ByName_Snapshot()
    {
        var path = GetFilePath("test1.gcdump");
        var (code, stdout, stderr) = await RunCliAsync("filter", "-n", "LeakyPage", "-r", "10", path);
        Assert.Equal(0, code);
        Assert.True(string.IsNullOrEmpty(stderr), stderr);

        await VerifyXunit.Verifier.Verify(stdout)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("cli-filter");
    }

    [Fact]
    public async Task CLI_Roots_Table_Snapshot()
    {
        var path = GetFilePath("test1.gcdump");
        var (code, stdout, stderr) = await RunCliAsync("roots", "-n", "LeakyPage", path);
        Assert.Equal(0, code);
        Assert.True(string.IsNullOrEmpty(stderr), stderr);

        await VerifyXunit.Verifier.Verify(stdout)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("cli-roots-table");
    }

    [Fact]
    public async Task CLI_Roots_Tree_Snapshot()
    {
        var path = GetFilePath("test1.gcdump");
        var (code, stdout, stderr) = await RunCliAsync("roots", "-n", "LeakyPage", "--tree", path);
        Assert.Equal(0, code);
        Assert.True(string.IsNullOrEmpty(stderr), stderr);

        await VerifyXunit.Verifier.Verify(stdout)
            .UseDirectory(Path.Combine(GetProjectDir(), "Snapshots"))
            .UseTextForParameters("cli-roots-tree");
    }
}
