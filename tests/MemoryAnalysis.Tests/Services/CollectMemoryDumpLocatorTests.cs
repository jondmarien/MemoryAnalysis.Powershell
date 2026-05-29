using System.IO;
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
}
