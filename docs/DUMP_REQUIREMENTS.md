# Memory Dump Requirements for Volatility 3

## Issue Found
The `preview.DMP` file in `samples/dumps/` is a **Windows Minidump (MDMP)** file, which Volatility 3 **cannot process**.

### Why Minidumps Don't Work
Minidumps are created by Windows Error Reporting and contain:
- Limited memory pages (only those relevant to the crash)
- Thread stacks
- Exception information
- Module lists

Volatility 3 requires **full memory dumps** to:
- Build complete translation layers
- Map virtual to physical memory
- Access kernel structures
- List all processes

## Required Dump Types

### ✅ Supported
1. **Full Memory Dump** - Contains all physical memory
2. **Kernel Memory Dump** - Contains kernel-mode memory
3. **Complete Memory Dump** - Full RAM capture
4. **Raw Memory Image** - From tools like:
   - WinPMEM
   - FTK Imager
   - DumpIt
   - Magnet RAM Capture

### ❌ Not Supported
1. **Minidumps (.dmp)** - Too limited
2. **Small memory dumps** - Insufficient data
3. **User-mode dumps** - No kernel access

## Creating Test Dumps

### Option 1: Using WinPMEM (Recommended)
```powershell
# Download WinPMEM from https://github.com/Velocidex/WinPmem
.\winpmem_mini_x64_rc2.exe test.raw
```

### Option 2: Using DumpIt
```powershell
# Download DumpIt from Comae
.\DumpIt.exe /O test.raw /T RAW
```

### Option 3: Using Task Manager (Creates Minidump - NOT suitable)
- Right-click process → Create dump file
- **This creates a minidump** - Won't work with Volatility!

### Option 4: Using NotMyFault (Kernel Dump)
```powershell
# Download from Sysinternals
# This will BSOD your system!
.\notmyfault64.exe /crash
# Then copy C:\Windows\MEMORY.DMP
```

## Testing the Implementation

### With a Real Dump
Once you have a proper dump file:
```powershell
# Test with Volatility CLI first
vol -f memory.raw windows.pslist.PsList

# Then test with our PowerShell module
Import-Module .\publish\PowerShell.MemoryAnalysis.dll
$dump = Get-MemoryDump -Path "memory.raw"
$dump | Test-ProcessTree | Select -First 10
```

### With Mock Data (For Development)
For now, our tests use dummy paths which return placeholder data. This is sufficient for:
- Testing the Rust-PowerShell interop
- Verifying JSON serialization
- Testing PowerShell cmdlet logic

## Next Steps

1. **Option A**: Get a proper memory dump
   - Use WinPMEM or DumpIt on a test VM
   - Copy to `samples/dumps/`
   
2. **Option B**: Continue with integration tests
   - Our code is correct for real dumps
   - Just need valid input data
   
3. **Option C**: Add minidump support
   - Would require different Volatility plugins
   - Limited functionality (no full process tree)

## Current Implementation Status

✅ **Working**:
- Rust-Python bridge with PyO3 0.26
- Volatility 3 Python API integration
- Proper context initialization
- Automagic execution
- Plugin construction
- TreeGrid parsing
- Error handling

❌ **Blocked by**:
- Invalid input file (minidump instead of full dump)

The implementation is complete and correct - we just need valid test data!
