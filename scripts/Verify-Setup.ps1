#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies the PowerShell Memory Analysis Module development environment setup.

.DESCRIPTION
    This script checks all required dependencies and their versions for the
    PowerShell Memory Analysis Module project, including Rust, .NET, Python,
    and Volatility3.

.EXAMPLE
    .\Verify-Setup.ps1
    Runs all verification checks and displays a summary.

.EXAMPLE
    .\Verify-Setup.ps1 -Verbose
    Runs all checks with detailed output.

.NOTES
    Project: PowerShell Memory Analysis Module
    Location: J:\projects\personal-projects\MemoryAnalysis
#>

[CmdletBinding()]
param()

# Color helpers
function Write-Success {
    param([string]$Message)
    Write-Host "✅ " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Failure {
    param([string]$Message)
    Write-Host "❌ " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

function Write-SectionHeader {
    param([string]$Title)
    Write-Host "`n" -NoNewline
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " $Title" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
}

# Results tracking
$script:FailureCount = 0
$script:SuccessCount = 0
$script:Results = @()

function Add-Result {
    param(
        [string]$Component,
        [bool]$Success,
        [string]$Details,
        [string]$Expected = "",
        [string]$Actual = ""
    )
    
    $script:Results += [PSCustomObject]@{
        Component = $Component
        Success   = $Success
        Details   = $Details
        Expected  = $Expected
        Actual    = $Actual
    }
    
    if ($Success) {
        $script:SuccessCount++
        Write-Success "$Component : $Details"
    } else {
        $script:FailureCount++
        Write-Failure "$Component : $Details"
        if ($Expected) {
            Write-Host "    Expected: $Expected" -ForegroundColor Yellow
            Write-Host "    Actual: $Actual" -ForegroundColor Yellow
        }
    }
}

# Start verification
Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║  PowerShell Memory Analysis Module - Environment Verification ║" -ForegroundColor Magenta
Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host "  Project Location: $PWD" -ForegroundColor Gray
Write-Host "  Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

#region Rust & Cargo Verification
Write-SectionHeader "Rust Toolchain"

try {
    $rustVersion = rustc --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        $versionMatch = $rustVersion -match "rustc (\d+\.\d+\.\d+)"
        if ($versionMatch) {
            $version = $matches[1]
            $major, $minor, $patch = $version.Split('.')
            if ([int]$major -ge 1 -and [int]$minor -ge 74) {
                Add-Result -Component "Rust" -Success $true -Details "rustc $version" -Expected "1.74.0+" -Actual $version
            } else {
                Add-Result -Component "Rust" -Success $false -Details "Version too old" -Expected "1.74.0+" -Actual $version
            }
        }
    }
} catch {
    Add-Result -Component "Rust" -Success $false -Details "Not installed or not in PATH" -Expected "1.74.0+"
}

try {
    $cargoVersion = cargo --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Add-Result -Component "Cargo" -Success $true -Details $cargoVersion
    }
} catch {
    Add-Result -Component "Cargo" -Success $false -Details "Not installed or not in PATH"
}

# Verify Cargo environment variables
$cargoHome = $env:CARGO_HOME
$cargoInstallRoot = $env:CARGO_INSTALL_ROOT
$rustupHome = $env:RUSTUP_HOME

if ($cargoHome -eq "J:\packages\cargo") {
    Add-Result -Component "CARGO_HOME" -Success $true -Details $cargoHome
} else {
    Add-Result -Component "CARGO_HOME" -Success $false -Details "Not set correctly" -Expected "J:\packages\cargo" -Actual $cargoHome
}

if ($cargoInstallRoot -eq "J:\packages\cargo") {
    Add-Result -Component "CARGO_INSTALL_ROOT" -Success $true -Details $cargoInstallRoot
} else {
    Add-Result -Component "CARGO_INSTALL_ROOT" -Success $false -Details "Not set correctly" -Expected "J:\packages\cargo" -Actual $cargoInstallRoot
}

if ($rustupHome -eq "J:\packages\cargo\rustup") {
    Add-Result -Component "RUSTUP_HOME" -Success $true -Details $rustupHome
} else {
    Add-Result -Component "RUSTUP_HOME" -Success $false -Details "Not set correctly" -Expected "J:\packages\cargo\rustup" -Actual $rustupHome
}
#endregion

