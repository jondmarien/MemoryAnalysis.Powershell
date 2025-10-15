---
external help file: PowerShell.MemoryAnalysis.dll-Help.xml
Module Name: MemoryAnalysis
online version:
schema: 2.0.0
---

# Get-ProcessCommandLine

## SYNOPSIS
{{ Fill in the Synopsis }}

## SYNTAX

```
Get-ProcessCommandLine -MemoryDump <MemoryDump> [-Pid <UInt32>] [-ProcessName <String>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
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

### -MemoryDump
{{ Fill MemoryDump Description }}

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
{{ Fill Pid Description }}

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
{{ Fill ProcessName Description }}

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

### PowerShell.MemoryAnalysis.Models.CommandLineInfo

## NOTES

## RELATED LINKS
