using System;
using System.Linq;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Models;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets;

/// <summary>
/// <para type="synopsis">Lists DLLs loaded by processes in a memory dump.</para>
/// <para type="description">
/// The Get-ProcessDll cmdlet uses Volatility 3's DllList plugin to enumerate
/// all DLLs loaded by processes in a memory dump.
/// </para>
/// </summary>
[Cmdlet(VerbsCommon.Get, "ProcessDll")]
[OutputType(typeof(DllInfo))]
public class GetProcessDllCommand : PSCmdlet
{
    private ILogger<GetProcessDllCommand>? _logger;
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

    /// <summary>
    /// Filter by DLL name (supports wildcards).
    /// </summary>
    [Parameter]
    [SupportsWildcards]
    public string? DllName { get; set; }

    protected override void BeginProcessing()
    {
        _logger = LoggingService.GetLogger<GetProcessDllCommand>();
        _rustInterop = new RustInteropService();
    }

    protected override void ProcessRecord()
    {
        if (MemoryDump == null) return;

        try
        {
            var progress = new ProgressRecord(1, "Listing DLLs", 
                $"Scanning {MemoryDump.FileName}...") { PercentComplete = -1 };
            WriteProgress(progress);
            
            _logger?.LogInformation("Listing DLLs from: {Path}", MemoryDump.Path);

            var dlls = _rustInterop!.ListDlls(MemoryDump.Path, Pid);
            
            progress.StatusDescription = $"Processing {dlls.Length} DLLs...";
            progress.PercentComplete = 100;
            WriteProgress(progress);

            // Apply filters
            var filtered = dlls.AsEnumerable();

            if (!string.IsNullOrEmpty(ProcessName))
            {
                var pattern = new WildcardPattern(ProcessName, WildcardOptions.IgnoreCase);
                filtered = filtered.Where(d => pattern.IsMatch(d.ProcessName));
            }

            if (!string.IsNullOrEmpty(DllName))
            {
                var pattern = new WildcardPattern(DllName, WildcardOptions.IgnoreCase);
                filtered = filtered.Where(d => pattern.IsMatch(d.DllName));
            }

            foreach (var dll in filtered)
            {
                WriteObject(dll);
            }
            
            progress.RecordType = ProgressRecordType.Completed;
            WriteProgress(progress);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error listing DLLs");
            WriteError(new ErrorRecord(
                ex,
                "DllListingFailed",
                ErrorCategory.InvalidOperation,
                MemoryDump));
        }
    }

    protected override void EndProcessing()
    {
        _rustInterop?.Dispose();
    }
}