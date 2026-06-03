using ComposeNowGateway.Models;
using ComposeNowGateway.Services.Favorites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComposeNowGateway.Controllers;

[ApiController]
[Authorize]
[Route("api/favorite-groups")]
public sealed class FavoriteGroupsController(
    ISiteFavoriteGroupsClient favoriteGroups
) : ControllerBase
{
    private readonly ISiteFavoriteGroupsClient _favoriteGroups = favoriteGroups;

    [HttpGet]
    public async Task<ActionResult<List<FavoriteGroupResponse>>> GetGroups(
        [FromQuery] string? pluginType,
        CancellationToken cancellationToken
    )
    {
        var groups = await _favoriteGroups.GetGroupsAsync(AuthorizationHeader(), pluginType, cancellationToken);
        return groups is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(groups);
    }

    [HttpGet("{groupId:guid}")]
    public async Task<ActionResult<FavoriteGroupResponse>> GetGroup(
        Guid groupId,
        [FromQuery] string? pluginType,
        CancellationToken cancellationToken
    )
    {
        var group = await _favoriteGroups.GetGroupAsync(AuthorizationHeader(), groupId, pluginType, cancellationToken);
        return group is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(group);
    }

    [HttpPost]
    public async Task<ActionResult<FavoriteGroupResponse>> CreateGroup(
        [FromBody] FavoriteGroupRequest request,
        [FromQuery] string? pluginType,
        CancellationToken cancellationToken
    )
    {
        var group = await _favoriteGroups.CreateGroupAsync(AuthorizationHeader(), request, pluginType, cancellationToken);
        return group is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(group);
    }

    [HttpPut("{groupId:guid}")]
    public async Task<ActionResult<FavoriteGroupResponse>> UpdateGroup(
        Guid groupId,
        [FromBody] FavoriteGroupRequest request,
        [FromQuery] string? pluginType,
        CancellationToken cancellationToken
    )
    {
        var group = await _favoriteGroups.UpdateGroupAsync(AuthorizationHeader(), groupId, request, pluginType, cancellationToken);
        return group is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(group);
    }

    [HttpDelete("{groupId:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid groupId, CancellationToken cancellationToken)
    {
        return await _favoriteGroups.DeleteGroupAsync(AuthorizationHeader(), groupId, cancellationToken)
            ? NoContent()
            : StatusCode(StatusCodes.Status502BadGateway);
    }

    [HttpPost("{groupId:guid}/plugins/{pluginId:guid}")]
    public async Task<ActionResult<FavoriteGroupResponse>> AddPlugin(
        Guid groupId,
        Guid pluginId,
        [FromQuery] string? pluginType,
        CancellationToken cancellationToken
    )
    {
        var group = await _favoriteGroups.AddPluginAsync(AuthorizationHeader(), groupId, pluginId, pluginType, cancellationToken);
        return group is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(group);
    }

    [HttpDelete("{groupId:guid}/plugins/{pluginId:guid}")]
    public async Task<ActionResult<FavoriteGroupResponse>> RemovePlugin(
        Guid groupId,
        Guid pluginId,
        [FromQuery] string? pluginType,
        CancellationToken cancellationToken
    )
    {
        var group = await _favoriteGroups.RemovePluginAsync(AuthorizationHeader(), groupId, pluginId, pluginType, cancellationToken);
        return group is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(group);
    }

    private string AuthorizationHeader()
    {
        return Request.Headers.Authorization.ToString();
    }
}
