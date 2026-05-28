namespace TwitchDropsMiner.Windows.Core;

public sealed class ProcessOutputEventArgs : EventArgs
{
    public ProcessOutputEventArgs(string line)
    {
        Line = line;
    }

    public string Line { get; }
}
