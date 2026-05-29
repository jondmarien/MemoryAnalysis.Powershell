using PowerShell.MemoryAnalysis.Services;
using Xunit;

namespace MemoryAnalysis.Tests.Services;

public class MemoryDumpDiscoveryMergeTests
{
    [Fact]
    public void DiscoverWithCollectMemoryDumpOutput_MergesSearchAndScriptOutputTrees()
    {
        var root = CreateTempDirectory();
        try
        {
            var scriptDir = Path.Combine(root, "third-party", "Collect-MemoryDump");
            Directory.CreateDirectory(scriptDir);
            var scriptPath = Path.Combine(scriptDir, "Collect-MemoryDump.ps1");
            File.WriteAllText(scriptPath, "# stub");

            var inSearch = Path.Combine(root, "case", "search.raw");
            Directory.CreateDirectory(Path.GetDirectoryName(inSearch)!);
            File.WriteAllText(inSearch, new string('a', 64));

            var inOutput = Path.Combine(scriptDir, "HOST", "out.raw");
            Directory.CreateDirectory(Path.GetDirectoryName(inOutput)!);
            File.WriteAllText(inOutput, new string('b', 128));

            var service = new MemoryDumpDiscoveryService();
            var results = service.DiscoverWithCollectMemoryDumpOutput(root, maxSearchDepth: 2, scriptPath);

            Assert.Equal(2, results.Count);
            Assert.Contains(results, d => d.Path == Path.GetFullPath(inSearch));
            Assert.Contains(results, d => d.Path == Path.GetFullPath(inOutput));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DiscoverWithCollectMemoryDumpOutput_IgnoresMissingScriptPath()
    {
        var root = CreateTempDirectory();
        try
        {
            var dump = Path.Combine(root, "only.raw");
            File.WriteAllText(dump, "x");

            var service = new MemoryDumpDiscoveryService();
            var results = service.DiscoverWithCollectMemoryDumpOutput(
                root,
                maxSearchDepth: 0,
                collectMemoryDumpScriptPath: Path.Combine(root, "missing.ps1"));

            Assert.Single(results);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DiscoverAfterAcquisition_OrdersBySizeDescending()
    {
        var root = CreateTempDirectory();
        try
        {
            var scriptDir = Path.Combine(root, "third-party", "Collect-MemoryDump");
            Directory.CreateDirectory(scriptDir);
            var scriptPath = Path.Combine(scriptDir, "Collect-MemoryDump.ps1");
            File.WriteAllText(scriptPath, "# stub");

            File.WriteAllText(Path.Combine(root, "small.raw"), "a");
            var large = Path.Combine(scriptDir, "large.raw");
            File.WriteAllText(large, new string('x', 200));

            var service = new MemoryDumpDiscoveryService();
            var results = service.DiscoverAfterAcquisition(root, scriptPath);

            Assert.Equal(2, results.Count);
            Assert.Equal(Path.GetFullPath(large), results[0].Path);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DiscoverAfterAcquisition_FindsDumpOnlyUnderScriptOutput()
    {
        var root = CreateTempDirectory();
        try
        {
            var scriptDir = Path.Combine(root, "third-party", "Collect-MemoryDump");
            Directory.CreateDirectory(scriptDir);
            var scriptPath = Path.Combine(scriptDir, "Collect-MemoryDump.ps1");
            File.WriteAllText(scriptPath, "# stub");

            var onlyInOutput = Path.Combine(scriptDir, "capture.raw");
            File.WriteAllText(onlyInOutput, new string('z', 50));

            var service = new MemoryDumpDiscoveryService();
            var results = service.DiscoverAfterAcquisition(root, scriptPath);

            Assert.Single(results);
            Assert.Equal(Path.GetFullPath(onlyInOutput), results[0].Path);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mad-merge-" + Guid.NewGuid())).FullName;
}
