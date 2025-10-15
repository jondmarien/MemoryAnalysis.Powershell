---
inclusion: always
---

# Documentation Standards

## Code Documentation

### Rust Documentation

**Use rustdoc comments for all public APIs:**

```rust
/// Analyzes process tree from a memory dump.
///
/// This function loads a memory dump, extracts the process list using
/// Volatility 3, and builds a hierarchical process tree structure.
///
/// # Arguments
///
/// * `dump_path` - Path to the memory dump file (RAW, VMEM, DMP, or ELF format)
/// * `filter` - Optional process name filter (supports wildcards)
///
/// # Returns
///
/// Returns a `Vec<ProcessInfo>` containing all processes found in the dump.
/// Processes are ordered by PID.
///
/// # Errors
///
/// This function will return an error if:
/// - The dump file cannot be read
/// - The dump format is not recognized
/// - Volatility framework fails to initialize
/// - Process extraction fails
///
/// # Examples
///
/// ```
/// use rust_bridge::analyze_processes;
///
/// // Analyze all processes
/// let processes = analyze_processes("/path/to/dump.vmem", None)?;
/// println!("Found {} processes", processes.len());
///
/// // Filter by name
/// let explorer = analyze_processes("/path/to/dump.vmem", Some("explorer*"))?;
/// ```
///
/// # Performance
///
/// Typical execution time: 2-5 seconds for a 4GB dump.
/// Memory overhead: ~500MB during analysis.
///
/// # Safety
///
/// This function is safe to call from multiple threads, but note that
/// Python GIL acquisition may serialize some operations.
pub fn analyze_processes(
    dump_path: &str,
    filter: Option<&str>
) -> Result<Vec<ProcessInfo>> {
    // Implementation
}
```

### C# XML Documentation

**Use XML comments for all public APIs:**

```csharp
/// <summary>
/// Loads a memory dump file for forensic analysis.
/// </summary>
/// <remarks>
/// This cmdlet loads a memory dump file and prepares it for analysis.
/// Supported formats include RAW, VMEM, DMP, and ELF.
/// 
/// The cmdlet can optionally validate the dump format and detect the
/// operating system profile automatically.
/// </remarks>
/// <example>
/// <code>
/// # Load a memory dump
/// $dump = Get-MemoryDump -Path C:\dumps\memory.vmem
/// 
/// # Load with validation
/// $dump = Get-MemoryDump -Path C:\dumps\memory.raw -Validate
/// 
/// # Auto-detect OS profile
/// $dump = Get-MemoryDump -Path C:\dumps\memory.dmp -DetectProfile
/// </code>
/// </example>
/// <para>
/// For more information about memory dump formats, see:
/// https://volatility3.readthedocs.io/
/// </para>
[Cmdlet(VerbsCommon.Get, "MemoryDump")]
[OutputType(typeof(MemoryDump))]
public class GetMemoryDumpCommand : PSCmdlet
{
    /// <summary>
    /// Gets or sets the path to the memory dump file.
    /// </summary>
    /// <value>
    /// A string containing the full or relative path to the dump file.
    /// </value>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public string Path { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether to validate the dump format.
    /// </summary>
    /// <value>
    /// <c>true</c> to validate the dump format; otherwise, <c>false</c>.
    /// </value>
    [Parameter]
    public SwitchParameter Validate { get; set; }
}
```

## PowerShell Help Documentation

### Comment-Based Help

**Add to each cmdlet file:**

```csharp
/*
.SYNOPSIS
Loads a memory dump file for forensic analysis.

.DESCRIPTION
The Get-MemoryDump cmdlet loads a memory dump file and prepares it for
analysis using the Volatility 3 framework. The cmdlet supports multiple
dump formats including RAW, VMEM, DMP, and ELF.

The cmdlet can optionally validate the dump format and automatically
detect the operating system profile.

.PARAMETER Path
Specifies the path to the memory dump file. This parameter is required
and accepts pipeline input.

.PARAMETER Validate
Validates the dump format and structure. This may increase load time
but ensures the dump is valid before analysis.

.PARAMETER DetectProfile
Automatically detects the operating system profile from the dump.
This is useful when the OS version is unknown.

.INPUTS
System.String
You can pipe a string containing the dump file path to Get-MemoryDump.

.OUTPUTS
PowerShell.MemoryAnalysis.Models.MemoryDump
Returns a MemoryDump object containing dump metadata and analysis context.

.EXAMPLE
PS> Get-MemoryDump -Path C:\dumps\memory.vmem

Loads a memory dump from the specified path.

.EXAMPLE
PS> Get-MemoryDump -Path C:\dumps\memory.raw -Validate

Loads and validates a memory dump file.

.EXAMPLE
PS> Get-ChildItem *.vmem | Get-MemoryDump

Loads multiple memory dumps from the current directory.

.EXAMPLE
PS> $dump = Get-MemoryDump -Path memory.dmp -DetectProfile
PS> $dump.OsProfile

Loads a dump with automatic OS profile detection and displays the profile.

.NOTES
Supported dump formats:
- RAW: Raw physical memory dump
- VMEM: VMware memory snapshot
- DMP: Windows crash dump
- ELF: Linux core dump

Performance considerations:
- Large dumps (>4GB) may take 30+ seconds to load
- Validation adds 10-20% overhead
- Profile detection adds 5-10 seconds

.LINK
Test-ProcessTree

.LINK
Find-Malware

.LINK
Online documentation: https://github.com/yourusername/MemoryAnalysis
*/
```

### External Help Files

**Create `MemoryAnalysis-help.xml`:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<helpItems schema="maml">
  <command:command>
    <command:details>
      <command:name>Get-MemoryDump</command:name>
      <command:verb>Get</command:verb>
      <command:noun>MemoryDump</command:noun>
      <maml:description>
        <maml:para>Loads a memory dump file for forensic analysis.</maml:para>
      </maml:description>
    </command:details>
    <!-- Additional help content -->
  </command:command>
</helpItems>
```

