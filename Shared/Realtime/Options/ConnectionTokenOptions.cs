namespace Shared.Realtime.Options;

/// <summary>
/// Options used to issue short-lived connection tokens for SignalR.
/// </summary>
public sealed class ConnectionTokenOptions
{
    public const string SectionName = "RealtimeToken";

    public string Issuer { get; init; } = "WebApp";
    public string Audience { get; init; } = "App.Real";
    public string Secret { get; init; } = "CHANGE_ME_CONNECTION_TOKEN_SECRET";
    public int LifetimeSeconds { get; init; } = 90; // 60–120 per requirement
}




