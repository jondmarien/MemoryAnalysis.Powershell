# Memory Dump Acquisition Helper
# This script helps you create a memory dump for testing

Write-Host @"

=== Memory Dump Acquisition Guide ===

To create a memory dump for testing, you have several options:

1. COMAE TOOLKIT (DumpIt) - Easiest, Free
   Download: https://www.comae.com/
   
   Usage:
   - Download and extract DumpIt
   - Run DumpIt.exe as Administrator
   - It will create a .raw file in the same directory
   
2. FTK Imager - Free, GUI-based
   Download: https://www.exterro.com/ftk-imager
   
   Usage:
   - Open FTK Imager as Administrator
   - File -> Capture Memory
   - Choose destination and click "Capture Memory"
   
3. WinPmem - Free, Command-line
   Download: https://github.com/Velocidex/WinPmem/releases
   
   Usage:
   - Run as Administrator: winpmem_mini_x64.exe memory.raw
   
4. Task Manager (Quick test - small dump)
   - Open Task Manager as Administrator
   - Find a process (e.g., explorer.exe)
   - Right-click -> Create dump file
   - This creates a process dump (not full memory, but good for testing)

=== Creating a Test Dump with Task Manager ===

"@ -ForegroundColor Cyan

$choice = Read-Host @"

Would you like to create a quick test dump using Task Manager? (y/n)
This will create a small process dump for testing the module
"@

if ($choice -eq 'y') {
    Write-Host "`nOpening Task Manager..." -ForegroundColor Yellow
    Write-Host @"
    
Steps:
1. Task Manager will open
2. Go to the "Details" tab
3. Find a process (like 'explorer.exe' or 'powershell.exe')
4. Right-click the process -> Create dump file
5. The .dmp file will be saved to: C:\Users\$env:USERNAME\AppData\Local\Temp\
6. Copy it to: J:\projects\personal-projects\MemoryAnalysis\samples\

Press Enter to open Task Manager...
"@ -ForegroundColor Green
    
    Read-Host
    
    # Open Task Manager
    Start-Process "taskmgr.exe"
    
    # Create samples directory
    $samplesDir = "J:\projects\personal-projects\MemoryAnalysis\samples"
    if (!(Test-Path $samplesDir)) {
        New-Item -ItemType Directory -Path $samplesDir | Out-Null
        Write-Host "`nCreated samples directory: $samplesDir" -ForegroundColor Green
    }
    
    Write-Host "`nAfter creating the dump file, copy it to:" -ForegroundColor Yellow
    Write-Host "  $samplesDir" -ForegroundColor Cyan
    Write-Host "`nThen test with:" -ForegroundColor Yellow
    Write-Host "  Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1" -ForegroundColor Cyan
    Write-Host "  Get-MemoryDump -Path $samplesDir\<your-dump-file>.dmp" -ForegroundColor Cyan
}
else {
    Write-Host "`nFor full system memory dumps, I recommend:" -ForegroundColor Yellow
    Write-Host "  1. Download DumpIt from https://www.comae.com/" -ForegroundColor Cyan
    Write-Host "  2. Run as Administrator" -ForegroundColor Cyan
    Write-Host "  3. It will create a .raw file" -ForegroundColor Cyan
    Write-Host "`nNote: Full memory dumps can be several GB in size!" -ForegroundColor Yellow
}

Write-Host "`n=== Alternative: Use Sample Dumps ===" -ForegroundColor Cyan
Write-Host @"

If you want to test with existing samples, check out:
- Volatility Test Images: https://github.com/volatilityfoundation/volatility/wiki/Memory-Samples
- DFIR Training Images: Various forensic training sites provide sample dumps

"@ -ForegroundColor Gray

Write-Host "=== Quick Test with Dummy File ===" -ForegroundColor Cyan
Write-Host "Want to create a dummy file just to test the cmdlet loading? (y/n): " -NoNewline -ForegroundColor Yellow

$dummyChoice = Read-Host

if ($dummyChoice -eq 'y') {
    $samplesDir = "J:\projects\personal-projects\MemoryAnalysis\samples"
    if (!(Test-Path $samplesDir)) {
        New-Item -ItemType Directory -Path $samplesDir | Out-Null
    }
    
    $dummyPath = Join-Path $samplesDir "test-dummy.raw"
    
    # Create a dummy file with some data
    $dummyData = "DUMMY MEMORY DUMP FOR TESTING`n" * 1000
    $dummyData | Out-File -FilePath $dummyPath -Encoding ASCII
    
    Write-Host "`n✓ Created dummy test file: $dummyPath" -ForegroundColor Green
    Write-Host "`nTest it with:" -ForegroundColor Yellow
    Write-Host "  Import-Module .\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1" -ForegroundColor Cyan
    Write-Host "  Get-MemoryDump -Path $dummyPath" -ForegroundColor Cyan
}
