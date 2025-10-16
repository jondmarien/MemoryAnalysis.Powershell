Import-Module "J:\projects\personal-projects\MemoryAnalysis\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1" -Force

Write-Host "=" * 80
Write-Host "CACHE INTEGRATION DEBUG TEST"
Write-Host "=" * 80
Write-Host ""

# Load dump
$dump = Get-MemoryDump -Path "F:\physmem.raw"
Write-Host "Dump loaded: $($dump.Path)"
Write-Host ""

# Check cache BEFORE
$before = Get-CacheInfo
Write-Host "CACHE STATE BEFORE ANALYSIS:"
$before | Format-Table -AutoSize
$beforeTotal = ($before | Measure-Object -Property CacheHits, CacheMisses -Sum)
Write-Host "Total Hits: $($beforeTotal[0].Sum) | Total Misses: $($beforeTotal[1].Sum)"
Write-Host ""

# Run first analysis
Write-Host "Running first analysis (should miss cache)..."
$time1 = Measure-Command {
    $procs = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
}
Write-Host "Time: $($time1.TotalSeconds)s"
Write-Host ""

# Check cache AFTER first
$after1 = Get-CacheInfo
Write-Host "CACHE STATE AFTER FIRST ANALYSIS:"
$after1 | Format-Table -AutoSize
$after1Total = ($after1 | Measure-Object -Property CacheHits, CacheMisses -Sum)
Write-Host "Total Hits: $($after1Total[0].Sum) | Total Misses: $($after1Total[1].Sum)"
Write-Host ""

# Run second analysis (should hit cache)
Write-Host "Running second analysis (should hit cache)..."
$time2 = Measure-Command {
    $procs = Test-ProcessTree -MemoryDump $dump -ErrorAction SilentlyContinue | Out-Null
}
Write-Host "Time: $($time2.TotalSeconds)s"
Write-Host ""

# Check cache AFTER second
$after2 = Get-CacheInfo
Write-Host "CACHE STATE AFTER SECOND ANALYSIS:"
$after2 | Format-Table -AutoSize
$after2Total = ($after2 | Measure-Object -Property CacheHits, CacheMisses -Sum)
Write-Host "Total Hits: $($after2Total[0].Sum) | Total Misses: $($after2Total[1].Sum)"
Write-Host ""

# Analysis
Write-Host "ANALYSIS:"
Write-Host "First analysis time: $($time1.TotalSeconds)s"
Write-Host "Second analysis time: $($time2.TotalSeconds)s"
Write-Host "Cache hits recorded: $($after2Total[0].Sum)"
Write-Host "Cache misses recorded: $($after2Total[1].Sum)"

if ($after2Total[0].Sum -eq 0 -and $after2Total[1].Sum -eq 0) {
    Write-Host ""
    Write-Host "⚠️  ISSUE FOUND: Cache is not being used by cmdlets!"
    Write-Host "The cmdlets are calling RustInteropService directly, not CachingService."
    Write-Host "This means the cache infrastructure is built but not integrated."
} elseif ($time2.TotalSeconds -lt $time1.TotalSeconds) {
    Write-Host ""
    Write-Host "✓ Cache is working and improving performance!"
} else {
    Write-Host ""
    Write-Host "⚠️  Cache was accessed but didn't improve performance."
}
