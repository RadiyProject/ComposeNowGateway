using System.Net.Http.Json;
using ComposeNowGateway.Models;
using Microsoft.Extensions.Options;

namespace ComposeNowGateway.Services.Auth;

public sealed class SiteAuthClient(
    HttpClient http,
    IOptions<GatewayOptions> options,
    ILogger<SiteAuthClient> logger
) : ISiteAuthClient
{
    private readonly HttpClient _http = http;
    private readonly GatewayOptions _options = options.Value;
    private readonly ILogger<SiteAuthClient> _logger = logger;

    public async Task<AuthTokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        return await PostAsync("api/auth/login", request, cancellationToken);
    }

    public async Task<AuthTokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return await PostAsync("api/auth/refresh", new RefreshRequest(refreshToken), cancellationToken);
    }

    private async Task<AuthTokenResponse?> PostAsync<T>(
        string path,
        T body,
        CancellationToken cancellationToken
    )
    {
        var baseUri = new Uri(_options.SiteBaseUrl.TrimEnd('/') + "/");
        using var response = await _http.PostAsJsonAsync(
            new Uri(baseUri, path),
            body,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "ComposeNowSite auth request failed. Path={Path}, Status={Status}",
                path,
                response.StatusCode
            );

            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthTokenResponse>(
            cancellationToken: cancellationToken
        );
    }
}
