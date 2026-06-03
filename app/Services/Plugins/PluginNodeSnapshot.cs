namespace ComposeNowGateway.Services.Plugins;

public sealed record PluginNodeSnapshot(
    string NodeId,
    string WebSocketUrl,
    string[] Plugins,
    int ActiveSessions,
    int MaxSessions,
    double LoadPercent,
    bool Draining,
    DateTimeOffset UpdatedAt
);

