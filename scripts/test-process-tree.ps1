Import-Module 'J:\projects\personal-projects\MemoryAnalysis\PowerShell.MemoryAnalysis\publish\PowerShell.MemoryAnalysis.dll' -Force

$dumpPath = 'J:\projects\personal-projects\MemoryAnalysis\samples\dumps\preview.DMP'
Write-Host "Loading memory dump: $dumpPath" -ForegroundColor Cyan

$dump = Get-MemoryDump -Path $dumpPath
Write-Host "Dump loaded: $($dump.FileName)" -ForegroundColor Green

Write-Host "`nAnalyzing process tree..." -ForegroundColor Cyan
$processes = $dump | Test-ProcessTree

Write-Host "Found $($processes.Count) processes" -ForegroundColor Green

Write-Host "`nFirst 10 processes:" -ForegroundColor Cyan
$processes | Select-Object -First 10 | Format-Table ProcessId, ProcessName, ParentProcessId, Offset, Threads, Handles -AutoSize
