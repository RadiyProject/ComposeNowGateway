using System.Net.Http.Headers;
using System.Net.Http.Json;
using ComposeNowGateway.Models;
using Microsoft.Extensions.Options;

namespace ComposeNowGateway.Services.Favorites;

public sealed class SiteFavoriteGroupsClient(
    HttpClient http,
    IOptions<GatewayOptions> options,
    ILogger<SiteFavoriteGroupsClient> logger
) : ISiteFavoriteGroupsClient
{
    private readonly HttpClient _http = http;
    private readonly GatewayOptions _options = options.Value;
    private readonly ILogger<SiteFavoriteGroupsClient> _logger = logger;

    public async Task<List<FavoriteGroupResponse>?> GetGroupsAsync(
        string authorization,
        string? pluginType,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            BuildPath("api/favorite-groups", pluginType),
            authorization,
            body: null,
            cancellationToken
        );

        return await ReadAsync<List<FavoriteGroupResponse>>(response, cancellationToken);
    }

    public async Task<FavoriteGroupResponse?> GetGroupAsync(
        string authorization,
        Guid groupId,
        string? pluginType,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            BuildPath($"api/favorite-groups/{groupId}", pluginType),
            authorization,
            body: null,
            cancellationToken
        );

        return await ReadAsync<FavoriteGroupResponse>(response, cancellationToken);
    }

    public async Task<FavoriteGroupResponse?> CreateGroupAsync(
        string authorization,
        FavoriteGroupRequest request,
        string? pluginType,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            BuildPath("api/favorite-groups", pluginType),
            authorization,
            request,
            cancellationToken
        );

        return await ReadAsync<FavoriteGroupResponse>(response, cancellationToken);
    }

    public async Task<FavoriteGroupResponse?> UpdateGroupAsync(
        string authorization,
        Guid groupId,
        FavoriteGroupRequest request,
        string? pluginType,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Put,
            BuildPath($"api/favorite-groups/{groupId}", pluginType),
            authorization,
            request,
            cancellationToken
        );

        return await ReadAsync<FavoriteGroupResponse>(response, cancellationToken);
    }

    public async Task<bool> DeleteGroupAsync(
        string authorization,
        Guid groupId,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"api/favorite-groups/{groupId}",
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

    public async Task<FavoriteGroupResponse?> AddPluginAsync(
        string authorization,
        Guid groupId,
        Guid pluginId,
        string? pluginType,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            BuildPath($"api/favorite-groups/{groupId}/plugins/{pluginId}", pluginType),
            authorization,
            body: null,
            cancellationToken
        );

        return await ReadAsync<FavoriteGroupResponse>(response, cancellationToken);
    }

    public async Task<FavoriteGroupResponse?> RemovePluginAsync(
        string authorization,
        Guid groupId,
        Guid pluginId,
        string? pluginType,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            BuildPath($"api/favorite-groups/{groupId}/plugins/{pluginId}", pluginType),
            authorization,
            body: null,
            cancellationToken
        );

        return await ReadAsync<FavoriteGroupResponse>(response, cancellationToken);
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
            "ComposeNowSite favorite groups request failed. Status={Status}, Body={Body}",
            response.StatusCode,
            responseBody
        );
    }

    private static string BuildPath(string path, string? pluginType)
    {
        return string.IsNullOrWhiteSpace(pluginType)
            ? path
            : $"{path}?pluginType={Uri.EscapeDataString(pluginType)}";
    }
}
