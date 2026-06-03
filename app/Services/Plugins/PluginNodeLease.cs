namespace ComposeNowGateway.Services.Plugins;

public sealed record PluginNodeLease(
    string LeaseId,
    string NodeId,
    string PluginName,
    string WebSocketUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt
);

