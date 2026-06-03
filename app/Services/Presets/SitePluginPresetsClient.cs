using System.Net.Http.Headers;
using System.Net.Http.Json;
using ComposeNowGateway.Models;
using Microsoft.Extensions.Options;

namespace ComposeNowGateway.Services.Presets;

public sealed class SitePluginPresetsClient(
    HttpClient http,
    IOptions<GatewayOptions> options,
    ILogger<SitePluginPresetsClient> logger
) : ISitePluginPresetsClient
{
    private readonly HttpClient _http = http;
    private readonly GatewayOptions _options = options.Value;
    private readonly ILogger<SitePluginPresetsClient> _logger = logger;

    public async Task<List<PluginPresetResponse>?> GetPresetsAsync(
        string authorization,
        Guid pluginId,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            Path(pluginId),
            authorization,
            body: null,
            cancellationToken
        );

        return await ReadAsync<List<PluginPresetResponse>>(response, cancellationToken);
    }

    public async Task<PluginPresetResponse?> GetPresetAsync(
        string authorization,
        Guid pluginId,
        Guid presetId,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"{Path(pluginId)}/{presetId}",
            authorization,
            body: null,
            cancellationToken
        );

        return await ReadAsync<PluginPresetResponse>(response, cancellationToken);
    }

    public async Task<PluginPresetResponse?> CreatePresetAsync(
        string authorization,
        Guid pluginId,
        PluginPresetRequest request,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            Path(pluginId),
            authorization,
            request,
            cancellationToken
        );

        return await ReadAsync<PluginPresetResponse>(response, cancellationToken);
    }

    public async Task<bool> DeletePresetAsync(
        string authorization,
        Guid pluginId,
        Guid presetId,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"{Path(pluginId)}/{presetId}",
            authorization,
            body: null,
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        await LogFailureAsync(response, cancellationToken);
        return false;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string authorization,
        object? body,
        CancellationToken cancellationToken
    )
    {
        var baseUri = new Uri(_options.SiteBaseUrl.TrimEnd('/') + "/");
        using var request = new HttpRequestMessage(method, new Uri(baseUri, path));
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _http.SendAsync(request, cancellationToken);
    }

    private async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            await LogFailureAsync(response, cancellationToken);
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task LogFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "ComposeNowSite plugin presets request failed. Status={Status}, Body={Body}",
            response.StatusCode,
            responseBody
        );
    }

    private static string Path(Guid pluginId)
    {
        return $"api/plugins/{pluginId}/presets";
    }
}
