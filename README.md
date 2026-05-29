# PowerShell Memory Analysis Module

A high-performance PowerShell module for memory dump forensics using the Volatility 3 framework with a Rust/Python bridge.

> **Current status:** Analysis cmdlets are production-ready. **Memory dump discovery** and optional **WinPMEM live acquisition** (via the [Collect-MemoryDump](https://github.com/jondmarien/Collect-MemoryDump) submodule) are integrated on Windows.  
> **Latest:** `Resolve-MemoryDumpPath`, `Invoke-MemoryDumpAcquisition`, and `Start-MemoryAnalysis` — search case folders, prompt for acquisition, then load into Volatility.  
> **Note:** `Get-NetworkConnection` and `Find-Malware` are not exported on Windows 11 Build 26100 (Volatility 3 compatibility).  
> See [architecture.md](docs/architecture.md), [DUMP_REQUIREMENTS.md](docs/DUMP_REQUIREMENTS.md), and [PROJECT_STATUS.md](docs/PROJECT_STATUS.md).

## 🏗️ Build Status
[![Build and Test](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/build-and-test.yml)
[![Update Lines of Code Statistics](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/loc-counter.yml/badge.svg)](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/loc-counter.yml)
[![codecov](https://codecov.io/gh/jondmarien/MemoryAnalysis.Powershell/branch/main/graph/badge.svg)](https://codecov.io/gh/jondmarien/MemoryAnalysis.Powershell)

## 📊 Lines of Code Statistics

![LOC Statistics](loc-stats.svg)


## Repository Structure

This is a **monorepo** with Git submodules:

| Path | Repository | Role |
|------|------------|------|
| *(this repo)* | [MemoryAnalysis.Powershell](https://github.com/jondmarien/MemoryAnalysis.Powershell.git) | PowerShell analysis module (MIT) |
| `rust-bridge/` | [rust-bridge](https://github.com/jondmarien/rust-bridge.git) | Rust PyO3 bridge to Volatility 3 |
| `third-party/Collect-MemoryDump/` | [Collect-MemoryDump](https://github.com/jondmarien/Collect-MemoryDump.git) | Windows live memory acquisition (GPL-3.0, fork of [LETHAL-FORENSICS](https://github.com/LETHAL-FORENSICS/Collect-MemoryDump)) |

Clone with submodules:

```powershell
git clone --recurse-submodules https://github.com/jondmarien/MemoryAnalysis.Powershell.git
```

See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and [docs/plans/collect-memorydump-integration-plan.md](docs/plans/collect-memorydump-integration-plan.md) for licensing and acquisition integration.

## Features

- **High performance** — Rust PyO3 bridge with sub-100ms overhead per call
- **Memory dump discovery** — BFS search for `.raw`, `.vmem`, `.dmp` (≥100 MB), and other image extensions; default depth 1 (cwd + one subfolder)
- **Live acquisition (Windows)** — Optional WinPMEM capture through Collect-MemoryDump (GPL-3.0 script; WinPMEM binary not redistributed)
- **Orchestration** — `Start-MemoryAnalysis` resolves or acquires a dump, then runs `Get-MemoryDump`
- **Caching** — LRU cache with TTL, file watching, and cache management cmdlets
- **Volatility 3** — Process trees, command lines, DLLs, and more via the Rust bridge
- **PowerShell native** — Pipeline-friendly cmdlets with custom formatting
- **Parallel processing** — True parallel execution with GIL detach for multi-dump workloads

## Requirements

- **PowerShell:** 7.7.0 or later (Core only; 7.7.0-preview.2 recommended for development)
- **.NET:** 11.0 SDK (preview)
- **Python:** 3.14+ with Volatility 3 2.28.0 (`pip install -r requirements.txt` or `uv pip install -r requirements.txt`)
- **Rust:** 1.90.0+ (for building from source)
- **Docker:** Required for GitHub Actions testing with `act`

## Installation

1. Clone the repository with submodules:

```powershell
# Clone with submodules in one command
git clone --recurse-submodules https://github.com/jondmarien/MemoryAnalysis.Powershell.git
cd MemoryAnalysis

# OR if already cloned without submodules:
git submodule update --init --recursive
```

2. Build the Rust bridge:

```powershell
cd rust-bridge
cargo build --release
cd ..
```

3. Build the PowerShell module:

```powershell
dotnet publish PowerShell.MemoryAnalysis\PowerShell.MemoryAnalysis.csproj -c Release -o PowerShell.MemoryAnalysis\publish
```

4. (Windows only) Install WinPMEM for live acquisition — download `winpmem_mini_x64_rc2.exe` into `third-party\Collect-MemoryDump\Tools\WinPMEM\` per the [Collect-MemoryDump fork README](https://github.com/jondmarien/Collect-MemoryDump).

5. Import the module:

```powershell
Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1
```

## Cmdlets

The module exports **13 cmdlets** (plus `Analyze-ProcessTree` as an alias for `Test-ProcessTree`). Markdown help: [docs/help/](docs/help/).

| Cmdlet | Purpose |
|--------|---------|
| `Get-MemoryDump` | Load a dump into Volatility (`-Path` required) |
| `Resolve-MemoryDumpPath` | Discover a dump or prompt for WinPMEM acquisition (Windows) |
| `Invoke-MemoryDumpAcquisition` | Run Collect-MemoryDump / WinPMEM only |
| `Start-MemoryAnalysis` | Discover → optional acquire → `Get-MemoryDump` |
| `Test-ProcessTree` | Process hierarchy analysis (`Analyze-ProcessTree`) |
| `Get-ProcessCommandLine` | Command-line extraction |
| `Get-ProcessDll` | Loaded DLL listing |
| `Get-CacheInfo`, `Clear-Cache`, `Watch-MemoryDumpFile`, `Stop-WatchingMemoryDumpFile`, `Get-WatchedMemoryDumpFiles`, `Test-CacheValidity` | Cache and file-watch management |

`Find-Malware` and `Get-NetworkConnection` are implemented but **not exported** (Windows 11 Build 26100 Volatility limitations).

### Get-MemoryDump

Loads a memory dump file for analysis. `-Path` remains **mandatory** for direct loads.

```powershell
# Basic usage
$dump = Get-MemoryDump -Path C:\dumps\memory.vmem

# With validation
$dump = Get-MemoryDump -Path C:\dumps\memory.raw -Validate

# With OS profile detection
$dump = Get-MemoryDump -Path C:\dumps\memory.dmp -DetectProfile
```

### Resolve-MemoryDumpPath, Invoke-MemoryDumpAcquisition, Start-MemoryAnalysis

Find a memory image under a search root (default: current directory, depth `1`) or prompt for WinPMEM capture on Windows when none is found.

| Parameter | Default | Notes |
|-----------|---------|--------|
| `-SearchPath` | `.` | Root of the discovery scan |
| `-MaxSearchDepth` | `1` | `0` = cwd only; increase for deep case folders |
| `-NoAcquire` | off | Discovery only; error if no dump (also used in CI) |
| `-Force` | off | Skip large-dump confirmation prompt |
| `-PassThru` | off | Return path string instead of `DiscoveredMemoryDump` object |

**WinPMEM:** Place `winpmem_mini_x64_rc2.exe` under `third-party/Collect-MemoryDump/Tools/WinPMEM/`. Run PowerShell **as Administrator** for live acquisition. In CI (`GITHUB_ACTIONS=true`), live acquisition is blocked unless you pass `-NoAcquire` or `-Path`.

```powershell
# Discover and load (prompts if no dump; offers WinPMEM on Windows)
$dump = Start-MemoryAnalysis

# Search a case folder three levels deep, no live acquisition
$dump = Start-MemoryAnalysis -SearchPath D:\Cases\2026-001 -MaxSearchDepth 3 -NoAcquire

# Resolve path only (no Volatility load)
$resolved = Resolve-MemoryDumpPath -SearchPath . -MaxSearchDepth 1
$dump = Get-MemoryDump -Path $resolved.Path

# Explicit path (skips discovery)
Resolve-MemoryDumpPath -Path C:\dumps\host.raw

# Run acquisition only (Windows + WinPMEM); returns path to discovered .raw
$dumpPath = Invoke-MemoryDumpAcquisition -SearchPath D:\Cases\host-01
```

### Test-ProcessTree

Analyzes process hierarchies in a memory dump.

**Alias:** `Analyze-ProcessTree`

```powershell
# Analyze all processes
Get-MemoryDump -Path memory.vmem | Test-ProcessTree

# Filter by process name
Test-ProcessTree -MemoryDump $dump -ProcessName "explorer*"

# Tree view with suspicious process flagging
Test-ProcessTree -MemoryDump $dump -Format Tree -FlagSuspicious

# Filter by PID
Test-ProcessTree -MemoryDump $dump -Pid 1234

# JSON output
Test-ProcessTree -MemoryDump $dump -Format JSON
```

### Get-ProcessCommandLine

Extracts command line arguments for processes.

```powershell
# Get all command lines
Get-MemoryDump -Path memory.vmem | Get-ProcessCommandLine

# Filter by process name
Get-ProcessCommandLine -MemoryDump $dump -ProcessName "powershell*"

# Get for specific PID
Get-ProcessCommandLine -MemoryDump $dump -Pid 1234
```

### Get-ProcessDll

Lists DLLs loaded by processes.

```powershell
# Get all DLLs
Get-MemoryDump -Path memory.vmem | Get-ProcessDll

# DLLs for specific process
Get-ProcessDll -MemoryDump $dump -Pid 1234

# Find suspicious DLLs
Get-ProcessDll -MemoryDump $dump -DllName "*malware*"
```

### ⚠️ Get-NetworkConnection (Disabled - Win11 26100 Incompatibility)

Extracts network connections from memory.

**Status:** Implemented but disabled due to Volatility 3 incompatibility with Windows 11 Build 26100.  
**Issue:** `PagedInvalidAddressException` in Windows 11 kernel pool structures.

```powershell
# Get all connections (disabled)
Get-MemoryDump -Path memory.vmem | Get-NetworkConnection

# Filter by state
Get-NetworkConnection -MemoryDump $dump -State ESTABLISHED
```

### ⚠️ Find-Malware (Disabled - Win11 26100 Incompatibility)

Detects potential malware in memory dumps.

**Status:** Implemented but disabled due to Volatility 3 incompatibility with Windows 11 Build 26100.  
**Issue:** Returns zero detections on Windows 11 Build 26100.

```powershell
# Full malware scan (disabled)
Get-MemoryDump -Path memory.vmem | Find-Malware

# Quick scan with high confidence threshold
Find-Malware -MemoryDump $dump -QuickScan -MinimumConfidence 75

# Filter by severity
Find-Malware -MemoryDump $dump -Severity High,Critical
```

## Examples

### Discovery-first workflow (recommended)

```powershell
Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1

# Search cwd + one subfolder; prompt for WinPMEM on Windows if empty
$dump = Start-MemoryAnalysis

# Or: resolve only, then load explicitly
$found = Resolve-MemoryDumpPath -SearchPath D:\Cases\2026-001 -MaxSearchDepth 2 -NoAcquire
$dump = Get-MemoryDump -Path $found.Path -Validate
```

### Basic memory dump analysis

```powershell
# Load and analyze a memory dump (explicit path)
$dump = Get-MemoryDump -Path C:\evidence\suspicious.vmem -Validate

# Get process tree
$processes = Test-ProcessTree -MemoryDump $dump -FlagSuspicious

# Display suspicious processes
$processes | Where-Object IsSuspicious | Format-Table

# Scan for malware
$threats = Find-Malware -MemoryDump $dump -MinimumConfidence 60
$threats | Format-List
```

### Pipeline Processing

```powershell
# Analyze multiple dumps
Get-ChildItem C:\dumps\*.vmem | 
    Get-MemoryDump | 
    Find-Malware -QuickScan |
    Where-Object {$_.Severity -eq 'Critical'} |
    Export-Csv malware-findings.csv
```

### Cache management

**Cache cmdlets:**

```powershell
# View cache statistics
Get-CacheInfo

# Clear all caches
Clear-Cache -Force -Confirm:$false

# Watch a memory dump file for changes
Watch-MemoryDumpFile -Path F:\physmem.raw

# Stop watching a file
Stop-WatchingMemoryDumpFile -Path F:\physmem.raw

# List currently watched files
Get-WatchedMemoryDumpFiles

# Validate cache against file changes
Test-CacheValidity
```

**Cache Performance:**
- >80% hit rate on repeated analysis
- <2 seconds for cached operations
- Automatic invalidation on file changes
- TTL-based expiration (2 hours default)

### Parallel processing

```powershell
# Analyze multiple dumps in parallel (true parallel execution)
Get-ChildItem C:\dumps\*.vmem | ForEach-Object -ThrottleLimit 4 -Parallel {
    Import-Module MemoryAnalysis
    $dump = Get-MemoryDump -Path $_.FullName
    $processes = Test-ProcessTree -MemoryDump $dump
    
    [PSCustomObject]@{
        File = $_.Name
        ProcessCount = $processes.Count
        ThreadId = [System.Threading.Thread]::CurrentThread.ManagedThreadId
    }
}

# Test parallel performance
.\Test-ParallelProcessing.ps1 -DumpPath "C:\dumps\" -ThrottleLimit 4
```

**Recommended ThrottleLimit values:**
- Small dumps (<500MB): 4-8 threads
- Medium dumps (500MB-5GB): 2-4 threads  
- Large dumps (>5GB): 2 threads
- Very large dumps (>50GB): 1 thread (sequential)

### Comprehensive Investigation

```powershell
# Complete memory forensics workflow
$dump = Get-MemoryDump -Path evidence.raw -Validate -DetectProfile

# Analyze processes
$processes = Test-ProcessTree -MemoryDump $dump -FlagSuspicious
$suspicious = $processes | Where-Object IsSuspicious

Write-Host "Found $($suspicious.Count) suspicious processes" -ForegroundColor Yellow
$suspicious | Format-Table Name, Pid, SuspiciousReasons

# Scan for malware
$malware = Find-Malware -MemoryDump $dump -GenerateReport
$malware | Group-Object Severity | 
    Select-Object Name, Count | 
    Format-Table -AutoSize
```

## Architecture

```text
┌──────────────────────────────────────────────────────────┐
│  PowerShell.MemoryAnalysis (C# / MIT)                    │
│  Start-MemoryAnalysis → Resolve-MemoryDumpPath           │
│  Invoke-MemoryDumpAcquisition → Get-MemoryDump           │
│  Test-ProcessTree | Get-ProcessCommandLine | cache …     │
└──────────────────────────────────────────────────────────┘
         │ discovery / acquisition              │ analysis
         ▼                                    ▼
┌─────────────────────────┐      ┌────────────────────────────┐
│ Collect-MemoryDump.ps1  │      │ rust-bridge (PyO3 / FFI)   │
│ WinPMEM (GPL script;    │      │ → Volatility 3 (Python)    │
│  binary not bundled)    │      └────────────────────────────┘
└─────────────────────────┘
```

Details: [docs/architecture.md](docs/architecture.md).

## Development

### Building from Source

1. **Rust Bridge**:

```powershell
cd rust-bridge
cargo build --release
```

2. **C# Module**:

```powershell
dotnet build PowerShell.MemoryAnalysis\PowerShell.MemoryAnalysis.csproj
```

3. **Run tests**:

```powershell
# Rust (from rust-bridge/)
cargo test
cargo clippy -- -D warnings

# C# unit tests + coverage (from repo root)
dotnet test tests/MemoryAnalysis.Tests/MemoryAnalysis.Tests.csproj --collect:"XPlat Code Coverage"

# Pester integration tests (after building/publishing the module)
pwsh -NoProfile -Command "Invoke-Pester -Path tests/integration-tests -Output Detailed"
```

Legacy scripts under `scripts/` (`Test-RustInterop.ps1`, `Test-GetMemoryDump.ps1`, etc.) are still available for manual checks.

## Project structure

```text
MemoryAnalysis.Powershell/          # MIT — main module
├── .gitmodules
├── rust-bridge/                    # Submodule — PyO3 → Volatility 3
├── third-party/
│   └── Collect-MemoryDump/         # Submodule — WinPMEM acquisition (GPL-3.0)
├── PowerShell.MemoryAnalysis/
│   ├── Cmdlets/                    # Including discovery & acquisition
│   ├── Services/                   # Discovery, Collect-MemoryDump runner/locator
│   ├── Models/
│   └── MemoryAnalysis.psd1
├── tests/
│   ├── MemoryAnalysis.Tests/       # xUnit (services, cmdlets via runspace)
│   └── integration-tests/          # Pester (module load, discovery, help)
├── docs/
│   ├── architecture.md
│   ├── DUMP_REQUIREMENTS.md
│   ├── help/                       # platyPS markdown per cmdlet
│   └── plans/
├── benchmarks/                     # Measure-Performance.ps1 (CI per OS)
├── scripts/                        # Build, help generation, manual tests
└── .github/workflows/              # Parallel ci-windows / ubuntu / macos
```

## Performance

- **Rust-Python overhead**: < 100ms per operation ✅
- **Memory efficiency**: Successfully handles 98GB memory dumps
- **Process extraction**: 830 processes from 98GB dump in seconds
- **Parallel processing**: ✨ **TRUE parallel execution** with GIL detach ✅
  - Multiple dumps analyzed concurrently
  - Recommended: 2-4 threads for large dumps, 4-8 for small dumps
  - Test speedup: 1.5-2.5x on I/O-heavy workloads

## CI/CD

GitHub Actions ([build-and-test.yml](.github/workflows/build-and-test.yml)) runs **three parallel platform pipelines** (`ci-windows`, `ci-ubuntu`, `ci-macos`). Each pipeline is sequential: Rust → C# → build artifact → Pester integration tests.

| Stage | What runs |
|-------|-----------|
| Rust | `cargo test`, `clippy`, `rustfmt`, tarpaulin → Codecov |
| C# | `dotnet test` with XPlat code coverage → Codecov |
| Build | Published module artifact per runner |
| Integration | Full `tests/integration-tests/` with Pester 5+ |
| Benchmarks | `benchmark-windows` / `ubuntu` / `macos` after each platform CI (PRs and `main`) |

See [.github/workflows/README.md](.github/workflows/README.md) for job details.

## Contributing

Contributions are welcome! Please read our contributing guidelines and submit pull requests.

## License

Copyright (c) 2025. All rights reserved.

## Acknowledgments

- [Volatility 3](https://github.com/volatilityfoundation/volatility3) - Memory forensics framework
- [PyO3](https://github.com/PyO3/pyo3) - Rust-Python bindings
- PowerShell Team - PowerShell SDK

## Support

For issues and questions, please open an issue on GitHub.
