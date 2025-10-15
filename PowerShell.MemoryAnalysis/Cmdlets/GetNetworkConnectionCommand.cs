using System;
using System.Linq;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Models;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets;

/// <summary>
/// <para type="synopsis">Scans network connections in a memory dump.</para>
/// <para type="description">
/// The Get-NetworkConnection cmdlet uses Volatility 3's NetScan plugin to enumerate
/// all network connections (TCP/UDP) in a memory dump.
/// </para>
/// </summary>
[Cmdlet(VerbsCommon.Get, "NetworkConnection")]
[OutputType(typeof(NetworkConnectionInfo))]
public class GetNetworkConnectionCommand : PSCmdlet
{
    private ILogger<GetNetworkConnectionCommand>? _logger;
    private RustInteropService? _rustInterop;

    /// <summary>
    /// Memory dump to analyze.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNull]
    public MemoryDump? MemoryDump { get; set; }

    /// <summary>
    /// Filter by connection state (LISTENING, ESTABLISHED, CLOSED, etc.).
    /// </summary>
    [Parameter]
    public string? State { get; set; }

    /// <summary>
    /// Filter by protocol (TCP, UDP, etc.).
    /// </summary>
    [Parameter]
    public string? Protocol { get; set; }

    /// <summary>
    /// Filter by specific Process ID.
    /// </summary>
    [Parameter]
    public uint? Pid { get; set; }

    protected override void BeginProcessing()
    {
        _logger = LoggingService.GetLogger<GetNetworkConnectionCommand>();
        _rustInterop = new RustInteropService();
    }

    protected override void ProcessRecord()
    {
        if (MemoryDump == null) return;

        try
        {
            _logger.LogInformation("Scanning network connections from: {Path}", MemoryDump.Path);

            var connections = _rustInterop.ScanNetworkConnections(MemoryDump.Path);

            // Apply filters
            var filtered = connections.AsEnumerable();

            if (!string.IsNullOrEmpty(State))
            {
                filtered = filtered.Where(c => c.State.Equals(State, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(Protocol))
            {
                filtered = filtered.Where(c => c.Protocol.Equals(Protocol, StringComparison.OrdinalIgnoreCase));
            }

            if (Pid.HasValue)
            {
                filtered = filtered.Where(c => c.Pid == Pid.Value);
            }

            foreach (var connection in filtered)
            {
                WriteObject(connection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning network connections");
            WriteError(new ErrorRecord(
                ex,
                "NetworkScanFailed",
                ErrorCategory.InvalidOperation,
                MemoryDump));
        }
    }

    protected override void EndProcessing()
    {
        _rustInterop?.Dispose();
    }
}