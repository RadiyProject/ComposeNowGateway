using System.Security.Claims;

namespace ComposeNowGateway.Services.Sessions;

public interface IGatewaySessionStore
{
    Task<GatewaySession> UpsertAsync(
        ClaimsPrincipal user,
        string pluginNodeUrl,
        CancellationToken cancellationToken
    );

    Task DeleteAsync(string sessionId, CancellationToken cancellationToken);
}
