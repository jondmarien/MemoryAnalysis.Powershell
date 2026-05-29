using System;
using System.IO;
using System.Linq;
using PowerShell.MemoryAnalysis.Services;
using Xunit;

namespace MemoryAnalysis.Tests.Services;

public class MemoryDumpDiscoveryServiceTests
{
    [Fact]
    public void Discover_FindsFileInCurrentDirectory()
    {
        var root = CreateTempDirectory();
        try
        {
            var dump = Path.Combine(root, "sample.raw");
            File.WriteAllBytes(dump, new byte[MemoryDumpDiscoveryService.DefaultMinDumpSizeBytes]);

            var service = new MemoryDumpDiscoveryService();
            var results = service.Discover(root, maxSearchDepth: 0);

            Assert.Single(results);
            Assert.Equal(dump, results[0].Path);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Discover_FindsFileOneLevelDown()
    {
        var root = CreateTempDirectory();
        try
        {
            var sub = Path.Combine(root, "case01");
            Directory.CreateDirectory(sub);
            var dump = Path.Combine(sub, "host.raw");
            File.WriteAllBytes(dump, new byte[MemoryDumpDiscoveryService.DefaultMinDumpSizeBytes]);

            var service = new MemoryDumpDiscoveryService();
            var results = service.Discover(root, maxSearchDepth: 1);

            Assert.Single(results);
            Assert.Equal(dump, results[0].Path);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Discover_ExcludesSmallDmpMinidump()
    {
        var root = CreateTempDirectory();
        try
        {
            var mini = Path.Combine(root, "wer.dmp");
            File.WriteAllBytes(mini, new byte[1024]);

            var service = new MemoryDumpDiscoveryService();
            var results = service.Discover(root, maxSearchDepth: 0);

            Assert.Empty(results);
            Assert.False(service.IsCandidateDumpFile(mini));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Discover_RespectsMaxSearchDepth()
    {
        var root = CreateTempDirectory();
        try
        {
            var deep = Path.Combine(root, "a", "b");
            Directory.CreateDirectory(deep);
            var dump = Path.Combine(deep, "deep.raw");
            File.WriteAllBytes(dump, new byte[MemoryDumpDiscoveryService.DefaultMinDumpSizeBytes]);

            var service = new MemoryDumpDiscoveryService();
            var shallow = service.Discover(root, maxSearchDepth: 1);
            var deepSearch = service.Discover(root, maxSearchDepth: 3);

            Assert.Empty(shallow);
            Assert.Single(deepSearch);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mad-test-" + Guid.NewGuid())).FullName;
}
