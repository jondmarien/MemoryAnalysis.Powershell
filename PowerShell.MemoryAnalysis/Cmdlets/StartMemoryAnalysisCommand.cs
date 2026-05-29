using System;
using System.Collections;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Models;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets;

/// <summary>
/// Resolves a memory dump path (with optional acquisition) and loads it via Get-MemoryDump.
/// </summary>
[Cmdlet(VerbsLifecycle.Start, "MemoryAnalysis")]
[OutputType(typeof(MemoryDump))]
public class StartMemoryAnalysisCommand : PSCmdlet
{
    private ILogger<StartMemoryAnalysisCommand>? _logger;

    [Parameter]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    [Parameter]
    public string SearchPath { get; set; } = ".";

    [Parameter]
    [ValidateRange(0, 32)]
    public int MaxSearchDepth { get; set; } = MemoryDumpDiscoveryService.DefaultMaxSearchDepth;

    [Parameter]
    public SwitchParameter NoAcquire { get; set; }

    [Parameter]
    public SwitchParameter Force { get; set; }

    [Parameter]
    public SwitchParameter Validate { get; set; }

    protected override void BeginProcessing()
    {
        _logger = LoggingService.GetLogger<StartMemoryAnalysisCommand>();
    }

    protected override void ProcessRecord()
    {
        try
        {
            var dumpPath = ResolveDumpPath();
            _logger?.LogInformation("Loading memory dump from {Path}", dumpPath);

            var parameters = new Hashtable { ["Path"] = dumpPath };
            if (Validate.IsPresent)
            {
                parameters["Validate"] = true;
            }

            var results = InvokeCommand.InvokeScript("Get-MemoryDump", parameters);
            foreach (var result in results)
            {
                WriteObject(result);
            }
        }
        catch (Exception ex) when (ex is not PipelineStoppedException)
        {
            _logger?.LogError(ex, "Start-MemoryAnalysis failed");
            ThrowTerminatingError(new ErrorRecord(
                ex,
                "StartMemoryAnalysisFailed",
                ErrorCategory.OperationStopped,
                null));
        }
    }

    private string ResolveDumpPath()
    {
        if (!string.IsNullOrWhiteSpace(Path))
        {
            var parameters = new Hashtable { ["Path"] = Path };
            var explicitResult = InvokeCommand.InvokeScript("Resolve-MemoryDumpPath", parameters);
            if (explicitResult.Count == 0 || explicitResult[0]?.BaseObject is not DiscoveredMemoryDump explicitDump)
            {
                throw new InvalidOperationException("Resolve-MemoryDumpPath did not return a dump.");
            }

            return explicitDump.Path;
        }

        var searchParameters = new Hashtable
        {
            ["SearchPath"] = SearchPath,
            ["MaxSearchDepth"] = MaxSearchDepth
        };
        if (NoAcquire.IsPresent)
        {
            searchParameters["NoAcquire"] = true;
        }

        if (Force.IsPresent)
        {
            searchParameters["Force"] = true;
        }

        var searchResult = InvokeCommand.InvokeScript("Resolve-MemoryDumpPath", searchParameters);
        if (searchResult.Count == 0 || searchResult[0]?.BaseObject is not DiscoveredMemoryDump discovered)
        {
            throw new InvalidOperationException(
                "No memory dump was resolved. Use -Path, place a dump in the search directory, or approve acquisition.");
        }

        return discovered.Path;
    }
}
