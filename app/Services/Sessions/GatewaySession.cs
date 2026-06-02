namespace ComposeNowGateway.Services.Sessions;

public sealed record GatewaySession(
    string Id,
    string UserId,
    string Login,
    string PluginNodeUrl,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