#region .NET SDK Verification
Write-SectionHeader ".NET SDK"

try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        $versionMatch = $dotnetVersion -match "(\d+\.\d+\.\d+)"
        if ($versionMatch) {
            $version = $matches[1]
            $major = $version.Split('.')[0]
            if ([int]$major -ge 9) {
                Add-Result -Component ".NET SDK" -Success $true -Details "Version $version" -Expected "9.0.0+" -Actual $version
            } else {
                Add-Result -Component ".NET SDK" -Success $false -Details "Version too old" -Expected "9.0.0+" -Actual $version
            }
        }
    }
} catch {
    Add-Result -Component ".NET SDK" -Success $false -Details "Not installed or not in PATH" -Expected "9.0.0+"
}

# Verify .NET project exists and builds
$csprojPath = "PowerShell.MemoryAnalysis\PowerShell.MemoryAnalysis.csproj"
if (Test-Path $csprojPath) {
    Add-Result -Component ".NET Project" -Success $true -Details "PowerShell.MemoryAnalysis.csproj exists"
    
    try {
        $buildResult = dotnet build $csprojPath --verbosity quiet 2>&1
        if ($LASTEXITCODE -eq 0) {
            Add-Result -Component ".NET Build" -Success $true -Details "Project builds successfully"
        } else {
            Add-Result -Component ".NET Build" -Success $false -Details "Build failed"
        }
    } catch {
        Add-Result -Component ".NET Build" -Success $false -Details "Build error: $_"
    }
} else {
    Add-Result -Component ".NET Project" -Success $false -Details "PowerShell.MemoryAnalysis.csproj not found"
}

# Check PowerShell SDK package
$csprojContent = Get-Content $csprojPath -Raw -ErrorAction SilentlyContinue
if ($csprojContent -match 'Microsoft\.PowerShell\.SDK.*?Version="([^"]+)"') {
    $psVersion = $matches[1]
    Add-Result -Component "PowerShell SDK" -Success $true -Details "Version $psVersion" -Expected "7.6.0-preview.5" -Actual $psVersion
} else {
    Add-Result -Component "PowerShell SDK" -Success $false -Details "Package not found in project"
}
#endregion

#region Python & Volatility3 Verification
Write-SectionHeader "Python Environment"

$pythonExe = ".\volatility-env\Scripts\python.exe"

if (Test-Path $pythonExe) {
    try {
        $pythonVersion = & $pythonExe --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            $versionMatch = $pythonVersion -match "Python (\d+\.\d+\.\d+)"
            if ($versionMatch) {
                $version = $matches[1]
                $major, $minor = $version.Split('.')[0..1]
                if ([int]$major -eq 3 -and [int]$minor -eq 12) {
                    Add-Result -Component "Python venv" -Success $true -Details "Python $version" -Expected "3.12.x" -Actual $version
                } else {
                    Add-Result -Component "Python venv" -Success $false -Details "Wrong version" -Expected "3.12.x" -Actual $version
                }
            }
        }
    } catch {
        Add-Result -Component "Python venv" -Success $false -Details "Error checking version: $_"
    }
} else {
    Add-Result -Component "Python venv" -Success $false -Details "Virtual environment not found at .\volatility-env"
}

# Check Volatility3
$volExe = ".\volatility-env\Scripts\vol.exe"
if (Test-Path $volExe) {
    try {
        $volOutput = & $volExe -h 2>&1 | Select-Object -First 1
        if ($volOutput -match "Volatility 3 Framework (\d+\.\d+\.\d+)") {
            $volVersion = $matches[1]
            if ($volVersion -eq "2.26.2") {
                Add-Result -Component "Volatility3" -Success $true -Details "Version $volVersion" -Expected "2.26.2" -Actual $volVersion
            } else {
                Add-Result -Component "Volatility3" -Success $false -Details "Wrong version" -Expected "2.26.2" -Actual $volVersion
            }
        }
    } catch {
        Add-Result -Component "Volatility3" -Success $false -Details "Error running vol.exe: $_"
    }
} else {
    Add-Result -Component "Volatility3" -Success $false -Details "vol.exe not found"
}

