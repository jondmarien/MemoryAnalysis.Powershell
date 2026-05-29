---
external help file: PowerShell.MemoryAnalysis.dll-Help.xml
Module Name: MemoryAnalysis
online version: https://github.com/jondmarien/MemoryAnalysis.Powershell
schema: 2.0.0
---

# Get-MemoryDump

## SYNOPSIS

Loads a memory dump file for analysis with Volatility 3.

## SYNTAX

```
Get-MemoryDump [-Path] <String> [-Validate] [-DetectProfile] [-DebugMode] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION

The `Get-MemoryDump` cmdlet opens a memory image and prepares it for Volatility 3 analysis through the Rust bridge. Supported formats include raw images (`.raw`, `.mem`, `.bin`, `.lime`), VMware dumps (`.vmem`), AFF4 (`.aff`), and large crash dumps (`.dmp` ≥ 100 MB).

Use `Resolve-MemoryDumpPath` or `Start-MemoryAnalysis` when you need to discover or acquire a dump before loading. **-Path** is required for this cmdlet.

## EXAMPLES

### Example 1: Load a raw memory image

```powershell
PS C:\> $dump = Get-MemoryDump -Path C:\dumps\memory.raw
```

Loads the file and returns a `MemoryDump` object for pipeline cmdlets.

### Example 2: Load with validation and profile detection

```powershell
PS C:\> $dump = Get-MemoryDump -Path C:\evidence\host.vmem -Validate -DetectProfile
```

Validates structure and attempts OS profile detection before analysis.

### Example 3: Pipeline from discovery

```powershell
PS C:\> $found = Resolve-MemoryDumpPath -SearchPath D:\Cases\2026-001 -NoAcquire
PS C:\> Get-MemoryDump -Path $found.Path | Test-ProcessTree
```

## PARAMETERS

### -DebugMode
Enable detailed debug output

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DetectProfile
Automatically detect the OS profile

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Path to the memory dump file

```yaml
Type: String
Parameter Sets: (All)
Aliases: FilePath, FullName

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -Validate
Validate the memory dump file structure

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
Determines how progress records are handled. Valid values: Continue (default), SilentlyContinue, Stop, Inquire, Ignore, Suspend, and Break.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

## OUTPUTS

### PowerShell.MemoryAnalysis.Models.MemoryDump

## NOTES

Requires a full physical memory image. WER minidumps under 100 MB are not supported. See [DUMP_REQUIREMENTS.md](https://github.com/jondmarien/MemoryAnalysis.Powershell/blob/main/docs/DUMP_REQUIREMENTS.md).

## RELATED LINKS

[Resolve-MemoryDumpPath](Resolve-MemoryDumpPath.md)

[Start-MemoryAnalysis](Start-MemoryAnalysis.md)
