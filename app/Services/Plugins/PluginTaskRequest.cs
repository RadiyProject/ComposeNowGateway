namespace ComposeNowGateway.Services.Plugins;

public sealed record PluginTaskRequest(
    string TaskId,
    string PluginName,
    string SessionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset DeadlineAt
);

