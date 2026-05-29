---
external help file: PowerShell.MemoryAnalysis.dll-Help.xml
Module Name: MemoryAnalysis
online version: https://github.com/jondmarien/MemoryAnalysis.Powershell
schema: 2.0.0
---

# Invoke-MemoryDumpAcquisition

## SYNOPSIS

Captures a Windows memory image using Collect-MemoryDump (WinPMEM).

## SYNTAX

```
Invoke-MemoryDumpAcquisition [[-SearchPath] <String>] [[-Tool] <String>] [[-CollectMemoryDumpScript] <String>]
 [<CommonParameters>]
```

## DESCRIPTION

Invokes `Collect-MemoryDump.ps1` from the `third-party/Collect-MemoryDump` submodule with **-WinPMEM**. This cmdlet is **Windows-only** and requires:

- Git submodule initialized
- `winpmem_mini_x64_rc2.exe` in `Tools/WinPMEM/`
- Administrator privileges (recommended)

After capture, the cmdlet re-scans for new dump files under **-SearchPath** and the Collect-MemoryDump output directory.

v1 supports **WinPMEM** only (`-Tool WinPMEM`).

## EXAMPLES

### Example 1: Default WinPMEM capture

```powershell
PS C:\> Invoke-MemoryDumpAcquisition
```

Runs acquisition from the current directory context and lists discovered dumps.

### Example 2: Custom script path

```powershell
PS C:\> Invoke-MemoryDumpAcquisition -CollectMemoryDumpScript D:\Tools\Collect-MemoryDump.ps1
```

## PARAMETERS

### -CollectMemoryDumpScript

Optional full path to `Collect-MemoryDump.ps1`. When omitted, the module locates the submodule relative to the installed module path.

```yaml
Type: String
Required: False
```

### -SearchPath

Directory to search after acquisition completes. Default: current location.

```yaml
Type: String
Required: False
Default value: .
```

### -Tool

Acquisition tool. Only `WinPMEM` is supported in v1.

```yaml
Type: String
Required: False
Default value: WinPMEM
```

## OUTPUTS

### System.String

Paths of discovered dump files after acquisition.

## NOTES

GPL-3.0 script in submodule; not embedded in the MIT-licensed DLL. Not run in GitHub Actions CI.

## RELATED LINKS

[Resolve-MemoryDumpPath](Resolve-MemoryDumpPath.md)
