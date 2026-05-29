using System;

namespace PowerShell.MemoryAnalysis.Models;

/// <summary>
/// A memory dump file discovered on disk before analysis.
/// </summary>
public class DiscoveredMemoryDump
{
    public string Path { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime LastWriteTimeUtc { get; set; }

    public string Size => FormatSize(SizeBytes);

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    public override string ToString() => $"{System.IO.Path.GetFileName(Path)} ({Size})";
}
