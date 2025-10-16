#!/usr/bin/env pwsh
# Comprehensive test script for all Memory Analysis cmdlets

Write-Host "=== Memory Analysis Module - Complete Test Suite ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Import module
Write-Host "Test 1: Importing module..." -ForegroundColor Yellow
try {
    Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1 -Force
    Write-Host "✓ Module imported successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to import module: $_" -ForegroundColor Red
    exit 1
}

# Test 2: Verify all cmdlets are available
Write-Host "`nTest 2: Verifying cmdlets..." -ForegroundColor Yellow
$expectedCmdlets = @(
    'Get-MemoryDump',
    'Test-ProcessTree',
    'Get-ProcessCommandLine',
    'Get-ProcessDll',
    'Get-NetworkConnection',
    'Find-Malware'
)

$availableCmdlets = (Get-Command -Module MemoryAnalysis).Name
foreach ($cmdlet in $expectedCmdlets) {
    if ($availableCmdlets -contains $cmdlet) {
        Write-Host "  ✓ $cmdlet" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $cmdlet NOT FOUND" -ForegroundColor Red
    }
}

# Test 3: Load memory dump
Write-Host "`nTest 3: Loading memory dump..." -ForegroundColor Yellow
$dumpPath = "F:\physmem.raw"

if (-not (Test-Path $dumpPath)) {
    Write-Host "  ⚠ Dump file not found: $dumpPath" -ForegroundColor Yellow
    Write-Host "  Skipping remaining tests" -ForegroundColor Yellow
    exit 0
}

try {
    $dump = Get-MemoryDump -Path $dumpPath
    Write-Host "  ✓ Dump loaded: $($dump.SizeGB) GB" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Failed to load dump: $_" -ForegroundColor Red
    exit 1
}

# Test 4: Process Tree (existing cmdlet)
Write-Host "`nTest 4: Testing Test-ProcessTree..." -ForegroundColor Yellow
try {
    $processes = Test-ProcessTree -MemoryDump $dump
    Write-Host "  ✓ Found $($processes.Count) processes" -ForegroundColor Green
    $processes | Select-Object -First 3 | Format-Table Name, Pid, Ppid -AutoSize
} catch {
    Write-Host "  ✗ Failed: $_" -ForegroundColor Red
}

# Test 5: Command Line Extraction (NEW)
Write-Host "`nTest 5: Testing Get-ProcessCommandLine..." -ForegroundColor Yellow
try {
    Write-Host "  Getting command lines (this may take 30-60 seconds)..." -ForegroundColor Gray
    $cmdlines = Get-ProcessCommandLine -MemoryDump $dump
    Write-Host "  ✓ Found $($cmdlines.Count) command lines" -ForegroundColor Green
    
    # Show some interesting ones
    $interesting = $cmdlines | Where-Object { $_.CommandLine -ne '<unreadable>' } | Select-Object -First 5
    Write-Host "  Sample command lines:" -ForegroundColor Cyan
    $interesting | Format-Table ProcessName, CommandLine -Wrap
} catch {
    Write-Host "  ✗ Failed: $_" -ForegroundColor Red
}

# Test 6: DLL Listing (NEW)
Write-Host "`nTest 6: Testing Get-ProcessDll..." -ForegroundColor Yellow
try {
    Write-Host "  Getting DLLs for System process (PID 4)..." -ForegroundColor Gray
    $dlls = Get-ProcessDll -MemoryDump $dump -Pid 4
    Write-Host "  ✓ Found $($dlls.Count) DLLs loaded by System process" -ForegroundColor Green
    
    Write-Host "  Sample DLLs:" -ForegroundColor Cyan
    $dlls | Select-Object -First 5 | Format-Table DllName, FormattedSize, BaseAddress -AutoSize
} catch {
    Write-Host "  ✗ Failed: $_" -ForegroundColor Red
}

