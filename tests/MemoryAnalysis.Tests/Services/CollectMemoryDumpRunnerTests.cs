using System.Management.Automation;
using PowerShell.MemoryAnalysis.Services;
using Xunit;

namespace MemoryAnalysis.Tests.Services;

public class CollectMemoryDumpRunnerTests
{
    [Fact]
    public void Run_ExecutesStubScriptSuccessfully()
    {
        var root = CreateTempDirectory();
        try
        {
            var scriptPath = Path.Combine(root, "Collect-MemoryDump.ps1");
            File.WriteAllText(scriptPath, "param([switch]$WinPMEM)\nWrite-Output 'ok'");

            var exception = Record.Exception(() => CollectMemoryDumpRunner.Run(scriptPath, "-WinPMEM"));
            Assert.Null(exception);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Run_InvokesOnErrorWhenScriptWritesError()
    {
        var root = CreateTempDirectory();
        try
        {
            var scriptPath = Path.Combine(root, "Collect-MemoryDump.ps1");
            File.WriteAllText(scriptPath, "Write-Error 'simulated failure'");

            ErrorRecord? captured = null;
            var ex = Assert.Throws<InvalidOperationException>(() =>
                CollectMemoryDumpRunner.Run(scriptPath, "-WinPMEM", e => captured = e));

            Assert.Contains("errors", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(captured);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Run_ThrowsForInvalidScriptPath()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CollectMemoryDumpRunner.Run(string.Empty, "-WinPMEM"));
    }

    private static string CreateTempDirectory() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cmd-runner-" + Guid.NewGuid())).FullName;
}
