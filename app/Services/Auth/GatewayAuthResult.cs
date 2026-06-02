using System.Security.Claims;
using ComposeNowGateway.Models;

namespace ComposeNowGateway.Services.Auth;

public sealed record GatewayAuthResult(
    bool Success,
    ClaimsPrincipal? User,
    AuthTokenResponse? RefreshedTokens,
    string? Error
)
{
    public static GatewayAuthResult Failed(string error) => new(false, null, null, error);
}
