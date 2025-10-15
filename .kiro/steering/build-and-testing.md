---
inclusion: always
---

# Build and Testing Standards

## Build Process

### Rust Bridge Build

```powershell
# Development build
cd rust-bridge
cargo build

# Release build (optimized)
cargo build --release

# Run tests
cargo test

# Run with code coverage
cargo tarpaulin --out Html
```

### C# Module Build

```powershell
# Build the module
dotnet build PowerShell.MemoryAnalysis/PowerShell.MemoryAnalysis.csproj

# Publish for distribution
dotnet publish PowerShell.MemoryAnalysis/PowerShell.MemoryAnalysis.csproj `
    -c Release `
    -o PowerShell.MemoryAnalysis/publish

# Run C# tests
dotnet test
```

### Full Build Script

Create `build.ps1` in project root:

```powershell
#!/usr/bin/env pwsh

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

Write-Host "Building PowerShell Memory Analysis Module" -ForegroundColor Cyan

# Build Rust bridge
Write-Host "`nBuilding Rust bridge..." -ForegroundColor Yellow
Push-Location rust-bridge
if ($Configuration -eq 'Release') {
    cargo build --release
} else {
    cargo build
}
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    throw "Rust build failed"
}
Pop-Location

# Build C# module
Write-Host "`nBuilding C# module..." -ForegroundColor Yellow
dotnet build PowerShell.MemoryAnalysis/PowerShell.MemoryAnalysis.csproj `
    -c $Configuration

if ($LASTEXITCODE -ne 0) {
    throw "C# build failed"
}

# Copy Rust library to output
$rustLib = if ($Configuration -eq 'Release') {
    "rust-bridge/target/release/rust_bridge.dll"
} else {
    "rust-bridge/target/debug/rust_bridge.dll"
}

$outputDir = "PowerShell.MemoryAnalysis/bin/$Configuration/net9.0"
Copy-Item $rustLib $outputDir -Force

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
```

## Testing Strategy

### Test Coverage Requirements

- **Rust**: Minimum 85% code coverage
- **C#**: Minimum 80% code coverage
- **Integration**: All cmdlets must have integration tests

### Rust Unit Tests

**Location**: Same file as implementation or `rust-bridge/tests/`

```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_process_info_creation() {
        let process = ProcessInfo {
            pid: 1234,
            ppid: 100,
            name: "test.exe".to_string(),
            command_line: "test.exe --arg".to_string(),
            create_time: "2025-01-01T00:00:00".to_string(),
            threads: 4,
            handles: 100,
        };
        
        assert_eq!(process.pid, 1234);
        assert_eq!(process.name, "test.exe");
    }
    
    #[test]
    fn test_json_serialization() {
        let process = create_test_process();
        let json = serde_json::to_string(&process).unwrap();
        
        assert!(json.contains("1234"));
        assert!(json.contains("test.exe"));
    }
    
    #[test]
    #[should_panic(expected = "Invalid path")]
    fn test_invalid_dump_path() {
        load_memory_dump("").unwrap();
    }
}
```

### C# Unit Tests

**Location**: `tests/csharp-tests/`

```csharp
using Xunit;
using PowerShell.MemoryAnalysis.Cmdlets;
using PowerShell.MemoryAnalysis.Models;

namespace PowerShell.MemoryAnalysis.Tests
{
    public class GetMemoryDumpCommandTests
    {
        [Fact]
        public void Constructor_InitializesProperties()
        {
            var cmdlet = new GetMemoryDumpCommand();
            Assert.NotNull(cmdlet);
        }
        
        [Theory]
        [InlineData("test.vmem")]
        [InlineData("C:\\dumps\\memory.raw")]
        public void Path_SetAndGet_ReturnsCorrectValue(string path)
        {
            var cmdlet = new GetMemoryDumpCommand { Path = path };
            Assert.Equal(path, cmdlet.Path);
        }
        
        [Fact]
        public void ProcessRecord_WithValidDump_ReturnsMemoryDump()
        {
            // Arrange
            var cmdlet = new GetMemoryDumpCommand
            {
                Path = "test_data/mini_dump.raw"
            };
            
            // Act
            var results = InvokeCmdlet(cmdlet);
            
            // Assert
            Assert.Single(results);
            var dump = Assert.IsType<MemoryDump>(results[0]);
            Assert.NotNull(dump.Path);
        }
        
        private List<object> InvokeCmdlet(PSCmdlet cmdlet)
        {
            var results = new List<object>();
            // Cmdlet invocation logic
            return results;
        }
    }
}
```

### PowerShell Integration Tests

**Location**: `tests/integration-tests/`

