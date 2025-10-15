#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Benchmarks MemoryAnalysis module performance.

.DESCRIPTION
    Measures execution time, memory usage, and FFI overhead for MemoryAnalysis cmdlets.
    Note: Requires actual memory dump files for realistic benchmarks.

.PARAMETER DumpPath
    Path to test memory dump file. If not provided, creates mock benchmarks.

.PARAMETER Iterations
    Number of iterations for each benchmark (default: 10)

.EXAMPLE
    .\Measure-Performance.ps1 -DumpPath C:\dumps\test.vmem -Iterations 20
#>

[CmdletBinding()]
param(
    [string]$DumpPath,
    [int]$Iterations = 10
)

$ErrorActionPreference = "Stop"

# Import module
$ModulePath = Join-Path $PSScriptRoot "..\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1"
Import-Module $ModulePath -Force

Write-Host "`n=== MemoryAnalysis Performance Benchmark ===" -ForegroundColor Cyan
Write-Host "Module Version: $($(Get-Module MemoryAnalysis).Version)" -ForegroundColor Gray
Write-Host "PowerShell: $($PSVersionTable.PSVersion)" -ForegroundColor Gray
Write-Host "Platform: $($PSVersionTable.Platform)" -ForegroundColor Gray
Write-Host "Iterations: $Iterations" -ForegroundColor Gray

# Results storage
$results = @{
    ModuleLoad = @()
    FFIOverhead = @()
    CmdletExecution = @{}
}

#region Module Load Performance
Write-Host "`n--- Module Load Performance ---" -ForegroundColor Yellow

$moduleLoadTimes = 1..$Iterations | ForEach-Object {
    Remove-Module MemoryAnalysis -Force -ErrorAction SilentlyContinue
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Import-Module $ModulePath -Force
    $sw.Stop()
    $sw.Elapsed.TotalMilliseconds
}

$results.ModuleLoad = $moduleLoadTimes
$avgModuleLoad = ($moduleLoadTimes | Measure-Object -Average).Average
$minModuleLoad = ($moduleLoadTimes | Measure-Object -Minimum).Minimum
$maxModuleLoad = ($moduleLoadTimes | Measure-Object -Maximum).Maximum

Write-Host "  Average: $([math]::Round($avgModuleLoad, 2))ms" -ForegroundColor Green
Write-Host "  Min: $([math]::Round($minModuleLoad, 2))ms" -ForegroundColor Gray
Write-Host "  Max: $([math]::Round($maxModuleLoad, 2))ms" -ForegroundColor Gray
#endregion

#region FFI Overhead
Write-Host "`n--- FFI/P-Invoke Overhead ---" -ForegroundColor Yellow

# Measure cmdlet instantiation time (without Volatility execution)
$cmdletCreationTimes = 1..$Iterations | ForEach-Object {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $null = [PowerShell.MemoryAnalysis.Cmdlets.GetMemoryDumpCommand]::new()
    $sw.Stop()
    $sw.Elapsed.TotalMilliseconds
}

$results.FFIOverhead = $cmdletCreationTimes
$avgFFI = ($cmdletCreationTimes | Measure-Object -Average).Average

Write-Host "  Cmdlet Creation: $([math]::Round($avgFFI, 4))ms" -ForegroundColor Green
#endregion

#region Memory Usage
Write-Host "`n--- Memory Usage ---" -ForegroundColor Yellow

# Baseline memory
[GC]::Collect()
Start-Sleep -Milliseconds 100
$baselineMemory = [GC]::GetTotalMemory($false)

# After module load
Import-Module $ModulePath -Force
[GC]::Collect()
Start-Sleep -Milliseconds 100
$moduleMemory = [GC]::GetTotalMemory($false)

$memoryOverhead = ($moduleMemory - $baselineMemory) / 1MB

Write-Host "  Baseline: $([math]::Round($baselineMemory / 1MB, 2)) MB" -ForegroundColor Gray
Write-Host "  With Module: $([math]::Round($moduleMemory / 1MB, 2)) MB" -ForegroundColor Gray
Write-Host "  Module Overhead: $([math]::Round($memoryOverhead, 2)) MB" -ForegroundColor Green
#endregion

