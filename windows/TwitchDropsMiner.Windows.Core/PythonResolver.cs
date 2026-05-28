namespace TwitchDropsMiner.Windows.Core;

public sealed class PythonResolver
{
    private readonly Func<string, bool> _directoryExists;
    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, string?> _getEnvironmentVariable;
    private readonly IReadOnlyList<string>? _pathDirectories;

    public PythonResolver(
        Func<string, string?>? getEnvironmentVariable = null,
        Func<string, bool>? fileExists = null,
        Func<string, bool>? directoryExists = null,
        IReadOnlyList<string>? pathDirectories = null
    )
    {
        _getEnvironmentVariable = getEnvironmentVariable ?? Environment.GetEnvironmentVariable;
        _fileExists = fileExists ?? File.Exists;
        _directoryExists = directoryExists ?? Directory.Exists;
        _pathDirectories = pathDirectories;
    }

    public PythonCommand Resolve(string repositoryRoot)
    {
        PythonCommand? command = TryResolve(repositoryRoot);
        if (command is not null)
        {
            return command;
        }

        throw new InvalidOperationException(
            "No Python launcher was found. Install Python 3.12+, run `uv sync`, "
                + "or set TDM_PYTHON to a Python executable before starting the Windows app."
        );
    }

    public PythonCommand? TryResolve(string repositoryRoot)
    {
        string? explicitPython = _getEnvironmentVariable("TDM_PYTHON");
        if (!string.IsNullOrWhiteSpace(explicitPython))
        {
            string trimmed = explicitPython.Trim();
            if (Path.IsPathFullyQualified(trimmed) && !_fileExists(trimmed))
            {
                throw new InvalidOperationException(
                    $"TDM_PYTHON points to a file that does not exist: {trimmed}"
                );
            }

            return new PythonCommand(trimmed);
        }

        foreach (string localPython in GetLocalPythonCandidates(repositoryRoot))
        {
            if (_fileExists(localPython))
            {
                return new PythonCommand(localPython);
            }
        }

        string? uv = FindExecutable("uv.exe");
        if (uv is not null)
        {
            return new PythonCommand(uv, ["run", "python"]);
        }

        string? py = FindExecutable("py.exe");
        if (py is not null)
        {
            return new PythonCommand(py, ["-3.12"]);
        }

        string? python = FindExecutable("python.exe") ?? FindExecutable("python3.exe");
        return python is not null ? new PythonCommand(python) : null;
    }

    private IEnumerable<string> GetLocalPythonCandidates(string repositoryRoot)
    {
        yield return Path.Combine(repositoryRoot, "env", "Scripts", "python.exe");
        yield return Path.Combine(repositoryRoot, ".venv", "Scripts", "python.exe");
    }

    private string? FindExecutable(string executableName)
    {
        foreach (string directory in GetPathDirectories())
        {
            if (string.IsNullOrWhiteSpace(directory) || !_directoryExists(directory))
            {
                continue;
            }

            string candidate = Path.Combine(directory, executableName);
            if (_fileExists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private IEnumerable<string> GetPathDirectories()
    {
        if (_pathDirectories is not null)
        {
            return _pathDirectories;
        }

        string? path = _getEnvironmentVariable("PATH");
        return string.IsNullOrWhiteSpace(path)
            ? []
            : path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
    }
}
