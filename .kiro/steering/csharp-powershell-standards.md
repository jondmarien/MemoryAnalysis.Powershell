---
inclusion: fileMatch
fileMatchPattern: "PowerShell.MemoryAnalysis/**/*.cs"
---

# C# PowerShell Module Development Standards

## Project Structure

```
PowerShell.MemoryAnalysis/
├── Cmdlets/              # PowerShell cmdlet implementations
├── Models/               # Data models and DTOs
├── Services/             # Business logic and interop
├── MemoryAnalysis.psd1   # Module manifest
└── MemoryAnalysis.Format.ps1xml  # Custom formatting
```

## Cmdlet Development Standards

### Cmdlet Class Structure

**ALWAYS follow this pattern:**

```csharp
using System;
using System.Management.Automation;
using Microsoft.Extensions.Logging;

namespace PowerShell.MemoryAnalysis.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "MemoryDump")]
    [OutputType(typeof(MemoryDump))]
    public class GetMemoryDumpCommand : PSCmdlet
    {
        // Parameters
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }
        
        // Services
        private ILogger<GetMemoryDumpCommand> _logger;
        private RustInteropService _rustInterop;
        
        // Lifecycle methods
        protected override void BeginProcessing()
        {
            _logger = LoggingService.GetLogger<GetMemoryDumpCommand>();
            _rustInterop = new RustInteropService();
        }
        
        protected override void ProcessRecord()
        {
            try
            {
                // Implementation
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }
        
        protected override void EndProcessing()
        {
            // Cleanup
        }
        
        private void HandleError(Exception ex)
        {
            _logger.LogError(ex, "Error in cmdlet");
            WriteError(new ErrorRecord(
                ex,
                "ErrorId",
                ErrorCategory.InvalidOperation,
                Path
            ));
        }
    }
}
```

### Parameter Standards

**Use appropriate validation attributes:**

```csharp
// Required string parameter
[Parameter(Mandatory = true, Position = 0)]
[ValidateNotNullOrEmpty]
public string Path { get; set; }

// Optional switch parameter
[Parameter]
public SwitchParameter Validate { get; set; }

// Enum parameter with validation
[Parameter]
[ValidateSet("Tree", "Flat", "JSON")]
public string Format { get; set; } = "Flat";

// Pipeline input
[Parameter(ValueFromPipeline = true)]
public MemoryDump MemoryDump { get; set; }

// Array parameter
[Parameter]
[ValidateNotNullOrEmpty]
public string[] ProcessNames { get; set; }
```

### Progress Reporting

**ALWAYS report progress for long-running operations:**

```csharp
protected override void ProcessRecord()
{
    var progress = new ProgressRecord(
        activityId: 1,
        activity: "Loading Memory Dump",
        statusDescription: $"Processing {Path}"
    );
    
    progress.PercentComplete = 0;
    WriteProgress(progress);
    
    // Do work...
    
    progress.PercentComplete = 50;
    progress.StatusDescription = "Validating dump format";
    WriteProgress(progress);
    
    // More work...
    
    progress.PercentComplete = 100;
    progress.RecordType = ProgressRecordType.Completed;
    WriteProgress(progress);
}
```

### Error Handling

**Use appropriate error categories:**

```csharp
private void HandleError(Exception ex, string path)
{
    ErrorCategory category = ex switch
    {
        FileNotFoundException => ErrorCategory.ObjectNotFound,
        UnauthorizedAccessException => ErrorCategory.PermissionDenied,
        InvalidDataException => ErrorCategory.InvalidData,
        _ => ErrorCategory.InvalidOperation
    };
    
    var errorRecord = new ErrorRecord(
        exception: ex,
        errorId: $"{GetType().Name}.{category}",
        errorCategory: category,
        targetObject: path
    );
    
    WriteError(errorRecord);
}
```

### Verbose and Debug Output

```csharp
protected override void ProcessRecord()
{
    WriteVerbose($"Loading memory dump from: {Path}");
    WriteDebug($"Dump size: {new FileInfo(Path).Length} bytes");
    
    // Implementation
    
    WriteVerbose("Memory dump loaded successfully");
}
```

## Rust Interop Standards

### P/Invoke Declarations

**Place all P/Invoke in `Services/RustInterop.cs`:**

```csharp
using System;
using System.Runtime.InteropServices;

namespace PowerShell.MemoryAnalysis.Services
{
    public class RustInteropService
    {
        private const string DllName = "rust_bridge";
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int analyze_processes(
            [MarshalAs(UnmanagedType.LPStr)] string dumpPath,
            out IntPtr outJson,
            out int outLen
        );
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_string(IntPtr ptr);
        
        public ProcessInfo[] AnalyzeProcesses(string dumpPath)
        {
            int result = analyze_processes(
                dumpPath,
                out IntPtr jsonPtr,
                out int jsonLen
            );
            
            if (result != 0)
            {
                throw new InvalidOperationException(
                    $"Rust bridge returned error code: {result}"
                );
            }
            
            try
            {
                string json = Marshal.PtrToStringAnsi(jsonPtr, jsonLen);
                return JsonSerializer.Deserialize<ProcessInfo[]>(json);
            }
            finally
            {
                free_string(jsonPtr);
            }
        }
    }
}
```

### Memory Management

**ALWAYS free unmanaged memory:**

```csharp
public string GetProcessList(string dumpPath)
{
    IntPtr ptr = IntPtr.Zero;
    try
    {
        ptr = rust_get_process_list(dumpPath);
        if (ptr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Rust function returned null");
        }
        return Marshal.PtrToStringAnsi(ptr);
    }
    finally
    {
        if (ptr != IntPtr.Zero)
        {
            rust_free_string(ptr);
        }
    }
}
```

