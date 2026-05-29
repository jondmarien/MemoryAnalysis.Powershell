using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using Microsoft.PowerShell;

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

        var initialState = InitialSessionState.CreateDefault();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            initialState.ExecutionPolicy = ExecutionPolicy.Bypass;
        }

        using var ps = System.Management.Automation.PowerShell.Create();
        ps.Runspace = RunspaceFactory.CreateRunspace(initialState);
        ps.Runspace.Open();

        // Use '--' so -WinPMEM is passed into $args for scripts without a param() block.
        // Quoted positionals alone still bind as switches on Windows PowerShell hosts.
        ps.AddScript(
            $"Set-Location -LiteralPath '{EscapeSingleQuoted(scriptDir)}'; " +
            $"& '{EscapeSingleQuoted(scriptPath)}' -- {toolArgument}");

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
