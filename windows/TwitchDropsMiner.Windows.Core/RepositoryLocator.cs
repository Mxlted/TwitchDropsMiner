namespace TwitchDropsMiner.Windows.Core;

public sealed class RepositoryLocator
{
    private readonly IReadOnlyList<string> _startDirectories;

    public RepositoryLocator(IEnumerable<string> startDirectories)
    {
        _startDirectories = startDirectories
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static RepositoryLocator ForCurrentApp()
    {
        return new RepositoryLocator([AppContext.BaseDirectory, Environment.CurrentDirectory]);
    }

    public string Locate()
    {
        foreach (string startDirectory in _startDirectories)
        {
            DirectoryInfo? current = new(startDirectory);
            while (current is not null)
            {
                if (IsRepositoryRoot(current.FullName))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not find the TwitchDropsMiner repository root. Start this app from inside "
                + "the repository folder or keep it under the windows directory."
        );
    }

    private static bool IsRepositoryRoot(string directory)
    {
        return File.Exists(Path.Combine(directory, "main.py"))
            && File.Exists(Path.Combine(directory, "pyproject.toml"))
            && Directory.Exists(Path.Combine(directory, "src"));
    }
}
