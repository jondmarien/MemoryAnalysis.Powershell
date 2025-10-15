# PowerShell Memory Analysis Module

A high-performance PowerShell module for memory dump forensics using the Volatility 3 framework with a Rust/Python bridge.

> **Current Status:** 🔄 In Development - Phase 1 (Rust-Python Bridge) 85% complete  
> See [PROJECT_STATUS.md](docs/PROJECT_STATUS.md) for detailed progress tracking.

## Repository Structure

This is a **monorepo** with a Git submodule:

- **Main Repository:** [MemoryAnalysis.Powershell](https://github.com/jondmarien/MemoryAnalysis.Powershell.git) - PowerShell module and documentation
- **Submodule:** [rust-bridge](https://github.com/jondmarien/rust-bridge.git) - Rust PyO3 bridge to Volatility 3

The Rust bridge is maintained as a separate repository but linked as a submodule for seamless development.

## Features

- 🚀 **High Performance** - Rust-based bridge with sub-100ms overhead
- 🔍 **Comprehensive Analysis** - Process trees, malware detection, and more
- 🐍 **Volatility 3 Integration** - Full access to Volatility 3 plugins
- 💻 **PowerShell Native** - Seamless pipeline integration
- 📊 **Custom Formatting** - Beautiful output with custom views
- 🎯 **Malware Detection** - Multi-technique detection with confidence scoring

## Requirements

- PowerShell 7.6.0 or later
- .NET 10.0 SDK
- Python 3.12+ with Volatility 3
- Rust 1.90.0 or later (for development)

## Installation

1. Clone the repository with submodules:

```powershell
# Clone with submodules in one command
git clone --recurse-submodules https://github.com/jondmarien/MemoryAnalysis.Powershell.git
cd MemoryAnalysis

# OR if already cloned without submodules:
git submodule init
git submodule update
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

3. Import the module:

```powershell
Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1
```

## Cmdlets

### ✅ Get-MemoryDump (Available)

Loads a memory dump file for analysis.

```powershell
# Basic usage
$dump = Get-MemoryDump -Path C:\dumps\memory.vmem

# With validation
$dump = Get-MemoryDump -Path C:\dumps\memory.raw -Validate

# With OS profile detection
$dump = Get-MemoryDump -Path C:\dumps\memory.dmp -DetectProfile
```

### ✅ Test-ProcessTree (Available)

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

### 🔄 Get-ProcessCommandLine (In Development - Rust Complete)

Extracts command line arguments for processes.

```powershell
# Get all command lines
Get-MemoryDump -Path memory.vmem | Get-ProcessCommandLine

# Filter by process name
Get-ProcessCommandLine -MemoryDump $dump -ProcessName "powershell*"

# Get for specific PID
Get-ProcessCommandLine -MemoryDump $dump -Pid 1234
```

### 🔄 Get-ProcessDll (In Development - Rust Complete)

Lists DLLs loaded by processes.

```powershell
# Get all DLLs
Get-MemoryDump -Path memory.vmem | Get-ProcessDll

# DLLs for specific process
Get-ProcessDll -MemoryDump $dump -Pid 1234

# Find suspicious DLLs
Get-ProcessDll -MemoryDump $dump -DllName "*malware*"
```

### ⏳ Get-NetworkConnection (Planned)

Extracts network connections from memory.

```powershell
# Get all connections
Get-MemoryDump -Path memory.vmem | Get-NetworkConnection

# Filter by state
Get-NetworkConnection -MemoryDump $dump -State ESTABLISHED
```

### ⏳ Find-Malware (Planned)

Detects potential malware in memory dumps.

```powershell
# Full malware scan
Get-MemoryDump -Path memory.vmem | Find-Malware

# Quick scan with high confidence threshold
Find-Malware -MemoryDump $dump -QuickScan -MinimumConfidence 75

# Filter by severity
Find-Malware -MemoryDump $dump -Severity High,Critical
```

## Examples

### Basic Memory Dump Analysis

```powershell
# Load and analyze a memory dump
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
┌─────────────────────────────────────────────┐
│         PowerShell Cmdlets (C#)             │
│   Get-MemoryDump | Test-ProcessTree         │
│            Find-Malware                     │
└─────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────┐
│         Rust-Python Bridge (PyO3)           │
│    High-performance FFI with P/Invoke       │
└─────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────┐
│         Volatility 3 Framework              │
│    Memory forensics plugins and analysis    │
└─────────────────────────────────────────────┘
```

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

3. **Run Tests**:

```powershell
# Rust tests
cd rust-bridge
cargo test

# PowerShell tests
.\Test-RustInterop.ps1
.\Test-GetMemoryDump.ps1
```

## Project Structure

```tree
MemoryAnalysis/ (main repo)
├── .gitmodules              # Submodule configuration
├── rust-bridge/             # 🔗 Git submodule (separate repo)
│   ├── src/
│   │   ├── lib.rs           # FFI exports
│   │   ├── python_manager.rs
│   │   ├── volatility.rs
│   │   ├── process_analysis.rs
│   │   ├── types.rs
│   │   └── error.rs
│   ├── Cargo.toml
│   └── README.md
├── PowerShell.MemoryAnalysis/
│   ├── Cmdlets/             # PowerShell cmdlets
│   ├── Models/              # Data models
│   ├── Services/            # Business logic
│   ├── MemoryAnalysis.psd1  # Module manifest
│   ├── MemoryAnalysis.Format.ps1xml
│   └── README.md
├── docs/
│   ├── PROJECT_STATUS.md    # Development progress tracker
│   ├── PHASE2_CMDLINE_INTEGRATION.md
│   ├── PHASE2_DLL_INTEGRATION.md
│   └── plans/
├── .kiro/steering/          # Project steering docs
├── scripts/                 # Test and verification scripts
└── WARP.md                  # AI agent guidance
```

## Performance

- **Rust-Python overhead**: < 100ms per operation
- **Memory efficiency**: < 1GB RAM overhead per dump
- **Parallel processing**: Full support with ForEach-Object -Parallel

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
