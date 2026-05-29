using System.Runtime.InteropServices;
using PowerShell.MemoryAnalysis.Services;
using Xunit;

namespace MemoryAnalysis.Tests.Services;

public class CollectMemoryDumpLocatorTests
{
    [Fact]
    public void FindScriptPath_LocatesSubmoduleFromRepoRoot()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot == null)
        {
            return;
        }

        var script = CollectMemoryDumpLocator.FindScriptPath(repoRoot);
        Assert.NotNull(script);
        Assert.True(File.Exists(script));
        Assert.EndsWith("Collect-MemoryDump.ps1", script);
    }

    [Fact]
    public void FindScriptPath_ReturnsNullForEmptyStart()
    {
        Assert.Null(CollectMemoryDumpLocator.FindScriptPath(null));
        Assert.Null(CollectMemoryDumpLocator.FindScriptPath("   "));
    }

    [Fact]
    public void FindScriptPath_WalksUpFromChildDirectory()
    {
        var root = CreateTempDirectory();
        try
        {
            var scriptDir = Path.Combine(root, "third-party", "Collect-MemoryDump");
            Directory.CreateDirectory(scriptDir);
            var scriptPath = Path.Combine(scriptDir, "Collect-MemoryDump.ps1");
            File.WriteAllText(scriptPath, "# stub");

            var nested = Path.Combine(root, "a", "b");
            Directory.CreateDirectory(nested);

            var found = CollectMemoryDumpLocator.FindScriptPath(nested);
            Assert.Equal(scriptPath, found);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GetWinPmemExecutablePath_Uses64BitNameOn64BitOs()
    {
        var script = Path.Combine(CreateTempDirectory(), "Collect-MemoryDump.ps1");
        File.WriteAllText(script, "# stub");

        var winPmem = CollectMemoryDumpLocator.GetWinPmemExecutablePath(script);
        if (Environment.Is64BitOperatingSystem)
        {
            Assert.EndsWith("winpmem_mini_x64_rc2.exe", winPmem);
        }
        else
        {
            Assert.EndsWith("winpmem_mini_x86.exe", winPmem);
        }

        Directory.Delete(Path.GetDirectoryName(script)!, recursive: true);
    }

    [Fact]
    public void GetCollectMemoryDumpOutputRoot_ReturnsScriptDirectory()
    {
        var script = Path.Combine(CreateTempDirectory(), "Collect-MemoryDump.ps1");
        File.WriteAllText(script, "# stub");
        var root = CollectMemoryDumpLocator.GetCollectMemoryDumpOutputRoot(script);
        Assert.Equal(Path.GetDirectoryName(script), root);
        Directory.Delete(root!, recursive: true);
    }

    [Fact]
    public void IsWindows_MatchesRuntime()
    {
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), CollectMemoryDumpLocator.IsWindows());
    }

    [Fact]
    public void RelativeScriptPath_UsesThirdPartyLayout()
    {
        Assert.Contains("third-party", CollectMemoryDumpLocator.RelativeScriptPath);
        Assert.Contains("Collect-MemoryDump", CollectMemoryDumpLocator.RelativeScriptPath);
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, CollectMemoryDumpLocator.RelativeScriptPath);
            if (File.Exists(candidate))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static string CreateTempDirectory() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cmd-locator-" + Guid.NewGuid())).FullName;
}
