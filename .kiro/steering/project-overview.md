---
inclusion: always
---

# PowerShell Memory Analysis Module - Project Overview

## Project Identity

This is a **multi-language forensics project** combining:
- **Rust** (PyO3 bridge layer)
- **C#** (PowerShell cmdlets)
- **Python** (Volatility 3 integration)

The module provides enterprise-grade memory forensics capabilities through PowerShell cmdlets.

## Core Architecture

### Three-Layer Design

```
PowerShell Cmdlets (C#/.NET 9)
        ↓
Rust Bridge (PyO3 + FFI)
        ↓
Volatility 3 (Python)
```

### Key Components

1. **PowerShell Module** (`PowerShell.MemoryAnalysis/`)
   - Binary cmdlets in C#
   - Models and services
   - Module manifest and formatting

2. **Rust Bridge** (`rust-bridge/`)
   - PyO3 Python embedding
   - FFI exports for C# P/Invoke
   - Type-safe data marshaling

3. **Python Integration**
   - Volatility 3 framework
   - Plugin orchestration
   - Memory dump analysis

## Target Cmdlets

- `Get-MemoryDump` - Load and validate memory dumps
- `Test-ProcessTree` (Analyze-ProcessTree) - Process hierarchy analysis
- `Find-Malware` - Multi-technique malware detection
- `Get-VolatilityPlugin` - Dynamic plugin discovery

## Technology Stack

- **PowerShell SDK**: 7.6.0-preview.5
- **.NET**: 9.0 RC2
- **Rust**: 1.70+ with PyO3 0.20+
- **Python**: 3.11+ with Volatility 3.2+

## Performance Targets

- Rust-Python overhead: < 100ms
- Initial dump load: < 30s for 4GB dumps
- Memory overhead: < 1GB per loaded dump
- Test coverage: > 85% (Rust), > 80% (C#)

## Cross-Platform Support

- Windows (x64, ARM64)
- Linux (x64)
- macOS (x64, ARM64)
