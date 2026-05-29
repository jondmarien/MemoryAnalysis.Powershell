# PowerShell Memory Analysis Module - Project Status

**Last updated:** 2026-05-29  
**Current phase:** Collect-MemoryDump integration merged; production analysis + discovery/acquisition on `main`

## Current snapshot

| Area | Status |
|------|--------|
| **Analysis cmdlets** | Production-ready (`Get-MemoryDump`, `Test-ProcessTree`, command line, DLLs, cache) |
| **Discovery & acquisition** | Merged — `Resolve-MemoryDumpPath`, `Invoke-MemoryDumpAcquisition`, `Start-MemoryAnalysis` |
| **CI** | Green on `main` — parallel Windows / Ubuntu / macOS pipelines + per-OS benchmarks |
| **Tests** | 96 xUnit tests; Pester integration tests (`tests/integration-tests/`) |
| **Help** | platyPS markdown for all exported cmdlets under `docs/help/` |
| **Codecov** | Rust + C# coverage uploaded per platform |
| **Win11 26100** | `Get-NetworkConnection` / `Find-Malware` still disabled (Volatility 3 upstream) |

**Quick links:** [architecture.md](architecture.md) · [DUMP_REQUIREMENTS.md](DUMP_REQUIREMENTS.md) · [collect-memorydump-integration-plan.md](plans/collect-memorydump-integration-plan.md) · [README](../README.md)

---

## Collect-MemoryDump integration (2026-05)

