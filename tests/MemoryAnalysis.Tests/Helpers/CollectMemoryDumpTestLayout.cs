namespace MemoryAnalysis.Tests.Helpers;

/// <summary>
/// Creates a minimal Collect-MemoryDump directory layout for acquisition tests.
/// </summary>
internal static class CollectMemoryDumpTestLayout
{
    public static CollectMemoryDumpFixture Create(string root, string? dumpFileName = "captured.raw")
    {
        var scriptDir = Path.Combine(root, "third-party", "Collect-MemoryDump");
        Directory.CreateDirectory(Path.Combine(scriptDir, "Tools", "WinPMEM"));
        var scriptPath = Path.Combine(scriptDir, "Collect-MemoryDump.ps1");
        var dumpPath = dumpFileName == null ? null : Path.Combine(root, dumpFileName);
        var scriptBody = dumpPath == null
            ? "if ($args.Count -lt 1 -or $args[0] -ne '-WinPMEM') { exit 0 }; Write-Output 'noop'"
            : $"if ($args.Count -lt 1 -or $args[0] -ne '-WinPMEM') {{ exit 0 }}; Set-Content -LiteralPath '{dumpPath.Replace("'", "''")}' -Value 'acquired'";
        File.WriteAllText(scriptPath, scriptBody);
        File.WriteAllText(
            Path.Combine(scriptDir, "Tools", "WinPMEM", "winpmem_mini_x64_rc2.exe"),
            string.Empty);

        return new CollectMemoryDumpFixture(root, scriptPath, dumpPath);
    }

    internal readonly record struct CollectMemoryDumpFixture(string Root, string ScriptPath, string? DumpPath);
}
