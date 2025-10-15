using System.Text.Json.Serialization;

namespace PowerShell.MemoryAnalysis.Models;

/// <summary>
/// Represents command line information for a process in memory.
/// </summary>
public class CommandLineInfo
{
    /// <summary>
    /// Process ID
    /// </summary>
    [JsonPropertyName("pid")]
    public uint Pid { get; set; }
    
    /// <summary>
    /// Process name
    /// </summary>
    [JsonPropertyName("process_name")]
    public string ProcessName { get; set; } = string.Empty;
    
    /// <summary>
    /// Full command line with arguments
    /// </summary>
    [JsonPropertyName("command_line")]
    public string CommandLine { get; set; } = string.Empty;
}
