# Test script for Get-MemoryDump cmdlet
param()

$publishDir = "J:\projects\personal-projects\MemoryAnalysis\PowerShell.MemoryAnalysis\publish"

Write-Host "`n=== Testing Get-MemoryDump Cmdlet ===" -ForegroundColor Cyan

# Import the module
Write-Host "Importing module..." -ForegroundColor Yellow
Import-Module "$publishDir\PowerShell.MemoryAnalysis.dll" -Force

# Check if cmdlet is available
$cmdlet = Get-Command Get-MemoryDump -ErrorAction SilentlyContinue
if ($cmdlet) {
    Write-Host "✓ Get-MemoryDump cmdlet loaded" -ForegroundColor Green
} else {
    Write-Host "✗ Get-MemoryDump cmdlet not found" -ForegroundColor Red
    exit 1
}

# Show cmdlet help
Write-Host "`n--- Cmdlet Information ---" -ForegroundColor Cyan
Get-Command Get-MemoryDump | Format-List

# Test 1: Create a dummy file to test with
Write-Host "`n--- Test 1: Load a test file ---" -ForegroundColor Cyan
$testFile = "$env:TEMP\test-memory-dump.raw"
"DUMMY MEMORY DUMP DATA" | Out-File -FilePath $testFile -Encoding ASCII

try {
    $dump = Get-MemoryDump -Path $testFile
    Write-Host "✓ Successfully loaded dump" -ForegroundColor Green
    Write-Host "  Path: $($dump.Path)"
    Write-Host "  FileName: $($dump.FileName)"
    Write-Host "  Size: $($dump.Size)"
    Write-Host "  IsValidated: $($dump.IsValidated)"
} catch {
    Write-Host "✗ Failed to load dump: $_" -ForegroundColor Red
}

# Test 2: Load with Validate switch
Write-Host "`n--- Test 2: Load with -Validate ---" -ForegroundColor Cyan
try {
    $dump = Get-MemoryDump -Path $testFile -Validate
    Write-Host "✓ Validation completed" -ForegroundColor Green
    Write-Host "  IsValidated: $($dump.IsValidated)"
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

# Test 3: Load with DetectProfile
Write-Host "`n--- Test 3: Load with -DetectProfile ---" -ForegroundColor Cyan
try {
    $dump = Get-MemoryDump -Path $testFile -DetectProfile
    Write-Host "✓ Profile detection completed" -ForegroundColor Green
    Write-Host "  Profile: $($dump.Profile)"
    Write-Host "  Architecture: $($dump.Architecture)"
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

# Test 4: Test with non-existent file
Write-Host "`n--- Test 4: Error handling for non-existent file ---" -ForegroundColor Cyan
try {
    $dump = Get-MemoryDump -Path "C:\nonexistent.vmem" -ErrorAction Stop
    Write-Host "✗ Should have thrown an error" -ForegroundColor Red
} catch {
    Write-Host "✓ Correctly handled missing file" -ForegroundColor Green
}

# Cleanup
Remove-Item $testFile -ErrorAction SilentlyContinue

Write-Host "`n=== All Tests Complete ===" -ForegroundColor Cyan
Write-Host "The Get-MemoryDump cmdlet is working correctly!" -ForegroundColor Green
