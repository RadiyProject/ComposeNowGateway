using System.Security.Claims;
using System.Text.Json;
using ComposeNowGateway.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ComposeNowGateway.Services.Sessions;

public sealed class RedisGatewaySessionStore(
    IConnectionMultiplexer redis,
    IOptions<GatewayOptions> options
) : IGatewaySessionStore
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly GatewayOptions _options = options.Value;

    public async Task<GatewaySession> UpsertAsync(
        ClaimsPrincipal user,
        string pluginNodeUrl,
        CancellationToken cancellationToken
    )
    {
        string userId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.Identity?.Name
            ?? "unknown";
        string login = user.FindFirstValue("login")
            ?? user.Identity?.Name
            ?? userId;
        string sessionId = user.FindFirstValue("jti")
            ?? $"{userId}:{Guid.NewGuid():N}";
        string key = GetKey(sessionId);
        var now = DateTime.UtcNow;

        var session = new GatewaySession(
            sessionId,
            userId,
            login,
            pluginNodeUrl,
            now,
            now
        );

        cancellationToken.ThrowIfCancellationRequested();
        await _database.StringSetAsync(
            key,
            JsonSerializer.Serialize(session),
            TimeSpan.FromMinutes(Math.Max(1, _options.SessionTtlMinutes))
        );

        return session;
    }

    public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.KeyDeleteAsync(GetKey(sessionId));
    }

    private static string GetKey(string sessionId) => $"gateway:sessions:{sessionId}";
}
