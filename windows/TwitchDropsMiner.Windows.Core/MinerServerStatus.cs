namespace TwitchDropsMiner.Windows.Core;

public sealed class MinerServerStatus
{
    public MinerServerStatus(bool isOnline, string status, string loginStatus)
    {
        IsOnline = isOnline;
        Status = status;
        LoginStatus = loginStatus;
    }

    public bool IsOnline { get; }

    public string Status { get; }

    public string LoginStatus { get; }

    public static MinerServerStatus Offline { get; } = new(false, "Offline", "Unknown");
}
