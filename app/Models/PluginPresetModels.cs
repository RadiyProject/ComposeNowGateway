namespace ComposeNowGateway.Models;

public sealed record PluginPresetResponse(
    Guid Id,
    Guid PluginId,
    string Name,
    string Description,
    Dictionary<string, double> Values
);

public sealed record PluginPresetRequest(
    string Name,
    string Description,
    Dictionary<string, double>? Values
);
