# CI/CD Workflows

## Overview

Automated build, test, and release pipelines for the MemoryAnalysis PowerShell module using GitHub Actions.

## Workflows

### 1. Build and Test (`build-and-test.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Manual trigger (`workflow_dispatch`)

**Jobs:**

#### Rust Unit Tests (3 platforms)
- **Platforms:** Windows, Linux, macOS
- **Steps:**
  - Setup Rust toolchain (stable)
  - Setup Python 3.12 + Volatility3
  - Run Cargo tests (20 tests)
  - Run Clippy linting
  - Check code formatting
- **Cache:** Cargo registry and build artifacts

#### C# Unit Tests (3 platforms)
- **Platforms:** Windows, Linux, macOS  
- **Steps:**
  - Setup .NET 10.0
  - Restore dependencies
  - Build test project
  - Run xUnit tests (33 tests)
  - Generate code coverage (Ubuntu only)
  - Upload to Codecov
- **Coverage:** Cobertura XML format

#### Build PowerShell Module (3 platforms)
- **Platforms:** Windows, Linux, macOS
- **Dependencies:** Requires Rust and C# tests to pass
- **Steps:**
  - Setup .NET, Rust, Python
  - Build Rust bridge (release mode)
  - Build PowerShell module (`dotnet publish`)
  - Copy platform-specific native libraries
    - Windows: `rust_bridge.dll`
    - Linux: `librust_bridge.so`
    - macOS: `librust_bridge.dylib`
  - Generate MAML help files (Windows only)
  - Upload build artifacts
- **Artifacts:** Module binaries for each platform (7-day retention)

#### PowerShell Integration Tests (3 platforms)
- **Platforms:** Windows, Linux, macOS
- **Dependencies:** Requires module build
- **Steps:**
  - Install Pester 5+
  - Download build artifacts
  - Run Pester tests (21/25 passing, 4 skipped)
  - Upload test results
- **Test Output:** NUnitXml format for CI

#### Performance Benchmarks (Windows only)
- **Platform:** Windows latest
- **Trigger:** Only on push to `main` branch
- **Steps:**
  - Download Windows build artifacts
  - Run `Measure-Performance.ps1`
  - Upload benchmark results (30-day retention)
- **Metrics:** Module load, FFI overhead, memory usage

## Status Badges

Add to README.md:

```markdown
[![Build and Test](https://github.com/jondmarien/MemoryAnalysis.Powershell/workflows/Build%20and%20Test/badge.svg?branch=main)](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/build-and-test.yml)
[![Update Lines of Code Statistics](https://github.com/jondmarien/MemoryAnalysis.Powershell/workflows/Update%20Lines%20of%20Code%20Statistics/badge.svg?branch=main)](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/loc-counter.yml)
[![codecov](https://codecov.io/gh/jondmarien/MemoryAnalysis.Powershell/branch/main/graph/badge.svg)](https://codecov.io/gh/jondmarien/MemoryAnalysis.Powershell)
```

## Build Matrix

| Platform | Rust Tests | C# Tests | Module Build | Integration Tests |
|----------|-----------|----------|--------------|-------------------|
| Windows  | ✅ | ✅ | ✅ | ✅ |
| Linux    | ✅ | ✅ | ✅ | ✅ |
| macOS    | ✅ | ✅ | ✅ | ✅ |

## Environment Variables

```yaml
DOTNET_VERSION: '10.0.x'      # .NET preview version
RUST_VERSION: 'stable'         # Rust toolchain
PYTHON_VERSION: '3.12'         # Python for Volatility3
```

## Caching Strategy

### Cargo Cache
- **Key:** `${{ runner.os }}-cargo-${{ hashFiles('**/Cargo.lock') }}`
- **Paths:**
  - `~/.cargo/bin/`
  - `~/.cargo/registry/`
  - `rust-bridge/target/`
- **Benefits:** ~2-3x faster Rust builds

### NuGet Cache (implicit)
- Handled by `setup-dotnet` action
- Caches package restore

## Artifacts

### Build Artifacts
- **Name:** `MemoryAnalysis-{os}`
- **Contents:** Complete module build with native libraries
- **Retention:** 7 days
- **Size:** ~50-100 MB per platform

### Test Results
- **Name:** `test-results-{os}`
- **Contents:** Pester test outputs
- **Retention:** 7 days

### Benchmark Results
- **Name:** `benchmark-results`
- **Contents:** Performance JSON files
- **Retention:** 30 days

## Local Testing

Run CI checks locally before pushing:

### Rust Tests
```bash
cd rust-bridge
cargo test
cargo clippy
cargo fmt --check
```

### C# Tests
```bash
dotnet test tests/MemoryAnalysis.Tests/MemoryAnalysis.Tests.csproj
```

### PowerShell Integration Tests
```powershell
Import-Module Pester -MinimumVersion 5.0
Invoke-Pester tests/integration-tests/Module.Tests.ps1
```

## Troubleshooting

### Build Failures

**Rust tests failing:**
- Ensure Python + Volatility3 installed
- Check Rust toolchain version

**C# tests failing:**
- Verify .NET 10.0 preview installed
- Check NuGet package restore

**Integration tests failing:**
- Ensure Pester 5+ installed
- Verify module artifacts downloaded correctly

### Platform-Specific Issues

**Windows:**
- PowerShell 7+ required for tests
- Visual Studio Build Tools may be needed

**Linux:**
- Install `build-essential` for Rust compilation
- Ensure `libpython3.12` available

**macOS:**
- Xcode Command Line Tools required
- May need Homebrew for dependencies

## Phase 3.5 Status

✅ **Complete:**
- [x] Multi-platform CI/CD pipeline (Windows, Linux, macOS)
- [x] Automated Rust unit tests
- [x] Automated C# unit tests
- [x] Automated PowerShell integration tests
- [x] Build artifact publishing
- [x] Performance benchmark automation
- [x] Code coverage reporting
- [x] Test result archiving
- [x] Documentation complete

## Future Enhancements

- [ ] Automated releases on git tags
- [ ] PowerShell Gallery publication
- [ ] Docker image builds
- [ ] Security scanning (Snyk, Dependabot)
- [ ] Performance regression detection
- [ ] Slack/Teams notifications

---

*See Phase 4.3 for release automation workflows*
