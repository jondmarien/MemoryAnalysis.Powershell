using System.Text.Json.Serialization;

namespace PowerShell.MemoryAnalysis.Models;

/// <summary>
/// Represents a network connection found in memory.
/// </summary>
public class NetworkConnectionInfo
{
    /// <summary>
    /// Process ID that owns this connection
    /// </summary>
    [JsonPropertyName("pid")]
    public uint Pid { get; set; }
    
    /// <summary>
    /// Process name that owns this connection
    /// </summary>
    [JsonPropertyName("process_name")]
    public string ProcessName { get; set; } = string.Empty;
    
    /// <summary>
    /// Local IP address
    /// </summary>
    [JsonPropertyName("local_address")]
    public string LocalAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Local port number
    /// </summary>
    [JsonPropertyName("local_port")]
    public ushort LocalPort { get; set; }
    
    /// <summary>
    /// Foreign/remote IP address
    /// </summary>
    [JsonPropertyName("foreign_address")]
    public string ForeignAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Foreign/remote port number
    /// </summary>
    [JsonPropertyName("foreign_port")]
    public ushort ForeignPort { get; set; }
    
    /// <summary>
    /// Protocol (TCP, UDP, etc.)
    /// </summary>
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;
    
    /// <summary>
    /// Connection state (ESTABLISHED, LISTENING, CLOSED, etc.)
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
    
    /// <summary>
    /// Connection creation time
    /// </summary>
    [JsonPropertyName("created_time")]
    public string CreatedTime { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets formatted local endpoint
    /// </summary>
    public string LocalEndpoint => $"{LocalAddress}:{LocalPort}";
    
    /// <summary>
    /// Gets formatted foreign endpoint
    /// </summary>
    public string ForeignEndpoint => $"{ForeignAddress}:{ForeignPort}";
}
