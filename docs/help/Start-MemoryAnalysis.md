---
external help file: PowerShell.MemoryAnalysis.dll-Help.xml
Module Name: MemoryAnalysis
online version: https://github.com/jondmarien/MemoryAnalysis.Powershell
schema: 2.0.0
---

# Start-MemoryAnalysis

## SYNOPSIS

Resolves a memory dump path (with optional discovery or acquisition) and loads it via Get-MemoryDump.

## SYNTAX

```
Start-MemoryAnalysis [[-Path] <String>] [[-SearchPath] <String>] [[-MaxSearchDepth] <Int32>] [-NoAcquire] [-Force] [-Validate]
 [<CommonParameters>]
```

## DESCRIPTION

Orchestrates the typical analyst workflow:

1. **Resolve** — `Resolve-MemoryDumpPath` (discovery, optional WinPMEM on Windows)
2. **Load** — `Get-MemoryDump` with the resolved path

Use **-Path** to skip discovery and load a known file. Use **-NoAcquire** in automation or CI. Use **-Validate** to run dump validation during load.

Default **MaxSearchDepth** is `1` (current directory + one subdirectory level).

## EXAMPLES

### Example 1: Interactive start from current directory

```powershell
PS C:\Cases> $dump = Start-MemoryAnalysis
PS C:\Cases> $dump | Test-ProcessTree | Select-Object -First 20
```

### Example 2: Known dump, validated

```powershell
PS C:\> Start-MemoryAnalysis -Path C:\dumps\host.raw -Validate
```

### Example 3: Lab folder, no live capture

```powershell
PS C:\> Start-MemoryAnalysis -SearchPath D:\Lab -MaxSearchDepth 2 -NoAcquire
```

## PARAMETERS

### -Force

Passed to discovery; skips large-file confirmation for a single candidate.

```yaml
Type: SwitchParameter
Required: False
```

### -MaxSearchDepth

Discovery depth. Default `1`.

```yaml
Type: Int32
Required: False
Default value: 1
```

### -NoAcquire

Discovery only; never prompt for WinPMEM.

```yaml
Type: SwitchParameter
Required: False
```

### -Path

Explicit dump path (bypasses directory search).

```yaml
Type: String
Required: False
```

### -SearchPath

Discovery root when **-Path** is omitted. Default `.`.

```yaml
Type: String
Required: False
Default value: .
```

### -Validate

Enable `Get-MemoryDump -Validate`.

```yaml
Type: SwitchParameter
Required: False
```

## OUTPUTS

### PowerShell.MemoryAnalysis.Models.MemoryDump

## NOTES

`Get-MemoryDump -Path` remains mandatory when calling `Get-MemoryDump` directly.

## RELATED LINKS

[Get-MemoryDump](Get-MemoryDump.md)

[Resolve-MemoryDumpPath](Resolve-MemoryDumpPath.md)
