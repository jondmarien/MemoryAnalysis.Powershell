# Memory Dump Requirements

Volatility 3 and this module require a **full physical memory image**, not a crash minidump.

## Supported vs unsupported

### Supported

| Type | Extensions | Notes |
|------|------------|--------|
| Raw physical image | `.raw`, `.vmem`, `.mem`, `.img`, `.bin`, `.lime` | Preferred for labs |
| AFF4 / other | `.aff` | If produced by your toolchain |
| Large crash dumps | `.dmp` ≥ 100 MB | Treated as candidate full dumps |

### Not supported

| Type | Why |
|------|-----|
| WER minidumps | `.dmp` &lt; 100 MB — filtered by `MemoryDumpDiscoveryService` |
| Per-process dumps | Task Manager “Create dump file” — user-mode only |
| Empty files | Size must be &gt; 0 |

The sample `preview.DMP` under `samples/dumps/` (if present) is typically a **minidump** and will fail Volatility automagic.

## Automatic discovery (module)

`Resolve-MemoryDumpPath` and `Start-MemoryAnalysis` search for dumps before loading Volatility:

- **Default depth:** `MaxSearchDepth = 1` (search directory + one subdirectory level)
- **Override:** `-MaxSearchDepth 0` (cwd only) through `32` (deep case folders)
- **Skip acquisition:** `-NoAcquire` (discovery only; error if none found)
- **Explicit file:** `Resolve-MemoryDumpPath -Path C:\dumps\host.raw`

```powershell
Resolve-MemoryDumpPath -SearchPath D:\Cases\2026-001 -MaxSearchDepth 3 -NoAcquire
Start-MemoryAnalysis -SearchPath . -NoAcquire
```

## Live acquisition (Windows)

When no dump is found and `-NoAcquire` is not set, the module can offer **WinPMEM** via the Collect-MemoryDump fork:

1. Initialize submodule: `git submodule update --init third-party/Collect-MemoryDump`
2. Place `winpmem_mini_x64_rc2.exe` in `third-party/Collect-MemoryDump/Tools/WinPMEM/`
3. Run PowerShell **as Administrator**
4. `Invoke-MemoryDumpAcquisition` or accept the prompt from `Resolve-MemoryDumpPath`

Output layout (upstream):  
`third-party/Collect-MemoryDump/<HOSTNAME>/<timestamp>-Collect-MemoryDump/Memory/WinPMEM/<COMPUTERNAME>.raw`  
(archive step may produce `.7z` — prefer keeping `.raw` for Volatility until fork supports predictable raw output.)

**CI / automation:** Use `-NoAcquire` and provide a dump path. Live acquisition is disabled when `GITHUB_ACTIONS=true`.

## Creating dumps manually

### WinPMEM (recommended)

```powershell
# Standalone
.\winpmem_mini_x64_rc2.exe C:\dumps\host.raw

# Via this repo (after Tools/ setup)
Invoke-MemoryDumpAcquisition
```

### DumpIt

```powershell
.\DumpIt.exe /O test.raw /T RAW
```

### Do not use for Volatility

- Task Manager → Create dump file (minidump)
- Small `.dmp` files from WER

## Validation workflow

```powershell
# 1. Volatility CLI (optional sanity check)
vol -f memory.raw windows.pslist.PsList

# 2. Module
Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1
$dump = Get-MemoryDump -Path .\memory.raw -Validate
$dump | Test-ProcessTree | Select-Object -First 10

# 3. Discovery path
Start-MemoryAnalysis -SearchPath C:\dumps -MaxSearchDepth 2 -NoAcquire
```

## References

- [architecture.md](architecture.md) — monorepo and discovery design
- [plans/collect-memorydump-integration-plan.md](plans/collect-memorydump-integration-plan.md) — integration phases
- [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md) — GPL submodule and tool licensing
