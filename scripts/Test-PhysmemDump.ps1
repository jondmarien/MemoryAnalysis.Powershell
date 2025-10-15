#!/usr/bin/env pwsh
# Test script for analyzing F:\physmem.raw

Write-Host "`n=== PowerShell Memory Analysis - Testing F:\physmem.raw ===`n" -ForegroundColor Cyan

# Enable debug logging
$env:RUST_BRIDGE_DEBUG = "1"

# Import module
Write-Host "[1/3] Importing module..." -ForegroundColor Yellow
Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1 -Force

# Load memory dump
Write-Host "`n[2/3] Loading memory dump F:\physmem.raw (97.99 GB)..." -ForegroundColor Yellow
$dump = Get-MemoryDump -Path F:\physmem.raw
Write-Host "✓ Loaded: $($dump.FileName) - $($dump.Size)" -ForegroundColor Green

# Analyze process tree
Write-Host "`n[3/3] Analyzing process tree (this may take 1-2 minutes for large dump)..." -ForegroundColor Yellow
try {
    $processes = $dump | Test-ProcessTree -ErrorAction Stop
    
    if ($processes.Count -gt 0) {
        Write-Host "✓ SUCCESS: Found $($processes.Count) processes!`n" -ForegroundColor Green
        
        # Display first 15 processes
        Write-Host "First 15 processes:" -ForegroundColor Cyan
        $processes | Select-Object -First 15 | Format-Table Pid, Ppid, Name, Threads, Handles -AutoSize
        
        # Display some interesting processes
        Write-Host "`nSystem processes:" -ForegroundColor Cyan
        $processes | Where-Object { $_.Name -match 'system|csrss|lsass|services' } | 
            Select-Object -First 10 | Format-Table Pid, Ppid, Name, Threads, Handles -AutoSize
            
        # Check for suspicious processes if flag available
        $suspicious = $processes | Where-Object { $_.Name -match 'cmd|powershell|wscript|cscript' }
        if ($suspicious) {
            Write-Host "`nShell processes (cmd, PowerShell, scripts):" -ForegroundColor Yellow
            $suspicious | Format-Table Pid, Ppid, Name, Threads, Handles -AutoSize
        }
    } else {
        Write-Host "✗ No processes found in dump" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Details: $($_.Exception.InnerException)" -ForegroundColor Yellow
}

# Check if debug log was created
Write-Host "`nDebug log status:" -ForegroundColor Cyan
if (Test-Path .\rust-bridge-debug.log) {
    Write-Host "✓ Debug log created at .\rust-bridge-debug.log" -ForegroundColor Green
    Write-Host "`nLast 10 log entries:" -ForegroundColor Cyan
    Get-Content .\rust-bridge-debug.log -Tail 10
} else {
    Write-Host "✗ No debug log found (debug mode may not be enabled)" -ForegroundColor Yellow
}

Write-Host "`n=== Test Complete ===`n" -ForegroundColor Cyan
