using System.Collections.Generic;
using System.Management.Automation;

namespace PowerShell.MemoryAnalysis.Services;

/// <summary>
/// Writes messages to the PowerShell host from binary cmdlets.
/// </summary>
internal static class PSHostWriter
{
    public static void WriteHost(PSCmdlet cmdlet, string message)
    {
        cmdlet.WriteInformation(message, new[] { "PSHOST" });
    }
}
