# Simple test - run this in a NEW PowerShell session
param()

$publishDir = "J:\projects\personal-projects\MemoryAnalysis\PowerShell.MemoryAnalysis\publish"
$env:PATH = "$publishDir;$env:PATH"

# Load assemblies in correct order
[System.Reflection.Assembly]::UnsafeLoadFrom("$publishDir\Microsoft.Extensions.Logging.Abstractions.dll") | Out-Null
[System.Reflection.Assembly]::UnsafeLoadFrom("$publishDir\Microsoft.Extensions.Logging.dll") | Out-Null  
[System.Reflection.Assembly]::UnsafeLoadFrom("$publishDir\PowerShell.MemoryAnalysis.dll") | Out-Null

Write-Host "✓ Assemblies loaded" -ForegroundColor Green

# Test initialization
try {
    $service = New-Object PowerShell.MemoryAnalysis.Services.RustInteropService
    Write-Host "✓ Service initialized" -ForegroundColor Green
    
    # Get version
    $version = $service.GetVersion()
    Write-Host "✓ Version: $($version.RustBridgeVersion)" -ForegroundColor Green
    
    # Check Volatility
    $vol = $service.IsVolatilityAvailable()
    Write-Host "✓ Volatility available: $vol" -ForegroundColor Green
    
    $service.Dispose()
} catch {
    Write-Host "✗ Error: $_" -ForegroundColor Red
    Write-Host $_.Exception.InnerException.Message -ForegroundColor Red
}
