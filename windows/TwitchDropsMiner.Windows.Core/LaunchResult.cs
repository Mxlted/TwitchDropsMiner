namespace TwitchDropsMiner.Windows.Core;

public sealed class LaunchResult
{
    private LaunchResult(bool started, int? processId, string commandLine)
    {
        Started = started;
        ProcessId = processId;
        CommandLine = commandLine;
    }

    public bool Started { get; }

    public int? ProcessId { get; }

    public string CommandLine { get; }

    public static LaunchResult AlreadyRunning(int processId) =>
        new(false, processId, "Already running");

    public static LaunchResult StartedProcess(int processId, string commandLine) =>
        new(true, processId, commandLine);
}