**Status:** Phases 0–4 **complete** on `main` (PR #4). Phase 5 (manual WinPMEM on a physical Windows VM) remains optional validation.

### Delivered

| Component | Purpose |
|-----------|---------|
| `MemoryDumpDiscoveryService` | BFS discovery; minidump filter; merge with Collect-MemoryDump output tree |
| `CollectMemoryDumpLocator` / `CollectMemoryDumpRunner` | Find script; invoke `Collect-MemoryDump.ps1 -- -WinPMEM` (no GPL link in DLL) |
| `Resolve-MemoryDumpPath` | Discover dump or prompt for WinPMEM (Windows); CI guard when `GITHUB_ACTIONS` |
| `Invoke-MemoryDumpAcquisition` | Explicit WinPMEM capture entry point |
| `Start-MemoryAnalysis` | Discover → optional acquire → `Get-MemoryDump` |
| `DiscoveredMemoryDump` | Model with human-readable size formatting |
| Submodule | `third-party/Collect-MemoryDump` (GPL-3.0; WinPMEM binary not redistributed) |
| Docs | `docs/architecture.md`, `THIRD_PARTY_NOTICES.md`, updated `DUMP_REQUIREMENTS.md` |

### Locked product rules

- **Default search depth:** `MaxSearchDepth = 1` (cwd + one subdirectory)
- **`Get-MemoryDump -Path`:** still required for direct load (backward compatible)
- **CI:** live acquisition blocked unless `-NoAcquire` or `-Path` is provided

### Remaining (Phase 5)

- Manual validation: elevated PowerShell, real WinPMEM capture, dump appears under Collect-MemoryDump output layout

---

## CI/CD and quality (2026-05)

**Workflows:** `.github/workflows/ci-windows.yml`, `ci-ubuntu.yml`, `ci-macos.yml` → reusable `platform-pipeline.yml` per OS.

| Stage | Windows | Ubuntu | macOS |
|-------|---------|--------|-------|
| Rust test + clippy + tarpaulin | ✅ | ✅ | ✅ |
| C# xUnit + coverage | ✅ | ✅ | ✅ |
| Build artifact | ✅ | ✅ | ✅ |
| Pester integration | ✅ | ✅ | ✅ |
| Benchmarks (PR + `main`) | ✅ | ✅ | ✅ |

**Test layout:**

- `tests/MemoryAnalysis.Tests/` — xUnit (services, cmdlets via runspace, discovery, runner)
- `tests/integration-tests/` — Pester (module manifest, `Resolve-MemoryDumpPath`, help when MAML present)

---

## Exported cmdlets (module manifest)

**13 cmdlets** (+ `Analyze-ProcessTree` alias):

`Get-MemoryDump`, `Resolve-MemoryDumpPath`, `Invoke-MemoryDumpAcquisition`, `Start-MemoryAnalysis`, `Test-ProcessTree`, `Get-ProcessCommandLine`, `Get-ProcessDll`, `Get-CacheInfo`, `Clear-Cache`, `Watch-MemoryDumpFile`, `Stop-WatchingMemoryDumpFile`, `Get-WatchedMemoryDumpFiles`, `Test-CacheValidity`

**Not exported (Win11 Build 26100):** `Get-NetworkConnection`, `Find-Malware`

---

## Overview (historical plan)

This document tracks development against `docs/plans/powershell-memory-analysis-development-plan.md`. Phases 1–3 and Phase 5.1–5.2 (parallel processing, caching) are complete. Collect-MemoryDump integration is documented in `docs/plans/collect-memorydump-integration-plan.md`.

---

## Phase 1: Rust-Python Bridge (PyO3 Layer)

### ✅ Task 1.1: Project Foundation - **COMPLETE**

**Status:** ✅ Done  
**Completed:** 2025-10-14

- ✅ Rust library initialized with PyO3 dependencies
- ✅ build.rs configured for Python embedding
- ✅ Basic error handling with `anyhow` crate
- ✅ Module structure created

**Files Created:**
- `rust-bridge/Cargo.toml`
- `rust-bridge/src/lib.rs`
- `rust-bridge/build.rs`

---

### ✅ Task 1.2: Python Interpreter Management - **COMPLETE**

**Status:** ✅ Done  
**Completed:** 2025-10-14

- ✅ Singleton Python interpreter with lazy initialization
- ✅ Python path configuration and module loading
- ✅ Proper cleanup and shutdown procedures
- ✅ GIL management with PyO3

**Files Created:**
- `rust-bridge/src/python_manager.rs`

---

### ✅ Task 1.3: Volatility 3 Integration - **COMPLETE**

**Status:** ✅ Done  
**Completed:** 2025-10-14

- ✅ Volatility framework initialization
- ✅ Memory dump loading functions
- ✅ Plugin execution capabilities (PsList working)
- ✅ Result extraction and serialization

**Files Created:**
- `rust-bridge/src/volatility.rs`
- `rust-bridge/src/error.rs`

**Achievements:**
- Successfully integrated with Volatility 3 using Context API
- Implemented automagics execution (critical discovery!)
- TreeGrid visitor pattern working correctly
- Extracting **830 processes** from 98GB memory dump

---

### ✅ Task 1.4: Memory Analysis Functions - **COMPLETE** 

**Status:** ✅ 100% Complete (All 5 features implemented at Rust layer)  
**Started:** 2025-10-14  
**Completed:** 2025-10-15  
**Note:** Network and malware features disabled in C# due to Windows 11 Build 26100 incompatibility

#### ✅ Rust Layer Complete:
- ✅ **Process list analysis** (`windows.pslist.PsList` plugin) - **COMPLETE**
- ✅ **Command line extraction** (`windows.cmdline.CmdLine` plugin) - **COMPLETE**
- ✅ **DLL listing** (`windows.dlllist.DllList` plugin) - **COMPLETE**

#### ⚠️ Implemented But Disabled (Windows 11 Build 26100 Incompatibility):
- ⚠️ **Network connections** (`windows.netscan.NetScan` plugin) - **DISABLED**
- ⚠️ **Malware detection** (`windows.malfind.Malfind`, `windows.psxview.PsXview`) - **DISABLED**

---

### ✅ Task 1.5: Data Serialization Layer - **COMPLETE**

**Status:** ✅ Done  
**Completed:** 2025-10-15

---

## Phase 2: PowerShell Binary Module (C# Layer)

### ✅ Tasks 2.1–2.5 - **COMPLETE**

Core cmdlets, manifest, formatting, and build scripts delivered 2025-10-14 – 2025-10-15. See git history for file-level detail.

**2026 addition:** discovery/acquisition cmdlets and services (see Collect-MemoryDump section above).

---

## Phase 5: Advanced Features

### ✅ Task 5.1: Parallel Processing - **COMPLETE** (2025-10-15)

GIL detach in Rust FFI; `ForEach-Object -Parallel` supported; ~1.5–2.5× speedup on I/O-heavy workloads.

### ✅ Task 5.2: Caching and Performance Optimization - **COMPLETE** (2025-10-16)

LRU cache, file watching, cache management cmdlets, `Test-CachePerformance.ps1`.

---

## Known limitations

1. **Windows 11 Build 26100** — Volatility 3 issues with NetScan / Malfind; cmdlets not exported.
2. **WinPMEM** — User must place binaries under `third-party/Collect-MemoryDump/Tools/WinPMEM/`; administrator recommended for capture.
3. **GPL boundary** — Collect-MemoryDump invoked as external script only (see `THIRD_PARTY_NOTICES.md`).

---

## Environment (CI and development)

| Component | Version |
|-----------|---------|
| PowerShell | 7.7.0-preview.2 (CI `POWERSHELL_VERSION`) |
| .NET SDK | 11.0 (preview) |
| Rust | 1.90.0+ |
| Python | 3.14+ / Volatility 3 via `requirements.txt` |
| Runners | `windows-2025-vs2026`, `ubuntu-latest`, `macos-latest` |

**Clone with submodules:**

```powershell
git clone --recurse-submodules https://github.com/jondmarien/MemoryAnalysis.Powershell.git
cd MemoryAnalysis.Powershell
```

**Build:**

```powershell
cd rust-bridge; cargo build --release; cd ..
dotnet publish PowerShell.MemoryAnalysis\PowerShell.MemoryAnalysis.csproj -c Release -o PowerShell.MemoryAnalysis\publish
Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1
```

---

## Project health

| Metric | Status | Notes |
|--------|--------|-------|
| **Build (main)** | ✅ Green | Parallel platform pipelines |
| **Unit tests** | ✅ Green | 96 xUnit tests |
| **Integration tests** | ✅ Green | Pester on all three OSes in CI |
| **Documentation** | ✅ Green | README, architecture, help markdown, DUMP_REQUIREMENTS |
| **Discovery/acquisition** | ✅ Merged | Phase 5 manual VM test optional |
| **Codecov** | 🟡 Partial | Uploads active; patch % varies by PR |
| **Win11 plugins** | ⚠️ Blocked | Upstream Volatility 3 |

---

## Milestones

| Milestone | Status | Date |
|-----------|--------|------|
| Core Rust bridge + analysis cmdlets | ✅ | 2025-10-15 |
| Parallel processing + caching | ✅ | 2025-10-16 |
| CI on Windows / Ubuntu / macOS | ✅ | 2026-05 |
| Collect-MemoryDump integration (code + CI + docs) | ✅ | 2026-05-29 |
| Manual WinPMEM field validation | 🔜 Optional | — |
| PowerShell Gallery / public release | 🔜 | After Win11 plugin path or documented scope |

---

## References

- **Development plan:** `docs/plans/powershell-memory-analysis-development-plan.md`
- **Collect-MemoryDump plan:** `docs/plans/collect-memorydump-integration-plan.md`
- **Architecture:** `docs/architecture.md`
- **WARP guide:** `WARP.md`
- **Workflows:** `.github/workflows/README.md`

---

**Legend:** ✅ Complete · 🔄 In progress · 🔜 Planned · ⚠️ Limitation / upstream blocker · 🟡 Attention
