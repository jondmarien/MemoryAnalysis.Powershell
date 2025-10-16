using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Models;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets;

/// <summary>
/// <para type="synopsis">Analyzes process hierarchies in a memory dump.</para>
/// <para type="description">
/// The Analyze-ProcessTree cmdlet analyzes the process tree structure in a memory dump,
/// showing parent-child relationships and identifying suspicious processes.
/// It can filter by PID, process name, or parent process, and supports multiple output formats.
/// </para>
/// <example>
///   <code>Get-MemoryDump -Path memory.vmem | Analyze-ProcessTree</code>
///   <para>Analyzes all processes in the memory dump.</para>
/// </example>
/// <example>
///   <code>Analyze-ProcessTree -MemoryDump $dump -Pid 1234</code>
///   <para>Analyzes process with PID 1234 and its children.</para>
/// </example>
/// <example>
///   <code>Analyze-ProcessTree -MemoryDump $dump -Format Tree</code>
///   <para>Displays processes in tree format.</para>
/// </example>
/// </summary>
[Cmdlet(VerbsDiagnostic.Test, "ProcessTree")]
[Alias("Analyze-ProcessTree")]
[OutputType(typeof(ProcessTreeInfo))]
public class AnalyzeProcessTreeCommand : PSCmdlet
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private ILogger<AnalyzeProcessTreeCommand>? _logger;
    private RustInteropService? _rustInterop;
    private CachingService? _cachingService;

    /// <summary>
    /// <para type="description">Memory dump to analyze.</para>
    /// </summary>
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        HelpMessage = "Memory dump to analyze")]
    [ValidateNotNull]
    public MemoryDump? MemoryDump { get; set; }

    /// <summary>
    /// <para type="description">Filter by specific Process ID.</para>
    /// </summary>
    [Parameter(HelpMessage = "Filter by specific Process ID")]
    [Alias("ProcessId")]
    public uint? Pid { get; set; }

    /// <summary>
    /// <para type="description">Filter by process name (supports wildcards).</para>
    /// </summary>
    [Parameter(HelpMessage = "Filter by process name (supports wildcards)")]
    [SupportsWildcards]
    public string? ProcessName { get; set; }

    /// <summary>
    /// <para type="description">Filter by parent Process ID.</para>
    /// </summary>
    [Parameter(HelpMessage = "Filter by parent Process ID")]
    [Alias("ParentProcessId")]
    public uint? ParentPid { get; set; }

    /// <summary>
    /// <para type="description">Output format: Tree, Flat, or JSON.</para>
    /// </summary>
    [Parameter(HelpMessage = "Output format: Tree, Flat, or JSON")]
    [ValidateSet("Tree", "Flat", "JSON")]
    public string Format { get; set; } = "Flat";

    /// <summary>
    /// <para type="description">Include command line arguments in the output.</para>
    /// </summary>
    [Parameter(HelpMessage = "Include command line arguments")]
    public SwitchParameter IncludeCommandLine { get; set; }

    /// <summary>
    /// <para type="description">Flag suspicious processes based on heuristics.</para>
    /// </summary>
    [Parameter(HelpMessage = "Flag suspicious processes")]
    public SwitchParameter FlagSuspicious { get; set; }

    /// <summary>
    /// <para type="description">Enable detailed debug output.</para>
    /// </summary>
    [Parameter(HelpMessage = "Enable detailed debug output")]
    public SwitchParameter DebugMode { get; set; }

    /// <summary>
    /// Initialize the cmdlet.
    /// </summary>
    protected override void BeginProcessing()
    {
        // Enable Rust bridge debug logging if -DebugMode flag is set
        if (DebugMode.IsPresent)
        {
            Environment.SetEnvironmentVariable("RUST_BRIDGE_DEBUG", "1");
            WriteVerbose("Debug logging enabled for Rust bridge");
        }

        _logger = LoggingService.GetLogger<AnalyzeProcessTreeCommand>();
        _logger.LogInformation("Analyze-ProcessTree cmdlet starting");

        try
        {
            _rustInterop = new RustInteropService();
            _cachingService = new CachingService();
            if (DebugMode.IsPresent)
            {
                WriteVerbose("Rust interop and caching services initialized");
            }
            _logger.LogDebug("Rust interop and caching services initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Rust interop service");
            ThrowTerminatingError(new ErrorRecord(
                ex,
                "RustInteropInitializationFailed",
                ErrorCategory.ResourceUnavailable,
                null));
        }
    }

    /// <summary>
    /// Process each memory dump.
    /// </summary>
    protected override void ProcessRecord()
    {
        if (MemoryDump == null)
        {
            WriteError(new ErrorRecord(
                new ArgumentNullException(nameof(MemoryDump)),
                "MemoryDumpNull",
                ErrorCategory.InvalidArgument,
                null));
            return;
        }

        try
        {
            if (_logger?.IsEnabled(LogLevel.Information) == true)
            {
                _logger.LogInformation("Analyzing process tree for dump: {DumpPath}", MemoryDump.Path);
            }

            // Show progress
            var progressRecord = new ProgressRecord(
                1,
                "Analyzing Process Tree",
                $"Processing {MemoryDump.FileName}")
            {
                PercentComplete = 0
            };
            WriteProgress(progressRecord);

            // Get process list from Rust (with caching)
            progressRecord.PercentComplete = 25;
            progressRecord.StatusDescription = "Retrieving process list...";
            WriteProgress(progressRecord);

            // Get raw process data from cache (which wraps RustInteropService)
            // Cache stores ProcessInfo[] directly from Rust, not ProcessTreeInfo[]
            var rawProcesses = _cachingService?.GetOrCacheProcesses(
                MemoryDump.Path,
                () => _rustInterop!.ListProcesses(MemoryDump.Path).Select(p => new ProcessInfo
                {
                    Pid = p.Pid,
                    Ppid = p.Ppid,
                    Name = p.Name,
                    Offset = p.Offset,
                    Threads = p.Threads,
                    Handles = p.Handles,
                    CreateTime = p.CreateTime
                }).ToArray()
            );
            if (DebugMode.IsPresent)
            {
                var cacheStats = _cachingService?.GetAllCacheStatistics();
                var procStats = cacheStats?["Processes"];
                WriteVerbose($"Retrieved {rawProcesses?.Length ?? 0} processes - Cache hits: {procStats?.CacheHits}, misses: {procStats?.CacheMisses}");
            }
            
            if (rawProcesses == null || rawProcesses.Length == 0)
            {
                WriteWarning($"No processes found in dump: {MemoryDump.FileName}");
                return;
            }

            // Convert to ProcessTreeInfo
            progressRecord.PercentComplete = 50;
            progressRecord.StatusDescription = "Building process tree...";
            WriteProgress(progressRecord);

            var processes = ConvertToProcessTreeInfo(rawProcesses);
            if (DebugMode.IsPresent)
            {
                WriteVerbose($"Converted to {processes.Count} ProcessTreeInfo objects");
            }

            // Apply filters
            if (Pid.HasValue || !string.IsNullOrEmpty(ProcessName) || ParentPid.HasValue)
            {
                processes = ApplyFilters(processes);
            }

            // Flag suspicious processes if requested
            if (FlagSuspicious.IsPresent)
            {
                progressRecord.PercentComplete = 75;
                progressRecord.StatusDescription = "Analyzing for suspicious activity...";
                WriteProgress(progressRecord);

                FlagSuspiciousProcesses(processes);
            }

            // Output based on format
            progressRecord.PercentComplete = 90;
            progressRecord.StatusDescription = "Formatting output...";
            WriteProgress(progressRecord);

            OutputResults(processes);

            progressRecord.PercentComplete = 100;
            progressRecord.StatusDescription = "Complete";
            WriteProgress(progressRecord);

            if (_logger?.IsEnabled(LogLevel.Information) == true)
            {
                _logger.LogInformation("Process tree analysis completed. Found {ProcessCount} processes", processes.Count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error analyzing process tree");
            WriteError(new ErrorRecord(
                ex,
                "ProcessTreeAnalysisFailed",
                ErrorCategory.InvalidOperation,
                MemoryDump));
        }
    }

    /// <summary>
    /// Convert raw process info to ProcessTreeInfo objects.
    /// </summary>
    private static List<ProcessTreeInfo> ConvertToProcessTreeInfo(ProcessInfo[] rawProcesses)
    {
        var processes = rawProcesses.Select(p => new ProcessTreeInfo
        {
            Pid = p.Pid,
            Ppid = p.Ppid,
            Name = p.Name ?? "Unknown",
            Offset = p.Offset ?? "0x0",
            Threads = p.Threads,
            Handles = p.Handles,
            CreateTime = p.CreateTime ?? "Unknown"
        }).ToList();

        // Build tree structure - handle duplicate PIDs gracefully
        var processDict = new Dictionary<uint, ProcessTreeInfo>();
        foreach (var proc in processes)
        {
            if (!processDict.ContainsKey(proc.Pid))
            {
                processDict[proc.Pid] = proc;
            }
            // Skip duplicates - might be parsing artifacts
        }
        
        foreach (var process in processes)
        {
            if (process.Ppid != 0 && processDict.TryGetValue(process.Ppid, out ProcessTreeInfo? parent))
            {
                parent.Children.Add(process);
                process.Depth = parent.Depth + 1;
            }
        }

        return processes;
    }

    /// <summary>
    /// Apply filters to the process list.
    /// </summary>
    private List<ProcessTreeInfo> ApplyFilters(List<ProcessTreeInfo> processes)
    {
        var filtered = processes.AsEnumerable();

        if (Pid.HasValue)
        {
            filtered = filtered.Where(p => p.Pid == Pid.Value || IsDescendantOf(p, Pid.Value, processes));
        }

        if (!string.IsNullOrEmpty(ProcessName))
        {
            var pattern = new WildcardPattern(ProcessName, WildcardOptions.IgnoreCase);
            filtered = filtered.Where(p => pattern.IsMatch(p.Name));
        }

        if (ParentPid.HasValue)
        {
            filtered = filtered.Where(p => p.Ppid == ParentPid.Value);
        }

        return [.. filtered];
    }

    /// <summary>
    /// Check if a process is a descendant of another process.
    /// </summary>
    private static bool IsDescendantOf(ProcessTreeInfo process, uint ancestorPid, List<ProcessTreeInfo> allProcesses)
    {
        var current = process;
        while (current.Ppid != 0)
        {
            if (current.Ppid == ancestorPid)
                return true;

            current = allProcesses.FirstOrDefault(p => p.Pid == current.Ppid);
            if (current == null)
                break;
        }
        return false;
    }

    /// <summary>
    /// Flag suspicious processes based on heuristics.
    /// </summary>
    private static void FlagSuspiciousProcesses(List<ProcessTreeInfo> processes)
    {
        foreach (var process in processes)
        {
            var reasons = new List<string>();

            // Suspicious parent-child relationships
            if (process.Name.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase) ||
                process.Name.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
            {
                var parent = processes.FirstOrDefault(p => p.Pid == process.Ppid);
                if (parent != null && (parent.Name.Equals("winword.exe", StringComparison.OrdinalIgnoreCase) ||
                                      parent.Name.Equals("excel.exe", StringComparison.OrdinalIgnoreCase)))
                {
                    reasons.Add("Shell spawned by Office application");
                }
            }

            // Unusual parent processes
            if (process.Ppid == 0 && !process.Name.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add("Process with no parent (orphaned)");
            }

            // Processes with suspicious names
            if (process.Name.Contains("svchost", StringComparison.OrdinalIgnoreCase) && process.Ppid != 0)
            {
                var parent = processes.FirstOrDefault(p => p.Pid == process.Ppid);
                if (parent != null && !parent.Name.Equals("services.exe", StringComparison.OrdinalIgnoreCase))
                {
                    reasons.Add("svchost.exe not spawned by services.exe");
                }
            }

            if (reasons.Count > 0)
            {
                process.IsSuspicious = true;
                process.SuspiciousReasons = reasons;
            }
        }
    }

    /// <summary>
    /// Output results based on the selected format.
    /// </summary>
    private void OutputResults(List<ProcessTreeInfo> processes)
    {
        switch (Format.ToUpperInvariant())
        {
            case "TREE":
                OutputTreeFormat(processes);
                break;

            case "JSON":
                OutputJsonFormat(processes);
                break;

            case "FLAT":
            default:
                OutputFlatFormat(processes);
                break;
        }
    }

    /// <summary>
    /// Output in tree format.
    /// </summary>
    private void OutputTreeFormat(List<ProcessTreeInfo> processes)
    {
        // Find root processes (no parent or parent doesn't exist)
        var roots = processes.Where(p => p.Ppid == 0 || !processes.Any(x => x.Pid == p.Ppid)).ToList();

        foreach (var root in roots)
        {
            WriteObject(root.ToTreeString());
        }
    }

    /// <summary>
    /// Output in flat format.
    /// </summary>
    private void OutputFlatFormat(List<ProcessTreeInfo> processes)
    {
        foreach (var process in processes.OrderBy(p => p.Pid))
        {
            WriteObject(process);
        }
    }

    /// <summary>
    /// Output in JSON format.
    /// </summary>
    private void OutputJsonFormat(List<ProcessTreeInfo> processes)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(processes, JsonOptions);
        WriteObject(json);
    }

    /// <summary>
    /// Clean up resources.
    /// </summary>
    protected override void EndProcessing()
    {
        _rustInterop?.Dispose();
        _logger?.LogInformation("Analyze-ProcessTree cmdlet completed");
    }
}
