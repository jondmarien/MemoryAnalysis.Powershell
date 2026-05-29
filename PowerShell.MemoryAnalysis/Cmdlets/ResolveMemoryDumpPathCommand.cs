using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Models;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets;

/// <summary>
/// Discovers an existing memory dump or optionally offers live acquisition on Windows.
/// </summary>
[Cmdlet("Resolve", "MemoryDumpPath", DefaultParameterSetName = "Search")]
[OutputType(typeof(DiscoveredMemoryDump))]
public class ResolveMemoryDumpPathCommand : PSCmdlet
{
    private ILogger<ResolveMemoryDumpPathCommand>? _logger;
    private MemoryDumpDiscoveryService _discovery = new();

    [Parameter(ParameterSetName = "Explicit", Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    [Parameter(ParameterSetName = "Search")]
    public string SearchPath { get; set; } = ".";

    [Parameter(ParameterSetName = "Search")]
    [ValidateRange(0, 32)]
    public int MaxSearchDepth { get; set; } = MemoryDumpDiscoveryService.DefaultMaxSearchDepth;

    [Parameter(ParameterSetName = "Search")]
    public SwitchParameter NoAcquire { get; set; }

    [Parameter(ParameterSetName = "Search")]
    public SwitchParameter Force { get; set; }

    [Parameter(ParameterSetName = "Search")]
    public SwitchParameter PassThru { get; set; }

    protected override void BeginProcessing()
    {
        _logger = LoggingService.GetLogger<ResolveMemoryDumpPathCommand>();
    }

    protected override void ProcessRecord()
    {
        try
        {
            if (ParameterSetName == "Explicit")
            {
                EmitSingle(ResolveExplicitPath(Path!));
                return;
            }

            var searchRoot = ResolveSearchRoot(SearchPath);
            var candidates = DiscoverAll(searchRoot, MaxSearchDepth);

            if (candidates.Count == 0)
            {
                if (NoAcquire || IsContinuousIntegration())
                {
                    var message = IsContinuousIntegration()
                        ? $"No memory dump found under {searchRoot}. Live acquisition is disabled in CI; use -NoAcquire explicitly, provide -Path, or place a dump in the search directory."
                        : $"No memory dump found under {searchRoot}.";
                    ThrowTerminatingError(new ErrorRecord(
                        new FileNotFoundException(message),
                        "MemoryDumpNotFound",
                        ErrorCategory.ObjectNotFound,
                        searchRoot));
                    return;
                }

                if (!CollectMemoryDumpLocator.IsWindows())
                {
                    WriteError(new ErrorRecord(
                        new PlatformNotSupportedException(
                            "Live memory acquisition is only supported on Windows. Provide -Path or place a dump in the search directory."),
                        "AcquisitionWindowsOnly",
                        ErrorCategory.NotImplemented,
                        null));
                    return;
                }

                if (!PromptForAcquisition(searchRoot))
                {
                    WriteVerbose("Memory acquisition declined by user.");
                    return;
                }

                InvokeAcquisitionScript(searchRoot);
                candidates = DiscoverAfterAcquisition(searchRoot);
            }

            if (candidates.Count == 0)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new FileNotFoundException("No memory dump found after search and optional acquisition."),
                    "MemoryDumpNotFound",
                    ErrorCategory.ObjectNotFound,
                    searchRoot));
                return;
            }

            var selected = SelectCandidate(candidates, searchRoot);
            EmitSingle(selected);
        }
        catch (Exception ex) when (ex is not PipelineStoppedException)
        {
            _logger?.LogError(ex, "Resolve-MemoryDumpPath failed");
            ThrowTerminatingError(new ErrorRecord(ex, "ResolveMemoryDumpPathFailed", ErrorCategory.InvalidOperation, null));
        }
    }

    private string ResolveSearchRoot(string searchPath)
    {
        var resolved = SessionState.Path.GetUnresolvedProviderPathFromPSPath(searchPath);
        if (!Directory.Exists(resolved))
        {
            throw new DirectoryNotFoundException($"Search path not found: {resolved}");
        }

        return resolved;
    }

    private DiscoveredMemoryDump ResolveExplicitPath(string path)
    {
        var resolved = SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException($"Memory dump file not found: {resolved}", resolved);
        }

        if (!_discovery.IsCandidateDumpFile(resolved))
        {
            WriteWarning(
                $"File exists but may not be a full memory image (check extension/size): {resolved}");
        }

        var info = new FileInfo(resolved);
        return new DiscoveredMemoryDump
        {
            Path = info.FullName,
            SizeBytes = info.Length,
            LastWriteTimeUtc = info.LastWriteTimeUtc
        };
    }

    private List<DiscoveredMemoryDump> DiscoverAll(string searchRoot, int maxDepth)
    {
        var merged = new Dictionary<string, DiscoveredMemoryDump>(StringComparer.OrdinalIgnoreCase);

        foreach (var dump in _discovery.Discover(searchRoot, maxDepth))
        {
            merged[dump.Path] = dump;
        }

        var moduleRoot = FindModuleRoot();
        var scriptPath = CollectMemoryDumpLocator.FindScriptPath(moduleRoot);
        if (scriptPath != null)
        {
            var outputRoot = CollectMemoryDumpLocator.GetCollectMemoryDumpOutputRoot(scriptPath);
            var extraDepth = Math.Max(maxDepth, 3);
            foreach (var dump in _discovery.Discover(outputRoot, extraDepth))
            {
                merged[dump.Path] = dump;
            }
        }

        return merged.Values.ToList();
    }

    private List<DiscoveredMemoryDump> DiscoverAfterAcquisition(string searchRoot)
    {
        var depth = Math.Max(MaxSearchDepth, 3);
        return DiscoverAll(searchRoot, depth);
    }

    private DiscoveredMemoryDump SelectCandidate(IReadOnlyList<DiscoveredMemoryDump> candidates, string searchRoot)
    {
        if (candidates.Count == 1)
        {
            var only = candidates[0];
            if (!Force && only.SizeBytes > 10L * 1024 * 1024 * 1024)
            {
                var confirmLarge = Host.UI.PromptForChoice(
                    "Large memory dump",
                    $"Use {only} ({only.SizeBytes:N0} bytes)?",
                    YesNoChoices(),
                    0);
                if (confirmLarge != 0)
                {
                    throw new PipelineStoppedException();
                }
            }

            return only;
        }

        PSHostWriter.WriteHost(this, $"Multiple memory dumps found under {searchRoot}:");
        for (var i = 0; i < candidates.Count; i++)
        {
            PSHostWriter.WriteHost(this, $"  [{i + 1}] {candidates[i].Path} ({candidates[i].Size})");
        }

        while (true)
        {
            PSHostWriter.WriteHost(this, $"Select dump [1-{candidates.Count}]:");
            var input = Host.UI.ReadLine();
            if (int.TryParse(input, out var index) && index >= 1 && index <= candidates.Count)
            {
                return candidates[index - 1];
            }

            WriteWarning("Invalid selection. Enter a number from the list.");
        }
    }

    private bool PromptForAcquisition(string searchRoot)
    {
        PSHostWriter.WriteHost(this, $"No memory dump found in {searchRoot}.");
        var choice = Host.UI.PromptForChoice(
            "Memory acquisition",
            "Collect a forensically sound memory image with WinPMEM? Requires Administrator privileges and WinPMEM under third-party/Collect-MemoryDump/Tools/.",
            YesNoChoices(),
            1);
        return choice == 0;
    }

    private void InvokeAcquisitionScript(string searchRoot)
    {
        var moduleRoot = FindModuleRoot();
        var scriptPath = CollectMemoryDumpLocator.FindScriptPath(moduleRoot);
        if (scriptPath == null)
        {
            throw new FileNotFoundException(
                "Collect-MemoryDump.ps1 not found. Run: git submodule update --init --recursive");
        }

        var winPmem = CollectMemoryDumpLocator.GetWinPmemExecutablePath(scriptPath);
        if (!File.Exists(winPmem))
        {
            throw new FileNotFoundException(
                $"WinPMEM not found at {winPmem}. Download WinPMEM and place it under Collect-MemoryDump/Tools/WinPMEM/ per the fork README.",
                winPmem);
        }

        if (CollectMemoryDumpLocator.IsWindows() && !IsElevated())
        {
            WriteWarning("Process is not running elevated. WinPMEM acquisition may fail without Administrator rights.");
        }

        WriteVerbose($"Running Collect-MemoryDump: {scriptPath} -WinPMEM");
        PSHostWriter.WriteHost(this, "Starting memory acquisition (WinPMEM). This may take several minutes...");

        CollectMemoryDumpRunner.Run(scriptPath, "-WinPMEM", WriteError);
        PSHostWriter.WriteHost(this, "Memory acquisition finished. Searching for dump files...");
    }

    private static bool IsElevated()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
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

    private void EmitSingle(DiscoveredMemoryDump dump)
    {
        WriteObject(dump);
        if (PassThru)
        {
            WriteObject(dump.Path);
        }
    }

    private static Collection<ChoiceDescription> YesNoChoices() =>
        new(new[] { new ChoiceDescription("&Yes"), new ChoiceDescription("&No") });

    private static bool IsContinuousIntegration() =>
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("MEMORYANALYSIS_CI"), "1", StringComparison.OrdinalIgnoreCase);
}
