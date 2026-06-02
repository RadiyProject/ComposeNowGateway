using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ComposeNowGateway.Services.Auth;

public sealed class GatewayTokenValidator(
    IConfiguration configuration,
    ISiteAuthClient siteAuth
) : IGatewayTokenValidator
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ISiteAuthClient _siteAuth = siteAuth;

    public async Task<GatewayAuthResult> ValidateOrRefreshAsync(
        string? accessToken,
        string? refreshToken,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(accessToken)
            && TryValidate(accessToken, validateLifetime: true, out var principal))
        {
            return new GatewayAuthResult(true, principal, null, null);
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return GatewayAuthResult.Failed("Authentication required.");
        }

        var tokens = await _siteAuth.RefreshAsync(refreshToken, cancellationToken);
        if (tokens is null || !TryValidate(tokens.AccessToken, validateLifetime: true, out principal))
        {
            return GatewayAuthResult.Failed("Refresh token is invalid or expired.");
        }

        return new GatewayAuthResult(true, principal, tokens, null);
    }

    private bool TryValidate(
        string accessToken,
        bool validateLifetime,
        out ClaimsPrincipal? principal
    )
    {
        try
        {
            principal = new JwtSecurityTokenHandler().ValidateToken(
                accessToken,
                CreateValidationParameters(validateLifetime),
                out _
            );

            return principal.Identity?.IsAuthenticated == true;
        }
        catch
        {
            principal = null;
            return false;
        }
    }

    private TokenValidationParameters CreateValidationParameters(bool validateLifetime)
    {
        string signingKey = _configuration["Jwt:SigningKey"]
            ?? Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
            ?? "compose-now-development-signing-key-change-me-please-32";

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"] ?? "ComposeNowSite",
            ValidAudience = _configuration["Jwt:Audience"] ?? "ComposeNowClients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            NameClaimType = "login",
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}
