#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test parallel processing capabilities of the MemoryAnalysis module.

.DESCRIPTION
    This script demonstrates true parallel execution of memory dump analysis
    using PowerShell's ForEach-Object -Parallel feature with the MemoryAnalysis module.
    
    The Rust bridge now releases the Python GIL during I/O operations, allowing
    multiple threads to process different dumps concurrently.

.PARAMETER DumpPath
    Path to a single memory dump file for testing, or a directory containing multiple dumps.

.PARAMETER ThrottleLimit
    Maximum number of parallel operations. Recommended: 2-4 for large dumps, 4-8 for small dumps.

.EXAMPLE
    .\Test-ParallelProcessing.ps1 -DumpPath "F:\physmem.raw" -ThrottleLimit 4
    
.EXAMPLE
    .\Test-ParallelProcessing.ps1 -DumpPath "C:\dumps\" -ThrottleLimit 4
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$DumpPath = "F:\physmem.raw",
    
    [Parameter(Mandatory=$false)]
    [int]$ThrottleLimit = 2
)

$ErrorActionPreference = "Stop"

# Import the module
Write-Host "Importing MemoryAnalysis module..." -ForegroundColor Cyan
Import-Module "$PSScriptRoot\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1" -Force

# Determine if we have a single file or directory
if (Test-Path $DumpPath -PathType Container) {
    $dumps = Get-ChildItem -Path $DumpPath -Include "*.raw","*.vmem","*.dmp" -File
    Write-Host "Found $($dumps.Count) memory dumps in directory" -ForegroundColor Green
} elseif (Test-Path $DumpPath -PathType Leaf) {
    $dumps = @(Get-Item $DumpPath)
    Write-Host "Testing with single dump: $(Split-Path $DumpPath -Leaf)" -ForegroundColor Green
} else {
    Write-Host "Error: Path not found: $DumpPath" -ForegroundColor Red
    exit 1
}

