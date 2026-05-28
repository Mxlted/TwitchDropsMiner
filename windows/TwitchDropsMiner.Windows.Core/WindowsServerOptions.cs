namespace TwitchDropsMiner.Windows.Core;

public sealed class WindowsServerOptions
{
    public WindowsServerOptions(Uri? serverUri = null)
    {
        ServerUri = serverUri ?? new Uri("http://127.0.0.1:8080/");
    }

    public Uri ServerUri { get; }

    public Uri StatusUri => new(ServerUri, "api/status");

    public Uri CloseUri => new(ServerUri, "api/close");
}
