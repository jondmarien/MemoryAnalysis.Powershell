# Phase 2: Command Line Extraction Integration

**Feature:** Command Line Extraction  
**Status:** Rust Layer Complete ✅ | C# Layer TODO ⏳  
**Rust FFI Function:** `rust_bridge_get_command_lines`

---

## Completed (Phase 1)

- ✅ Rust implementation in `rust-bridge/src/process_analysis.rs`
- ✅ `CommandLineInfo` struct in `rust-bridge/src/types.rs`
- ✅ FFI export `rust_bridge_get_command_lines()` in `rust-bridge/src/lib.rs`
- ✅ Builds successfully with `cargo build --release`

---

## TODO: Phase 2 C# Integration

### Step 1: Add C# Data Model

**File:** `PowerShell.MemoryAnalysis/Models/CommandLineInfo.cs`

```csharp
using System.Text.Json.Serialization;

namespace PowerShell.MemoryAnalysis.Models;

/// <summary>
/// Represents command line information for a process.
/// </summary>
public class CommandLineInfo
{
    [JsonPropertyName("pid")]
    public uint Pid { get; set; }
    
    [JsonPropertyName("process_name")]
    public string ProcessName { get; set; } = string.Empty;
    
    [JsonPropertyName("command_line")]
    public string CommandLine { get; set; } = string.Empty;
}
```

---

### Step 2: Add C# P/Invoke Wrapper

**File:** `PowerShell.MemoryAnalysis/Services/RustInterop.cs`

Add to the RustInteropService class:

```csharp
// Add to P/Invoke declarations section
[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
private static extern IntPtr rust_bridge_get_command_lines(
    [MarshalAs(UnmanagedType.LPStr)] string dumpPath
);

// Add public method
/// <summary>
/// Get command line arguments for processes in the memory dump.
/// </summary>
/// <param name="dumpPath">Path to the memory dump file</param>
/// <returns>Array of CommandLineInfo objects</returns>
public CommandLineInfo[] GetCommandLines(string dumpPath)
{
    if (string.IsNullOrWhiteSpace(dumpPath))
    {
        throw new ArgumentException("Dump path cannot be null or empty", nameof(dumpPath));
    }

    if (!File.Exists(dumpPath))
    {
        throw new FileNotFoundException($"Memory dump file not found: {dumpPath}", dumpPath);
    }

    IntPtr ptr = IntPtr.Zero;
    try
    {
        _logger.LogInformation("Getting command lines from dump: {DumpPath}", dumpPath);
        
        ptr = rust_bridge_get_command_lines(dumpPath);
        if (ptr == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to get command lines from dump: {dumpPath}");
        }

        string json = MarshalStringFromRust(ptr);
        var commandLines = JsonSerializer.Deserialize<CommandLineInfo[]>(json) 
            ?? throw new InvalidOperationException("Failed to deserialize command line information");
        
        _logger.LogInformation("Retrieved {Count} command lines from dump", commandLines.Length);
        return commandLines;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting command lines from dump: {DumpPath}", dumpPath);
        throw;
    }
    finally
    {
        if (ptr != IntPtr.Zero)
        {
            rust_bridge_free_string(ptr);
        }
    }
}
```

---

### Step 3: Option A - Extend Test-ProcessTree Cmdlet

**File:** `PowerShell.MemoryAnalysis/Cmdlets/AnalyzeProcessTreeCommand.cs`

Add parameter:

```csharp
/// <summary>
/// <para type="description">Include command line arguments in the output.</para>
/// </summary>
[Parameter(HelpMessage = "Include command line arguments")]
public SwitchParameter IncludeCommandLine { get; set; }
```

In `ProcessRecord()`, after getting processes:

```csharp
// If command lines requested, fetch and merge
if (IncludeCommandLine.IsPresent)
{
    progressRecord.StatusDescription = "Retrieving command lines...";
    WriteProgress(progressRecord);
    
    try
    {
        var commandLines = _rustInterop.GetCommandLines(MemoryDump.Path);
        var cmdDict = commandLines.ToDictionary(c => c.Pid);
        
        foreach (var process in processes)
        {
            if (cmdDict.TryGetValue(process.Pid, out var cmdInfo))
            {
                process.CommandLine = cmdInfo.CommandLine;
            }
        }
        
        if (DebugMode.IsPresent)
        {
            WriteVerbose($"Merged {commandLines.Length} command lines with process tree");
        }
    }
    catch (Exception ex)
    {
        WriteWarning($"Failed to retrieve command lines: {ex.Message}");
    }
}
```

