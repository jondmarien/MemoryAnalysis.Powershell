# AGENTS.md

## Cursor Cloud specific instructions

### Project Overview

PowerShell Memory Analysis Module — a PowerShell module for memory dump forensics bridging C#/.NET cmdlets → Rust FFI (PyO3) → Python Volatility 3. No external services (no databases, web servers, or Docker containers required).

### Tool Versions

| Tool | Required Version | Install Command |
|---|---|---|
| .NET SDK | 10.0 (preview) | `wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh && chmod +x /tmp/dotnet-install.sh && /tmp/dotnet-install.sh --channel 10.0 --quality preview` |
| Rust | 1.90.0+ (stable) | `rustup default stable` |
| PowerShell | 7.6.0-preview.5 | Install via `.deb` package from GitHub releases |
| Python | 3.12+ | Pre-installed |
| python3.12-dev | Required | `sudo apt-get install -y python3.12-dev` (needed for Rust/PyO3 linking) |

### Environment Variables

These must be set (already in `~/.bashrc` after setup):
- `PATH` must include `$HOME/.dotnet` and `$HOME/.local/bin`
- `DOTNET_ROOT` must be set to `$HOME/.dotnet`

### Build Order (important)

1. `git submodule update --init --recursive` — the `rust-bridge` submodule must be initialized first
2. `cd rust-bridge && cargo build --release` — build Rust native library
3. `dotnet publish PowerShell.MemoryAnalysis/PowerShell.MemoryAnalysis.csproj -c Release -o PowerShell.MemoryAnalysis/publish` — build C# module
4. `cp rust-bridge/target/release/librust_bridge.so PowerShell.MemoryAnalysis/publish/` — copy native lib to publish dir

### Running Tests

- **Rust tests:** `cd rust-bridge && cargo test --verbose`
- **Rust lint:** `cd rust-bridge && cargo clippy -- -D warnings`
- **Rust format check:** `cd rust-bridge && cargo fmt -- --check`
- **C# tests:** `dotnet test tests/MemoryAnalysis.Tests/MemoryAnalysis.Tests.csproj --verbosity normal`
- **PowerShell integration tests:** `pwsh-preview -Command 'Import-Module Pester -MinimumVersion 5.0; Invoke-Pester -Path tests/integration-tests/Module.Tests.ps1 -CI'`

### Known Gotchas

- The `.csproj` references `rust_bridge.dll` (Windows naming) but on Linux the built artifact is `librust_bridge.so`. You must manually copy it to the publish directory.
- PowerShell integration tests have 3 pre-existing failures (cmdlet count expectation mismatch, missing help examples, missing parameter descriptions). These are not environment issues.
- The error-handling integration test for `Get-ProcessDll` will hang because Pester doesn't suppress mandatory parameter prompts. If running integration tests non-interactively, be prepared to kill the process or set a timeout.
- The module uses `net10.0` target framework, which requires the .NET 10.0 preview SDK. Standard .NET 8 or 9 SDKs will not work.
- The Rust bridge uses PyO3 which requires `python3.12-dev` (development headers/libraries) to link against `libpython3.12`.
