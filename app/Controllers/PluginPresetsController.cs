using ComposeNowGateway.Models;
using ComposeNowGateway.Services.Presets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComposeNowGateway.Controllers;

[ApiController]
[Authorize]
[Route("api/plugins/{pluginId:guid}/presets")]
public sealed class PluginPresetsController(
    ISitePluginPresetsClient presets
) : ControllerBase
{
    private readonly ISitePluginPresetsClient _presets = presets;

    [HttpGet]
    public async Task<ActionResult<List<PluginPresetResponse>>> GetPresets(
        Guid pluginId,
        CancellationToken cancellationToken
    )
    {
        var result = await _presets.GetPresetsAsync(AuthorizationHeader(), pluginId, cancellationToken);
        return result is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(result);
    }

    [HttpGet("{presetId:guid}")]
    public async Task<ActionResult<PluginPresetResponse>> GetPreset(
        Guid pluginId,
        Guid presetId,
        CancellationToken cancellationToken
    )
    {
        var result = await _presets.GetPresetAsync(AuthorizationHeader(), pluginId, presetId, cancellationToken);
        return result is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PluginPresetResponse>> CreatePreset(
        Guid pluginId,
        [FromBody] PluginPresetRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await _presets.CreatePresetAsync(AuthorizationHeader(), pluginId, request, cancellationToken);
        return result is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(result);
    }

    [HttpDelete("{presetId:guid}")]
    public async Task<IActionResult> DeletePreset(
        Guid pluginId,
        Guid presetId,
        CancellationToken cancellationToken
    )
    {
        return await _presets.DeletePresetAsync(AuthorizationHeader(), pluginId, presetId, cancellationToken)
            ? NoContent()
            : StatusCode(StatusCodes.Status502BadGateway);
    }

    private string AuthorizationHeader()
    {
        return Request.Headers.Authorization.ToString();
    }
}
