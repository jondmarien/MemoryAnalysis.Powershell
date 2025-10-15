using System;
using System.Collections.Generic;

namespace PowerShell.MemoryAnalysis.Models;

/// <summary>
/// Represents detailed process information extracted from a memory dump.
/// </summary>
public class ProcessTreeInfo
{
    /// <summary>
    /// Process ID.
    /// </summary>
    public uint Pid { get; set; }

    /// <summary>
    /// Parent Process ID.
    /// </summary>
    public uint Ppid { get; set; }

    /// <summary>
    /// Process name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Memory offset of the process structure.
    /// </summary>
    public string Offset { get; set; } = string.Empty;

    /// <summary>
    /// Number of threads.
    /// </summary>
    public uint Threads { get; set; }

    /// <summary>
    /// Number of handles.
    /// </summary>
    public uint Handles { get; set; }

    /// <summary>
    /// Process creation time.
    /// </summary>
    public string CreateTime { get; set; } = string.Empty;

    /// <summary>
    /// Command line arguments (if available).
    /// </summary>
    public string? CommandLine { get; set; }

    /// <summary>
    /// Process tree depth (0 for root processes).
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Child processes.
    /// </summary>
    public List<ProcessTreeInfo> Children { get; set; } = new();

    /// <summary>
    /// Whether this process is suspicious.
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    /// Reasons why this process is flagged as suspicious.
    /// </summary>
    public List<string> SuspiciousReasons { get; set; } = new();

    /// <summary>
    /// Get a tree-formatted string representation.
    /// </summary>
    public string ToTreeString()
    {
        return ToTreeString(0);
    }

    /// <summary>
    /// Get a tree-formatted string with indentation.
    /// </summary>
    private string ToTreeString(int depth)
    {
        var indent = new string(' ', depth * 2);
        var prefix = depth > 0 ? "├─ " : "";
        var suspicious = IsSuspicious ? " [SUSPICIOUS]" : "";
        var result = $"{indent}{prefix}{Name} (PID: {Pid}){suspicious}\n";

        foreach (var child in Children)
        {
            result += child.ToTreeString(depth + 1);
        }

        return result;
    }

    /// <summary>
    /// Get a summary string.
    /// </summary>
    public override string ToString()
    {
        return $"{Name} (PID: {Pid}, PPID: {Ppid}, Threads: {Threads})";
    }
}
