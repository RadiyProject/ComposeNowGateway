using ComposeNowGateway.Models;
using ComposeNowGateway.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ComposeNowGateway.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ISiteAuthClient siteAuth,
    IConfiguration configuration
) : ControllerBase
{
    private readonly ISiteAuthClient _siteAuth = siteAuth;
    private readonly IConfiguration _configuration = configuration;

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

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return NoContent();
        }

        bool success = await _siteAuth.LogoutAsync(request.RefreshToken, cancellationToken);
        return success ? NoContent() : StatusCode(StatusCodes.Status502BadGateway);
    }

    [HttpGet("profile-redirect")]
    public IActionResult ProfileRedirect(
        [FromQuery] string accessToken,
        [FromQuery] string refreshToken
    )
    {
        return TokenCookieRedirect(accessToken, refreshToken, "/profile");
    }

    [HttpGet("subscriptions-redirect")]
    public IActionResult SubscriptionsRedirect(
        [FromQuery] string accessToken,
        [FromQuery] string refreshToken
    )
    {
        return TokenCookieRedirect(accessToken, refreshToken, "/subscriptions");
    }

    private IActionResult TokenCookieRedirect(string accessToken, string refreshToken, string targetPath)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return BadRequest("Tokens are required.");
        }

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };

        Response.Cookies.Append("compose_now_access", accessToken, options);
        Response.Cookies.Append("compose_now_refresh", refreshToken, options);

        string sitePublicUrl =
            _configuration["Gateway:SitePublicUrl"]
            ?? Environment.GetEnvironmentVariable("COMPOSE_NOW_SITE_PUBLIC_URL")
            ?? "http://localhost:5002";

        return Redirect(sitePublicUrl.TrimEnd('/') + targetPath);
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
