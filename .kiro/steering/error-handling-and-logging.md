---
inclusion: always
---

# Error Handling and Logging Standards

## Error Handling Philosophy

### Fail Fast, Fail Clearly

- Detect errors as early as possible
- Provide clear, actionable error messages
- Include context about what was being attempted
- Log errors before propagating them

### Error Categories

1. **User Errors**: Invalid input, missing files, incorrect parameters
2. **System Errors**: Out of memory, disk full, permission denied
3. **Integration Errors**: Rust bridge failures, Python errors, Volatility issues
4. **Logic Errors**: Unexpected states, assertion failures

## Rust Error Handling

### Error Type Hierarchy

```rust
use thiserror::Error;

#[derive(Error, Debug)]
pub enum MemoryAnalysisError {
    #[error("Memory dump not found: {path}")]
    DumpNotFound { path: String },
    
    #[error("Invalid dump format: {format}. Expected one of: {expected}")]
    InvalidFormat { format: String, expected: String },
    
    #[error("Python interpreter error: {0}")]
    PythonError(String),
    
    #[error("Volatility plugin error: {plugin} - {message}")]
    PluginError { plugin: String, message: String },
    
    #[error("Serialization error: {0}")]
    SerializationError(#[from] serde_json::Error),
    
    #[error("IO error: {0}")]
    IoError(#[from] std::io::Error),
    
    #[error("Analysis failed: {0}")]
    AnalysisError(String),
}

pub type Result<T> = std::result::Result<T, MemoryAnalysisError>;
```

### Error Context

**ALWAYS add context when propagating errors:**

```rust
use anyhow::{Context, Result};

pub fn load_memory_dump(path: &str) -> Result<MemoryDump> {
    let file = std::fs::File::open(path)
        .context(format!("Failed to open memory dump at: {}", path))?;
    
    let metadata = file.metadata()
        .context("Failed to read file metadata")?;
    
    if metadata.len() == 0 {
        anyhow::bail!("Memory dump file is empty: {}", path);
    }
    
    parse_dump(file)
        .context("Failed to parse memory dump format")?
}
```

### Python Error Handling

```rust
use pyo3::prelude::*;

pub fn call_volatility_plugin(plugin_name: &str) -> Result<String> {
    Python::with_gil(|py| {
        let result = py
            .import("volatility3.framework")?
            .call_method1("run_plugin", (plugin_name,))
            .map_err(|e| {
                MemoryAnalysisError::PythonError(
                    format!("Plugin '{}' failed: {}", plugin_name, e)
                )
            })?;
        
        result
            .extract::<String>()
            .map_err(|e| {
                MemoryAnalysisError::SerializationError(
                    format!("Failed to extract result: {}", e)
                )
            })
    })
}
```

### FFI Error Handling

```rust
use std::panic;

#[no_mangle]
pub extern "C" fn analyze_processes(
    dump_path: *const c_char,
    out_json: *mut *mut c_char,
    out_error: *mut *mut c_char
) -> i32 {
    // Catch panics to prevent unwinding across FFI boundary
    let result = panic::catch_unwind(|| {
        // Validate pointers
        if dump_path.is_null() {
            return Err("dump_path is null".to_string());
        }
        
        // Convert C string to Rust string
        let path = unsafe {
            match CStr::from_ptr(dump_path).to_str() {
                Ok(s) => s,
                Err(e) => return Err(format!("Invalid UTF-8: {}", e)),
            }
        };
        
        // Perform analysis
        match analyze_processes_impl(path) {
            Ok(json) => {
                unsafe {
                    *out_json = CString::new(json)
                        .unwrap()
                        .into_raw();
                }
                Ok(())
            }
            Err(e) => Err(format!("Analysis failed: {}", e)),
        }
    });
    
    match result {
        Ok(Ok(())) => 0,  // Success
        Ok(Err(e)) | Err(_) => {
            // Set error message
            let error_msg = match result {
                Ok(Err(e)) => e,
                Err(_) => "Panic occurred during analysis".to_string(),
            };
            
            unsafe {
                *out_error = CString::new(error_msg)
                    .unwrap()
                    .into_raw();
            }
            -1  // Error
        }
    }
}
```

## C# Error Handling

### Exception Handling in Cmdlets

