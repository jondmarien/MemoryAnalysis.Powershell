#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates MAML help files for the MemoryAnalysis module.

.DESCRIPTION
    This script uses platyPS to generate MAML (Microsoft Assistance Markup Language)
    help files from the module's XML documentation and creates markdown documentation.

.EXAMPLE
    .\Generate-Help.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Import platyPS
Import-Module platyPS -ErrorAction Stop

# Paths
$ModulePath = Join-Path $PSScriptRoot "..\PowerShell.MemoryAnalysis\publish"
$DocsPath = Join-Path $PSScriptRoot "..\docs\help"
$OutputPath = Join-Path $ModulePath "en-US"

Write-Host "Generating help files for MemoryAnalysis module..." -ForegroundColor Cyan

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputPath)) {
    New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null
}

# Import the module
Write-Host "Importing module from: $ModulePath" -ForegroundColor Yellow
Import-Module (Join-Path $ModulePath "MemoryAnalysis.psd1") -Force

# Generate markdown help files
Write-Host "Generating Markdown documentation..." -ForegroundColor Yellow
New-MarkdownHelp -Module MemoryAnalysis -OutputFolder $DocsPath -Force -ErrorAction Continue

# Generate MAML (XML) help file from markdown
Write-Host "Generating MAML help file..." -ForegroundColor Yellow
New-ExternalHelp -Path $DocsPath -OutputPath $OutputPath -Force

Write-Host "`nHelp generation complete!" -ForegroundColor Green
Write-Host "  Markdown docs: $DocsPath" -ForegroundColor Gray
Write-Host "  MAML help: $OutputPath\MemoryAnalysis-help.xml" -ForegroundColor Gray

# Test the help
Write-Host "`nTesting help content..." -ForegroundColor Cyan
$testCmdlets = @('Get-MemoryDump', 'Analyze-ProcessTree', 'Get-ProcessCommandLine', 'Get-ProcessDll')

foreach ($cmdlet in $testCmdlets) {
    try {
        $help = Get-Help $cmdlet -Full -ErrorAction Stop
        $hasExamples = $help.examples.example.Count -gt 0
        $hasSynopsis = -not [string]::IsNullOrWhiteSpace($help.Synopsis)
        
        $status = if ($hasSynopsis -and $hasExamples) { "✓" } else { "⚠" }
        Write-Host "  $status $cmdlet - Synopsis: $hasSynopsis, Examples: $($help.examples.example.Count)" -ForegroundColor $(if ($hasSynopsis -and $hasExamples) { "Green" } else { "Yellow" })
    }
    catch {
        Write-Host "  ✗ $cmdlet - Error: $_" -ForegroundColor Red
    }
}

Write-Host "`nDone! Re-import the module to use the new help:" -ForegroundColor Cyan
Write-Host "  Remove-Module MemoryAnalysis; Import-Module '$ModulePath\MemoryAnalysis.psd1'" -ForegroundColor Gray