# Check Python packages
if (Test-Path $pythonExe) {
    try {
        $pipList = uv pip list --python $pythonExe 2>&1 | Select-Object -Skip 2
        
        $packages = @{
            'volatility3' = '2.26.2'
            'pefile'      = '2024.8.26'
            'capstone'    = '5.0.6'
            'yara-python' = '4.5.4'
        }
        
        foreach ($pkg in $packages.Keys) {
            $expectedVersion = $packages[$pkg]
            $found = $pipList | Where-Object { $_ -match "^$pkg\s+(\S+)" }
            
            if ($found) {
                $actualVersion = ($found -split '\s+')[1]
                if ($actualVersion -eq $expectedVersion) {
                    Add-Result -Component "Package: $pkg" -Success $true -Details "Version $actualVersion"
                } else {
                    Add-Result -Component "Package: $pkg" -Success $false -Details "Version mismatch" -Expected $expectedVersion -Actual $actualVersion
                }
            } else {
                Add-Result -Component "Package: $pkg" -Success $false -Details "Not installed" -Expected $expectedVersion
            }
        }
    } catch {
        Add-Result -Component "Python Packages" -Success $false -Details "Error checking packages: $_"
    }
}
#endregion

#region Rust Project Verification
Write-SectionHeader "Rust Project"

$cargoToml = "rust-bridge\Cargo.toml"
if (Test-Path $cargoToml) {
    Add-Result -Component "Rust Project" -Success $true -Details "Cargo.toml exists"
} else {
    Add-Result -Component "Rust Project" -Success $false -Details "Cargo.toml not found"
}

$rustLib = "rust-bridge\src\lib.rs"
if (Test-Path $rustLib) {
    Add-Result -Component "Rust Library" -Success $true -Details "lib.rs exists"
} else {
    Add-Result -Component "Rust Library" -Success $false -Details "lib.rs not found"
}
#endregion

#region Project Structure Verification
Write-SectionHeader "Project Structure"

$expectedDirs = @(
    "volatility-env",
    "rust-bridge",
    "PowerShell.MemoryAnalysis",
    ".vscode",
    "docs"
)

foreach ($dir in $expectedDirs) {
    if (Test-Path $dir -PathType Container) {
        Add-Result -Component "Directory: $dir" -Success $true -Details "Exists"
    } else {
        Add-Result -Component "Directory: $dir" -Success $false -Details "Not found"
    }
}

$expectedFiles = @(
    "docs\SETUP.md",
    "docs\SETUP_COMPLETE.md",
    ".vscode\extensions.json"
)

foreach ($file in $expectedFiles) {
    if (Test-Path $file) {
        Add-Result -Component "File: $file" -Success $true -Details "Exists"
    } else {
        Add-Result -Component "File: $file" -Success $false -Details "Not found"
    }
}
#endregion

#region Summary
Write-SectionHeader "Verification Summary"

$totalChecks = $script:SuccessCount + $script:FailureCount
$successRate = if ($totalChecks -gt 0) { [math]::Round(($script:SuccessCount / $totalChecks) * 100, 1) } else { 0 }

Write-Host "`nTotal Checks: " -NoNewline
Write-Host $totalChecks -ForegroundColor Cyan

Write-Host "Passed: " -NoNewline
Write-Host $script:SuccessCount -ForegroundColor Green

Write-Host "Failed: " -NoNewline
Write-Host $script:FailureCount -ForegroundColor Red

Write-Host "Success Rate: " -NoNewline
Write-Host "$successRate%" -ForegroundColor $(if ($successRate -eq 100) { 'Green' } elseif ($successRate -ge 80) { 'Yellow' } else { 'Red' })

if ($script:FailureCount -eq 0) {
    Write-Host "`n✨ All checks passed! Environment is correctly configured. ✨" -ForegroundColor Green
    Write-Host "   Ready for Phase 1 development! 🚀" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Some checks failed. Please review the errors above. ⚠️" -ForegroundColor Yellow
    Write-Host "   Refer to SETUP.md for troubleshooting guidance." -ForegroundColor Yellow
}

Write-Host "`n═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Export results to JSON if requested
if ($VerbosePreference -eq 'Continue') {
    Write-Verbose "Exporting detailed results..."
    $resultsJson = $script:Results | ConvertTo-Json -Depth 3
    $resultsPath = "verification-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $resultsJson | Out-File $resultsPath -Encoding UTF8
    Write-Host "Detailed results saved to: $resultsPath" -ForegroundColor Gray
}

# Exit with appropriate code
exit $script:FailureCount
#endregion