```csharp
protected override void ProcessRecord()
{
    try
    {
        ValidateParameters();
        
        WriteVerbose($"Processing memory dump: {Path}");
        
        var dump = _rustInterop.LoadMemoryDump(Path);
        
        WriteObject(dump);
    }
    catch (FileNotFoundException ex)
    {
        var error = new ErrorRecord(
            ex,
            "MemoryDumpNotFound",
            ErrorCategory.ObjectNotFound,
            Path
        );
        error.ErrorDetails = new ErrorDetails(
            $"Memory dump file not found: {Path}\n" +
            $"Please verify the path and try again."
        );
        WriteError(error);
    }
    catch (UnauthorizedAccessException ex)
    {
        var error = new ErrorRecord(
            ex,
            "AccessDenied",
            ErrorCategory.PermissionDenied,
            Path
        );
        error.ErrorDetails = new ErrorDetails(
            $"Access denied to memory dump: {Path}\n" +
            $"Please check file permissions."
        );
        WriteError(error);
    }
    catch (InvalidDataException ex)
    {
        var error = new ErrorRecord(
            ex,
            "InvalidDumpFormat",
            ErrorCategory.InvalidData,
            Path
        );
        error.ErrorDetails = new ErrorDetails(
            $"Invalid memory dump format: {ex.Message}\n" +
            $"Supported formats: RAW, VMEM, DMP, ELF"
        );
        WriteError(error);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error processing memory dump");
        
        var error = new ErrorRecord(
            ex,
            "UnexpectedError",
            ErrorCategory.InvalidOperation,
            Path
        );
        error.ErrorDetails = new ErrorDetails(
            $"An unexpected error occurred: {ex.Message}\n" +
            $"Please check the logs for more details."
        );
        WriteError(error);
    }
}
```

### Rust Interop Error Handling

```csharp
public class RustInteropService
{
    public ProcessInfo[] AnalyzeProcesses(string dumpPath)
    {
        IntPtr jsonPtr = IntPtr.Zero;
        IntPtr errorPtr = IntPtr.Zero;
        
        try
        {
            int result = analyze_processes(
                dumpPath,
                out jsonPtr,
                out errorPtr
            );
            
            if (result != 0)
            {
                string errorMsg = errorPtr != IntPtr.Zero
                    ? Marshal.PtrToStringAnsi(errorPtr)
                    : "Unknown error from Rust bridge";
                
                throw new InvalidOperationException(
                    $"Rust bridge error: {errorMsg}"
                );
            }
            
            if (jsonPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Rust bridge returned null result"
                );
            }
            
            string json = Marshal.PtrToStringAnsi(jsonPtr);
            
            return JsonSerializer.Deserialize<ProcessInfo[]>(json)
                ?? throw new InvalidOperationException(
                    "Failed to deserialize process information"
                );
        }
        finally
        {
            if (jsonPtr != IntPtr.Zero)
                free_string(jsonPtr);
            if (errorPtr != IntPtr.Zero)
                free_string(errorPtr);
        }
    }
}
```

### Custom Exceptions

```csharp
namespace PowerShell.MemoryAnalysis.Exceptions
{
    public class MemoryDumpException : Exception
    {
        public string DumpPath { get; }
        
        public MemoryDumpException(string message, string dumpPath)
            : base(message)
        {
            DumpPath = dumpPath;
        }
        
        public MemoryDumpException(
            string message,
            string dumpPath,
            Exception innerException)
            : base(message, innerException)
        {
            DumpPath = dumpPath;
        }
    }
    
    public class VolatilityPluginException : Exception
    {
        public string PluginName { get; }
        
        public VolatilityPluginException(string message, string pluginName)
            : base(message)
        {
            PluginName = pluginName;
        }
    }
}
```

## Logging Standards

### Rust Logging

```rust
use log::{debug, info, warn, error};

pub fn analyze_memory_dump(path: &str) -> Result<Analysis> {
    info!("Starting memory dump analysis: {}", path);
    
    debug!("Initializing Python interpreter");
    ensure_python_initialized();
    
    debug!("Loading memory dump from: {}", path);
    let dump = load_dump(path)
        .map_err(|e| {
            error!("Failed to load dump: {}", e);
            e
        })?;
    
    info!("Dump loaded successfully: {} bytes", dump.size);
    
    debug!("Extracting process list");
    let processes = extract_processes(&dump)?;
    
    info!("Found {} processes", processes.len());
    
    if processes.is_empty() {
        warn!("No processes found in memory dump");
    }
    
    Ok(Analysis { processes })
}
```

