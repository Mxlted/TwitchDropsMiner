namespace TwitchDropsMiner.Windows.Core;

public sealed class ProcessExitedEventArgs : EventArgs
{
    public ProcessExitedEventArgs(int? exitCode)
    {
        ExitCode = exitCode;
    }

    public int? ExitCode { get; }
}
