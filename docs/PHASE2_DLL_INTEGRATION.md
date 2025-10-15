# Phase 2: DLL Listing Integration

**Feature:** DLL Listing  
**Status:** Rust Layer Complete ✅ | C# Layer TODO ⏳  
**Rust FFI Function:** `rust_bridge_list_dlls`

---

## Completed (Phase 1)

- ✅ Rust implementation in `rust-bridge/src/process_analysis.rs`
- ✅ `DllInfo` struct in `rust-bridge/src/types.rs`
- ✅ FFI export `rust_bridge_list_dlls()` in `rust-bridge/src/lib.rs`
- ✅ Builds successfully with `cargo build --release`
- ✅ Supports optional PID filtering (pass 0 for all, or specific PID)

---

## TODO: Phase 2 C# Integration

### Step 1: Add C# Data Model

**File:** `PowerShell.MemoryAnalysis/Models/DllInfo.cs`

```csharp
using System.Text.Json.Serialization;

namespace PowerShell.MemoryAnalysis.Models;

/// <summary>
/// Represents a DLL loaded by a process in memory.
/// </summary>
public class DllInfo
{
    [JsonPropertyName("pid")]
    public uint Pid { get; set; }
    
    [JsonPropertyName("process_name")]
    public string ProcessName { get; set; } = string.Empty;
    
    [JsonPropertyName("base_address")]
    public string BaseAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("size")]
    public ulong Size { get; set; }
    
    [JsonPropertyName("dll_name")]
    public string DllName { get; set; } = string.Empty;
    
    [JsonPropertyName("dll_path")]
    public string DllPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets formatted size in human-readable format (KB/MB).
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (Size < 1024)
                return $"{Size} B";
            else if (Size < 1024 * 1024)
                return $"{Size / 1024.0:F2} KB";
            else
                return $"{Size / (1024.0 * 1024.0):F2} MB";
        }
    }
}
```

---

### Step 2: Add C# P/Invoke Wrapper

**File:** `PowerShell.MemoryAnalysis/Services/RustInterop.cs`

Add to the RustInteropService class:

```csharp
// Add to P/Invoke declarations section
[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
private static extern IntPtr rust_bridge_list_dlls(
    [MarshalAs(UnmanagedType.LPStr)] string dumpPath,
    uint pid
);

// Add public method
/// <summary>
/// List DLLs loaded by processes in the memory dump.
/// </summary>
/// <param name="dumpPath">Path to the memory dump file</param>
/// <param name="pid">Optional process ID to filter by (null = all processes)</param>
/// <returns>Array of DllInfo objects</returns>
public DllInfo[] ListDlls(string dumpPath, uint? pid = null)
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
        var pidFilter = pid ?? 0; // 0 = no filter
        var filterMsg = pid.HasValue ? $" (filtered to PID {pid.Value})" : "";
        
        _logger.LogInformation("Listing DLLs from dump: {DumpPath}{Filter}", dumpPath, filterMsg);
        
        ptr = rust_bridge_list_dlls(dumpPath, pidFilter);
        if (ptr == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to list DLLs from dump: {dumpPath}");
        }

        string json = MarshalStringFromRust(ptr);
        var dlls = JsonSerializer.Deserialize<DllInfo[]>(json) 
            ?? throw new InvalidOperationException("Failed to deserialize DLL information");
        
        _logger.LogInformation("Retrieved {Count} DLLs from dump", dlls.Length);
        return dlls;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error listing DLLs from dump: {DumpPath}", dumpPath);
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

### Step 3: Create Get-ProcessDll Cmdlet

**File:** `PowerShell.MemoryAnalysis/Cmdlets/GetProcessDllCommand.cs`

```csharp
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
            _logger.LogInformation("Listing DLLs from: {Path}", MemoryDump.Path);

            var dlls = _rustInterop.ListDlls(MemoryDump.Path, Pid);

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing DLLs");
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
```

---

### Step 4: Update Module Manifest

**File:** `PowerShell.MemoryAnalysis/MemoryAnalysis.psd1`

Add to `CmdletsToExport`:

```powershell
CmdletsToExport = @(
    'Get-MemoryDump',
    'Test-ProcessTree',
    'Find-Malware',
    'Get-ProcessDll'  # Add this
)
```

---

### Step 5: Add Help Documentation

Add comment-based help to the cmdlet:

```csharp
/*
.SYNOPSIS
Lists DLLs loaded by processes in a memory dump.

.DESCRIPTION
The Get-ProcessDll cmdlet uses Volatility 3's DllList plugin to enumerate
all DLLs (Dynamic Link Libraries) loaded by processes in a memory dump.
This is useful for identifying loaded modules, detecting DLL injection,
and analyzing process dependencies.

.PARAMETER MemoryDump
Specifies the memory dump to analyze. Accepts pipeline input.

.PARAMETER Pid
Filters results to a specific process ID. This significantly speeds up
the operation when you only need DLLs for one process.

.PARAMETER ProcessName
Filters results by process name. Supports wildcards.

.PARAMETER DllName
Filters results by DLL name. Supports wildcards.

.EXAMPLE
PS> Get-MemoryDump -Path memory.vmem | Get-ProcessDll | Select-Object -First 10

Lists the first 10 DLLs from all processes.

.EXAMPLE
PS> Get-MemoryDump -Path memory.vmem | Get-ProcessDll -ProcessName "explorer.exe"

Lists all DLLs loaded by explorer.exe.

.EXAMPLE
PS> Get-ProcessDll -MemoryDump $dump -Pid 1234

Gets all DLLs loaded by process 1234.

.EXAMPLE
PS> Get-MemoryDump -Path memory.vmem | Get-ProcessDll -DllName "*.dll" | Group-Object DllName | Sort-Object Count -Descending

Finds the most commonly loaded DLLs across all processes.

.EXAMPLE
PS> Get-ProcessDll -MemoryDump $dump -DllName "*kernel32*"

Finds all processes that have loaded kernel32.dll or similar.

.OUTPUTS
PowerShell.MemoryAnalysis.Models.DllInfo

.NOTES
- DLL listing can be slow on large dumps (1-2 minutes for 100GB)
- Use -Pid parameter to speed up when targeting specific processes
- Some DLL paths may be <unreadable> if paged out of memory
- Base addresses are in hex format (e.g., "0x7ff8a0000000")

.LINK
Test-ProcessTree
.LINK
Get-MemoryDump
*/
```

---

### Step 6: Testing

**Test Script:** `scripts/Test-DllFeature.ps1`

```powershell
#!/usr/bin/env pwsh

