# Third-Party Notices

This repository combines several components under different licenses. The
**MemoryAnalysis.Powershell** project code (C# module, documentation, and build
scripts in this repository, excluding submodules) is licensed under the **MIT
License** — see [LICENSE](LICENSE).

Git **submodules** are separate works with their own licenses. When you clone
with `--recurse-submodules`, you receive them as distinct trees.

## Submodules

### rust-bridge

- **Path:** `rust-bridge/`
- **Repository:** https://github.com/jondmarien/rust-bridge.git
- **Purpose:** Rust/PyO3 FFI to Volatility 3
- **License:** See `rust-bridge/LICENSE` or repository default (add explicit
  license file in that repo if missing).

### Collect-MemoryDump

- **Path:** `third-party/Collect-MemoryDump/`
- **Repository:** https://github.com/jondmarien/Collect-MemoryDump.git
- **Upstream:** https://github.com/LETHAL-FORENSICS/Collect-MemoryDump (fork)
- **Purpose:** Forensically sound Windows live memory acquisition (PowerShell)
- **License:** **GNU General Public License v3.0** — see
  `third-party/Collect-MemoryDump/LICENSE`
- **Copyright:** Martin Willing / LETHAL-FORENSICS (see script headers)

If you distribute this monorepo (or a build that includes the Collect-MemoryDump
submodule), you must comply with **GPL-3.0** for that component, including
providing corresponding source for the version you ship. Fork-specific changes
are documented in the fork repository.

The main MIT-licensed module **invokes** Collect-MemoryDump as a separate script;
it does not incorporate GPL source into the compiled .NET assembly.

## Python / runtime dependencies (not vendored)

| Component | License | Notes |
|-----------|---------|--------|
| [Volatility 3](https://github.com/volatilityfoundation/volatility3) | VSL | Installed via `pip` / `requirements.txt` |
| [PyO3](https://github.com/PyO3/pyo3) | Apache-2.0 OR MIT | Rust crate dependency |

## Memory acquisition binaries (not distributed)

Collect-MemoryDump can call external tools (WinPMEM, DumpIt, Magnet tools, etc.).
**This project does not bundle those binaries.** You must obtain them separately
and place them under `third-party/Collect-MemoryDump/Tools/` per the fork README.
Their licenses are defined by each vendor.

## Disclaimer

This project is intended for **education, research, and authorized incident
response**. Memory acquisition affects live systems and may require
administrator privileges. You are responsible for lawful use on systems you are
permitted to examine.
