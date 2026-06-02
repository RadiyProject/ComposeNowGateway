using ComposeNowGateway.Services.Proxy;
using Microsoft.AspNetCore.Mvc;

namespace ComposeNowGateway.Controllers;

[ApiController]
public sealed class WebSocketController(IWebSocketProxyService proxy) : ControllerBase
{
    private readonly IWebSocketProxyService _proxy = proxy;

    [Route("/ws")]
    public Task Connect(CancellationToken cancellationToken)
    {
        return _proxy.ProxyAsync(HttpContext, cancellationToken);
    }
}
