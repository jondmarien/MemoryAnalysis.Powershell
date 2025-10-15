# PowerShell.MemoryAnalysis Module

PowerShell binary module (C#) for memory forensics analysis.

> **Part of:** [MemoryAnalysis.Powershell](https://github.com/jondmarien/MemoryAnalysis.Powershell.git)

## Overview

This is the PowerShell-facing component of the Memory Analysis module. It provides cmdlets that wrap the Rust-Python bridge for memory forensics using Volatility 3.

## Architecture

```text
PowerShell Pipeline
       ↓
┌──────────────────────────┐
│  Cmdlets (this module)   │
│  - Get-MemoryDump        │
│  - Test-ProcessTree      │
│  - Get-ProcessCommandLine│
│  - Get-ProcessDll        │
│  - Find-Malware          │
└──────────────────────────┘
       ↓ P/Invoke
┌──────────────────────────┐
│  RustInteropService.cs   │
│  - P/Invoke declarations │
│  - Memory marshaling     │
│  - JSON deserialization  │
└──────────────────────────┘
       ↓ FFI (C ABI)
┌──────────────────────────┐
│  rust_bridge.dll         │
│  - Volatility 3 bridge   │
└──────────────────────────┘
```

## Project Structure

```tree
PowerShell.MemoryAnalysis/
├── Cmdlets/
│   ├── GetMemoryDumpCommand.cs           # ✅ Production Ready
│   ├── AnalyzeProcessTreeCommand.cs      # ✅ Production Ready
│   ├── GetProcessCommandLineCommand.cs   # ✅ Production Ready
│   ├── GetProcessDllCommand.cs           # ✅ Production Ready
│   ├── GetNetworkConnectionCommand.cs    # ⚠️ Disabled (Win11 26100)
│   └── FindMalwareCommand.cs             # ⚠️ Disabled (Win11 26100)
├── Models/
│   ├── MemoryDump.cs                     # Memory dump metadata
│   ├── ProcessInfo.cs                    # Process information
│   ├── ProcessTreeInfo.cs                # Process tree node
│   ├── CommandLineInfo.cs                # ✅ Complete
│   ├── DllInfo.cs                        # ✅ Complete
│   ├── NetworkConnectionInfo.cs          # ⚠️ Disabled (Win11 26100)
│   └── MalwareDetection.cs               # ⚠️ Disabled (Win11 26100)
├── Services/
│   ├── RustInterop.cs                    # P/Invoke to Rust bridge
│   └── LoggingService.cs                 # Logging configuration
├── MemoryAnalysis.psd1                   # Module manifest
├── MemoryAnalysis.Format.ps1xml          # Custom formatting
└── PowerShell.MemoryAnalysis.csproj      # Project file
```

## Building

### Prerequisites

- .NET 10.0 SDK
- PowerShell 7.6.0 or later (Core only, not Windows PowerShell 5.1)
- Rust bridge DLL (built from `rust-bridge/`)
- Python 3.12+ with Volatility 3

### Build Commands

```powershell
# Debug build
dotnet build PowerShell.MemoryAnalysis.csproj

# Release build
dotnet build PowerShell.MemoryAnalysis.csproj -c Release

# Publish (creates ready-to-use module)
dotnet publish PowerShell.MemoryAnalysis.csproj -c Release -o publish
```

### Import Module

```powershell
Import-Module .\publish\MemoryAnalysis.psd1
```

## Cmdlets

### ✅ Get-MemoryDump

**Status:** Production Ready - Tested with 98GB memory dumps

Loads a memory dump file for analysis.

```csharp
[Cmdlet(VerbsCommon.Get, "MemoryDump")]
[OutputType(typeof(MemoryDump))]
public class GetMemoryDumpCommand : PSCmdlet
```

**Parameters:**

- `Path` (mandatory) - Path to memory dump file
- `Validate` - Validate dump integrity
- `DetectProfile` - Auto-detect OS profile

### ✅ Test-ProcessTree (Analyze-ProcessTree)

**Status:** Production Ready - Extracts 830+ processes from 98GB dumps

Analyzes process hierarchies in a memory dump.

```csharp
[Cmdlet(VerbsDiagnostic.Test, "ProcessTree")]
[Alias("Analyze-ProcessTree")]
[OutputType(typeof(ProcessTreeInfo))]
public class AnalyzeProcessTreeCommand : PSCmdlet
```

**Parameters:**

- `MemoryDump` (mandatory, pipeline) - Memory dump to analyze
- `ProcessName` - Filter by process name (wildcards)
- `Pid` - Filter by specific PID
- `ParentPid` - Filter by parent PID
- `Format` - Output format (Tree, Flat, JSON)
- `FlagSuspicious` - Mark suspicious processes
- `DebugMode` - Enable debug logging

### ✅ Get-ProcessCommandLine

**Status:** Production Ready

Extracts command line arguments for processes.

**Parameters:**
- `MemoryDump` (mandatory, pipeline) - Memory dump to analyze
- `ProcessName` - Filter by process name (wildcards)
- `Pid` - Filter by specific PID

**Integration Guide:** `docs/PHASE2_CMDLINE_INTEGRATION.md`

### ✅ Get-ProcessDll

**Status:** Production Ready

Lists DLLs loaded by processes.

**Parameters:**
- `MemoryDump` (mandatory, pipeline) - Memory dump to analyze
- `Pid` - Optional: filter by specific PID (0 or omit for all processes)
- `DllName` - Optional: filter by DLL name (wildcards)

**Integration Guide:** `docs/PHASE2_DLL_INTEGRATION.md`

### ⚠️ Get-NetworkConnection

**Status:** Disabled - Win11 Build 26100 Incompatibility

Extracts network connections from memory dumps.

**Issue:** Volatility 3 `PagedInvalidAddressException` on Windows 11 Build 26100.

### ⚠️ Find-Malware

**Status:** Disabled - Win11 Build 26100 Incompatibility

Multi-technique malware detection.

**Issue:** Returns zero detections on Windows 11 Build 26100.

## P/Invoke Pattern

All Rust bridge calls follow this pattern:

```csharp
[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
private static extern IntPtr rust_bridge_function_name(
    [MarshalAs(UnmanagedType.LPStr)] string param
);

public Model[] WrapperMethod(string param)
{
    IntPtr ptr = IntPtr.Zero;
    try
    {
        ptr = rust_bridge_function_name(param);
        if (ptr == IntPtr.Zero) throw new InvalidOperationException(...);
        
        string json = MarshalStringFromRust(ptr);
        return JsonSerializer.Deserialize<Model[]>(json);
    }
    finally
    {
        if (ptr != IntPtr.Zero)
            rust_bridge_free_string(ptr);
    }
}
```

**Important:** Always free returned strings with `rust_bridge_free_string()`!

## Data Models

### ProcessInfo (snake_case → PascalCase)

```csharp
public class ProcessInfo
{
    [JsonPropertyName("pid")]
    public uint Pid { get; set; }
    
    [JsonPropertyName("ppid")]
    public uint Ppid { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    // ... other fields with [JsonPropertyName] attributes
}
```

**Note:** Always use `[JsonPropertyName]` to map Rust's snake_case to C#'s PascalCase!

## Testing

### Unit Tests

```powershell
# Test Rust interop
.\Test-RustInterop.ps1

# Test Get-MemoryDump
.\Test-GetMemoryDump.ps1

# Test process tree
.\Test-ProcessTree.ps1
```

### Manual Testing

```powershell
# Load test dump
$dump = Get-MemoryDump -Path F:\physmem.raw

# Test process listing
$procs = Test-ProcessTree -MemoryDump $dump
Write-Host "Found $($procs.Count) processes"

# Test filtering
$explorer = Test-ProcessTree -MemoryDump $dump -ProcessName "explorer*"
$explorer | Format-List
```

## Logging

### Configuration

Logging uses `Microsoft.Extensions.Logging`:

```csharp
var logger = LoggingService.GetLogger<CommandName>();
logger.LogInformation("Message");
logger.LogError(exception, "Error message");
```

### Output

Logs go to:

- PowerShell Verbose stream (`Write-Verbose`)
- Debug output (`Write-Debug`)
- Rust bridge debug log (if enabled)

## Error Handling

Standard PowerShell error handling:

```csharp
try
{
    // Operation
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error description");
    WriteError(new ErrorRecord(
        ex,
        "ErrorIdentifier",
        ErrorCategory.InvalidOperation,
        targetObject));
}
```

## Module Manifest (MemoryAnalysis.psd1)

```powershell
@{
    ModuleVersion = '0.1.0'
    GUID = 'your-guid-here'
    Author = 'Your Name'
    Description = 'Memory forensics analysis using Volatility 3'
    
    PowerShellVersion = '7.6'
    DotNetFrameworkVersion = '10.0'
    
    RootModule = 'PowerShell.MemoryAnalysis.dll'
    
    CmdletsToExport = @(
        'Get-MemoryDump',
        'Test-ProcessTree',
        # Add new cmdlets here
    )
    
    FormatsToProcess = @('MemoryAnalysis.Format.ps1xml')
}
```

## Custom Formatting

Define custom views in `MemoryAnalysis.Format.ps1xml`:

```xml
<View>
    <Name>ProcessTreeInfo</Name>
    <ViewSelectedBy>
        <TypeName>PowerShell.MemoryAnalysis.Models.ProcessTreeInfo</TypeName>
    </ViewSelectedBy>
    <TableControl>
        <TableHeaders>
            <TableColumnHeader>
                <Label>PID</Label>
            </TableColumnHeader>
            <!-- More columns -->
        </TableHeaders>
    </TableControl>
</View>
```

## Dependencies

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.0" />
  <PackageReference Include="PowerShellStandard.Library" Version="7.6.0-preview.5" />
  <PackageReference Include="System.Text.Json" Version="10.0.0" />
</ItemGroup>
```

## Contributing

1. Follow C# conventions and .NET naming guidelines
2. Add XML documentation comments to all public members
3. Include comment-based help for cmdlets
4. Add tests for new cmdlets
5. Update module manifest when adding cmdlets

## License

Copyright (c) 2025. All rights reserved.

## Links

- **Main Project:** [MemoryAnalysis.Powershell](https://github.com/jondmarien/MemoryAnalysis.Powershell.git)
- **Rust Bridge:** [rust-bridge](https://github.com/jondmarien/rust-bridge.git)
- **Volatility 3:** <https://github.com/volatilityfoundation/volatility3>
