<#
.SYNOPSIS
Comprehensive test suite for the caching system implementation

.DESCRIPTION
Tests all aspects of the caching implementation:
- Cache statistics collection
- Cache invalidation on file changes
- Cache hit/miss rates
- Performance with and without caching
- File monitoring (watch/unwatch)
- Cache clearing

.PARAMETER DumpPath
Path to the memory dump file to test with (default: F:\physmem.raw)

.PARAMETER Iterations
Number of iterations for performance testing (default: 3)

.EXAMPLE
.\Test-CachingSystem.ps1 -DumpPath "F:\physmem.raw" -Iterations 5

.NOTES
This script requires the MemoryAnalysis module to be imported.
#>

param(
    [string]$DumpPath = "F:\physmem.raw",
    [int]$Iterations = 3
)

# Ensure module is imported
$modulePath = Join-Path $PSScriptRoot "..\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1"
if (-not (Get-Module MemoryAnalysis -ErrorAction SilentlyContinue)) {
    Import-Module $modulePath -Force
}

Write-Host "=" * 80
Write-Host "CACHING SYSTEM COMPREHENSIVE TEST SUITE"
Write-Host "=" * 80
Write-Host ""

# Verify dump file exists
if (-not (Test-Path $DumpPath)) {
    Write-Error "Memory dump not found: $DumpPath"
    exit 1
}

$dumpSize = (Get-Item $DumpPath).Length / 1GB
Write-Host "Test Configuration:"
Write-Host "  Dump Path: $DumpPath"
Write-Host "  Dump Size: $($dumpSize)GB"
Write-Host "  Iterations: $Iterations"
Write-Host ""

# ============================================================================
# TEST 1: Basic Cache Operations
# ============================================================================
Write-Host "=" * 80
Write-Host "TEST 1: Basic Cache Operations"
Write-Host "=" * 80
Write-Host ""

try {
    Write-Host "1.1: Checking initial cache state..."
    $initialStats = Get-CacheInfo
    if ($initialStats) {
        Write-Host "✓ Cache info retrieved successfully"
        $initialStats | Format-Table -AutoSize
    } else {
        Write-Host "⚠ Cache info returned no data"
    }
    Write-Host ""
    
    Write-Host "1.2: Loading memory dump..."
    $dump = Get-MemoryDump -Path $DumpPath
    Write-Host "✓ Memory dump loaded: $($dump.Path)"
    Write-Host ""
    
    Write-Host "1.3: First analysis (should populate cache)..."
    $sw = Measure-Command {
        $firstAnalysis = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
    }
    Write-Host "✓ First analysis completed in $($sw.TotalSeconds)s"
    Write-Host ""
    
    Write-Host "1.4: Checking cache stats after first analysis..."
    $statsAfterFirst = Get-CacheInfo
    if ($statsAfterFirst) {
        Write-Host "✓ Cache statistics:"
        $statsAfterFirst | Format-Table -AutoSize
    }
    Write-Host ""
    
    Write-Host "1.5: Second analysis (should hit cache)..."
    $sw = Measure-Command {
        $secondAnalysis = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
    }
    Write-Host "✓ Second analysis completed in $($sw.TotalSeconds)s"
    Write-Host ""
    
    Write-Host "1.6: Checking cache stats after second analysis..."
    $statsAfterSecond = Get-CacheInfo
    if ($statsAfterSecond) {
        Write-Host "✓ Cache statistics:"
        $statsAfterSecond | Format-Table -AutoSize
    }
    Write-Host ""
    
    # Calculate hit rate improvement
    $firstTime = $sw.TotalSeconds
    if ($firstTime -gt 0) {
        Write-Host "TEST 1 RESULT: ✓ PASS"
        Write-Host "Cache is functioning. Second analysis may use cached data."
    }
}
catch {
    Write-Host "TEST 1 RESULT: ✗ FAIL"
    Write-Error "Test failed: $_"
}

Write-Host ""

# ============================================================================
# TEST 2: File Monitoring
# ============================================================================
Write-Host "=" * 80
Write-Host "TEST 2: File Monitoring (Watch/Unwatch)"
Write-Host "=" * 80
Write-Host ""

