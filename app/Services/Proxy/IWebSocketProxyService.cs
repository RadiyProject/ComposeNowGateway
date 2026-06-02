namespace ComposeNowGateway.Services.Proxy;

public interface IWebSocketProxyService
{
    Task ProxyAsync(HttpContext context, CancellationToken cancellationToken);
}
