using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PowerShell.MemoryAnalysis.Services;

/// <summary>
/// Executes the Collect-MemoryDump.ps1 script in an isolated runspace.
/// </summary>
public static class CollectMemoryDumpRunner
{
    public static void Run(string scriptPath, string toolArgument, Action<ErrorRecord>? onError = null)
    {
        var scriptDir = Path.GetDirectoryName(scriptPath)
            ?? throw new InvalidOperationException("Invalid script path.");

        using var ps = System.Management.Automation.PowerShell.Create();
        ps.Runspace = RunspaceFactory.CreateRunspace();
        ps.Runspace.Open();
        // Pass the tool flag as a quoted positional argument so scripts that parse $args[0]
        // (Collect-MemoryDump.ps1) and minimal test stubs both work without a param() block.
        ps.AddScript($"Set-Location -LiteralPath '{EscapeSingleQuoted(scriptDir)}'")
            .AddScript($"& '{EscapeSingleQuoted(scriptPath)}' '{EscapeSingleQuoted(toolArgument)}'");

        ps.Invoke();
        if (ps.HadErrors)
        {
            foreach (var err in ps.Streams.Error)
            {
                onError?.Invoke(err);
            }

            throw new InvalidOperationException("Collect-MemoryDump reported errors.");
        }
    }

    private static string EscapeSingleQuoted(string value) => value.Replace("'", "''");
}
