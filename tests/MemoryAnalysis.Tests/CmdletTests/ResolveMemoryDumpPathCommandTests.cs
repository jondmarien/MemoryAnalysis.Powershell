using System.Management.Automation;
using PowerShell.MemoryAnalysis.Cmdlets;
using PowerShell.MemoryAnalysis.Models;
using MemoryAnalysis.Tests.Helpers;
using Xunit;

namespace MemoryAnalysis.Tests.CmdletTests;

public class ResolveMemoryDumpPathCommandTests
{
    [Fact]
    public void Cmdlet_HasResolveVerb()
    {
        var attr = typeof(ResolveMemoryDumpPathCommand)
            .GetCustomAttributes(typeof(CmdletAttribute), false)
            .Cast<CmdletAttribute>()
            .Single();

        Assert.Equal("Resolve", attr.VerbName);
        Assert.Equal("MemoryDumpPath", attr.NounName);
    }

    [Fact]
    public void MaxSearchDepth_DefaultsToOne()
    {
        var cmdlet = new ResolveMemoryDumpPathCommand();
        Assert.Equal(1, cmdlet.MaxSearchDepth);
    }

    [Fact]
    public void Invoke_ExplicitPath_MissingFile_Throws()
    {
        using var helper = ModuleCommandHelper.Create();
        var missing = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".raw");
        Assert.Throws<CmdletInvocationException>(() =>
            helper.InvokeResolveMemoryDumpPath(("Path", missing)));
    }

    [Fact]
    public void Invoke_Search_NoDump_WithGitHubActions_Throws()
    {
        var root = CreateTempDirectory();
        var previous = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
        try
        {
            using var helper = ModuleCommandHelper.Create();
            Assert.Throws<CmdletInvocationException>(() =>
                helper.InvokeResolveMemoryDumpPath(("SearchPath", root)));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", previous);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_Search_LargeDump_ConfirmViaHost()
    {
        var root = CreateTempDirectory();
        try
        {
            var dumpPath = Path.Combine(root, "large.raw");
            using (var stream = File.Create(dumpPath))
            {
                stream.SetLength((10L * 1024 * 1024 * 1024) + 1);
            }

            using var helper = ModuleCommandHelper.Create(new TestPSHost(promptForChoiceResult: 0));
            var results = helper.InvokeResolveMemoryDumpPath(
                ("SearchPath", root),
                ("MaxSearchDepth", 0),
                ("NoAcquire", true));

            Assert.Single(results);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_ExplicitPath_ReturnsDiscoveredDump()
    {
        var root = CreateTempDirectory();
        try
        {
            var dumpPath = Path.Combine(root, "host.raw");
            File.WriteAllText(dumpPath, "dump");

            using var helper = ModuleCommandHelper.Create();
            var results = helper.InvokeResolveMemoryDumpPath(("Path", dumpPath));

            Assert.Single(results);
            var dump = Assert.IsType<DiscoveredMemoryDump>(results[0].BaseObject);
            Assert.Equal(Path.GetFullPath(dumpPath), dump.Path);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_Search_WithPassThru_ReturnsPathString()
    {
        var root = CreateTempDirectory();
        try
        {
            var dumpPath = Path.Combine(root, "host.raw");
            File.WriteAllText(dumpPath, "dump");

            using var helper = ModuleCommandHelper.Create();
            var results = helper.InvokeResolveMemoryDumpPath(
                ("SearchPath", root),
                ("MaxSearchDepth", 0),
                ("NoAcquire", true),
                ("Force", true),
                ("PassThru", true));

            Assert.Equal(2, results.Count);
            Assert.IsType<DiscoveredMemoryDump>(results[0].BaseObject);
            Assert.Equal(Path.GetFullPath(dumpPath), results[1].BaseObject);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_ExplicitNonCandidate_WritesWarningStillReturns()
    {
        var root = CreateTempDirectory();
        try
        {
            var dumpPath = Path.Combine(root, "notes.txt");
            File.WriteAllText(dumpPath, "not a dump extension");

            using var helper = ModuleCommandHelper.Create();
            var results = helper.InvokeResolveMemoryDumpPath(("Path", dumpPath));

            Assert.Single(results);
            Assert.NotEmpty(helper.PowerShell.Streams.Warning);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_Search_FindsSingleDump()
    {
        var root = CreateTempDirectory();
        try
        {
            var dumpPath = Path.Combine(root, "a.raw");
            File.WriteAllText(dumpPath, "x");

            using var helper = ModuleCommandHelper.Create();
            var results = helper.InvokeResolveMemoryDumpPath(
                ("SearchPath", root),
                ("MaxSearchDepth", 0),
                ("NoAcquire", true),
                ("Force", true));

            Assert.Single(results);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_Search_NoDump_WithNoAcquire_Throws()
    {
        var root = CreateTempDirectory();
        try
        {
            using var helper = ModuleCommandHelper.Create();
            Assert.Throws<CmdletInvocationException>(() =>
                helper.InvokeResolveMemoryDumpPath(
                    ("SearchPath", root),
                    ("NoAcquire", true)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_Search_NoDump_InCiEnvironment_Throws()
    {
        var root = CreateTempDirectory();
        var previous = Environment.GetEnvironmentVariable("MEMORYANALYSIS_CI");
        Environment.SetEnvironmentVariable("MEMORYANALYSIS_CI", "1");
        try
        {
            using var helper = ModuleCommandHelper.Create();
            Assert.Throws<CmdletInvocationException>(() =>
                helper.InvokeResolveMemoryDumpPath(("SearchPath", root)));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MEMORYANALYSIS_CI", previous);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_Search_InvalidDirectory_Throws()
    {
        using var helper = ModuleCommandHelper.Create();
        var missing = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid());
        Assert.Throws<CmdletInvocationException>(() =>
            helper.InvokeResolveMemoryDumpPath(("SearchPath", missing), ("NoAcquire", true)));
    }

    [Fact]
    public void Invoke_Search_MultipleDumps_SelectsFirstViaHost()
    {
        var root = CreateTempDirectory();
        try
        {
            // Discovery sorts by size (desc) then time; make ordering deterministic across OSes.
            File.WriteAllText(Path.Combine(root, "one.raw"), new string('x', 256));
            File.WriteAllText(Path.Combine(root, "two.raw"), "b");

            using var helper = ModuleCommandHelper.Create(new TestPSHost(readLine: "1"));
            var results = helper.InvokeResolveMemoryDumpPath(
                ("SearchPath", root),
                ("MaxSearchDepth", 0),
                ("NoAcquire", true),
                ("Force", true));

            Assert.Single(results);
            var dump = Assert.IsType<DiscoveredMemoryDump>(results[0].BaseObject);
            Assert.Contains("one.raw", dump.Path, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_Search_LargeDump_WithForce_SkipsPrompt()
    {
        var root = CreateTempDirectory();
        try
        {
            var dumpPath = Path.Combine(root, "large.raw");
            using (var stream = File.Create(dumpPath))
            {
                stream.SetLength((10L * 1024 * 1024 * 1024) + 1);
            }

            using var helper = ModuleCommandHelper.Create(new TestPSHost(promptForChoiceResult: 1));
            var results = helper.InvokeResolveMemoryDumpPath(
                ("SearchPath", root),
                ("MaxSearchDepth", 0),
                ("NoAcquire", true),
                ("Force", true));

            Assert.Single(results);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_Search_NoDump_OnNonWindows_WritesPlatformError()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var root = CreateTempDirectory();
        var previousGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        var previousCi = Environment.GetEnvironmentVariable("MEMORYANALYSIS_CI");
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
        Environment.SetEnvironmentVariable("MEMORYANALYSIS_CI", null);
        try
        {
            using var helper = ModuleCommandHelper.Create();
            helper.InvokeResolveMemoryDumpPath(("SearchPath", root));
            Assert.Contains(
                helper.PowerShell.Streams.Error,
                e => e.Exception is PlatformNotSupportedException);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", previousGitHubActions);
            Environment.SetEnvironmentVariable("MEMORYANALYSIS_CI", previousCi);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_Search_DeclinedAcquisition_OnWindows_ReturnsWithoutError()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = CreateTempDirectory();
        try
        {
            using var helper = ModuleCommandHelper.Create(new TestPSHost(promptForChoiceResult: 1));
            var results = helper.InvokeResolveMemoryDumpPath(("SearchPath", root));
            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "resolve-cmdlet-" + Guid.NewGuid())).FullName;
}
