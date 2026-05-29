using PowerShell.MemoryAnalysis.Models;
using Xunit;

namespace MemoryAnalysis.Tests.Models;

public class DiscoveredMemoryDumpTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776, "1 TB")]
    public void Size_FormatsHumanReadable(long bytes, string expectedSuffix)
    {
        var dump = new DiscoveredMemoryDump { SizeBytes = bytes };
        Assert.EndsWith(expectedSuffix, dump.Size);
    }

    [Fact]
    public void ToString_IncludesFileNameAndSize()
    {
        var dump = new DiscoveredMemoryDump
        {
            Path = "/tmp/case/host.raw",
            SizeBytes = 2048
        };

        var text = dump.ToString();
        Assert.Contains("host.raw", text);
        Assert.Contains("2 KB", text);
    }

    [Fact]
    public void Properties_RoundTrip()
    {
        var when = DateTime.UtcNow.AddHours(-2);
        var dump = new DiscoveredMemoryDump
        {
            Path = @"C:\dumps\mem.vmem",
            SizeBytes = 42,
            LastWriteTimeUtc = when
        };

        Assert.Equal(@"C:\dumps\mem.vmem", dump.Path);
        Assert.Equal(42, dump.SizeBytes);
        Assert.Equal(when, dump.LastWriteTimeUtc);
    }
}
