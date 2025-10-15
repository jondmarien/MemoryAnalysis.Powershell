Import-Module .\PowerShell.MemoryAnalysis\bin\Debug\net10.0\PowerShell.MemoryAnalysis.dll

$dump = Get-MemoryDump -Path 'F:\physmem.raw'

Test-ProcessTree -MemoryDump $dump 2>&1 | Out-Host