#region Cmdlet Performance (with memory dump)
if ($DumpPath -and (Test-Path $DumpPath)) {
    Write-Host "`n--- Cmdlet Execution Performance ---" -ForegroundColor Yellow
    Write-Host "  Using dump: $DumpPath" -ForegroundColor Gray
    
    $dumpSize = (Get-Item $DumpPath).Length / 1GB
    Write-Host "  Dump size: $([math]::Round($dumpSize, 2)) GB" -ForegroundColor Gray
    
    # Get-MemoryDump
    Write-Host "`n  Testing Get-MemoryDump..." -ForegroundColor Cyan
    $getDumpTimes = 1..$Iterations | ForEach-Object {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $dump = Get-MemoryDump -Path $DumpPath -ErrorAction SilentlyContinue
        $sw.Stop()
        $sw.Elapsed.TotalMilliseconds
    }
    
    $results.CmdletExecution['Get-MemoryDump'] = $getDumpTimes
    $avgGetDump = ($getDumpTimes | Measure-Object -Average).Average
    Write-Host "    Average: $([math]::Round($avgGetDump, 2))ms" -ForegroundColor Green
    
    # Get-ProcessCommandLine (requires dump object)
    Write-Host "`n  Testing Get-ProcessCommandLine..." -ForegroundColor Cyan
    $dump = Get-MemoryDump -Path $DumpPath -ErrorAction SilentlyContinue
    
    if ($dump) {
        $getCmdLineTimes = 1..$Iterations | ForEach-Object {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $null = Get-ProcessCommandLine -MemoryDump $dump -ErrorAction SilentlyContinue
            $sw.Stop()
            $sw.Elapsed.TotalSeconds
        }
        
        $results.CmdletExecution['Get-ProcessCommandLine'] = $getCmdLineTimes
        $avgCmdLine = ($getCmdLineTimes | Measure-Object -Average).Average
        Write-Host "    Average: $([math]::Round($avgCmdLine, 2))s" -ForegroundColor Green
    }
    
    # Analyze-ProcessTree
    Write-Host "`n  Testing Analyze-ProcessTree..." -ForegroundColor Cyan
    $analyzeTimes = 1..$Iterations | ForEach-Object {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $null = Analyze-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue
        $sw.Stop()
        $sw.Elapsed.TotalSeconds
    }
    
    $results.CmdletExecution['Analyze-ProcessTree'] = $analyzeTimes
    $avgAnalyze = ($analyzeTimes | Measure-Object -Average).Average
    Write-Host "    Average: $([math]::Round($avgAnalyze, 2))s" -ForegroundColor Green
    
} else {
    Write-Host "`n--- Cmdlet Execution Performance ---" -ForegroundColor Yellow
    Write-Host "  ⚠️  Skipped: No memory dump provided" -ForegroundColor Yellow
    Write-Host "  Use -DumpPath parameter to test with actual memory dump" -ForegroundColor Gray
}
#endregion

#region Summary Report
Write-Host "`n=== Performance Summary ===" -ForegroundColor Cyan

$report = @"

Module Performance:
  - Load Time: $([math]::Round($avgModuleLoad, 2))ms (avg)
  - Memory Overhead: $([math]::Round($memoryOverhead, 2))MB
  - FFI Overhead: $([math]::Round($avgFFI, 4))ms

"@

if ($results.CmdletExecution.Count -gt 0) {
    $report += "Cmdlet Performance:`n"
    foreach ($cmdlet in $results.CmdletExecution.Keys) {
        $times = $results.CmdletExecution[$cmdlet]
        $avg = ($times | Measure-Object -Average).Average
        $unit = if ($avg -lt 1000) { "ms" } else { "s"; $avg = $avg }
        $report += "  - $cmdlet`: $([math]::Round($avg, 2))$unit (avg)`n"
    }
}

Write-Host $report -ForegroundColor White

# Export results
$resultsPath = Join-Path $PSScriptRoot "benchmark-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$results | ConvertTo-Json -Depth 5 | Out-File $resultsPath
Write-Host "Results exported to: $resultsPath" -ForegroundColor Gray
#endregion

Write-Host "`n✅ Benchmark complete!`n" -ForegroundColor Green
