using System.Net;
using TwitchDropsMiner.Windows.Core;

try
{
    RepositoryLocatorFindsRootFromNestedDirectory();
    PythonResolverPrefersLocalEnvPython();
    PythonResolverFallsBackToUv();
    PythonResolverHonorsExplicitEnvironmentVariable();
    PythonResolverRejectsMissingExplicitExecutable();
    MinerStatusClientReadsNestedLoginStatus().GetAwaiter().GetResult();
    Console.WriteLine("Windows launcher tests passed.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

static void RepositoryLocatorFindsRootFromNestedDirectory()
{
    using TempRepository repository = TempRepository.Create();
    string nested = Path.Combine(repository.Root, "windows", "app", "bin");
    Directory.CreateDirectory(nested);

    RepositoryLocator locator = new([nested]);
    AssertEqual(repository.Root, locator.Locate(), nameof(RepositoryLocatorFindsRootFromNestedDirectory));
}

static void PythonResolverPrefersLocalEnvPython()
{
    using TempRepository repository = TempRepository.Create();
    string python = Path.Combine(repository.Root, "env", "Scripts", "python.exe");
    Directory.CreateDirectory(Path.GetDirectoryName(python)!);
    File.WriteAllText(python, string.Empty);

    PythonResolver resolver = new(getEnvironmentVariable: _ => null, pathDirectories: []);
    PythonCommand command = resolver.Resolve(repository.Root);

    AssertEqual(python, command.FileName, nameof(PythonResolverPrefersLocalEnvPython));
    AssertSequence([], command.PrefixArguments, nameof(PythonResolverPrefersLocalEnvPython));
}

static void PythonResolverFallsBackToUv()
{
    using TempRepository repository = TempRepository.Create();
    string toolDirectory = Path.Combine(repository.Root, "tools");
    Directory.CreateDirectory(toolDirectory);
    string uv = Path.Combine(toolDirectory, "uv.exe");
    File.WriteAllText(uv, string.Empty);

    PythonResolver resolver = new(
        getEnvironmentVariable: _ => null,
        pathDirectories: [toolDirectory]
    );
    PythonCommand command = resolver.Resolve(repository.Root);

    AssertEqual(uv, command.FileName, nameof(PythonResolverFallsBackToUv));
    AssertSequence(["run", "python"], command.PrefixArguments, nameof(PythonResolverFallsBackToUv));
}

static void PythonResolverHonorsExplicitEnvironmentVariable()
{
    using TempRepository repository = TempRepository.Create();
    string python = Path.Combine(repository.Root, "custom-python.exe");
    File.WriteAllText(python, string.Empty);

    PythonResolver resolver = new(
        getEnvironmentVariable: name => name == "TDM_PYTHON" ? python : null,
        pathDirectories: []
    );
    PythonCommand command = resolver.Resolve(repository.Root);

    AssertEqual(python, command.FileName, nameof(PythonResolverHonorsExplicitEnvironmentVariable));
}

static void PythonResolverRejectsMissingExplicitExecutable()
{
    using TempRepository repository = TempRepository.Create();
    string python = Path.Combine(repository.Root, "missing-python.exe");

    PythonResolver resolver = new(
        getEnvironmentVariable: name => name == "TDM_PYTHON" ? python : null,
        pathDirectories: []
    );

    try
    {
        resolver.Resolve(repository.Root);
    }
    catch (InvalidOperationException)
    {
        return;
    }

    throw new InvalidOperationException(
        $"{nameof(PythonResolverRejectsMissingExplicitExecutable)} failed."
    );
}

static async Task MinerStatusClientReadsNestedLoginStatus()
{
    HttpClient httpClient = new(new StaticJsonHandler(
        """
        {"status":"Idle","login":{"status":"Logged in","user_id":123}}
        """
    ))
    {
        BaseAddress = new Uri("http://127.0.0.1:8080/"),
    };

    using MinerStatusClient client = new(httpClient);
    MinerServerStatus status = await client.GetStatusAsync(new WindowsServerOptions());

    AssertEqual("Idle", status.Status, nameof(MinerStatusClientReadsNestedLoginStatus));
    AssertEqual("Logged in", status.LoginStatus, nameof(MinerStatusClientReadsNestedLoginStatus));
}

static void AssertEqual(string expected, string actual, string testName)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"{testName} failed. Expected '{expected}', got '{actual}'."
        );
    }
}

static void AssertSequence(
    IReadOnlyList<string> expected,
    IReadOnlyList<string> actual,
    string testName
)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException(
            $"{testName} failed. Expected [{string.Join(", ", expected)}], "
                + $"got [{string.Join(", ", actual)}]."
        );
    }
}

internal sealed class StaticJsonHandler : HttpMessageHandler
{
    private readonly string _content;

    public StaticJsonHandler(string content)
    {
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(_content),
        };
        return Task.FromResult(response);
    }
}

internal sealed class TempRepository : IDisposable
{
    private TempRepository(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public static TempRepository Create()
    {
        string root = Path.Combine(Path.GetTempPath(), $"tdm-windows-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "src"));
        File.WriteAllText(Path.Combine(root, "main.py"), string.Empty);
        File.WriteAllText(Path.Combine(root, "pyproject.toml"), string.Empty);
        return new TempRepository(root);
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
