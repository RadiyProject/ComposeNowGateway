using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ComposeNowGateway.Models;
using ComposeNowGateway.Services.Auth;
using ComposeNowGateway.Services.Plugins;
using ComposeNowGateway.Services.Sessions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ComposeNowGateway.Services.Proxy;

public sealed class WebSocketProxyService(
    IGatewayTokenValidator tokenValidator,
    IGatewaySessionStore sessionStore,
    IPluginNodeAllocator pluginNodeAllocator,
    IOptions<GatewayOptions> options,
    ILogger<WebSocketProxyService> logger
) : IWebSocketProxyService
{
    private const int BufferSize = 1024 * 64;

    private readonly IGatewayTokenValidator _tokenValidator = tokenValidator;
    private readonly IGatewaySessionStore _sessionStore = sessionStore;
    private readonly IPluginNodeAllocator _pluginNodeAllocator = pluginNodeAllocator;
    private readonly GatewayOptions _options = options.Value;
    private readonly ILogger<WebSocketProxyService> _logger = logger;

    public async Task ProxyAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket request expected.", cancellationToken);
            return;
        }

        string? accessToken = ReadToken(context, "accessToken", "Authorization");
        string? refreshToken = context.Request.Query["refreshToken"].ToString();
        var auth = await _tokenValidator.ValidateOrRefreshAsync(
            accessToken,
            refreshToken,
            cancellationToken
        );

        if (!auth.Success || auth.User is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(auth.Error ?? "Unauthorized.", cancellationToken);
            return;
        }

        string? targetUrl = await BuildTargetUrlAsync(context, cancellationToken);
        if (targetUrl is null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync(
                "No plugin node is currently available. Please reconnect.",
                cancellationToken
            );
            return;
        }

        var session = await _sessionStore.UpsertAsync(auth.User, targetUrl, cancellationToken);

        using var nodeSocket = new ClientWebSocket();
        try
        {
            await nodeSocket.ConnectAsync(new Uri(targetUrl), cancellationToken);
        }
        catch
        {
            await _sessionStore.DeleteAsync(session.Id, CancellationToken.None);
            throw;
        }

        using WebSocket clientSocket = await context.WebSockets.AcceptWebSocketAsync();

        _logger.LogInformation(
            "Gateway session opened. SessionId={SessionId}, Target={Target}",
            session.Id,
            targetUrl
        );

        if (auth.RefreshedTokens is not null)
        {
            await SendAuthTokensAsync(clientSocket, auth.RefreshedTokens, cancellationToken);
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var clientToNode = PumpAsync(clientSocket, nodeSocket, cts.Token);
        var nodeToClient = PumpAsync(nodeSocket, clientSocket, cts.Token);

        await Task.WhenAny(clientToNode, nodeToClient);
        await cts.CancelAsync();
        await _sessionStore.DeleteAsync(session.Id, CancellationToken.None);

        _logger.LogInformation("Gateway session closed. SessionId={SessionId}", session.Id);
    }

    private static string? ReadToken(HttpContext context, string queryName, string headerName)
    {
        string queryValue = context.Request.Query[queryName].ToString();
        if (!string.IsNullOrWhiteSpace(queryValue))
        {
            return queryValue;
        }

        string header = context.Request.Headers[headerName].ToString();
        const string bearerPrefix = "Bearer ";
        return header.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[bearerPrefix.Length..].Trim()
            : null;
    }

    private async Task<string?> BuildTargetUrlAsync(
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        string pluginName = context.Request.Query["plugin"].ToString();
        string sessionId = context.Request.Query["sessionId"].ToString();

        PluginNodeLease? lease = null;
        if (!string.IsNullOrWhiteSpace(pluginName))
        {
            lease = await _pluginNodeAllocator.AllocateAsync(
                pluginName,
                string.IsNullOrWhiteSpace(sessionId)
                    ? Guid.NewGuid().ToString("N")
                    : sessionId,
                cancellationToken
            );

            if (lease is null)
            {
                return null;
            }
        }

        var builder = new UriBuilder(lease?.WebSocketUrl ?? _options.PluginsWebSocketUrl);
        var query = new List<string>();

        foreach (var item in context.Request.Query)
        {
            if (IsGatewayOnlyQuery(item.Key))
            {
                continue;
            }

            AddQuery(query, item.Key, item.Value);
        }

        if (lease is not null)
        {
            AddQuery(query, "leaseId", lease.LeaseId);
            AddQuery(query, "nodeId", lease.NodeId);
        }

        builder.Query = string.Join("&", query);
        return builder.Uri.ToString();
    }

    private static bool IsGatewayOnlyQuery(string key)
    {
        return string.Equals(key, "accessToken", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "refreshToken", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddQuery(List<string> query, string key, StringValues values)
    {
        foreach (string? value in values)
        {
            query.Add(
                $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value ?? string.Empty)}"
            );
        }
    }

    private static void AddQuery(List<string> query, string key, string value)
    {
        query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
    }

    private static async Task SendAuthTokensAsync(
        WebSocket socket,
        AuthTokenResponse tokens,
        CancellationToken cancellationToken
    )
    {
        string json = JsonSerializer.Serialize(tokens);
        byte[] payload = Encoding.UTF8.GetBytes("auth tokens " + json);

        await socket.SendAsync(
            payload,
            WebSocketMessageType.Text,
            true,
            cancellationToken
        );
    }

    private static async Task PumpAsync(
        WebSocket source,
        WebSocket destination,
        CancellationToken cancellationToken
    )
    {
        var buffer = new byte[BufferSize];

        while (!cancellationToken.IsCancellationRequested
            && source.State == WebSocketState.Open
            && destination.State == WebSocketState.Open)
        {
            var result = await source.ReceiveAsync(buffer.AsMemory(), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await CloseAsync(destination, cancellationToken);
                return;
            }

            await destination.SendAsync(
                buffer.AsMemory(0, result.Count),
                result.MessageType,
                result.EndOfMessage,
                cancellationToken
            );
        }
    }

    private static async Task CloseAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "closed",
                cancellationToken
            );
        }
    }
}
