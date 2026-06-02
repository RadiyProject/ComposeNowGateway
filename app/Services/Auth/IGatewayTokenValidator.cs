namespace ComposeNowGateway.Services.Auth;

public interface IGatewayTokenValidator
{
    Task<GatewayAuthResult> ValidateOrRefreshAsync(
        string? accessToken,
        string? refreshToken,
        CancellationToken cancellationToken
    );
}