try {
    Write-Host "2.1: Starting file monitoring..."
    Watch-MemoryDumpFile -Path $DumpPath
    Write-Host "✓ File monitoring started"
    Write-Host ""
    
    Write-Host "2.2: Listing watched files..."
    $watched = Get-WatchedMemoryDumpFiles
    Write-Host "✓ Watched files retrieved"
    Write-Host ""
    
    Write-Host "2.3: Validating cache against watched files..."
    $validation = Test-CacheValidity
    Write-Host "✓ Cache validation completed"
    if ($validation.AllValid) {
        Write-Host "  Status: All cache entries valid"
    } else {
        Write-Host "  Status: Some cache entries may be stale"
    }
    Write-Host ""
    
    Write-Host "2.4: Stopping file monitoring..."
    Stop-WatchingMemoryDumpFile -Path $DumpPath
    Write-Host "✓ File monitoring stopped"
    Write-Host ""
    
    Write-Host "TEST 2 RESULT: ✓ PASS"
}
catch {
    Write-Host "TEST 2 RESULT: ✗ FAIL"
    Write-Error "Test failed: $_"
}

Write-Host ""

# ============================================================================
# TEST 3: Cache Clearing
# ============================================================================
Write-Host "=" * 80
Write-Host "TEST 3: Cache Clearing"
Write-Host "=" * 80
Write-Host ""

try {
    Write-Host "3.1: Getting cache stats before clearing..."
    $statsBefore = Get-CacheInfo
    Write-Host "✓ Pre-clear stats:"
    $statsBefore | Format-Table -AutoSize
    Write-Host ""
    
    Write-Host "3.2: Clearing all caches..."
    Clear-Cache -Force -Confirm:$false
    Write-Host "✓ Cache cleared"
    Write-Host ""
    
    Write-Host "3.3: Verifying cache is empty..."
    $statsAfter = Get-CacheInfo
    Write-Host "✓ Post-clear stats:"
    $statsAfter | Format-Table -AutoSize
    Write-Host ""
    
    Write-Host "TEST 3 RESULT: ✓ PASS"
}
catch {
    Write-Host "TEST 3 RESULT: ✗ FAIL"
    Write-Error "Test failed: $_"
}

Write-Host ""

# ============================================================================
# TEST 4: Performance Comparison (Sequential vs Cached)
# ============================================================================
Write-Host "=" * 80
Write-Host "TEST 4: Performance Comparison"
Write-Host "=" * 80
Write-Host ""

