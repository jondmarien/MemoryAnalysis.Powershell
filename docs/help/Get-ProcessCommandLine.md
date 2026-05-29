---
external help file: PowerShell.MemoryAnalysis.dll-Help.xml
Module Name: MemoryAnalysis
online version: https://github.com/jondmarien/MemoryAnalysis.Powershell
schema: 2.0.0
---

# Get-ProcessCommandLine

## SYNOPSIS

Extracts command line arguments for processes in a memory dump.

## SYNTAX

```
Get-ProcessCommandLine -MemoryDump <MemoryDump> [-Pid <UInt32>] [-ProcessName <String>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

The `Get-ProcessCommandLine` cmdlet uses Volatility 3's CmdLine plugin to recover command-line arguments for processes in a loaded memory dump. Pipe a `MemoryDump` from `Get-MemoryDump`, or pass **-MemoryDump** explicitly. Optional filters narrow results by PID or process name.

## EXAMPLES

### Example 1: All command lines in a dump

```powershell
PS C:\> Get-MemoryDump -Path C:\dumps\memory.vmem | Get-ProcessCommandLine
```

Returns `CommandLineInfo` objects for each process that has a recoverable command line.

### Example 2: Filter by process name

```powershell
PS C:\> Get-ProcessCommandLine -MemoryDump $dump -ProcessName "powershell*"
```

Returns command lines only for processes matching the wildcard.

### Example 3: Single process by PID

```powershell
PS C:\> Get-ProcessCommandLine -MemoryDump $dump -Pid 1234
```

## PARAMETERS

### -MemoryDump
Memory dump object returned by `Get-MemoryDump`.

```yaml
Type: MemoryDump
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Pid
Limit results to a specific process ID.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProcessName
Limit results to processes whose image name matches this pattern (wildcards supported).

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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

### PowerShell.MemoryAnalysis.Models.MemoryDump

## OUTPUTS

### PowerShell.MemoryAnalysis.Models.CommandLineInfo

## NOTES

## RELATED LINKS

[Get-MemoryDump](Get-MemoryDump.md)

[Test-ProcessTree](Test-ProcessTree.md)
