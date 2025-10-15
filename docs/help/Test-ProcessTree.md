---
external help file: PowerShell.MemoryAnalysis.dll-Help.xml
Module Name: MemoryAnalysis
online version:
schema: 2.0.0
---

# Test-ProcessTree

## SYNOPSIS
{{ Fill in the Synopsis }}

## SYNTAX

```
Test-ProcessTree [-MemoryDump] <MemoryDump> [-Pid <UInt32>] [-ProcessName <String>] [-ParentPid <UInt32>]
 [-Format <String>] [-IncludeCommandLine] [-FlagSuspicious] [-DebugMode] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

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

### -FlagSuspicious
Flag suspicious processes

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

### -Format
Output format: Tree, Flat, or JSON

```yaml
Type: String
Parameter Sets: (All)
Aliases:
Accepted values: Tree, Flat, JSON

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeCommandLine
Include command line arguments

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

### -MemoryDump
Memory dump to analyze

```yaml
Type: MemoryDump
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -ParentPid
Filter by parent Process ID

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases: ParentProcessId

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Pid
Filter by specific Process ID

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases: ProcessId

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProcessName
Filter by process name (supports wildcards)

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
{{ Fill ProgressAction Description }}

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

### PowerShell.MemoryAnalysis.Models.ProcessTreeInfo

## NOTES

## RELATED LINKS
