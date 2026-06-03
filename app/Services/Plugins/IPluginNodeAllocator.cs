namespace ComposeNowGateway.Services.Plugins;

public interface IPluginNodeAllocator
{
    Task<PluginNodeLease?> AllocateAsync(
        string pluginName,
        string sessionId,
        CancellationToken cancellationToken
    );
}

