using System.Net.Http.Json;
using System.Text.Json;

namespace TwitchDropsMiner.Windows.Core;

public sealed class MinerStatusClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public MinerStatusClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    }

    public async Task<MinerServerStatus> GetStatusAsync(
        WindowsServerOptions options,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                options.StatusUri,
                cancellationToken
            );
            if (!response.IsSuccessStatusCode)
            {
                return new MinerServerStatus(true, $"Initializing ({(int)response.StatusCode})", "Unknown");
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken
            );

            JsonElement root = document.RootElement;
            string status = TryGetDisplayText(root, "status") ?? "Online";
            string loginStatus = TryGetLoginStatus(root) ?? "Unknown";
            return new MinerServerStatus(true, status, loginStatus);
        }
        catch (Exception ex) when (IsStatusReadFailure(ex, cancellationToken))
        {
            return MinerServerStatus.Offline;
        }
    }

    public async Task RequestShutdownAsync(
        WindowsServerOptions options,
        CancellationToken cancellationToken = default
    )
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            options.CloseUri,
            new { },
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _httpClient.Dispose();

    private static bool IsStatusReadFailure(Exception ex, CancellationToken cancellationToken)
    {
        return !cancellationToken.IsCancellationRequested
            && ex is HttpRequestException or OperationCanceledException or JsonException;
    }

    private static string? TryGetLoginStatus(JsonElement element)
    {
        if (!element.TryGetProperty("login", out JsonElement login))
        {
            return null;
        }

        if (login.ValueKind == JsonValueKind.Object)
        {
            return TryGetDisplayText(login, "status");
        }

        return GetDisplayText(login);
    }

    private static string? TryGetDisplayText(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property)
            ? GetDisplayText(property)
            : null;
    }

    private static string? GetDisplayText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "True",
            JsonValueKind.False => "False",
            _ => null,
        };
    }
}