## Model Standards

### Data Models

**Use records for immutable data:**

```csharp
namespace PowerShell.MemoryAnalysis.Models
{
    public record MemoryDump
    {
        public string Path { get; init; }
        public string Format { get; init; }
        public long Size { get; init; }
        public string OsProfile { get; init; }
        public DateTime LoadedAt { get; init; }
        public bool IsValid { get; init; }
    }
    
    public record ProcessInfo
    {
        public uint Pid { get; init; }
        public uint Ppid { get; init; }
        public string Name { get; init; }
        public string CommandLine { get; init; }
        public DateTime CreateTime { get; init; }
        public uint Threads { get; init; }
        public uint Handles { get; init; }
        public bool IsSuspicious { get; init; }
        public string[] SuspiciousReasons { get; init; }
    }
}
```

### Enumerations

```csharp
public enum MalwareSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum DumpFormat
{
    Raw,
    Crash,
    Vmem,
    Elf,
    Auto
}
```

## Logging Standards

### Logger Configuration

```csharp
using Microsoft.Extensions.Logging;

namespace PowerShell.MemoryAnalysis.Services
{
    public static class LoggingService
    {
        private static ILoggerFactory _loggerFactory;
        
        static LoggingService()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .AddDebug()
                    .SetMinimumLevel(LogLevel.Information);
            });
        }
        
        public static ILogger<T> GetLogger<T>()
        {
            return _loggerFactory.CreateLogger<T>();
        }
    }
}
```

### Logging Patterns

```csharp
protected override void ProcessRecord()
{
    _logger.LogInformation(
        "Loading memory dump from {Path}",
        Path
    );
    
    try
    {
        var dump = _rustInterop.LoadMemoryDump(Path);
        
        _logger.LogDebug(
            "Dump loaded: Size={Size}, Format={Format}",
            dump.Size,
            dump.Format
        );
        
        WriteObject(dump);
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Failed to load memory dump from {Path}",
            Path
        );
        throw;
    }
}
```

## Testing Standards

### Unit Tests with xUnit

```csharp
using Xunit;
using PowerShell.MemoryAnalysis.Cmdlets;

namespace PowerShell.MemoryAnalysis.Tests
{
    public class GetMemoryDumpCommandTests
    {
        [Fact]
        public void ProcessRecord_ValidPath_ReturnsMemoryDump()
        {
            // Arrange
            var cmdlet = new GetMemoryDumpCommand
            {
                Path = "test.vmem"
            };
            
            // Act
            var results = cmdlet.Invoke().ToList();
            
            // Assert
            Assert.Single(results);
            Assert.IsType<MemoryDump>(results[0].BaseObject);
        }
        
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ProcessRecord_InvalidPath_ThrowsException(string path)
        {
            // Arrange
            var cmdlet = new GetMemoryDumpCommand { Path = path };
            
            // Act & Assert
            Assert.Throws<ParameterBindingException>(
                () => cmdlet.Invoke().ToList()
            );
        }
    }
}
```

## Module Manifest Standards

### MemoryAnalysis.psd1

```powershell
@{
    ModuleVersion = '1.0.0'
    GUID = 'your-guid-here'
    Author = 'Your Name'
    CompanyName = 'Your Company'
    Copyright = '(c) 2025. All rights reserved.'
    Description = 'Memory forensics analysis using Volatility 3'
    
    PowerShellVersion = '7.6'
    DotNetFrameworkVersion = '9.0'
    
    RootModule = 'PowerShell.MemoryAnalysis.dll'
    
    FunctionsToExport = @()
    CmdletsToExport = @(
        'Get-MemoryDump',
        'Test-ProcessTree',
        'Find-Malware',
        'Get-VolatilityPlugin'
    )
    VariablesToExport = @()
    AliasesToExport = @('Analyze-ProcessTree')
    
    FormatsToProcess = @('MemoryAnalysis.Format.ps1xml')
    
    PrivateData = @{
        PSData = @{
            Tags = @('Forensics', 'Memory', 'Volatility', 'Security')
            LicenseUri = 'https://github.com/yourusername/MemoryAnalysis/blob/main/LICENSE'
            ProjectUri = 'https://github.com/yourusername/MemoryAnalysis'
            ReleaseNotes = 'Initial release'
        }
    }
}
```

## Code Style

- Use C# 10+ features (records, pattern matching, etc.)
- Follow Microsoft C# coding conventions
- Use nullable reference types
- Prefer `var` for local variables when type is obvious
- Use expression-bodied members for simple properties/methods
- Add XML documentation comments for public APIs

```csharp
/// <summary>
/// Loads a memory dump file for forensic analysis.
/// </summary>
/// <param name="path">Path to the memory dump file.</param>
/// <param name="validate">Whether to validate the dump format.</param>
/// <returns>A MemoryDump object representing the loaded dump.</returns>
/// <exception cref="FileNotFoundException">
/// Thrown when the specified dump file does not exist.
/// </exception>
public MemoryDump LoadMemoryDump(string path, bool validate)
{
    // Implementation
}
```

## Performance Considerations

- Use `Span<T>` and `Memory<T>` for buffer operations
- Avoid unnecessary allocations in hot paths
- Use `StringBuilder` for string concatenation in loops
- Dispose of unmanaged resources properly
- Consider using `ValueTask` for async operations

## Naming Conventions

- Cmdlets: `Verb-Noun` (e.g., `Get-MemoryDump`)
- Classes: `VerbNounCommand` (e.g., `GetMemoryDumpCommand`)
- Private fields: `_camelCase` with underscore prefix
- Properties: `PascalCase`
- Methods: `PascalCase`
- Local variables: `camelCase`
