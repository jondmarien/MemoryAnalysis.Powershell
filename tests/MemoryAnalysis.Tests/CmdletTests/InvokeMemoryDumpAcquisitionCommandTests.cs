using System.Management.Automation;
using System.Runtime.InteropServices;
using PowerShell.MemoryAnalysis.Cmdlets;
using MemoryAnalysis.Tests.Helpers;
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
    public void Invoke_OnWindows_WithoutWinPmem_Throws()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var repoRoot = FindRepoRoot();
        if (repoRoot == null)
        {
            return;
        }

        using var helper = ModuleCommandHelper.Create();
        helper.PowerShell.AddCommand("Invoke-MemoryDumpAcquisition")
            .AddParameter("SearchPath", repoRoot);
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
            var scriptDir = Path.Combine(root, "third-party", "Collect-MemoryDump");
            Directory.CreateDirectory(Path.Combine(scriptDir, "Tools", "WinPMEM"));
            var scriptPath = Path.Combine(scriptDir, "Collect-MemoryDump.ps1");
            var dumpPath = Path.Combine(root, "captured.raw");
            File.WriteAllText(
                scriptPath,
                $"Set-Content -LiteralPath '{dumpPath.Replace("'", "''")}' -Value 'acquired'");
            File.WriteAllText(Path.Combine(scriptDir, "Tools", "WinPMEM", "winpmem_mini_x64_rc2.exe"), string.Empty);

            using var helper = ModuleCommandHelper.Create();
            helper.PowerShell.AddCommand("Invoke-MemoryDumpAcquisition")
                .AddParameter("SearchPath", root)
                .AddParameter("CollectMemoryDumpScript", scriptPath);

            var results = helper.PowerShell.Invoke();
            Assert.NotEmpty(results);
            Assert.Equal(Path.GetFullPath(dumpPath), results[0].BaseObject);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MemoryAnalysis.Powershell.sln"))
                || File.Exists(Path.Combine(dir.FullName, "PowerShell.MemoryAnalysis", "PowerShell.MemoryAnalysis.csproj")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static string CreateTempDirectory() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "invoke-acq-" + Guid.NewGuid())).FullName;
}
