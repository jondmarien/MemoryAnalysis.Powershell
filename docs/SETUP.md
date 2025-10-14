# PowerShell Memory Analysis Module - Development Setup

## Recommended VS Code Extensions

For optimal development experience, install the following VS Code extensions:

### Core Development

- **rust-analyzer** (`rust-lang.rust-analyzer`) - Rust language support with IntelliSense
- **C#** (`ms-dotnettools.csharp`) - C# language support and IntelliSense
- **PowerShell** (`ms-vscode.powershell`) - PowerShell language support and debugging
- **Python** (`ms-python.python`) - Python language support for Volatility3 integration

### Additional Recommended

- **C# Dev Kit** (`ms-dotnettools.csdevkit`) - Enhanced C# development tools
- **Better TOML** (`bungcip.better-toml`) - TOML file support for Cargo.toml
- **Error Lens** (`usernamehwe.errorlens`) - Inline error highlighting

## Development Environment

### Installed Tools

- **Rust**: 1.74.1 (with PyO3 0.26.0 support)
  - Cargo cache location: `J:\packages\cargo`
- **Python**: 3.12.11 (in virtual environment)
  - Virtual environment: `volatility-env\`
  - Volatility3: 2.26.2
- **.NET SDK**: 9.0
  - PowerShell SDK: 7.6.0-preview.5

### Project Structure

```text
J:\projects\personal-projects\MemoryAnalysis\
├── volatility-env\              (Python 3.12 virtual environment)
├── rust-bridge\                 (Rust library with PyO3)
│   ├── src\
│   │   └── lib.rs
│   └── Cargo.toml
└── PowerShell.MemoryAnalysis\   (.NET 9 class library)
    ├── Class1.cs
    └── PowerShell.MemoryAnalysis.csproj
```

### Quick Start

#### Activate Python Environment

```powershell
.\volatility-env\Scripts\Activate.ps1
```

#### Build Rust Library

```powershell
cd rust-bridge
cargo build
```

#### Build .NET Project

```powershell
cd PowerShell.MemoryAnalysis
dotnet build
```

#### Run Volatility3

```powershell
.\volatility-env\Scripts\vol.exe -h
```

### Environment Variables (Already Configured)

- `CARGO_HOME=J:\packages\cargo`
- `CARGO_INSTALL_ROOT=J:\packages\cargo`
- `RUSTUP_HOME=J:\packages\cargo\rustup`

## Next Steps

Refer to the main development plan for Phase 1 implementation details:

- Implement Rust-Python bridge with PyO3
- Create PowerShell cmdlets in C#
- Integrate Volatility3 functionality

## Troubleshooting

### Python Version

Ensure you're using Python 3.12.11 from the virtual environment:

```powershell
.\volatility-env\Scripts\python.exe --version
```

### Rust Version

Verify Rust 1.74+ is installed:

```powershell
rustc --version
cargo --version
```

### .NET Version

Check .NET 9 SDK:

```powershell
dotnet --version
```
