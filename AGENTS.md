# AGENTS.md

## Cursor Cloud specific instructions

### Project Overview

PowerShell Memory Analysis Module — a PowerShell module for memory dump forensics bridging C#/.NET cmdlets → Rust FFI (PyO3) → Python Volatility 3. Optional Windows live acquisition via the **Collect-MemoryDump** submodule (GPL-3.0, fork). No external services (no databases, web servers, or Docker containers required).

**License:** MIT for this repository’s own code — see [LICENSE](LICENSE). Submodules and runtime deps have separate terms — see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

### Tool Versions

| Tool | Required Version | Install Command |
|---|---|---|
| .NET SDK | 11.0 (preview) | `wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh && chmod +x /tmp/dotnet-install.sh && /tmp/dotnet-install.sh --channel 11.0 --quality preview` |
| Rust | stable (1.83+ for PyO3 0.28) | `rustup default stable` |
| PowerShell | 7.7.0-preview.2 | Linux: `.deb` from [GitHub releases](https://github.com/PowerShell/PowerShell/releases/tag/v7.7.0-preview.2) (`powershell-preview_7.7.0-preview.2-1.deb_amd64.deb`) |
| Python | 3.14+ | `uv python install 3.14` then `uv venv volatility-env --python 3.14` |
| Volatility 3 | 2.28.0 | `uv pip install -r requirements.txt` (in venv) |

PyO3 **0.28** is used; it supports Python 3.14 (tested upstream against 3.14 final). Link the interpreter via `PYO3_PYTHON` pointing at the venv Python when building `rust-bridge`.

### Environment Variables

These must be set (already in `~/.bashrc` after setup):
- `PATH` must include `$HOME/.dotnet` and `$HOME/.local/bin`
- `DOTNET_ROOT` must be set to `$HOME/.dotnet`
- `PYO3_PYTHON` — path to `volatility-env` Python (for `cargo build` in `rust-bridge`)
- `LD_LIBRARY_PATH` — must include the directory containing `libpython3.14.so` when using a `uv`-managed interpreter (e.g. `$(dirname $(readlink -f ../volatility-env/bin/python))/../lib` under the uv python install root)

### Build Order (important)

1. `git submodule update --init --recursive` — initializes `rust-bridge` and `third-party/Collect-MemoryDump`
2. `uv venv volatility-env --python 3.14 && uv pip install -r requirements.txt` — Python + Volatility
3. `cd rust-bridge && PYO3_PYTHON=../volatility-env/bin/python cargo build --release` — build Rust native library
4. `dotnet publish PowerShell.MemoryAnalysis/PowerShell.MemoryAnalysis.csproj -c Release -o PowerShell.MemoryAnalysis/publish` — build C# module
5. `cp rust-bridge/target/release/librust_bridge.so PowerShell.MemoryAnalysis/publish/` — copy native lib to publish dir (Linux)

### Running Tests

- **Rust tests:** `cd rust-bridge && PYO3_PYTHON=../volatility-env/bin/python cargo test --verbose`
- **Rust lint:** `cd rust-bridge && cargo clippy -- -D warnings`
- **Rust format check:** `cd rust-bridge && cargo fmt -- --check`
- **C# tests:** `dotnet test tests/MemoryAnalysis.Tests/MemoryAnalysis.Tests.csproj --verbosity normal`
- **PowerShell integration tests:** `pwsh-preview -Command 'Import-Module Pester -MinimumVersion 5.0; Invoke-Pester -Path tests/integration-tests/Module.Tests.ps1 -CI'`

### Known Gotchas

- The `.csproj` references `rust_bridge.dll` (Windows naming) but on Linux the built artifact is `librust_bridge.so`. You must manually copy it to the publish directory.
- PowerShell integration tests have 3 pre-existing failures (cmdlet count expectation mismatch, missing help examples, missing parameter descriptions). These are not environment issues.
- The error-handling integration test for `Get-ProcessDll` will hang because Pester doesn't suppress mandatory parameter prompts. If running integration tests non-interactively, be prepared to kill the process or set a timeout.
- The module uses `net11.0` and **Microsoft.PowerShell.SDK 7.7.0-preview.2**, which requires the .NET 11 preview SDK.
- The Rust bridge uses PyO3 0.28 against Python 3.14 from `volatility-env`; set `PYO3_PYTHON` before building.