Update `ProcessTreeInfo` model:

```csharp
public string? CommandLine { get; set; }
```

---

### Step 3: Option B - Create New Get-ProcessCommandLine Cmdlet

**File:** `PowerShell.MemoryAnalysis/Cmdlets/GetProcessCommandLineCommand.cs`

```csharp
using System;
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
            _logger.LogInformation("Extracting command lines from: {Path}", MemoryDump.Path);

            var commandLines = _rustInterop.GetCommandLines(MemoryDump.Path);

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting command lines");
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
```

---

### Step 4: Update Module Manifest

**File:** `PowerShell.MemoryAnalysis/MemoryAnalysis.psd1`

If creating new cmdlet, add to `CmdletsToExport`:

```powershell
CmdletsToExport = @(
    'Get-MemoryDump',
    'Test-ProcessTree',
    'Find-Malware',
    'Get-ProcessCommandLine'  # Add this
)
```

---

### Step 5: Add Help Documentation

Add comment-based help to the cmdlet:

```csharp
/*
.SYNOPSIS
Extracts command line arguments for processes in a memory dump.

.DESCRIPTION
The Get-ProcessCommandLine cmdlet uses Volatility 3's CmdLine plugin to extract
the full command line arguments for processes in a memory dump.

.PARAMETER MemoryDump
Specifies the memory dump to analyze. Accepts pipeline input.

.PARAMETER Pid
Filters results to a specific process ID.

.PARAMETER ProcessName
Filters results by process name. Supports wildcards.

.EXAMPLE
PS> Get-MemoryDump -Path memory.vmem | Get-ProcessCommandLine

Extracts all command lines from the memory dump.

.EXAMPLE
PS> Get-MemoryDump -Path memory.vmem | Get-ProcessCommandLine -ProcessName "powershell*"

Extracts command lines for PowerShell processes only.

.EXAMPLE
PS> Get-ProcessCommandLine -MemoryDump $dump -Pid 1234

Gets the command line for process 1234.

.OUTPUTS
PowerShell.MemoryAnalysis.Models.CommandLineInfo

.LINK
Test-ProcessTree
.LINK
Get-MemoryDump
*/
```

---

### Step 6: Testing

**Test Script:** `scripts/Test-CommandLineFeature.ps1`

```powershell
#!/usr/bin/env pwsh

Write-Host "Testing Command Line Extraction Feature" -ForegroundColor Cyan

Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1 -Force

$dump = Get-MemoryDump -Path F:\physmem.raw

Write-Host "`nTest 1: Get all command lines" -ForegroundColor Yellow
$cmdlines = Get-ProcessCommandLine -MemoryDump $dump
Write-Host "✓ Found $($cmdlines.Count) command lines" -ForegroundColor Green
$cmdlines | Select-Object -First 5 | Format-Table Pid, ProcessName, CommandLine -AutoSize

Write-Host "`nTest 2: Filter by process name" -ForegroundColor Yellow
$psCmdlines = Get-ProcessCommandLine -MemoryDump $dump -ProcessName "pwsh*"
Write-Host "✓ Found $($psCmdlines.Count) PowerShell command lines" -ForegroundColor Green
$psCmdlines | Format-List

Write-Host "`nTest 3: Include in process tree" -ForegroundColor Yellow
$procs = Test-ProcessTree -MemoryDump $dump -IncludeCommandLine -ProcessName "explorer*"
$procs | Format-List Name, Pid, CommandLine

Write-Host "`n✅ All tests passed!" -ForegroundColor Green
```

---

## Timeline

- **Estimated Time:** 1-2 hours
- **Priority:** Medium (optional enhancement)
- **Dependencies:** None (Rust layer complete)

---

## Notes

- Command line extraction can be slow on large dumps (30-60 seconds)
- Some command lines may be `<unreadable>` if paged out
- Consider adding `-QuickScan` parameter to limit results
- Add progress reporting for long operations
- Cache results if used multiple times on same dump
