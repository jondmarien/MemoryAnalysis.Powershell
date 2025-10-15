# How to File the Volatility3 Bug Report

## Steps to Report the Issue

1. **Go to the Volatility3 GitHub Issues page:**
   https://github.com/volatilityfoundation/volatility3/issues

2. **Click "New Issue"**

3. **Title:** `NetScan and Malfind fail on Windows 11 Build 26100 with PagedInvalidAddressException`

4. **Copy the contents of `VOLATILITY_BUG_REPORT.md`** into the issue description

5. **Add the "bug" label** if available

6. **Submit the issue**

## What to Expect

- The Volatility team may ask for additional information
- They may request the memory dump (you can decline if it's sensitive)
- They may provide a workaround or patch
- The issue may be fixed in a future release

## Workaround for Now

The following cmdlets are **working** on Windows 11 Build 26100:
- ✅ `Get-MemoryDump` - Load memory dumps
- ✅ `Test-ProcessTree` - Analyze process hierarchy (830 processes)
- ✅ `Get-ProcessCommandLine` - Extract command lines (830 entries)
- ✅ `Get-ProcessDll` - Enumerate loaded DLLs (30,947 DLLs)

The following cmdlets are **disabled** due to compatibility issues:
- ❌ `Get-NetworkConnection` - Disabled (PagedInvalidAddressException)
- ❌ `Find-Malware` - Disabled (returns 0 results)

## Module Status

The PowerShell Memory Analysis module has been updated to:
1. **Disable the non-working cmdlets** in the manifest
2. **Add compatibility notes** to the module description
3. **Update release notes** to reflect Windows 11 Build 26100 limitations

## When the Bug is Fixed

Once Volatility3 releases a fix:

1. Update Volatility3:
   ```powershell
   uv pip install --upgrade volatility3
   ```

2. Re-enable the cmdlets in `PowerShell.MemoryAnalysis\MemoryAnalysis.psd1`:
   ```powershell
   CmdletsToExport = @(
       'Get-MemoryDump'
       'Test-ProcessTree'
       'Get-ProcessCommandLine'
       'Get-ProcessDll'
       'Get-NetworkConnection'  # Re-enable after fix
       'Find-Malware'           # Re-enable after fix
   )
   ```

3. Rebuild the module:
   ```powershell
   .\Build.ps1
   ```

## Alternative Memory Dumps

If you need network and malware scanning:
- Create a memory dump from an older Windows version (Windows 10 21H2 or earlier)
- Volatility 3 has better support for older Windows builds
- The same module code will work once you have a compatible dump

## Contact

If you have questions about this bug report or the module, please check:
- Volatility3 GitHub: https://github.com/volatilityfoundation/volatility3
- Module README: `README.md`
- WARP Guide: `WARP.md`
