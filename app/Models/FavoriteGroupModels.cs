namespace ComposeNowGateway.Models;

public sealed record FavoriteGroupResponse(
    Guid Id,
    string Name,
    List<Guid> PluginIds,
    string Color
);

public sealed record FavoriteGroupRequest(
    string Name,
    string Color,
    List<Guid>? PluginIds
);
