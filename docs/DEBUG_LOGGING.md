# Debug Logging Guide

This document describes how to enable and use debug logging in the PowerShell Memory Analysis Module.

## Overview

The module supports configurable debug logging at both the Rust bridge and PowerShell cmdlet layers. Debug logging helps troubleshoot issues, understand internal operations, and diagnose problems during memory analysis.

## Enabling Debug Logging

### Method 1: PowerShell Cmdlet Parameter

All cmdlets support a `-Debug` parameter that enables detailed debug output:

```powershell
# Enable debug logging for Get-MemoryDump
Get-MemoryDump -Path F:\physmem.raw -Debug

# Enable debug logging for Analyze-ProcessTree
Get-MemoryDump -Path F:\physmem.raw | Analyze-ProcessTree -Debug

# Enable debug logging for the entire pipeline
Get-MemoryDump -Path F:\physmem.raw -Debug | Analyze-ProcessTree -Debug -FlagSuspicious
```

### Method 2: Environment Variable

Set the `RUST_BRIDGE_DEBUG` environment variable to enable Rust bridge debug logging:

```powershell
# Enable for current session
$env:RUST_BRIDGE_DEBUG = "1"

# Enable permanently (Windows)
[System.Environment]::SetEnvironmentVariable("RUST_BRIDGE_DEBUG", "1", "User")

# Disable debug logging
$env:RUST_BRIDGE_DEBUG = "0"
```

**Accepted values:** `1`, `true`, `yes`, `on` (case-insensitive)

### Method 3: PowerShell Verbose Preference

Use PowerShell's built-in verbose preference to control output verbosity:

```powershell
# Enable verbose output for current session
$VerbosePreference = "Continue"

# Use -Verbose flag on specific cmdlets
Get-MemoryDump -Path F:\physmem.raw -Verbose

# Combine with -Debug for maximum detail
Get-MemoryDump -Path F:\physmem.raw -Verbose -Debug
```

## What Gets Logged

### Rust Bridge Debug Output

When enabled, the Rust bridge logs:

- Python interpreter initialization and version
- Volatility 3 framework loading and configuration
- Dump file path and URL conversion
- Automagics execution progress
- Plugin construction and execution
- TreeGrid traversal and data extraction
- Process count and data parsing details
- Error messages with stack traces

**Log location:** `J:\projects\personal-projects\MemoryAnalysis\rust-bridge-debug.log`

### PowerShell Cmdlet Verbose Output

When `-Debug` or `-Verbose` is enabled:

- Rust interop service initialization status
- Number of raw process entries retrieved
- Conversion to PowerShell objects
- Filter application results
- Suspicious process detection findings
- Progress through analysis phases

## Example Usage Scenarios

### Debugging Failed Process Analysis

```powershell
# Enable all debug output
$env:RUST_BRIDGE_DEBUG = "1"
$VerbosePreference = "Continue"

$dump = Get-MemoryDump -Path F:\physmem.raw -Debug -Verbose
$processes = $dump | Analyze-ProcessTree -Debug -Verbose

# Check the Rust bridge log file
Get-Content J:\projects\personal-projects\MemoryAnalysis\rust-bridge-debug.log -Tail 50
```

### Troubleshooting Plugin Execution

```powershell
# Enable Rust debug logging only
$env:RUST_BRIDGE_DEBUG = "1"

# Run analysis
Get-MemoryDump -Path F:\physmem.raw | Analyze-ProcessTree

# Review detailed Rust bridge logs
notepad J:\projects\personal-projects\MemoryAnalysis\rust-bridge-debug.log
```

### Development and Testing

```powershell
# Clean debug log before test run
Remove-Item J:\projects\personal-projects\MemoryAnalysis\rust-bridge-debug.log -ErrorAction SilentlyContinue

# Run with all debug output
$env:RUST_BRIDGE_DEBUG = "1"
Get-MemoryDump -Path F:\physmem.raw -Debug | Analyze-ProcessTree -Debug -FlagSuspicious | Format-Table

# Analyze the log
Get-Content J:\projects\personal-projects\MemoryAnalysis\rust-bridge-debug.log
```

## Performance Impact

Debug logging has minimal performance impact:

- **When disabled:** No overhead (early returns in logging functions)
- **When enabled:** Small I/O overhead for file writes and stderr output

For production use, keep debug logging disabled unless troubleshooting.

## Debug Log Format

Rust bridge logs use the following format:

```
[YYYY-MM-DD HH:MM:SS] Message text
```

Example:
```
[2024-01-15 14:30:22] Processing dump: F:\physmem.raw
[2024-01-15 14:30:23] Successfully extracted 830 processes
```

## Disabling Debug Logging

```powershell
# Disable environment variable
$env:RUST_BRIDGE_DEBUG = "0"
# or
Remove-Item Env:\RUST_BRIDGE_DEBUG

# Stop using -Debug parameter (default)
Get-MemoryDump -Path F:\physmem.raw

# Reset verbose preference
$VerbosePreference = "SilentlyContinue"
```

## Best Practices

1. **Enable only when needed:** Debug logging is verbose and can clutter output
2. **Use progressive debugging:** Start with `-Verbose`, add `-Debug` if more detail is needed
3. **Check log files:** Rust bridge errors may only appear in the debug log file
4. **Clean logs periodically:** The debug log appends indefinitely; clean it before important test runs
5. **Combine methods:** Use environment variable + cmdlet parameter for full visibility

## Troubleshooting

### Debug output not appearing

```powershell
# Verify environment variable is set correctly
$env:RUST_BRIDGE_DEBUG

# Expected output: "1" or "true" or "yes" or "on"
```

### Log file not created

- Check write permissions on the project directory
- Ensure the Rust bridge DLL was rebuilt with the latest changes
- Verify the log path in the source code matches your environment

### Too much output

```powershell
# Use -Verbose alone without -Debug
Get-MemoryDump -Path F:\physmem.raw -Verbose

# Or redirect debug output
Get-MemoryDump -Path F:\physmem.raw -Debug 2> debug.txt
```

## See Also

- [PowerShell About Preference Variables](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_preference_variables)
- [Volatility 3 Documentation](https://volatility3.readthedocs.io/)
- Project README and development plan
