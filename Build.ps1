#!/usr/bin/env pwsh
# Build script for PowerShell Memory Analysis Module

param(
    [switch]$Clean,
    [switch]$SkipRust,
    [switch]$SkipCSharp,
    [switch]$SkipTests
)

Write-Host "=== PowerShell Memory Analysis Module Build Script ===" -ForegroundColor Cyan
Write-Host ""

# Set environment variables
$env:PYTHONPATH = "J:\projects\personal-projects\MemoryAnalysis\.venv\Lib\site-packages"
$env:PYO3_PYTHON = "J:\projects\personal-projects\MemoryAnalysis\.venv\Scripts\python.exe"
$env:PATH = "J:\projects\personal-projects\MemoryAnalysis\.venv\Scripts;" + $env:PATH

# Step 1: Build Rust bridge
if (-not $SkipRust) {
    Write-Host "Step 1: Building Rust bridge..." -ForegroundColor Yellow
    Push-Location rust-bridge
    
    try {
        if ($Clean) {
            Write-Host "  Cleaning Rust build artifacts..." -ForegroundColor Gray
            cargo clean
        }
        
        Write-Host "  Building release version..." -ForegroundColor Gray
        cargo build --release
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Rust bridge built successfully" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Rust build failed!" -ForegroundColor Red
            Pop-Location
            exit 1
        }
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "Step 1: Skipping Rust build" -ForegroundColor Gray
}

# Step 2: Copy Rust DLL to C# publish directory
Write-Host ""
Write-Host "Step 2: Copying Rust bridge DLL..." -ForegroundColor Yellow
try {
    Copy-Item -Path "rust-bridge\target\release\rust_bridge.dll" `
              -Destination "PowerShell.MemoryAnalysis\publish\rust_bridge.dll" `
              -Force
    Write-Host "  ✓ DLL copied successfully" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ Failed to copy DLL: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Copy Python DLL
Write-Host ""
Write-Host "Step 3: Copying Python DLL..." -ForegroundColor Yellow
try {
    $pythonDll = "C:\Users\nucle\AppData\Roaming\uv\python\cpython-3.12.11-windows-x86_64-none\python312.dll"
    if (Test-Path $pythonDll) {
        Copy-Item -Path $pythonDll `
                  -Destination "PowerShell.MemoryAnalysis\publish\python312.dll" `
                  -Force
        Write-Host "  ✓ Python DLL copied successfully" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Python DLL not found at expected location" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  ⚠ Failed to copy Python DLL: $_" -ForegroundColor Yellow
}

# Step 4: Build C# module
if (-not $SkipCSharp) {
    Write-Host ""
    Write-Host "Step 4: Building C# module..." -ForegroundColor Yellow
    
    try {
        $result = dotnet publish PowerShell.MemoryAnalysis\PowerShell.MemoryAnalysis.csproj `
                         -o PowerShell.MemoryAnalysis\publish `
                         --no-restore `
                         2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ C# module built successfully" -ForegroundColor Green
        } else {
            Write-Host "  ✗ C# build failed!" -ForegroundColor Red
            Write-Host $result -ForegroundColor Red
            exit 1
        }
    }
    catch {
        Write-Host "  ✗ C# build failed: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host ""
    Write-Host "Step 4: Skipping C# build" -ForegroundColor Gray
}

# Step 5: Run tests
if (-not $SkipTests) {
    Write-Host ""
    Write-Host "Step 5: Running basic smoke test..." -ForegroundColor Yellow
    
    try {
        $testResult = pwsh -NoProfile -Command {
            $env:PYTHONPATH = 'J:\projects\personal-projects\MemoryAnalysis\.venv\Lib\site-packages'
            Import-Module 'J:\projects\personal-projects\MemoryAnalysis\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1' -Force -ErrorAction Stop
            $commands = Get-Command -Module MemoryAnalysis
            Write-Output "Loaded $($commands.Count) cmdlets: $($commands.Name -join ', ')"
        }
        
        Write-Host "  $testResult" -ForegroundColor Gray
        Write-Host "  ✓ Module loads successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Module test failed: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host ""
    Write-Host "Step 5: Skipping tests" -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "To test the module, run:" -ForegroundColor White
Write-Host "  `$env:PYTHONPATH = 'J:\projects\personal-projects\MemoryAnalysis\.venv\Lib\site-packages'" -ForegroundColor Gray
Write-Host "  Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1 -Force" -ForegroundColor Gray
Write-Host ""
Write-Host "To run comprehensive tests:" -ForegroundColor White
Write-Host "  `$env:PYTHONPATH = 'J:\projects\personal-projects\MemoryAnalysis\.venv\Lib\site-packages'" -ForegroundColor Gray
Write-Host "  .\Test-AllCmdlets.ps1" -ForegroundColor Gray
Write-Host ""
