using ComposeNowGateway.Models;

namespace ComposeNowGateway.Services.Presets;

public interface ISitePluginPresetsClient
{
    Task<List<PluginPresetResponse>?> GetPresetsAsync(
        string authorization,
        Guid pluginId,
        CancellationToken cancellationToken
    );

    Task<PluginPresetResponse?> GetPresetAsync(
        string authorization,
        Guid pluginId,
        Guid presetId,
        CancellationToken cancellationToken
    );

    Task<PluginPresetResponse?> CreatePresetAsync(
        string authorization,
        Guid pluginId,
        PluginPresetRequest request,
        CancellationToken cancellationToken
    );

    Task<bool> DeletePresetAsync(
        string authorization,
        Guid pluginId,
        Guid presetId,
        CancellationToken cancellationToken
    );
}
