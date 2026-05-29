using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;

namespace MemoryAnalysis.Tests.Helpers;

/// <summary>
/// Minimal PS host for cmdlet tests that need PromptForChoice or ReadLine.
/// </summary>
internal sealed class TestPSHost : PSHost
{
    private readonly TestPSHostUserInterface _ui;

    public TestPSHost(int promptForChoiceResult = 0, params string[] readLines)
    {
        _ui = new TestPSHostUserInterface(promptForChoiceResult, readLines);
    }

    public override PSHostUserInterface UI => _ui;

    public override Guid InstanceId { get; } = Guid.NewGuid();

    public override string Name => "MemoryAnalysis.TestHost";

    public override Version Version => new(1, 0);

    public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

    public override void EnterNestedPrompt()
    {
    }

    public override void ExitNestedPrompt()
    {
    }

    public override void NotifyBeginApplication()
    {
    }

    public override void NotifyEndApplication()
    {
    }

    public override void SetShouldExit(int exitCode)
    {
    }

    private sealed class TestPSHostUserInterface : PSHostUserInterface
    {
        private readonly int _promptForChoiceResult;
        private readonly Queue<string> _readLines;

        public TestPSHostUserInterface(int promptForChoiceResult, string[] readLines)
        {
            _promptForChoiceResult = promptForChoiceResult;
            _readLines = new Queue<string>(
                readLines.Length > 0 ? readLines : new[] { "1" });
        }

        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName) =>
            throw new NotSupportedException();

        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options) =>
            throw new NotSupportedException();

        public override int PromptForChoice(
            string caption,
            string message,
            Collection<ChoiceDescription> choices,
            int defaultChoice) =>
            _promptForChoiceResult;

        public override Dictionary<string, PSObject> Prompt(
            string caption,
            string message,
            Collection<FieldDescription> descriptions) =>
            throw new NotSupportedException();

        public override PSHostRawUserInterface RawUI { get; } = new TestRawUi();

        public override string? ReadLine() =>
            _readLines.Count > 0 ? _readLines.Dequeue() : "1";

        public override System.Security.SecureString? ReadLineAsSecureString() => null;

        public override void Write(string value)
        {
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
        }

        public override void WriteLine(string value)
        {
        }

        public override void WriteLine()
        {
        }

        public override void WriteErrorLine(string value)
        {
        }

        public override void WriteDebugLine(string message)
        {
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
        }

        public override void WriteVerboseLine(string message)
        {
        }

        public override void WriteWarningLine(string message)
        {
        }
    }

    private sealed class TestRawUi : PSHostRawUserInterface
    {
        private static readonly Size DefaultSize = new(120, 40);

        public override Coordinates CursorPosition { get; set; }

        public override int CursorSize { get; set; } = 1;

        public override Coordinates WindowPosition { get; set; }

        public override Size BufferSize { get; set; } = DefaultSize;

        public override Size MaxPhysicalWindowSize => DefaultSize;

        public override Size MaxWindowSize => DefaultSize;

        public override Size WindowSize { get; set; } = DefaultSize;

        public override bool KeyAvailable => false;

        public override string WindowTitle { get; set; } = "Test";

        public override ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

        public override ConsoleColor ForegroundColor { get; set; } = ConsoleColor.Gray;

        public override KeyInfo ReadKey(ReadKeyOptions options) =>
            new KeyInfo(13, '\r', (ControlKeyStates)0, true);

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
        }

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle) =>
            new BufferCell[1, 1];

        public override void FlushInputBuffer()
        {
        }
    }
}
