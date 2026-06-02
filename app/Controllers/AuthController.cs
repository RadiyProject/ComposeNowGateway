using ComposeNowGateway.Models;
using ComposeNowGateway.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ComposeNowGateway.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ISiteAuthClient siteAuth
) : ControllerBase
{
    private readonly ISiteAuthClient _siteAuth = siteAuth;

    [HttpPost("login")]
    public async Task<ActionResult<GatewayAuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var tokens = await _siteAuth.LoginAsync(request, cancellationToken);
        return ToGatewayResponse(tokens);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<GatewayAuthResponse>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken
    )
    {
        var tokens = await _siteAuth.RefreshAsync(request.RefreshToken, cancellationToken);
        return ToGatewayResponse(tokens);
    }

    private ActionResult<GatewayAuthResponse> ToGatewayResponse(AuthTokenResponse? tokens)
    {
        if (tokens is null)
        {
            return Unauthorized();
        }

        return Ok(new GatewayAuthResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt,
            "/ws"
        ));
    }
}
