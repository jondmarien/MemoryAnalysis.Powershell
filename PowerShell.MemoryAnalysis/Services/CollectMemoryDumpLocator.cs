using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PowerShell.MemoryAnalysis.Services;

/// <summary>
/// Locates the Collect-MemoryDump submodule script and WinPMEM tool paths.
/// </summary>
public static class CollectMemoryDumpLocator
{
    public static readonly string RelativeScriptPath = Path.Combine(
        "third-party",
        "Collect-MemoryDump",
        "Collect-MemoryDump.ps1");

    public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static string? FindScriptPath(string? startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return null;
        }

        var dir = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, RelativeScriptPath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }

    public static string GetWinPmemExecutablePath(string scriptPath)
    {
        var scriptDir = Path.GetDirectoryName(scriptPath)
            ?? throw new InvalidOperationException("Invalid Collect-MemoryDump script path.");

        if (Environment.Is64BitOperatingSystem)
        {
            return Path.Combine(scriptDir, "Tools", "WinPMEM", "winpmem_mini_x64_rc2.exe");
        }

        return Path.Combine(scriptDir, "Tools", "WinPMEM", "winpmem_mini_x86.exe");
    }

    public static string GetCollectMemoryDumpOutputRoot(string scriptPath) =>
        Path.GetDirectoryName(scriptPath) ?? scriptPath;
}
