using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets;

/// <summary>
/// Runs the Collect-MemoryDump fork (WinPMEM by default) on Windows.
/// </summary>
[Cmdlet(VerbsLifecycle.Invoke, "MemoryDumpAcquisition")]
[OutputType(typeof(string))]
public class InvokeMemoryDumpAcquisitionCommand : PSCmdlet
{
    private ILogger<InvokeMemoryDumpAcquisitionCommand>? _logger;

    [Parameter]
    public string SearchPath { get; set; } = ".";

    [Parameter]
    [ValidateSet("WinPMEM")]
    public string Tool { get; set; } = "WinPMEM";

    [Parameter]
    public string? CollectMemoryDumpScript { get; set; }

    protected override void BeginProcessing()
    {
        _logger = LoggingService.GetLogger<InvokeMemoryDumpAcquisitionCommand>();
    }

    protected override void ProcessRecord()
    {
        if (!CollectMemoryDumpLocator.IsWindows())
        {
            ThrowTerminatingError(new ErrorRecord(
                new PlatformNotSupportedException("Memory acquisition is only supported on Windows."),
                "AcquisitionWindowsOnly",
                ErrorCategory.NotImplemented,
                null));
            return;
        }

        if (!string.Equals(Tool, "WinPMEM", StringComparison.OrdinalIgnoreCase))
        {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException("Only WinPMEM is supported in v1.", nameof(Tool)),
                "UnsupportedAcquisitionTool",
                ErrorCategory.InvalidArgument,
                Tool));
            return;
        }

        try
        {
            var moduleRoot = FindModuleRoot();
            var scriptPath = CollectMemoryDumpScript
                ?? CollectMemoryDumpLocator.FindScriptPath(moduleRoot)
                ?? throw new FileNotFoundException(
                    "Collect-MemoryDump.ps1 not found. Run: git submodule update --init --recursive");

            var winPmem = CollectMemoryDumpLocator.GetWinPmemExecutablePath(scriptPath);
            if (!File.Exists(winPmem))
            {
                throw new FileNotFoundException(
                    $"WinPMEM not found at {winPmem}. See third-party/Collect-MemoryDump README.",
                    winPmem);
            }

            PSHostWriter.WriteHost(this, "Starting WinPMEM acquisition via Collect-MemoryDump...");
            CollectMemoryDumpRunner.Run(scriptPath, "-WinPMEM", WriteError);

            var searchRoot = SessionState.Path.GetUnresolvedProviderPathFromPSPath(SearchPath);
            var discovery = new MemoryDumpDiscoveryService();
            var dumps = discovery.DiscoverAfterAcquisition(searchRoot, scriptPath).ToList();

            if (dumps.Count == 0)
            {
                WriteWarning("Acquisition completed but no .raw dump was discovered. Check Collect-MemoryDump output folders.");
                return;
            }

            WriteObject(dumps[0].Path);
            PSHostWriter.WriteHost(this, $"Discovered dump: {dumps[0].Path}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Invoke-MemoryDumpAcquisition failed");
            ThrowTerminatingError(new ErrorRecord(
                ex,
                "MemoryDumpAcquisitionFailed",
                ErrorCategory.OperationStopped,
                null));
        }
    }

    private string FindModuleRoot()
    {
        var modulePath = MyInvocation.MyCommand.Module?.Path;
        if (!string.IsNullOrEmpty(modulePath))
        {
            return System.IO.Path.GetDirectoryName(modulePath)!;
        }

        return SessionState.Path.CurrentLocation.Path;
    }
}
