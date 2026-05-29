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

Three top-level jobs (`ci-windows`, `ci-ubuntu`, `ci-macos`) run **in parallel**. Each invokes the reusable workflow [platform-pipeline.yml](platform-pipeline.yml), which runs this **sequential** chain on that runner only:

1. **Rust unit tests** → 2. **C# unit tests** → 3. **Build PowerShell module** → 4. **PowerShell integration tests**

Ubuntu and macOS no longer wait for Windows to finish Rust/C# before starting their own build; each OS owns its full pipeline.

#### Per-platform pipeline (`platform-pipeline.yml`)

**Rust unit tests**
- Setup Rust, Python 3.14, Volatility3
- Cargo test, Clippy, `rustfmt` check
- Rust coverage → Codecov

**C# unit tests** (after Rust on the same runner)
- Setup .NET 11
- Restore, build, run xUnit tests with coverage → Codecov

**Build PowerShell module** (after C# on the same runner)
- Build Rust bridge (release), `dotnet publish`
- Copy native library (`rust_bridge.dll` / `librust_bridge.so` / `librust_bridge.dylib`)
- Generate MAML help (Windows only)
- Upload `MemoryAnalysis-{runner}` artifacts (7-day retention)

**PowerShell integration tests** (after build on the same runner)
- Install Pester 5+, download build artifacts, run Pester with `-CI`
- Upload test results

#### Performance benchmarks (Windows only)
- **Depends on:** `ci-windows` (Windows pipeline complete)
- **Trigger:** Push to `main` only
- Downloads Windows build artifacts, runs `Measure-Performance.ps1`

## Status Badges

Add to README.md:

```markdown
[![Build and Test](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/build-and-test.yml)
[![Update Lines of Code Statistics](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/loc-counter.yml/badge.svg)](https://github.com/jondmarien/MemoryAnalysis.Powershell/actions/workflows/loc-counter.yml)
[![codecov](https://codecov.io/gh/jondmarien/MemoryAnalysis.Powershell/branch/main/graph/badge.svg)](https://codecov.io/gh/jondmarien/MemoryAnalysis.Powershell)
```

## Build Matrix

Each row is one parallel pipeline (steps run in order left → right):

| Platform | Rust → C# → Build → Integration → Benchmarks |
|----------|-----------------------------------------------|
| Windows  | ✅ (independent pipeline) |
| Linux    | ✅ (independent pipeline) |
| macOS    | ✅ (independent pipeline) |

## Environment Variables

```yaml
DOTNET_VERSION: '11.0.x'       # .NET SDK (in platform-pipeline.yml)
RUST_VERSION: 'stable'
PYTHON_VERSION: '3.14'
POWERSHELL_VERSION: '7.7.0-preview.2'
VOLATILITY3_VERSION: '2.28.0'
```

CI uses current GitHub Actions (Node 24): `actions/checkout@v6`, `actions/cache@v5`, `actions/setup-python@v6`, `actions/setup-dotnet@v5`, `codecov/codecov-action@v6`, `actions/upload-artifact@v7`, `actions/download-artifact@v8`.

Windows jobs use `windows-2025-vs2026` explicitly (avoids `windows-latest` redirect notices).

PowerShell 7.7 preview is installed per job and exposed as `PWSH_PREVIEW` (Windows zip extract does not add `pwsh-preview` to PATH).

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
- **Name:** `benchmark-results-{runner}` (e.g. `benchmark-results-ubuntu-latest`)
- **Contents:** Performance JSON files per OS
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
