using System;
using System.IO;

namespace PowerShell.MemoryAnalysis.Models;

/// <summary>
/// Represents a memory dump file loaded for analysis.
/// </summary>
public class MemoryDump
{
    /// <summary>
    /// Full path to the memory dump file.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// File name of the memory dump.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>
    /// Size of the memory dump file in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Size of the memory dump file in a human-readable format.
    /// </summary>
    public string Size => FormatSize(SizeBytes);

    /// <summary>
    /// Detected operating system profile (if available).
    /// </summary>
    public string? Profile { get; set; }

    /// <summary>
    /// Architecture of the memory dump (x86, x64, ARM, etc.).
    /// </summary>
    public string? Architecture { get; set; }

    /// <summary>
    /// Operating system version detected in the dump.
    /// </summary>
    public string? OSVersion { get; set; }

    /// <summary>
    /// Whether the dump has been validated.
    /// </summary>
    public bool IsValidated { get; set; }

    /// <summary>
    /// Timestamp when the dump was loaded.
    /// </summary>
    public DateTime LoadedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Format a byte size into human-readable format.
    /// </summary>
    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Get a summary string representation of the memory dump.
    /// </summary>
    public override string ToString()
    {
        return $"{FileName} ({Size})";
    }
}
