using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace PowerShell.MemoryAnalysis.Services;

/// <summary>
/// P/Invoke interop service for calling Rust bridge functions.
/// Handles native library loading and marshaling between C# and Rust.
/// </summary>
public class RustInteropService : IDisposable
{
    private const string LibraryName = "rust_bridge";
    private readonly ILogger<RustInteropService> _logger;
    private bool _initialized = false;
    private bool _disposed = false;

    public RustInteropService()
    {
        _logger = LoggingService.GetLogger<RustInteropService>();
        Initialize();
    }

    #region P/Invoke Declarations

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int rust_bridge_initialize();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int rust_bridge_check_volatility();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr rust_bridge_get_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void rust_bridge_free_string(IntPtr ptr);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr rust_bridge_list_processes([MarshalAs(UnmanagedType.LPWStr)] string dumpPath);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr rust_bridge_get_command_lines([MarshalAs(UnmanagedType.LPWStr)] string dumpPath);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr rust_bridge_list_dlls([MarshalAs(UnmanagedType.LPWStr)] string dumpPath, uint pid);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr rust_bridge_scan_network_connections([MarshalAs(UnmanagedType.LPWStr)] string dumpPath);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr rust_bridge_detect_malware([MarshalAs(UnmanagedType.LPWStr)] string dumpPath);

    #endregion

    /// <summary>
    /// Initialize the Rust bridge.
    /// </summary>
    private void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _logger.LogInformation("Initializing Rust bridge...");