if ($dumps.Count -eq 0) {
    Write-Host "Error: No memory dumps found" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Parallel Processing Test ===" -ForegroundColor Cyan
Write-Host "Dumps to analyze: $($dumps.Count)" -ForegroundColor Yellow
Write-Host "Throttle limit: $ThrottleLimit threads" -ForegroundColor Yellow
Write-Host "GIL Detach: ENABLED (true parallel execution)" -ForegroundColor Green

# Test 1: Sequential Processing (Baseline)
Write-Host "`n[Test 1/3] Sequential Processing (Baseline)" -ForegroundColor Cyan
$sequentialTime = Measure-Command {
    $sequentialResults = $dumps | ForEach-Object {
        Write-Host "  Processing: $($_.Name)..." -ForegroundColor Gray
        $dump = Get-MemoryDump -Path $_.FullName
        $processes = Test-ProcessTree -MemoryDump $dump
        [PSCustomObject]@{
            File = $_.Name
            ProcessCount = $processes.Count
            ThreadId = [System.Threading.Thread]::CurrentThread.ManagedThreadId
        }
    }
}

Write-Host "  Sequential time: $($sequentialTime.TotalSeconds.ToString('F2'))s" -ForegroundColor Yellow
Write-Host "  Total processes: $($sequentialResults | Measure-Object -Property ProcessCount -Sum | Select-Object -ExpandProperty Sum)" -ForegroundColor Green

# Test 2: Parallel Processing (ForEach-Object -Parallel)
Write-Host "`n[Test 2/3] Parallel Processing with -Parallel" -ForegroundColor Cyan
$parallelTime = Measure-Command {
    $parallelResults = $dumps | ForEach-Object -ThrottleLimit $ThrottleLimit -Parallel {
        $modulePath = "$using:PSScriptRoot\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1"
        Import-Module $modulePath -ErrorAction Stop
        
        Write-Host "  [Thread $([System.Threading.Thread]::CurrentThread.ManagedThreadId)] Processing: $($_.Name)..." -ForegroundColor Gray
        $dump = Get-MemoryDump -Path $_.FullName
        $processes = Test-ProcessTree -MemoryDump $dump
        
        [PSCustomObject]@{
            File = $_.Name
            ProcessCount = $processes.Count
            ThreadId = [System.Threading.Thread]::CurrentThread.ManagedThreadId
        }
    }
}

Write-Host "  Parallel time: $($parallelTime.TotalSeconds.ToString('F2'))s" -ForegroundColor Yellow
Write-Host "  Total processes: $($parallelResults | Measure-Object -Property ProcessCount -Sum | Select-Object -ExpandProperty Sum)" -ForegroundColor Green

# Calculate speedup
$speedup = $sequentialTime.TotalSeconds / $parallelTime.TotalSeconds
Write-Host "  Speedup: ${speedup}x" -ForegroundColor $(if ($speedup -gt 1.5) { "Green" } else { "Yellow" })

# Test 3: Parallel with Multiple Operations
Write-Host "`n[Test 3/3] Parallel Multi-Operation Test" -ForegroundColor Cyan
Write-Host "  Running process list + command lines + DLLs in parallel..." -ForegroundColor Gray

$multiOpTime = Measure-Command {
    $multiOpResults = $dumps | ForEach-Object -ThrottleLimit $ThrottleLimit -Parallel {
        $modulePath = "$using:PSScriptRoot\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1"
        Import-Module $modulePath -ErrorAction Stop
        
        $threadId = [System.Threading.Thread]::CurrentThread.ManagedThreadId
        $dump = Get-MemoryDump -Path $_.FullName
        
        # Run multiple analyses
        $processes = Test-ProcessTree -MemoryDump $dump
        $cmdlines = Get-ProcessCommandLine -MemoryDump $dump
        $dlls = Get-ProcessDll -MemoryDump $dump -Pid 4  # Just system process
        
        [PSCustomObject]@{
            File = $_.Name
            ThreadId = $threadId
            Processes = $processes.Count
            CommandLines = $cmdlines.Count
            DLLs = $dlls.Count
        }
    }
}

Write-Host "  Multi-op parallel time: $($multiOpTime.TotalSeconds.ToString('F2'))s" -ForegroundColor Yellow

# Display results
Write-Host "`n=== Results Summary ===" -ForegroundColor Cyan
$parallelResults | Format-Table -AutoSize

Write-Host "`n=== Thread Distribution ===" -ForegroundColor Cyan
$parallelResults | Group-Object ThreadId | 
    Select-Object @{N='ThreadId';E={$_.Name}}, Count | 
    Format-Table -AutoSize

Write-Host "`n=== Performance Summary ===" -ForegroundColor Cyan
[PSCustomObject]@{
    Test = "Sequential"
    Time = "$($sequentialTime.TotalSeconds.ToString('F2'))s"
    Speedup = "1.00x"
} | Format-Table

[PSCustomObject]@{
    Test = "Parallel (ThrottleLimit=$ThrottleLimit)"
    Time = "$($parallelTime.TotalSeconds.ToString('F2'))s"
    Speedup = "${speedup.ToString('F2')}x"
} | Format-Table

Write-Host "`n=== Recommendations ===" -ForegroundColor Cyan
if ($speedup -gt 1.8) {
    Write-Host "✓ Excellent parallelization! GIL detach is working." -ForegroundColor Green
} elseif ($speedup -gt 1.3) {
    Write-Host "✓ Good parallelization. Consider increasing ThrottleLimit for more speedup." -ForegroundColor Green
} else {
    Write-Host "⚠ Limited speedup. This may be due to I/O bottlenecks or small dump files." -ForegroundColor Yellow
    Write-Host "  Try with larger dumps or increase ThrottleLimit." -ForegroundColor Yellow
}

Write-Host "`nRecommended ThrottleLimit values:" -ForegroundColor Cyan
Write-Host "  Small dumps (<500MB): 4-8 threads" -ForegroundColor Gray
Write-Host "  Medium dumps (500MB-5GB): 2-4 threads" -ForegroundColor Gray
Write-Host "  Large dumps (>5GB): 2 threads" -ForegroundColor Gray
Write-Host "  Very large dumps (>50GB): 1 thread (sequential)" -ForegroundColor Gray

Write-Host "`n✓ Parallel processing test complete!" -ForegroundColor Green
