# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

PowerShell Memory Analysis Module - A high-performance PowerShell module for memory dump forensics that bridges PowerShell cmdlets (C#) with Volatility 3 framework (Python) via a Rust FFI layer using PyO3.

## Essential Build & Development Commands

### Building the Rust Bridge

```powershell
# From rust-bridge directory
cd rust-bridge
cargo build --release  # Release build (required for production)
cargo build            # Debug build (for development with extra logging)
cargo test             # Run Rust tests
```

**Critical:** The C# project expects `rust_bridge.dll` in either `rust-bridge/target/debug/` or `rust-bridge/target/release/`. The .csproj automatically copies the DLL to the C# output directory.

### Building the PowerShell Module

```powershell
# Build only
dotnet build PowerShell.MemoryAnalysis\PowerShell.MemoryAnalysis.csproj

# Build and prepare for import (recommended)
dotnet publish PowerShell.MemoryAnalysis\PowerShell.MemoryAnalysis.csproj -o PowerShell.MemoryAnalysis\publish
```

### Importing and Testing the Module

```powershell
# Import the module
Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1 -Force

# Run comprehensive test suite
.\scripts\Test-Module-Complete.ps1

# Run individual tests
.\scripts\Test-RustInterop.ps1
.\scripts\Test-GetMemoryDump.ps1
```

### Testing with Real Memory Dumps

```powershell
# Load a memory dump
$dump = Get-MemoryDump -Path F:\physmem.raw -Validate -DetectProfile

# Analyze process tree
$dump | Test-ProcessTree -FlagSuspicious | Format-Table

# Scan for malware
$dump | Find-Malware -MinimumConfidence 60 | Format-List
```

## High-Level Architecture

This project uses a **three-tier cross-language architecture**:

### 1. PowerShell Cmdlets Layer (C# / .NET 10.0)
- **Location:** `PowerShell.MemoryAnalysis/Cmdlets/`
- **Purpose:** User-facing PowerShell commands
- **Key Cmdlets:**
  - `Get-MemoryDump` - Loads and validates memory dump files
  - `Test-ProcessTree` (alias: `Analyze-ProcessTree`) - Analyzes process hierarchies
  - `Find-Malware` - Detects potential malware with confidence scoring
- **Models:** Data classes in `Models/` (MemoryDump, ProcessTreeInfo, MalwareResult)
- **Services:** `RustInteropService` handles P/Invoke calls to Rust bridge

### 2. Rust FFI Bridge Layer (Rust + PyO3)
- **Location:** `rust-bridge/src/`
- **Purpose:** High-performance bridge between C# and Python with <100ms overhead
- **Key Modules:**
  - `lib.rs` - FFI exports (`rust_bridge_*` C functions), debug logging
  - `python_manager.rs` - Python interpreter lifecycle management
  - `volatility.rs` - Volatility 3 framework integration
  - `process_analysis.rs` - Process data extraction and analysis
  - `types.rs` - Shared data structures and JSON serialization
  - `error.rs` - Error types and result handling
- **FFI Contract:** C ABI functions with string marshaling via `CString`/`CStr`

### 3. Python/Volatility 3 Backend
- **Purpose:** Actual memory forensics analysis via Volatility 3 plugins
- **Integration:** PyO3 auto-initializes Python interpreter and imports Volatility modules
- **Requirements:** Python 3.12+ with Volatility 3 installed (typically in `volatility-env`)

### Cross-Language Data Flow

```
PowerShell Pipeline
    ↓ (PSObject)
C# Cmdlet (e.g., Test-ProcessTree)
    ↓ (P/Invoke call)
RustInteropService.ListProcesses()
    ↓ (FFI / CString marshal)
rust_bridge_list_processes(dump_path)
    ↓ (PyO3 GIL)
ProcessAnalyzer.list_processes()
    ↓ (Python import)
Volatility 3 pslist plugin
    ↓ (TreeGrid traversal)
Extract process data
    ↓ (serde_json serialize)
JSON string → CString
    ↓ (P/Invoke return)
JsonSerializer.Deserialize<ProcessInfo[]>
    ↓ (Convert to PowerShell types)
ProcessTreeInfo[] PowerShell objects
```

**Key Design Note:** All inter-layer communication uses JSON serialization. The Rust bridge never holds GIL longer than necessary and releases strings via `rust_bridge_free_string()`.

## Debug Logging Configuration

This project has **extensive debug logging** that is critical for troubleshooting cross-language integration issues.

### Enabling Debug Logging

**Method 1: Environment Variable (Affects Rust Bridge)**
```powershell
$env:RUST_BRIDGE_DEBUG = "1"  # Enable
$env:RUST_BRIDGE_DEBUG = "0"  # Disable
```
Accepted values: `"1"`, `"true"`, `"yes"`, `"on"` (case-insensitive)

**Method 2: Cmdlet Parameter (Affects Both C# and Rust)**
```powershell
Get-MemoryDump -Path F:\physmem.raw -Debug
Test-ProcessTree -MemoryDump $dump -Debug
Find-Malware -MemoryDump $dump -Debug
```

**Method 3: PowerShell Verbose Preference**
```powershell
$VerbosePreference = "Continue"
Get-MemoryDump -Path F:\physmem.raw -Verbose
```

### Debug Log Output Locations

- **Rust Bridge:** `J:\projects\personal-projects\MemoryAnalysis\rust-bridge-debug.log`
  - Logs: Python initialization, Volatility plugin execution, TreeGrid traversal, process counts, errors
  - Format: `[YYYY-MM-DD HH:MM:SS] Message`
- **C# Services:** Uses `Microsoft.Extensions.Logging` to console/debug output
- **PowerShell:** Cmdlet verbose/debug streams via `WriteVerbose()`/`WriteDebug()`

**When Debugging:**
1. Enable all three methods for maximum visibility
2. Clear `rust-bridge-debug.log` before test runs: `Remove-Item rust-bridge-debug.log -ErrorAction SilentlyContinue`
3. Check log file if cmdlet fails silently: `Get-Content rust-bridge-debug.log -Tail 50`

See `DEBUG_LOGGING.md` for comprehensive details.

## Module Structure & Pipeline Workflow

### PowerShell Pipeline Design

All cmdlets are designed for **pipeline integration**:

```powershell
# Single-step pipeline
Get-MemoryDump -Path *.vmem | Test-ProcessTree -FlagSuspicious

# Multi-stage pipeline
Get-ChildItem C:\dumps\*.raw |
    Get-MemoryDump -Validate |
    Find-Malware -MinimumConfidence 75 |
    Where-Object {$_.Severity -eq 'Critical'} |
    Export-Csv critical-threats.csv
```

### Cmdlet-Specific Notes

#### Get-MemoryDump
- Accepts file paths from pipeline (`ValueFromPipeline`, `ValueFromPipelineByPropertyName`)
- Resolves wildcards and relative paths via `SessionState.Path.GetResolvedProviderPathFromPSPath()`
- Shows progress bars during validation/profile detection
- Returns `MemoryDump` objects with metadata (Path, Size, Profile, Architecture)

#### Test-ProcessTree (Analyze-ProcessTree)
- Accepts `MemoryDump` objects from pipeline
- Calls `RustInteropService.ListProcesses()` → `rust_bridge_list_processes()` FFI
- Supports filtering by `-ProcessName` (wildcards) and `-Pid`
- `-FlagSuspicious` marks processes with unusual characteristics
- Output formats: Default (Table), Tree, JSON

#### Find-Malware
- Accepts `MemoryDump` objects from pipeline
- Multi-technique detection: process anomalies, injection detection, hidden processes
- `-QuickScan` for faster analysis with reduced scope
- `-MinimumConfidence` threshold filtering (0-100 score)
- `-GenerateReport` creates detailed text reports

### Native DLL Loading Mechanism

The `.csproj` includes conditional copy rules:
```xml
<None Include="..\rust-bridge\target\debug\rust_bridge.dll" Condition="Exists(...)" />
<None Include="..\rust-bridge\target\release\rust_bridge.dll" Condition="Exists(...)" />
```

**Build Order:** Always build Rust bridge **before** C# module to ensure DLL is available.

## Requirements & Dependencies

- **PowerShell:** 7.6.0+ (PowerShell Core only, not Windows PowerShell 5.1)
- **.NET:** 10.0 SDK
- **Rust:** 1.90.0+ (with `cargo`)
- **Python:** 3.12+ with Volatility 3 installed
- **Volatility Environment:** Python packages must be in system or `volatility-env` virtual environment

**Package Dependencies:**
- C#: `Microsoft.PowerShell.SDK`, `System.Management.Automation`, `Microsoft.Extensions.Logging`
- Rust: `pyo3` (0.26.0 with `auto-initialize`), `tokio`, `serde`, `serde_json`, `anyhow`, `chrono`

## Important Implementation Notes

### P/Invoke String Marshaling
- **C# → Rust:** Strings passed as `[MarshalAs(UnmanagedType.LPStr)] string` → `*const c_char`
- **Rust → C#:** Return `*mut c_char` from `CString::into_raw()` → Marshal with `PtrToStringAnsi()`
- **Memory Management:** C# must call `rust_bridge_free_string()` to free Rust-allocated strings (handled in `finally` blocks)

### PyO3 GIL (Global Interpreter Lock)
- Python operations require acquiring GIL via `Python::attach(|py| { ... })`
- Rust bridge minimizes GIL hold time to prevent blocking
- Never hold GIL across FFI boundary returns

### Error Handling Layers
- **Rust:** `anyhow::Result` → FFI error codes (0=success, negative=error)
- **C#:** Check return pointers for `IntPtr.Zero`, throw exceptions with logging
- **PowerShell:** Cmdlets use `WriteError()` for non-terminating errors, `ThrowTerminatingError()` for fatal issues

### Custom Formatting
`MemoryAnalysis.Format.ps1xml` defines custom PowerShell views for output types. Automatically loaded by module manifest.

### Module Manifest Key Points
- Module GUID: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`
- Prerelease flag: `Prerelease = 'preview'`
- `CompatiblePSEditions = @('Core')` - **not compatible with Windows PowerShell 5.1**
- Aliases: `Analyze-ProcessTree` → `Test-ProcessTree`

## Working with This Codebase

When modifying code:

1. **Rust changes:** Rebuild Rust bridge (`cargo build --release`), then rebuild C# module
2. **C# changes:** Just rebuild C# module (`dotnet build`)
3. **Testing changes:** Always reimport module with `-Force` flag
4. **Adding cmdlets:** Update `MemoryAnalysis.psd1` `CmdletsToExport` array
5. **FFI changes:** Update both `lib.rs` FFI exports AND `RustInterop.cs` `[DllImport]` declarations
6. **Debugging FFI issues:** Enable debug logging and check `rust-bridge-debug.log` for marshaling errors

**Key Files to Understand:**
- `rust-bridge/src/lib.rs` - FFI boundary and debug infrastructure
- `PowerShell.MemoryAnalysis/Services/RustInterop.cs` - P/Invoke declarations and C# marshaling
- `PowerShell.MemoryAnalysis/MemoryAnalysis.psd1` - Module manifest with exported cmdlets
- `PowerShell.MemoryAnalysis/PowerShell.MemoryAnalysis.csproj` - Build configuration and DLL copy rules