        try
        {
            int result = rust_bridge_initialize();
            if (result != 0)
            {
                throw new InvalidOperationException($"Failed to initialize Rust bridge. Error code: {result}");
            }

            _logger.LogInformation("Rust bridge initialized successfully");
            _initialized = true;
        }
        catch (DllNotFoundException ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to load Rust bridge library '{LibraryName}.dll'. Ensure it's in the module directory.", LibraryName);
            }
            throw new InvalidOperationException(
                string.Format("Could not load the Rust bridge library ({0}.dll). Please ensure the native library is present in the module directory.", LibraryName), ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Rust bridge initialization");
            throw;
        }
    }

    /// <summary>
    /// Check if Volatility3 is available in the Python environment.
    /// </summary>
    /// <returns>True if Volatility3 is available, false otherwise</returns>
    public bool IsVolatilityAvailable()
    {
        EnsureInitialized();

        try
        {
            int result = rust_bridge_check_volatility();
            return result switch
            {
                1 => true,
                0 => false,
                _ => throw new InvalidOperationException($"Volatility check failed with error code: {result}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Volatility availability");
            throw;
        }
    }

    /// <summary>
    /// Get version information from the Rust bridge.
    /// </summary>
    /// <returns>Version information object</returns>
    public VersionInfo GetVersion()
    {
        EnsureInitialized();

        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = rust_bridge_get_version();
            if (ptr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to get version information from Rust bridge");
            }

            string json = MarshalStringFromRust(ptr);
            var versionInfo = JsonSerializer.Deserialize<VersionInfo>(json) ?? throw new InvalidOperationException("Failed to deserialize version information");
            return versionInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version information");
            throw;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                rust_bridge_free_string(ptr);
            }
        }
    }

    /// <summary>
    /// List processes in a memory dump.
    /// </summary>
    /// <param name="dumpPath">Path to the memory dump file</param>
    /// <returns>Array of process information</returns>
    public ProcessInfo[] ListProcesses(string dumpPath)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(dumpPath))
        {
            throw new ArgumentException("Dump path cannot be null or empty", nameof(dumpPath));
        }

        if (!File.Exists(dumpPath))
        {
            throw new FileNotFoundException($"Memory dump file not found: {dumpPath}", dumpPath);
        }

        IntPtr ptr = IntPtr.Zero;
        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Listing processes from dump: {DumpPath}", dumpPath);
            }

            ptr = rust_bridge_list_processes(dumpPath);
            if (ptr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to list processes from dump: {dumpPath}");
            }

            string json = MarshalStringFromRust(ptr);
            var processes = JsonSerializer.Deserialize<ProcessInfo[]>(json) ?? throw new InvalidOperationException("Failed to deserialize process information");
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Found {ProcessCount} processes in dump", processes.Length);
            }
            return processes;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error listing processes from dump: {DumpPath}", dumpPath);
            }
            throw;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                rust_bridge_free_string(ptr);
            }
        }
    }

    /// <summary>
    /// Get command line arguments for processes in a memory dump.
    /// </summary>
    /// <param name="dumpPath">Path to the memory dump file</param>
    /// <returns>Array of command line information</returns>
    public Models.CommandLineInfo[] GetCommandLines(string dumpPath)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(dumpPath))
        {
            throw new ArgumentException("Dump path cannot be null or empty", nameof(dumpPath));
        }

        if (!File.Exists(dumpPath))
        {
            throw new FileNotFoundException($"Memory dump file not found: {dumpPath}", dumpPath);
        }

        IntPtr ptr = IntPtr.Zero;
        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Getting command lines from dump: {DumpPath}", dumpPath);
            }

            ptr = rust_bridge_get_command_lines(dumpPath);
            if (ptr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to get command lines from dump: {dumpPath}");
            }

            string json = MarshalStringFromRust(ptr);
            var commandLines = JsonSerializer.Deserialize<Models.CommandLineInfo[]>(json) ??
                throw new InvalidOperationException("Failed to deserialize command line information");
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Retrieved {Count} command lines", commandLines.Length);
            }
            return commandLines;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error getting command lines from dump: {DumpPath}", dumpPath);
            }
            throw;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                rust_bridge_free_string(ptr);
            }
        }
    }

    /// <summary>
    /// List DLLs loaded by processes in a memory dump.
    /// </summary>
    /// <param name="dumpPath">Path to the memory dump file</param>
    /// <param name="pid">Optional process ID to filter by (null = all processes)</param>
    /// <returns>Array of DLL information</returns>
    public Models.DllInfo[] ListDlls(string dumpPath, uint? pid = null)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(dumpPath))
        {
            throw new ArgumentException("Dump path cannot be null or empty", nameof(dumpPath));
        }

        if (!File.Exists(dumpPath))
        {
            throw new FileNotFoundException($"Memory dump file not found: {dumpPath}", dumpPath);
        }

        IntPtr ptr = IntPtr.Zero;
        try
        {
            var pidFilter = pid ?? 0; // 0 = no filter
            if (pid.HasValue)
            {
                _logger.LogInformation("Listing DLLs from dump: {DumpPath} (filtered to PID {Pid})", dumpPath, pid.Value);
            }
            else
            {
                _logger.LogInformation("Listing DLLs from dump: {DumpPath}", dumpPath);
            }

            ptr = rust_bridge_list_dlls(dumpPath, pidFilter);
            if (ptr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to list DLLs from dump: {dumpPath}");
            }

            string json = MarshalStringFromRust(ptr);
            var dlls = JsonSerializer.Deserialize<Models.DllInfo[]>(json) ??
                throw new InvalidOperationException("Failed to deserialize DLL information");
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Retrieved {Count} DLLs", dlls.Length);
            }
            return dlls;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error listing DLLs from dump: {DumpPath}", dumpPath);
            }
            throw;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                rust_bridge_free_string(ptr);
            }
        }
    }

    /// <summary>
    /// Scan network connections in a memory dump.
    /// </summary>
    /// <param name="dumpPath">Path to the memory dump file</param>
    /// <returns>Array of network connection information</returns>
    public Models.NetworkConnectionInfo[] ScanNetworkConnections(string dumpPath)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(dumpPath))
        {
            throw new ArgumentException("Dump path cannot be null or empty", nameof(dumpPath));
        }

        if (!File.Exists(dumpPath))
        {
            throw new FileNotFoundException($"Memory dump file not found: {dumpPath}", dumpPath);
        }

        IntPtr ptr = IntPtr.Zero;
        try
        {
            _logger.LogInformation("Scanning network connections from dump: {DumpPath}", dumpPath);

            ptr = rust_bridge_scan_network_connections(dumpPath);
            if (ptr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to scan network connections from dump: {dumpPath}");
            }

            string json = MarshalStringFromRust(ptr);
            var connections = JsonSerializer.Deserialize<Models.NetworkConnectionInfo[]>(json) ??
                throw new InvalidOperationException("Failed to deserialize network connection information");
            _logger.LogInformation("Retrieved {Count} network connections", connections.Length);
            return connections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning network connections from dump: {DumpPath}", dumpPath);
            throw;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                rust_bridge_free_string(ptr);
            }
        }
    }

    /// <summary>
    /// Detect malware in a memory dump.
    /// </summary>
    /// <param name="dumpPath">Path to the memory dump file</param>
    /// <returns>Array of malware detections</returns>
    public Models.MalwareDetection[] DetectMalware(string dumpPath)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(dumpPath))
        {
            throw new ArgumentException("Dump path cannot be null or empty", nameof(dumpPath));
        }

        if (!File.Exists(dumpPath))
        {
            throw new FileNotFoundException($"Memory dump file not found: {dumpPath}", dumpPath);
        }

        IntPtr ptr = IntPtr.Zero;
        try
        {
            _logger.LogInformation("Detecting malware in dump: {DumpPath}", dumpPath);

            ptr = rust_bridge_detect_malware(dumpPath);
            if (ptr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to detect malware in dump: {dumpPath}");
            }

            string json = MarshalStringFromRust(ptr);
            var detections = JsonSerializer.Deserialize<Models.MalwareDetection[]>(json) ??
                throw new InvalidOperationException("Failed to deserialize malware detection information");
            _logger.LogInformation("Found {Count} potential malware detections", detections.Length);
            return detections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting malware in dump: {DumpPath}", dumpPath);
            throw;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                rust_bridge_free_string(ptr);
            }
        }
    }

    /// <summary>
    /// Marshal a string from Rust to C#.
    /// </summary>
    /// <param name="ptr">Pointer to the Rust string</param>
    /// <returns>Managed string</returns>
    private static string MarshalStringFromRust(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return string.Empty;
        }

        return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
    }

    /// <summary>
    /// Ensure the Rust bridge is initialized.
    /// </summary>
    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_initialized)
        {
            Initialize();
        }
    }

    /// <summary>
    /// Dispose of the service and release resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing Rust interop service");
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Version information from the Rust bridge.
/// </summary>
public class VersionInfo
{
    [JsonPropertyName("rust_bridge_version")]
    public string? RustBridgeVersion { get; set; }

    [JsonPropertyName("volatility_version")]
    public string? VolatilityVersion { get; set; }

    [JsonPropertyName("python_version")]
    public string? PythonVersion { get; set; }
}

/// <summary>
/// Process information from memory analysis.
/// </summary>
public class ProcessInfo
{
    [JsonPropertyName("pid")]
    public uint Pid { get; set; }

    [JsonPropertyName("ppid")]
    public uint Ppid { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("offset")]
    public string? Offset { get; set; }

    [JsonPropertyName("threads")]
    public uint Threads { get; set; }

    [JsonPropertyName("handles")]
    public uint Handles { get; set; }

    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }
}