### C# Logging

```csharp
using Microsoft.Extensions.Logging;

public class GetMemoryDumpCommand : PSCmdlet
{
    private ILogger<GetMemoryDumpCommand> _logger;
    
    protected override void BeginProcessing()
    {
        _logger = LoggingService.GetLogger<GetMemoryDumpCommand>();
        _logger.LogInformation("Get-MemoryDump cmdlet started");
    }
    
    protected override void ProcessRecord()
    {
        _logger.LogDebug(
            "Processing memory dump: Path={Path}, Validate={Validate}",
            Path,
            Validate.IsPresent
        );
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var dump = _rustInterop.LoadMemoryDump(Path, Validate.IsPresent);
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Memory dump loaded successfully: " +
                "Path={Path}, Size={Size}, Format={Format}, " +
                "LoadTime={LoadTime}ms",
                dump.Path,
                dump.Size,
                dump.Format,
                stopwatch.ElapsedMilliseconds
            );
            
            WriteObject(dump);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load memory dump: Path={Path}",
                Path
            );
            throw;
        }
    }
    
    protected override void EndProcessing()
    {
        _logger.LogInformation("Get-MemoryDump cmdlet completed");
    }
}
```

### Log Levels

Use appropriate log levels:

- **Trace**: Very detailed diagnostic information
- **Debug**: Detailed information for debugging
- **Information**: General informational messages
- **Warning**: Potentially harmful situations
- **Error**: Error events that might still allow the application to continue
- **Critical**: Very severe error events that might cause termination

### Structured Logging

```csharp
_logger.LogInformation(
    "Analysis completed: " +
    "DumpPath={DumpPath}, " +
    "ProcessCount={ProcessCount}, " +
    "SuspiciousCount={SuspiciousCount}, " +
    "Duration={Duration}ms",
    dumpPath,
    processCount,
    suspiciousCount,
    duration
);
```

## Error Messages

### User-Friendly Messages

**Bad:**
```
Error: ENOENT
```

**Good:**
```
Memory dump file not found: C:\dumps\memory.vmem

Please verify that:
1. The file path is correct
2. The file exists
3. You have permission to access the file

For more information, run: Get-Help Get-MemoryDump -Examples
```

### Include Remediation Steps

```csharp
error.ErrorDetails = new ErrorDetails(
    $"Failed to load memory dump: {ex.Message}\n\n" +
    $"Troubleshooting steps:\n" +
    $"1. Verify the file exists: Test-Path '{Path}'\n" +
    $"2. Check file permissions\n" +
    $"3. Ensure the file is a valid memory dump format\n" +
    $"4. Try running with -Validate switch for detailed diagnostics\n\n" +
    $"For more help: Get-Help Get-MemoryDump -Detailed"
);
```

## Debugging Support

### Verbose Output

```csharp
protected override void ProcessRecord()
{
    WriteVerbose($"Loading memory dump from: {Path}");
    WriteVerbose($"Validation enabled: {Validate.IsPresent}");
    
    var dump = _rustInterop.LoadMemoryDump(Path);
    
    WriteVerbose($"Dump format detected: {dump.Format}");
    WriteVerbose($"Dump size: {dump.Size:N0} bytes");
    WriteVerbose($"OS profile: {dump.OsProfile}");
    
    WriteObject(dump);
}
```

### Debug Output

```csharp
WriteDebug($"Rust bridge version: {_rustInterop.GetVersion()}");
WriteDebug($"Python version: {_rustInterop.GetPythonVersion()}");
WriteDebug($"Volatility version: {_rustInterop.GetVolatilityVersion()}");
```

### Progress Reporting

```csharp
var progress = new ProgressRecord(1, "Analyzing Memory Dump", "Initializing");

progress.PercentComplete = 0;
WriteProgress(progress);

// Load dump
progress.StatusDescription = "Loading memory dump";
progress.PercentComplete = 25;
WriteProgress(progress);

// Analyze
progress.StatusDescription = "Analyzing processes";
progress.PercentComplete = 50;
WriteProgress(progress);

// Complete
progress.StatusDescription = "Complete";
progress.PercentComplete = 100;
progress.RecordType = ProgressRecordType.Completed;
WriteProgress(progress);
```
