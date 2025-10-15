using System.Text.Json.Serialization;

namespace PowerShell.MemoryAnalysis.Models;

/// <summary>
/// Represents a DLL loaded by a process in memory.
/// </summary>
public class DllInfo
{
    /// <summary>
    /// Process ID that loaded this DLL
    /// </summary>
    [JsonPropertyName("pid")]
    public uint Pid { get; set; }
    
    /// <summary>
    /// Process name that loaded this DLL
    /// </summary>
    [JsonPropertyName("process_name")]
    public string ProcessName { get; set; } = string.Empty;
    
    /// <summary>
    /// Base address of DLL in memory (hexadecimal)
    /// </summary>
    [JsonPropertyName("base_address")]
    public string BaseAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Size of DLL in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public ulong Size { get; set; }
    
    /// <summary>
    /// DLL filename (e.g., "kernel32.dll")
    /// </summary>
    [JsonPropertyName("dll_name")]
    public string DllName { get; set; } = string.Empty;
    
    /// <summary>
    /// Full path to DLL (e.g., "C:\Windows\System32\kernel32.dll")
    /// </summary>
    [JsonPropertyName("dll_path")]
    public string DllPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets formatted size in human-readable format (KB/MB)
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (Size < 1024)
                return $"{Size} B";
            else if (Size < 1024 * 1024)
                return $"{Size / 1024.0:F2} KB";
            else
                return $"{Size / (1024.0 * 1024.0):F2} MB";
        }
    }
}
