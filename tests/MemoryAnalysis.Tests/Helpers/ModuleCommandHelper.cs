using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using PowerShell.MemoryAnalysis.Cmdlets;
using PsPowerShell = System.Management.Automation.PowerShell;

namespace MemoryAnalysis.Tests.Helpers;

/// <summary>
/// Imports the MemoryAnalysis binary module into a runspace for cmdlet integration tests.
/// </summary>
internal sealed class ModuleCommandHelper : IDisposable
{
    private readonly Runspace _runspace;
    private bool _disposed;

    public PsPowerShell PowerShell { get; }

    public static string ModuleAssemblyPath =>
        typeof(ResolveMemoryDumpPathCommand).Assembly.Location;

    private ModuleCommandHelper(Runspace runspace, PsPowerShell powerShell)
    {
        _runspace = runspace;
        PowerShell = powerShell;
    }

    public static ModuleCommandHelper Create(PSHost? host = null)
    {
        var initialState = InitialSessionState.CreateDefault();
        var runspace = host == null
            ? RunspaceFactory.CreateRunspace(initialState)
            : RunspaceFactory.CreateRunspace(host, initialState);
        runspace.Open();

        var powerShell = PsPowerShell.Create();
        powerShell.Runspace = runspace;
        powerShell.AddCommand("Import-Module").AddArgument(ModuleAssemblyPath).Invoke();

        if (powerShell.HadErrors)
        {
            var message = string.Join(
                Environment.NewLine,
                powerShell.Streams.Error.Select(static e => e.ToString()));
            throw new InvalidOperationException($"Failed to import MemoryAnalysis module: {message}");
        }

        powerShell.Commands.Clear();
        return new ModuleCommandHelper(runspace, powerShell);
    }

    public IList<PSObject> InvokeResolveMemoryDumpPath(params (string Name, object Value)[] parameters)
    {
        PowerShell.Commands.Clear();
        var command = PowerShell.AddCommand("Resolve-MemoryDumpPath");
        foreach (var (name, value) in parameters)
        {
            command = command.AddParameter(name, value);
        }

        return command.Invoke();
    }

    public IList<ErrorRecord> InvokeResolveMemoryDumpPathWithErrors(params (string Name, object Value)[] parameters)
    {
        PowerShell.Commands.Clear();
        var command = PowerShell.AddCommand("Resolve-MemoryDumpPath");
        foreach (var (name, value) in parameters)
        {
            command = command.AddParameter(name, value);
        }

        command.Invoke();
        return PowerShell.Streams.Error.ToList();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        PowerShell.Dispose();
        _runspace.Dispose();
        _disposed = true;
    }
}