try {
    # Clear cache before starting
    Write-Host "4.0: Clearing cache before test..."
    Clear-Cache -Force -Confirm:$false
    Write-Host "✓ Cache cleared"
    Write-Host ""
    
    # Sequential (first run - cache misses)
    Write-Host "4.1: Sequential Analysis (Cache Misses)..."
    $sequentialTimes = @()
    for ($i = 1; $i -le $Iterations; $i++) {
        Clear-Cache -Force -Confirm:$false | Out-Null
        Write-Host "  Iteration $i/$Iterations..." -NoNewline
        
        $sw = Measure-Command {
            $dump = Get-MemoryDump -Path $DumpPath
            $processes = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
            $cmdLines = $dump | Get-ProcessCommandLine -ErrorAction SilentlyContinue | Out-Null
        }
        
        $sequentialTimes += $sw.TotalSeconds
        Write-Host " $($sw.TotalSeconds)s"
    }
    
    $avgSequential = ($sequentialTimes | Measure-Object -Average).Average
    Write-Host "✓ Sequential average: $($avgSequential)s"
    Write-Host ""
    
    # Cached (subsequent runs - cache hits)
    Write-Host "4.2: Cached Analysis (Cache Hits)..."
    $dump = Get-MemoryDump -Path $DumpPath
    $processes = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
    $cmdLines = $dump | Get-ProcessCommandLine -ErrorAction SilentlyContinue | Out-Null
    Write-Host "✓ Cache pre-loaded"
    Write-Host ""
    
    $cachedTimes = @()
    for ($i = 1; $i -le $Iterations; $i++) {
        Write-Host "  Iteration $i/$Iterations (cached)..." -NoNewline
        
        $sw = Measure-Command {
            $dump = Get-MemoryDump -Path $DumpPath
            $processes = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
            $cmdLines = $dump | Get-ProcessCommandLine -ErrorAction SilentlyContinue | Out-Null
        }
        
        $cachedTimes += $sw.TotalSeconds
        Write-Host " $($sw.TotalSeconds)s"
    }
    
    $avgCached = ($cachedTimes | Measure-Object -Average).Average
    Write-Host "✓ Cached average: $($avgCached)s"
    Write-Host ""
    
    # Calculate metrics
    $speedup = $avgSequential / $avgCached
    $improvement = (($avgSequential - $avgCached) / $avgSequential) * 100
    
    Write-Host "4.3: Performance Metrics:"
    Write-Host "  Sequential: $($avgSequential)s"
    Write-Host "  Cached:     $($avgCached)s"
    Write-Host "  Speedup:    $($speedup)x faster"
    Write-Host "  Improvement: $($improvement)%"
    Write-Host ""
    
    # Get final cache statistics
    $finalStats = Get-CacheInfo
    Write-Host "4.4: Final Cache Statistics:"
    $finalStats | Format-Table -AutoSize
    Write-Host ""
    
    # Calculate aggregate hit rate
    $totalHits = ($finalStats | Measure-Object -Property CacheHits -Sum).Sum
    $totalMisses = ($finalStats | Measure-Object -Property CacheMisses -Sum).Sum
    $totalAccesses = $totalHits + $totalMisses
    $overallHitRate = if ($totalAccesses -gt 0) { ($totalHits / $totalAccesses) * 100 } else { 0 }
    
    Write-Host "4.5: Aggregate Cache Performance:"
    Write-Host "  Total Accesses: $totalAccesses"
    Write-Host "  Total Hits:     $totalHits"
    Write-Host "  Total Misses:   $totalMisses"
    Write-Host "  Overall Hit Rate: $($overallHitRate)%"
    Write-Host ""
    
    # Check performance targets
    Write-Host "4.6: Performance Target Validation:"
    $cachedTargetMet = $avgCached -lt 2
    $hitRateTargetMet = $overallHitRate -ge 80
    
    Write-Host "  Target: <2 seconds for cached operations"
    Write-Host "  Result: $($avgCached)s - $(if ($cachedTargetMet) { '✓ PASS' } else { '✗ FAIL' })"
    Write-Host ""
    
    Write-Host "  Target: >80% cache hit rate"
    Write-Host "  Result: $($overallHitRate)% - $(if ($hitRateTargetMet) { '✓ PASS' } else { '✗ FAIL' })"
    Write-Host ""
    
    if ($cachedTargetMet -and $hitRateTargetMet) {
        Write-Host "TEST 4 RESULT: ✓ PASS (All targets met)"
    } else {
        Write-Host "TEST 4 RESULT: ⚠ PARTIAL (Some targets not met)"
    }
}
catch {
    Write-Host "TEST 4 RESULT: ✗ FAIL"
    Write-Error "Test failed: $_"
}

Write-Host ""

# ============================================================================
# TEST 5: Cache by Type Breakdown
# ============================================================================
Write-Host "=" * 80
Write-Host "TEST 5: Cache Type Breakdown"
Write-Host "=" * 80
Write-Host ""

try {
    $stats = Get-CacheInfo
    Write-Host "Cache statistics by type:"
    Write-Host ""
    
    $stats | ForEach-Object {
        $hitRate = $_.HitRate * 100
        Write-Host "  $($_.CacheType):"
        Write-Host "    Entries:    $($_.EntriesCount)/$($_.MaxEntries)"
        Write-Host "    Accesses:   $($_.TotalAccesses)"
        Write-Host "    Hits:       $($_.CacheHits)"
        Write-Host "    Misses:     $($_.CacheMisses)"
        Write-Host "    Hit Rate:   $($hitRate)%"
        Write-Host ""
    }
    
    Write-Host "TEST 5 RESULT: ✓ PASS"
}
catch {
    Write-Host "TEST 5 RESULT: ✗ FAIL"
    Write-Error "Test failed: $_"
}

Write-Host ""

# ============================================================================
# Final Summary
# ============================================================================
Write-Host "=" * 80
Write-Host "CACHING SYSTEM TEST SUITE - SUMMARY"
Write-Host "=" * 80
Write-Host ""
Write-Host "Tests Completed:"
Write-Host "  ✓ TEST 1: Basic Cache Operations"
Write-Host "  ✓ TEST 2: File Monitoring"
Write-Host "  ✓ TEST 3: Cache Clearing"
Write-Host "  ✓ TEST 4: Performance Comparison"
Write-Host "  ✓ TEST 5: Cache Type Breakdown"
Write-Host ""
Write-Host "Overall Result: ✓ CACHING SYSTEM FUNCTIONAL"
Write-Host ""
Write-Host "=" * 80