Write-Host "Testing DLL Listing Feature" -ForegroundColor Cyan

Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1 -Force

$dump = Get-MemoryDump -Path F:\physmem.raw

Write-Host "`nTest 1: Get DLLs for specific process" -ForegroundColor Yellow
$systemDlls = Get-ProcessDll -MemoryDump $dump -Pid 4
Write-Host "✓ Found $($systemDlls.Count) DLLs for System process (PID 4)" -ForegroundColor Green
$systemDlls | Select-Object -First 5 | Format-Table DllName, BaseAddress, FormattedSize -AutoSize

Write-Host "`nTest 2: Find DLLs by name" -ForegroundColor Yellow
$ntdll = Get-ProcessDll -MemoryDump $dump -DllName "ntdll.dll"
Write-Host "✓ Found $($ntdll.Count) processes with ntdll.dll loaded" -ForegroundColor Green

Write-Host "`nTest 3: Get DLLs for specific process name" -ForegroundColor Yellow
$explorerDlls = Get-ProcessDll -MemoryDump $dump -ProcessName "explorer.exe"
Write-Host "✓ Found $($explorerDlls.Count) DLLs in explorer.exe" -ForegroundColor Green
$explorerDlls | Select-Object -First 5 | Format-List Pid, ProcessName, DllName, DllPath

Write-Host "`nTest 4: Find suspicious DLLs (non-standard paths)" -ForegroundColor Yellow
$allDlls = Get-ProcessDll -MemoryDump $dump
$suspicious = $allDlls | Where-Object { 
    $_.DllPath -notmatch 'C:\\Windows' -and 
    $_.DllPath -notmatch 'C:\\Program Files' -and
    $_.DllPath -ne '<unreadable>'
}
Write-Host "✓ Found $($suspicious.Count) DLLs in non-standard locations" -ForegroundColor Green
$suspicious | Select-Object -First 5 | Format-Table ProcessName, DllName, DllPath -AutoSize

Write-Host "`nTest 5: DLL statistics" -ForegroundColor Yellow
$stats = $allDlls | Group-Object DllName | Sort-Object Count -Descending | Select-Object -First 10
Write-Host "✓ Top 10 most commonly loaded DLLs:" -ForegroundColor Green
$stats | Format-Table Name, Count -AutoSize

Write-Host "`n✅ All tests passed!" -ForegroundColor Green
```

---

## Timeline

- **Estimated Time:** 1-2 hours
- **Priority:** High (core forensics feature)
- **Dependencies:** None (Rust layer complete)

---

## Notes

- DLL listing can be memory-intensive on large dumps
- Consider adding pagination or streaming for very large result sets
- PID filtering at Rust layer significantly improves performance
- Common use cases:
  - Detecting DLL injection attacks
  - Identifying malicious DLLs loaded from unusual paths
  - Analyzing process dependencies
  - Finding shared libraries across processes
- Base addresses are formatted as hex strings (e.g., "0x00007ff8a0000000")
- Size field is in bytes, use FormattedSize property for human-readable output
