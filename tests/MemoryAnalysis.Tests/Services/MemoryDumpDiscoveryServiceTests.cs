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

    [Fact]
    public void Discover_ThrowsForEmptySearchRoot()
    {
        var service = new MemoryDumpDiscoveryService();
        Assert.Throws<ArgumentException>(() => service.Discover("  ", 1));
    }

    [Fact]
    public void Discover_ThrowsForNegativeDepth()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new MemoryDumpDiscoveryService();
            Assert.Throws<ArgumentOutOfRangeException>(() => service.Discover(root, -1));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Discover_ThrowsWhenDirectoryMissing()
    {
        var service = new MemoryDumpDiscoveryService();
        Assert.Throws<DirectoryNotFoundException>(() =>
            service.Discover(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), 0));
    }

    [Fact]
    public void Discover_IncludesLargeDmpFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            var dump = Path.Combine(root, "full.dmp");
            File.WriteAllBytes(dump, new byte[MemoryDumpDiscoveryService.DefaultMinDumpSizeBytes]);

            var service = new MemoryDumpDiscoveryService();
            var results = service.Discover(root, 0);

            Assert.Single(results);
            Assert.True(service.IsCandidateDumpFile(dump));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Discover_FindsMultipleExtensions()
    {
        var root = CreateTempDirectory();
        try
        {
            foreach (var ext in new[] { ".vmem", ".mem", ".img" })
            {
                File.WriteAllText(Path.Combine(root, "f" + ext), "x");
            }

            var service = new MemoryDumpDiscoveryService();
            var results = service.Discover(root, 0);

            Assert.Equal(3, results.Count);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void IsCandidateDumpFile_ReturnsFalseForNonDumpExtension()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "notes.txt");
            File.WriteAllText(path, "hello");
            var service = new MemoryDumpDiscoveryService();
            Assert.False(service.IsCandidateDumpFile(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void IsCandidateDumpFile_ReturnsFalseForMissingFile()
    {
        var service = new MemoryDumpDiscoveryService();
        Assert.False(service.IsCandidateDumpFile(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".raw")));
    }

    private static string CreateTempDirectory() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mad-test-" + Guid.NewGuid())).FullName;
}
