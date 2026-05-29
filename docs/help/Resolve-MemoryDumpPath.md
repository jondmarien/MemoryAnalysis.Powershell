---
external help file: PowerShell.MemoryAnalysis.dll-Help.xml
Module Name: MemoryAnalysis
online version: https://github.com/jondmarien/MemoryAnalysis.Powershell
schema: 2.0.0
---

# Resolve-MemoryDumpPath

## SYNOPSIS

Discovers a memory dump under a directory tree or optionally starts WinPMEM acquisition on Windows.

## SYNTAX

### Search (Default)

```
Resolve-MemoryDumpPath [[-SearchPath] <String>] [[-MaxSearchDepth] <Int32>] [-NoAcquire] [-Force] [-PassThru]
 [<CommonParameters>]
```

### Explicit

```
Resolve-MemoryDumpPath -Path <String> [-PassThru] [<CommonParameters>]
```

## DESCRIPTION

`Resolve-MemoryDumpPath` searches for memory image files using `MemoryDumpDiscoveryService`. By default the search uses **MaxSearchDepth 1** (the search directory plus one level of subdirectories).

When no file is found:

- With **-NoAcquire**, the cmdlet terminates with an error.
- On **Windows**, without **-NoAcquire**, the user may be prompted to run Collect-MemoryDump (WinPMEM).
- On **non-Windows**, or when `GITHUB_ACTIONS` is set (CI), acquisition is not offered.

Also scans Collect-MemoryDump output folders under `third-party/Collect-MemoryDump/` when the submodule is present.

## EXAMPLES

### Example 1: Discover in the current directory

```powershell
PS C:\Cases> Resolve-MemoryDumpPath
```

Searches `.` with depth 1. If one dump is found, returns a `DiscoveredMemoryDump` object.

### Example 2: Deeper search without acquisition

```powershell
PS C:\> Resolve-MemoryDumpPath -SearchPath D:\Cases\2026-001 -MaxSearchDepth 3 -NoAcquire
```

### Example 3: Use an explicit path

```powershell
PS C:\> Resolve-MemoryDumpPath -Path C:\dumps\WORKSTATION.raw -PassThru
```

Returns the object and writes the path string when **-PassThru** is specified.

## PARAMETERS

### -Force

Skip confirmation for a single large dump (&gt; 10 GB). Does not bypass multi-file selection prompts.

```yaml
Type: SwitchParameter
Parameter Sets: Search
Required: False
```

### -MaxSearchDepth

Maximum directory depth below **-SearchPath**. Default is `1`.

```yaml
Type: Int32
Parameter Sets: Search
Required: False
Default value: 1
```

### -NoAcquire

Do not prompt for or run live memory acquisition.

```yaml
Type: SwitchParameter
Parameter Sets: Search
Required: False
```

### -PassThru

Also emit the resolved path as a string.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Required: False
```

### -Path

Explicit path to a memory image (skips directory discovery).

```yaml
Type: String
Parameter Sets: Explicit
Required: True
```

### -SearchPath

Root directory for discovery. Default is the current location.

```yaml
Type: String
Parameter Sets: Search
Required: False
Default value: .
```

## OUTPUTS

### PowerShell.MemoryAnalysis.Models.DiscoveredMemoryDump

## NOTES

Requires WinPMEM under `third-party/Collect-MemoryDump/Tools/WinPMEM/` for acquisition. See [DUMP_REQUIREMENTS.md](https://github.com/jondmarien/MemoryAnalysis.Powershell/blob/main/docs/DUMP_REQUIREMENTS.md).

## RELATED LINKS

[Start-MemoryAnalysis](Start-MemoryAnalysis.md)

[Invoke-MemoryDumpAcquisition](Invoke-MemoryDumpAcquisition.md)
