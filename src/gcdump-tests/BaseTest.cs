namespace DotNet.GCDump.Analyze.Tests;

public class BaseTest
{
    protected static string GetProjectDir()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++)
        {
            var csproj = Path.Combine(dir.FullName, "gcdump-tests.csproj");
            if (File.Exists(csproj))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate test project directory.");
    }

    protected static string GetFilePath(string fileName) =>
        Path.Combine(GetDataDir(), fileName);

    protected static string GetDataDir()
    {
        // AppContext.BaseDirectory points to bin/<config>/<tfm>/ for the test assembly
        // Navigate to repo root from that.
        var baseDir = AppContext.BaseDirectory;
        // search upwards for the repo root containing 'data' directory
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 6 && dir != null; i++)
        {
            var dataPath = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(dataPath))
                return dataPath;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate 'data' directory relative to test base directory.");
    }
}
