using System.Diagnostics;

namespace TwitchDropsMiner.Windows.Core;

public sealed class PythonCommand
{
    public PythonCommand(string fileName, IEnumerable<string>? prefixArguments = null)
    {
        FileName = fileName;
        PrefixArguments = (prefixArguments ?? []).ToArray();
    }

    public string FileName { get; }

    public IReadOnlyList<string> PrefixArguments { get; }

    public void AddArguments(ProcessStartInfo startInfo, IEnumerable<string> scriptArguments)
    {
        foreach (string argument in PrefixArguments.Concat(scriptArguments))
        {
            startInfo.ArgumentList.Add(argument);
        }
    }

    public string Format(IEnumerable<string> scriptArguments)
    {
        IEnumerable<string> parts = [FileName, .. PrefixArguments, .. scriptArguments];
        return string.Join(" ", parts.Select(Quote));
    }

    private static string Quote(string value)
    {
        return value.Any(char.IsWhiteSpace) ? $"\"{value.Replace("\"", "\\\"")}\"" : value;
    }
}
