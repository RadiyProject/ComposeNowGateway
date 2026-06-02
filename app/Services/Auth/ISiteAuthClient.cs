using ComposeNowGateway.Models;

namespace ComposeNowGateway.Services.Auth;

public interface ISiteAuthClient
{
    Task<AuthTokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthTokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}
