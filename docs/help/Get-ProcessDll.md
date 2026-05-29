---
external help file: PowerShell.MemoryAnalysis.dll-Help.xml
Module Name: MemoryAnalysis
online version: https://github.com/jondmarien/MemoryAnalysis.Powershell
schema: 2.0.0
---

# Get-ProcessDll

## SYNOPSIS

Lists DLLs loaded by processes in a memory dump.

## SYNTAX

```
Get-ProcessDll -MemoryDump <MemoryDump> [-Pid <UInt32>] [-ProcessName <String>] [-DllName <String>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

The `Get-ProcessDll` cmdlet uses Volatility 3's DllList plugin to enumerate DLLs loaded in a memory image. Accepts a `MemoryDump` from the pipeline or **-MemoryDump**. Filter by process PID, process name, or DLL name (wildcards supported on name parameters).

## EXAMPLES

### Example 1: List all DLLs

```powershell
PS C:\> Get-MemoryDump -Path C:\dumps\memory.vmem | Get-ProcessDll
```

Returns `DllInfo` records for loaded modules across all processes.

### Example 2: DLLs for one process

```powershell
PS C:\> Get-ProcessDll -MemoryDump $dump -Pid 1234
```

### Example 3: Hunt for a suspicious module name

```powershell
PS C:\> Get-ProcessDll -MemoryDump $dump -DllName "*malware*"
```

Returns only DLL paths matching the wildcard.

## PARAMETERS

### -DllName
Filter by DLL file name or path (wildcards supported).

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
Limit results to modules loaded by the specified process ID.

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

### PowerShell.MemoryAnalysis.Models.DllInfo

## NOTES

## RELATED LINKS

[Get-MemoryDump](Get-MemoryDump.md)

[Test-ProcessTree](Test-ProcessTree.md)
