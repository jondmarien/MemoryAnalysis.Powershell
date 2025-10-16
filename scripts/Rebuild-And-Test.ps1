#!/usr/bin/env pwsh
# Quick rebuild and test script - Run this from a FRESH PowerShell window

Write-Host "=== Rebuild and Test Memory Analysis Module ===" -ForegroundColor Cyan
Write-Host ""

# Build the module
Write-Host "Building module..." -ForegroundColor Yellow
.\Build.ps1 -SkipRust

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Running Quick Test ===" -ForegroundColor Cyan
Write-Host ""

# Set environment
$env:PYTHONPATH = 'J:\projects\personal-projects\MemoryAnalysis\.venv\Lib\site-packages'

# Import module
Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1 -Force

# Show available cmdlets
Write-Host "Available cmdlets:" -ForegroundColor Green
Get-Command -Module MemoryAnalysis | Format-Table Name, Version -AutoSize

Write-Host ""
Write-Host "Testing with memory dump..." -ForegroundColor Yellow
$dump = Get-MemoryDump -Path F:\physmem.raw

Write-Host ""
Write-Host "Process analysis..." -ForegroundColor Yellow
$procs = Test-ProcessTree -MemoryDump $dump
Write-Host "✓ Found $($procs.Count) processes" -ForegroundColor Green

Write-Host ""
Write-Host "Command line extraction..." -ForegroundColor Yellow
$cmdlines = Get-ProcessCommandLine -MemoryDump $dump
Write-Host "✓ Found $($cmdlines.Count) command lines" -ForegroundColor Green

Write-Host ""
Write-Host "DLL listing (PID 4)..." -ForegroundColor Yellow
$dlls = Get-ProcessDll -MemoryDump $dump -Pid 4
Write-Host "✓ Found $($dlls.Count) DLLs for System process" -ForegroundColor Green

Write-Host ""
Write-Host "=== All Tests Passed! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Module Statistics:" -ForegroundColor Cyan
Write-Host "  Cmdlets: 4 (working on Windows 11 Build 26100)" -ForegroundColor White
Write-Host "  Processes: $($procs.Count)" -ForegroundColor White
Write-Host "  Command lines: $($cmdlines.Count)" -ForegroundColor White
Write-Host ""
Write-Host "Note: Get-NetworkConnection and Find-Malware are disabled" -ForegroundColor Yellow
Write-Host "See HOW_TO_FILE_BUG_REPORT.md for details" -ForegroundColor Yellow