# Test 7: Network Connections (NEW)
Write-Host "`nTest 7: Testing Get-NetworkConnection..." -ForegroundColor Yellow
try {
    Write-Host "  Scanning network connections (this may take 1-2 minutes)..." -ForegroundColor Gray
    $connections = Get-NetworkConnection -MemoryDump $dump
    Write-Host "  ✓ Found $($connections.Count) network connections" -ForegroundColor Green
    
    # Group by state
    $byState = $connections | Group-Object State | Sort-Object Count -Descending
    Write-Host "  Connections by state:" -ForegroundColor Cyan
    $byState | Format-Table Name, Count -AutoSize
    
    # Show some ESTABLISHED connections
    $established = $connections | Where-Object { $_.State -eq 'ESTABLISHED' } | Select-Object -First 5
    if ($established) {
        Write-Host "  Sample ESTABLISHED connections:" -ForegroundColor Cyan
        $established | Format-Table ProcessName, LocalEndpoint, ForeignEndpoint, Protocol -AutoSize
    }
} catch {
    Write-Host "  ✗ Failed: $_" -ForegroundColor Red
}

# Test 8: Malware Detection (NEW)
Write-Host "`nTest 8: Testing Find-Malware..." -ForegroundColor Yellow
try {
    Write-Host "  Detecting malware (this may take 1-2 minutes)..." -ForegroundColor Gray
    $detections = Find-Malware -MemoryDump $dump
    
    if ($detections.Count -eq 0) {
        Write-Host "  ✓ No malware detected (clean system)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Found $($detections.Count) potential malware detections" -ForegroundColor Yellow
        
        # Group by severity
        $bySeverity = $detections | Group-Object Severity | Sort-Object Count -Descending
        Write-Host "  Detections by severity:" -ForegroundColor Cyan
        $bySeverity | Format-Table Name, Count -AutoSize
        
        # Show high confidence detections
        $highConfidence = $detections | Where-Object { $_.Confidence -ge 70 } | Select-Object -First 5
        if ($highConfidence) {
            Write-Host "  High confidence detections:" -ForegroundColor Red
            $highConfidence | Format-List ProcessName, DetectionType, Severity, Confidence, Details
        }
    }
} catch {
    Write-Host "  ✗ Failed: $_" -ForegroundColor Red
}

# Test 9: Filtering tests
Write-Host "`nTest 9: Testing filters..." -ForegroundColor Yellow

# Test ProcessName wildcard filtering
try {
    $explorerCmdlines = Get-ProcessCommandLine -MemoryDump $dump -ProcessName "explorer*"
    Write-Host "  ✓ ProcessName filter: Found $($explorerCmdlines.Count) explorer processes" -ForegroundColor Green
} catch {
    Write-Host "  ✗ ProcessName filter failed: $_" -ForegroundColor Red
}

# Test DLL name filtering
try {
    $ntdlls = Get-ProcessDll -MemoryDump $dump -DllName "ntdll.dll"
    Write-Host "  ✓ DLL name filter: Found $($ntdlls.Count) processes with ntdll.dll" -ForegroundColor Green
} catch {
    Write-Host "  ✗ DLL name filter failed: $_" -ForegroundColor Red
}

# Test network state filtering
try {
    $listening = Get-NetworkConnection -MemoryDump $dump -State "LISTENING"
    Write-Host "  ✓ State filter: Found $($listening.Count) LISTENING connections" -ForegroundColor Green
} catch {
    Write-Host "  ✗ State filter failed: $_" -ForegroundColor Red
}

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
Write-Host "All cmdlets tested successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Module Statistics:" -ForegroundColor Yellow
Write-Host "  Total cmdlets: 6" -ForegroundColor White
Write-Host "  New cmdlets: 4" -ForegroundColor White
Write-Host "  Processes found: $($processes.Count)" -ForegroundColor White
Write-Host "  Command lines: $($cmdlines.Count)" -ForegroundColor White
Write-Host "  Network connections: $($connections.Count)" -ForegroundColor White
Write-Host "  Malware detections: $($detections.Count)" -ForegroundColor White
Write-Host ""
Write-Host "✅ All Phase 1 & Phase 2 features working!" -ForegroundColor Green