## README Documentation

### Project README Structure

```markdown
# PowerShell Memory Analysis Module

Brief description of the project.

## Features

- Feature 1
- Feature 2
- Feature 3

## Requirements

- PowerShell 7.6+
- .NET 9.0
- Python 3.11+
- Volatility 3

## Installation

### From PowerShell Gallery

```powershell
Install-Module -Name MemoryAnalysis
```

### From Source

```powershell
git clone https://github.com/yourusername/MemoryAnalysis.git
cd MemoryAnalysis
.\build.ps1
Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1
```

## Quick Start

```powershell
# Load a memory dump
$dump = Get-MemoryDump -Path memory.vmem

# Analyze processes
$processes = Test-ProcessTree -MemoryDump $dump

# Scan for malware
$threats = Find-Malware -MemoryDump $dump
```

## Documentation

- [Architecture](docs/architecture.md)
- [Cmdlet Reference](docs/cmdlet-reference.md)
- [Development Guide](docs/development.md)
- [Troubleshooting](docs/troubleshooting.md)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md)

## License

Copyright (c) 2025. All rights reserved.
```

## Architecture Documentation

### Architecture Document Template

```markdown
# Architecture Overview

## System Architecture

[High-level architecture diagram]

## Components

### PowerShell Module (C#)

Description of the PowerShell module layer.

**Responsibilities:**
- Cmdlet implementation
- Parameter validation
- Pipeline integration
- Error handling

**Key Classes:**
- `GetMemoryDumpCommand`
- `TestProcessTreeCommand`
- `FindMalwareCommand`

### Rust Bridge

Description of the Rust bridge layer.

**Responsibilities:**
- Python interpreter management
- FFI exports
- Data marshaling
- Performance optimization

**Key Modules:**
- `python_manager`
- `volatility`
- `process_analysis`

### Volatility Integration

Description of Volatility 3 integration.

## Data Flow

[Data flow diagram]

1. User invokes PowerShell cmdlet
2. Cmdlet calls Rust bridge via P/Invoke
3. Rust bridge calls Python/Volatility
4. Results flow back through layers
5. PowerShell formats and displays output

## Design Decisions

### Why Rust for the Bridge?

Rationale for using Rust...

### Why Not Direct Python Integration?

Rationale for the architecture choice...

## Performance Characteristics

- Rust-Python overhead: <100ms
- Memory overhead: <1GB per dump
- Throughput: 1000+ processes/second

## Security Considerations

- Input validation
- Memory safety
- Error handling
- Privilege requirements
```

## API Documentation

### Generate API Docs

**Rust:**
```bash
cargo doc --no-deps --open
```

**C#:**
```bash
dotnet tool install -g docfx
docfx init
docfx build
```

## Inline Comments

### When to Comment

**DO comment:**
- Complex algorithms
- Non-obvious optimizations
- Workarounds for bugs
- Security-critical code
- Performance-critical sections

**DON'T comment:**
- Obvious code
- What the code does (use good names instead)
- Commented-out code (use version control)

### Good Comments

```rust
// SAFETY: This is safe because we've validated the pointer is non-null
// and points to a valid C string allocated by our code.
let path = unsafe { CStr::from_ptr(path_ptr).to_str()? };

// Performance: Using a BTreeMap here instead of HashMap because we need
// ordered iteration for the process tree display. Benchmarks show this
// is 15% faster for our typical use case (1000-5000 processes).
let mut process_map = BTreeMap::new();

// Workaround: Volatility 3.2.0 has a bug where it returns None for
// process names longer than 255 characters. We truncate here to avoid
// panics. See: https://github.com/volatilityfoundation/volatility3/issues/XXX
let name = process_name.chars().take(255).collect();
```

### Bad Comments

```rust
// Bad: States the obvious
// Increment counter
counter += 1;

// Bad: Explains what, not why
// Loop through processes
for process in processes {
    // ...
}

// Bad: Outdated comment
// TODO: Add error handling (already done)
let result = analyze_dump(path)?;
```

## Change Documentation

### CHANGELOG.md

```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- New feature X
- Support for Y

### Changed
- Improved performance of Z

### Fixed
- Bug in process tree analysis

## [1.0.0] - 2025-01-15

### Added
- Initial release
- Get-MemoryDump cmdlet
- Test-ProcessTree cmdlet
- Find-Malware cmdlet

[Unreleased]: https://github.com/user/repo/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/user/repo/releases/tag/v1.0.0
```

## Documentation Checklist

### For Each Feature

- [ ] Rust API documentation
- [ ] C# XML documentation
- [ ] PowerShell comment-based help
- [ ] README examples
- [ ] Architecture documentation (if significant)
- [ ] CHANGELOG entry
- [ ] Migration guide (if breaking change)

### For Each Release

- [ ] Update version numbers
- [ ] Update CHANGELOG
- [ ] Update README
- [ ] Generate API documentation
- [ ] Create release notes
- [ ] Update examples
- [ ] Test all documentation links
