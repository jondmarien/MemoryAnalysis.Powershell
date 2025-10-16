<#
.SYNOPSIS
Benchmark cache performance and measure hit/miss rates

.DESCRIPTION
Tests cache performance by analyzing memory dumps multiple times and measuring
cache hit rates, performance improvements, and memory usage.

.PARAMETER DumpPath
Path to the memory dump file to analyze (default: test dump if available)

.PARAMETER Iterations
Number of iterations for cache testing (default: 3)

.PARAMETER ThrottleLimit
Number of parallel threads for testing (default: 4)

.EXAMPLE
.\Test-CachePerformance.ps1 -DumpPath "F:\physmem.raw" -Iterations 5

.NOTES
This script requires the MemoryAnalysis module to be imported.
#>

param(
    [string]$DumpPath = "F:\physmem.raw",
    [int]$Iterations = 3,
    [int]$ThrottleLimit = 4
)

# Ensure module is imported
$modulePath = Join-Path $PSScriptRoot "..\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1"
if (-not (Get-Module MemoryAnalysis -ErrorAction SilentlyContinue)) {
    Import-Module $modulePath -Force
}

Write-Host "=" * 80
Write-Host "Cache Performance Benchmark"
Write-Host "=" * 80
Write-Host ""

# Verify dump file exists
if (-not (Test-Path $DumpPath)) {
    Write-Error "Memory dump not found: $DumpPath"
    exit 1
}

Write-Host "Configuration:"
Write-Host "  Dump Path: $DumpPath"
Write-Host "  Dump Size: $((Get-Item $DumpPath).Length / 1GB)GB"
Write-Host "  Iterations: $Iterations"
Write-Host "  Parallel Threads: $ThrottleLimit"
Write-Host ""

# Clear cache before starting
Write-Host "Clearing cache..."
Clear-Cache -Force -Confirm:$false
$initialStats = Get-CacheInfo

Write-Host ""
Write-Host "Initial Cache State:"
$initialStats | Format-Table -AutoSize

# Test 1: Sequential Analysis (Cache Misses)
Write-Host ""
Write-Host "=" * 80
Write-Host "TEST 1: Sequential Analysis (Cache Misses)"
Write-Host "=" * 80

$sequentialTimes = @()
$clearBeforeEach = $false

for ($i = 1; $i -le $Iterations; $i++) {
    if ($clearBeforeEach) {
        Clear-Cache -Force -Confirm:$false | Out-Null
    }

    Write-Host "Iteration $i/$Iterations..." -NoNewline
    
    $sw = Measure-Command {
        $dump = Get-MemoryDump -Path $DumpPath
        $processes = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
        $cmdLines = $dump | Get-ProcessCommandLine -ErrorAction SilentlyContinue | Out-Null
    }
    
    $sequentialTimes += $sw.TotalMilliseconds
    Write-Host " ${($sw.TotalSeconds):.2f}s"
}

$avgSequential = ($sequentialTimes | Measure-Object -Average).Average
Write-Host ""
Write-Host "Sequential Analysis Average: $($avgSequential)ms"
Write-Host "Times: $($sequentialTimes | ForEach-Object { "$($_)ms" } | Join-String -Separator ', ')"

# Test 2: Cached Analysis (Cache Hits)
Write-Host ""
Write-Host "=" * 80
Write-Host "TEST 2: Cached Analysis (Cache Hits)"
Write-Host "=" * 80

# First load dump and populate cache
Write-Host "Pre-loading cache..."
$dump = Get-MemoryDump -Path $DumpPath
$processes = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
$cmdLines = $dump | Get-ProcessCommandLine -ErrorAction SilentlyContinue | Out-Null

$cachedTimes = @()

for ($i = 1; $i -le $Iterations; $i++) {
    Write-Host "Iteration $i/$Iterations (cached)..." -NoNewline
    
    $sw = Measure-Command {
        $dump = Get-MemoryDump -Path $DumpPath
        $processes = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
        $cmdLines = $dump | Get-ProcessCommandLine -ErrorAction SilentlyContinue | Out-Null
    }
    
    $cachedTimes += $sw.TotalMilliseconds
    Write-Host " ${($sw.TotalSeconds):.2f}s"
}

$avgCached = ($cachedTimes | Measure-Object -Average).Average
Write-Host ""
Write-Host "Cached Analysis Average: $($avgCached)ms"
Write-Host "Times: $($cachedTimes | ForEach-Object { "$($_)ms" } | Join-String -Separator ', ')"

# Test 3: Cache Statistics
Write-Host ""
Write-Host "=" * 80
Write-Host "TEST 3: Cache Statistics"
Write-Host "=" * 80

$finalStats = Get-CacheInfo
Write-Host ""
$finalStats | Format-Table -AutoSize

# Calculate metrics
Write-Host ""
Write-Host "=" * 80
Write-Host "Performance Summary"
Write-Host "=" * 80

$speedup = $avgSequential / $avgCached
$improvement = (($avgSequential - $avgCached) / $avgSequential) * 100

Write-Host ""
Write-Host "Sequential (first run):  $($avgSequential)ms"
Write-Host "Cached (subsequent):     $($avgCached)ms"
Write-Host "Speed Improvement:       ${speedup}x faster"
Write-Host "Performance Gain:        $($improvement)%"
Write-Host ""

# Aggregate cache statistics
$totalHits = ($finalStats | Measure-Object -Property CacheHits -Sum).Sum
$totalMisses = ($finalStats | Measure-Object -Property CacheMisses -Sum).Sum
$totalAccesses = $totalHits + $totalMisses
$overallHitRate = if ($totalAccesses -gt 0) { ($totalHits / $totalAccesses) * 100 } else { 0 }

Write-Host "Overall Cache Performance:"
Write-Host "  Total Accesses:  $totalAccesses"
Write-Host "  Cache Hits:      $totalHits"
Write-Host "  Cache Misses:    $totalMisses"
Write-Host "  Hit Rate:        $($overallHitRate)%"
Write-Host ""

# Performance targets
Write-Host "Performance Targets (from steering docs):"
Write-Host "  Target: <2 seconds for cached operations"
Write-Host "  Actual: $($avgCached)ms"
Write-Host "  Status: $(if ($avgCached -lt 2000) { '✓ PASS' } else { '✗ FAIL' })"
Write-Host ""

Write-Host "Cache Target: >80% hit rate"
Write-Host "  Actual: $($overallHitRate)%"
Write-Host "  Status: $(if ($overallHitRate -ge 80) { '✓ PASS' } else { '✗ FAIL' })"
Write-Host ""

# Detailed cache breakdown
Write-Host "Cache Type Breakdown:"
$finalStats | ForEach-Object {
    $hitRate = $_.HitRate * 100
    Write-Host "  $($_.CacheType): $($hitRate)% hit rate ($($_.CacheHits) hits, $($_.CacheMisses) misses)"
}

Write-Host ""
Write-Host "=" * 80
Write-Host "Benchmark Complete"
Write-Host "=" * 80
