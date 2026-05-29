---
external help file: PowerShell.MemoryAnalysis.dll-Help.xml
Module Name: MemoryAnalysis
online version: https://github.com/jondmarien/MemoryAnalysis.Powershell
schema: 2.0.0
---

# Test-ProcessTree

## SYNOPSIS

Analyzes process hierarchies in a memory dump.

## SYNTAX

```
Test-ProcessTree [-MemoryDump] <MemoryDump> [-Pid <UInt32>] [-ProcessName <String>] [-ParentPid <UInt32>]
 [-Format <String>] [-IncludeCommandLine] [-FlagSuspicious] [-DebugMode] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION

The `Test-ProcessTree` cmdlet (alias `Analyze-ProcessTree`) walks the process tree in a loaded memory dump via Volatility 3. It reports parent-child relationships and can flag suspicious processes. Filter by PID, process name, or parent PID, and choose **Tree**, **Flat**, or **JSON** output.

## EXAMPLES

### Example 1: Analyze all processes

```powershell
PS C:\> Get-MemoryDump -Path C:\dumps\memory.vmem | Test-ProcessTree
```

Returns process tree information for every process in the dump.

### Example 2: Tree view with suspicious flagging

```powershell
PS C:\> Test-ProcessTree -MemoryDump $dump -Format Tree -FlagSuspicious
```

Displays a hierarchical tree and marks processes that match suspicious heuristics.

### Example 3: Filter by process name

```powershell
PS C:\> Test-ProcessTree -MemoryDump $dump -ProcessName "powershell*"
```

Limits results to processes whose names match the wildcard pattern.

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

### PowerShell.MemoryAnalysis.Models.ProcessTreeInfo

## NOTES

Alias: **Analyze-ProcessTree**

## RELATED LINKS

[Get-MemoryDump](Get-MemoryDump.md)

[Get-ProcessCommandLine](Get-ProcessCommandLine.md)
