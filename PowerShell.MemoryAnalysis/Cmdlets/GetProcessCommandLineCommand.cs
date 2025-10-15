using System;
using System.Linq;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Models;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets;

/// <summary>
/// <para type="synopsis">Extracts command line arguments for processes.</para>
/// <para type="description">
/// The Get-ProcessCommandLine cmdlet extracts command line arguments for all
/// processes in a memory dump using Volatility's CmdLine plugin.
/// </para>
/// </summary>
[Cmdlet(VerbsCommon.Get, "ProcessCommandLine")]
[OutputType(typeof(CommandLineInfo))]
public class GetProcessCommandLineCommand : PSCmdlet
{
    private ILogger<GetProcessCommandLineCommand>? _logger;
    private RustInteropService? _rustInterop;

    /// <summary>
    /// Memory dump to analyze.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNull]
    public MemoryDump? MemoryDump { get; set; }

    /// <summary>
    /// Filter by specific Process ID.
    /// </summary>
    [Parameter]
    public uint? Pid { get; set; }

    /// <summary>
    /// Filter by process name (supports wildcards).
    /// </summary>
    [Parameter]
    [SupportsWildcards]
    public string? ProcessName { get; set; }

    protected override void BeginProcessing()
    {
        _logger = LoggingService.GetLogger<GetProcessCommandLineCommand>();
        _rustInterop = new RustInteropService();
    }

    protected override void ProcessRecord()
    {
        if (MemoryDump == null) return;

        try
        {
            // Show progress
            var progress = new ProgressRecord(1, "Extracting Command Lines", 
                $"Analyzing {MemoryDump.FileName}...") { PercentComplete = -1 };
            WriteProgress(progress);
            
            _logger?.LogInformation("Extracting command lines from: {Path}", MemoryDump.Path);

            var commandLines = _rustInterop!.GetCommandLines(MemoryDump.Path);
            
            progress.StatusDescription = $"Processing {commandLines.Length} command lines...";
            progress.PercentComplete = 100;
            WriteProgress(progress);

            // Apply filters
            var filtered = commandLines.AsEnumerable();

            if (Pid.HasValue)
            {
                filtered = filtered.Where(c => c.Pid == Pid.Value);
            }

            if (!string.IsNullOrEmpty(ProcessName))
            {
                var pattern = new WildcardPattern(ProcessName, WildcardOptions.IgnoreCase);
                filtered = filtered.Where(c => pattern.IsMatch(c.ProcessName));
            }

            foreach (var cmdLine in filtered)
            {
                WriteObject(cmdLine);
            }
            
            // Complete progress
            progress.RecordType = ProgressRecordType.Completed;
            WriteProgress(progress);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error extracting command lines");
            WriteError(new ErrorRecord(
                ex,
                "CommandLineExtractionFailed",
                ErrorCategory.InvalidOperation,
                MemoryDump));
        }
    }

    protected override void EndProcessing()
    {
        _rustInterop?.Dispose();
    }
}