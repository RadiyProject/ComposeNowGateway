namespace ComposeNowGateway.Models;

public sealed class GatewayOptions
{
    public string SiteBaseUrl { get; set; } = "http://site:5001";
    public string PluginsWebSocketUrl { get; set; } = "ws://plugins:5001/ws";
    public int SessionTtlMinutes { get; set; } = 60;
}
