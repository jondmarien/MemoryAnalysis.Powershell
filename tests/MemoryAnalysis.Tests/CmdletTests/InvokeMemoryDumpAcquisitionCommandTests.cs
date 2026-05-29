using System.Management.Automation;
using System.Runtime.InteropServices;
using PowerShell.MemoryAnalysis.Cmdlets;
using MemoryAnalysis.Tests.Helpers;
using CollectMemoryDumpTestLayout = MemoryAnalysis.Tests.Helpers.CollectMemoryDumpTestLayout;
using Xunit;

namespace MemoryAnalysis.Tests.CmdletTests;

public class InvokeMemoryDumpAcquisitionCommandTests
{
    [Fact]
    public void Cmdlet_HasInvokeVerb()
    {
        var attr = typeof(InvokeMemoryDumpAcquisitionCommand)
            .GetCustomAttributes(typeof(CmdletAttribute), false)
            .Cast<CmdletAttribute>()
            .Single();

        Assert.Equal("Invoke", attr.VerbName);
        Assert.Equal("MemoryDumpAcquisition", attr.NounName);
    }

    [Fact]
    public void Tool_DefaultsToWinPMEM()
    {
        var cmdlet = new InvokeMemoryDumpAcquisitionCommand();
        Assert.Equal("WinPMEM", cmdlet.Tool);
    }

    [Fact]
    public void Invoke_OnNonWindows_ThrowsPlatformNotSupported()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        using var helper = ModuleCommandHelper.Create();
        helper.PowerShell.AddCommand("Invoke-MemoryDumpAcquisition");
        Assert.Throws<CmdletInvocationException>(() => helper.PowerShell.Invoke());
    }

    [Fact]
    public void Invoke_OnWindows_WithStubScriptAndWinPmem_DiscoversDump()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var root = CreateTempDirectory();
        try
        {
            var fixture = CollectMemoryDumpTestLayout.Create(root);

            using var helper = ModuleCommandHelper.Create();
            helper.PowerShell.AddCommand("Invoke-MemoryDumpAcquisition")
                .AddParameter("SearchPath", root)
                .AddParameter("CollectMemoryDumpScript", fixture.ScriptPath);

            var results = helper.PowerShell.Invoke();
            Assert.NotEmpty(results);
            Assert.Equal(Path.GetFullPath(fixture.DumpPath!), results[0].BaseObject);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_OnWindows_WhenNoDumpAfterScript_WritesWarning()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var root = CreateTempDirectory();
        try
        {
            var fixture = CollectMemoryDumpTestLayout.Create(root, dumpFileName: null);

            using var helper = ModuleCommandHelper.Create();
            helper.PowerShell.AddCommand("Invoke-MemoryDumpAcquisition")
                .AddParameter("SearchPath", root)
                .AddParameter("CollectMemoryDumpScript", fixture.ScriptPath);

            var results = helper.PowerShell.Invoke();
            Assert.Empty(results);
            Assert.NotEmpty(helper.PowerShell.Streams.Warning);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_OnWindows_WithMissingScriptPath_Throws()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var root = CreateTempDirectory();
        try
        {
            var missingScript = Path.Combine(root, "nope", "Collect-MemoryDump.ps1");
            using var helper = ModuleCommandHelper.Create();
            helper.PowerShell.AddCommand("Invoke-MemoryDumpAcquisition")
                .AddParameter("SearchPath", root)
                .AddParameter("CollectMemoryDumpScript", missingScript);

            Assert.Throws<CmdletInvocationException>(() => helper.PowerShell.Invoke());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Invoke_OnWindows_WithMissingWinPmem_Throws()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var root = CreateTempDirectory();
        try
        {
            var scriptDir = Path.Combine(root, "third-party", "Collect-MemoryDump");
            Directory.CreateDirectory(scriptDir);
            var scriptPath = Path.Combine(scriptDir, "Collect-MemoryDump.ps1");
            File.WriteAllText(scriptPath, "# no winpmem");

            using var helper = ModuleCommandHelper.Create();
            helper.PowerShell.AddCommand("Invoke-MemoryDumpAcquisition")
                .AddParameter("SearchPath", root)
                .AddParameter("CollectMemoryDumpScript", scriptPath);

            Assert.Throws<CmdletInvocationException>(() => helper.PowerShell.Invoke());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "invoke-acq-" + Guid.NewGuid())).FullName;
}
