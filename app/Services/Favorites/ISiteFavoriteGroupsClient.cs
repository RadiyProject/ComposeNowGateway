using ComposeNowGateway.Models;

namespace ComposeNowGateway.Services.Favorites;

public interface ISiteFavoriteGroupsClient
{
    Task<List<FavoriteGroupResponse>?> GetGroupsAsync(
        string authorization,
        string? pluginType,
        CancellationToken cancellationToken
    );

    Task<FavoriteGroupResponse?> GetGroupAsync(
        string authorization,
        Guid groupId,
        string? pluginType,
        CancellationToken cancellationToken
    );

    Task<FavoriteGroupResponse?> CreateGroupAsync(
        string authorization,
        FavoriteGroupRequest request,
        string? pluginType,
        CancellationToken cancellationToken
    );

    Task<FavoriteGroupResponse?> UpdateGroupAsync(
        string authorization,
        Guid groupId,
        FavoriteGroupRequest request,
        string? pluginType,
        CancellationToken cancellationToken
    );

    Task<bool> DeleteGroupAsync(
        string authorization,
        Guid groupId,
        CancellationToken cancellationToken
    );

    Task<FavoriteGroupResponse?> AddPluginAsync(
        string authorization,
        Guid groupId,
        Guid pluginId,
        string? pluginType,
        CancellationToken cancellationToken
    );

    Task<FavoriteGroupResponse?> RemovePluginAsync(
        string authorization,
        Guid groupId,
        Guid pluginId,
        string? pluginType,
        CancellationToken cancellationToken
    );
}