```powershell
# MemoryAnalysis.Tests.ps1

Describe "Get-MemoryDump" {
    BeforeAll {
        Import-Module "$PSScriptRoot/../../PowerShell.MemoryAnalysis/publish/MemoryAnalysis.psd1" -Force
    }
    
    Context "Parameter Validation" {
        It "Requires Path parameter" {
            { Get-MemoryDump } | Should -Throw
        }
        
        It "Accepts valid path" {
            { Get-MemoryDump -Path "test.vmem" -ErrorAction Stop } | 
                Should -Not -Throw
        }
    }
    
    Context "Memory Dump Loading" {
        It "Loads valid memory dump" {
            $dump = Get-MemoryDump -Path "test_data/mini_dump.raw"
            $dump | Should -Not -BeNullOrEmpty
            $dump.Path | Should -Be "test_data/mini_dump.raw"
        }
        
        It "Returns MemoryDump object" {
            $dump = Get-MemoryDump -Path "test_data/mini_dump.raw"
            $dump.GetType().Name | Should -Be "MemoryDump"
        }
    }
    
    Context "Pipeline Support" {
        It "Accepts pipeline input" {
            $result = "test_data/mini_dump.raw" | Get-MemoryDump
            $result | Should -Not -BeNullOrEmpty
        }
    }
}

Describe "Test-ProcessTree" {
    BeforeAll {
        $dump = Get-MemoryDump -Path "test_data/mini_dump.raw"
    }
    
    Context "Process Analysis" {
        It "Returns process list" {
            $processes = Test-ProcessTree -MemoryDump $dump
            $processes | Should -Not -BeNullOrEmpty
            $processes.Count | Should -BeGreaterThan 0
        }
        
        It "Filters by process name" {
            $processes = Test-ProcessTree -MemoryDump $dump -ProcessName "explorer*"
            $processes | Where-Object { $_.Name -like "explorer*" } | 
                Should -Not -BeNullOrEmpty
        }
    }
    
    Context "Output Formatting" {
        It "Supports Tree format" {
            { Test-ProcessTree -MemoryDump $dump -Format Tree } | 
                Should -Not -Throw
        }
        
        It "Supports JSON format" {
            $json = Test-ProcessTree -MemoryDump $dump -Format JSON
            { $json | ConvertFrom-Json } | Should -Not -Throw
        }
    }
}

Describe "Find-Malware" {
    BeforeAll {
        $dump = Get-MemoryDump -Path "test_data/mini_dump.raw"
    }
    
    Context "Malware Detection" {
        It "Runs malware scan" {
            $results = Find-Malware -MemoryDump $dump
            $results | Should -Not -BeNullOrEmpty
        }
        
        It "Filters by severity" {
            $critical = Find-Malware -MemoryDump $dump -Severity Critical
            $critical | ForEach-Object {
                $_.Severity | Should -Be "Critical"
            }
        }
        
        It "Respects confidence threshold" {
            $highConfidence = Find-Malware -MemoryDump $dump -MinimumConfidence 80
            $highConfidence | ForEach-Object {
                $_.Confidence | Should -BeGreaterOrEqual 80
            }
        }
    }
}
```

### Test Data Management

**Create test data directory:**

```
tests/
├── test_data/
│   ├── mini_dump.raw          # Small test dump (< 100MB)
│   ├── windows_dump.vmem      # Windows memory dump
│   ├── linux_dump.elf         # Linux core dump
│   └── README.md              # Test data documentation
```

**Test data README:**

```markdown
# Test Data

## mini_dump.raw
- Size: 50MB
- OS: Windows 10
- Purpose: Basic functionality testing

## windows_dump.vmem
- Size: 500MB
- OS: Windows 11
- Purpose: Full feature testing

## linux_dump.elf
- Size: 300MB
- OS: Ubuntu 22.04
- Purpose: Cross-platform testing
```

## Continuous Integration

### GitHub Actions Workflow

**Location**: `.github/workflows/build-and-test.yml`

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-rust:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Install Rust
      uses: actions-rs/toolchain@v1
      with:
        toolchain: stable
        override: true
    
    - name: Build Rust bridge
      run: cargo build --release
      working-directory: rust-bridge
    
    - name: Run Rust tests
      run: cargo test
      working-directory: rust-bridge
    
    - name: Upload Rust artifacts
      uses: actions/upload-artifact@v3
      with:
        name: rust-bridge-${{ matrix.os }}
        path: rust-bridge/target/release/

  build-csharp:
    needs: build-rust
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Download Rust artifacts
      uses: actions/download-artifact@v3
      with:
        name: rust-bridge-${{ matrix.os }}
        path: rust-bridge/target/release/
    
    - name: Build C# module
      run: dotnet build PowerShell.MemoryAnalysis/PowerShell.MemoryAnalysis.csproj
    
    - name: Run C# tests
      run: dotnet test
    
  integration-tests:
    needs: build-csharp
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Install PowerShell
      run: |
        wget https://github.com/PowerShell/PowerShell/releases/download/v7.6.0-preview.5/powershell_7.6.0-preview.5-1.deb_amd64.deb
        sudo dpkg -i powershell_7.6.0-preview.5-1.deb_amd64.deb
    
    - name: Run Pester tests
      run: pwsh -Command "Invoke-Pester tests/integration-tests/ -Output Detailed"
```

## Performance Testing

### Benchmark Tests

```rust
// rust-bridge/benches/performance.rs
use criterion::{black_box, criterion_group, criterion_main, Criterion};
use rust_bridge::*;

fn benchmark_process_analysis(c: &mut Criterion) {
    c.bench_function("analyze_processes", |b| {
        b.iter(|| {
            analyze_processes(black_box("test_data/mini_dump.raw"))
        })
    });
}

criterion_group!(benches, benchmark_process_analysis);
criterion_main!(benches);
```

### Performance Targets

- Rust-Python FFI overhead: < 100ms
- Memory dump load (4GB): < 30 seconds
- Process tree analysis: < 5 seconds
- Malware scan: < 60 seconds

## Code Quality Tools

### Rust

```bash
# Format code
cargo fmt

# Lint code
cargo clippy -- -D warnings

# Check for security vulnerabilities
cargo audit
```

### C#

```bash
# Format code
dotnet format

# Analyze code
dotnet build /p:TreatWarningsAsErrors=true

# Security scan
dotnet list package --vulnerable
```

### PowerShell

```powershell
# PSScriptAnalyzer
Invoke-ScriptAnalyzer -Path . -Recurse -Settings PSGallery
```

## Documentation Testing

### Ensure all cmdlets have help

```powershell
# Test help availability
Get-Command -Module MemoryAnalysis | ForEach-Object {
    $help = Get-Help $_.Name
    if (-not $help.Synopsis) {
        Write-Warning "$($_.Name) missing synopsis"
    }
    if (-not $help.Examples) {
        Write-Warning "$($_.Name) missing examples"
    }
}
```
