using System.Text.Json;
using StackExchange.Redis;

namespace ComposeNowGateway.Services.Plugins;

public sealed class RedisPluginNodeAllocator(
    ILogger<RedisPluginNodeAllocator> logger
) : IPluginNodeAllocator, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly TimeSpan LeaseTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan PendingTaskTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NodeMaxAge = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly double BusyThresholdPercent = ReadBusyThreshold();

    private readonly IConnectionMultiplexer _redis = ConnectionMultiplexer.Connect(BuildRedisOptions());

    private readonly ILogger<RedisPluginNodeAllocator> _logger = logger;

    public async Task<PluginNodeLease?> AllocateAsync(
        string pluginName,
        string sessionId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            IDatabase database = _redis.GetDatabase();
            TimeSpan waitTimeout = ReadWaitTimeout();
            var deadline = DateTimeOffset.UtcNow.Add(waitTimeout);
            bool pendingTaskPublished = false;

            while (true)
            {
                PluginNodeSnapshot? node = await FindNodeAsync(
                    database,
                    pluginName,
                    cancellationToken
                );

                if (node is not null)
                {
                    return await CreateLeaseAsync(
                        database,
                        node,
                        pluginName
                    );
                }

                if (!pendingTaskPublished)
                {
                    await PublishPendingTaskAsync(
                        database,
                        pluginName,
                        sessionId
                    );
                    pendingTaskPublished = true;
                }

                if (DateTimeOffset.UtcNow >= deadline)
                {
                    return null;
                }

                await Task.Delay(PollInterval, cancellationToken);
            }
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(
                exception,
                "Plugin node allocation failed because plugins Redis is unavailable. PluginName={PluginName}",
                pluginName
            );

            return null;
        }
    }

    private static ConfigurationOptions BuildRedisOptions()
    {
        int timeoutMs = ReadPositiveInt("PLUGINS_REDIS_TIMEOUT_MS", 15000);

        return new ConfigurationOptions
        {
            EndPoints = { $"{Environment.GetEnvironmentVariable("PLUGINS_REDIS_HOST") ?? "redis"}:6379" },
            Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
            AbortOnConnectFail = false,
            ConnectRetry = 3,
            ConnectTimeout = timeoutMs,
            AsyncTimeout = timeoutMs,
            SyncTimeout = timeoutMs
        };
    }

    private async Task<PluginNodeLease?> CreateLeaseAsync(
        IDatabase database,
        PluginNodeSnapshot node,
        string pluginName
    )
    {
        var now = DateTimeOffset.UtcNow;
        var lease = new PluginNodeLease(
            Guid.NewGuid().ToString("N"),
            node.NodeId,
            pluginName,
            node.WebSocketUrl,
            now,
            now.Add(LeaseTtl)
        );

        string serialized = JsonSerializer.Serialize(lease, JsonOptions);
        bool created = await database.StringSetAsync(
            PluginBrokerKeys.Lease(lease.LeaseId),
            serialized,
            LeaseTtl,
            When.NotExists
        );

        if (!created)
        {
            _logger.LogWarning(
                "Plugin node lease collision. LeaseId={LeaseId}, PluginName={PluginName}",
                lease.LeaseId,
                pluginName
            );

            return null;
        }

        await database.ListLeftPopAsync(PluginBrokerKeys.PendingTasks);

        return lease;
    }

    private static TimeSpan ReadWaitTimeout()
    {
        string? raw = Environment.GetEnvironmentVariable("PLUGIN_NODE_ALLOCATION_WAIT_SECONDS");
        return int.TryParse(raw, out int seconds) && seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.FromSeconds(10);
    }

    private static double ReadBusyThreshold()
    {
        string? raw = Environment.GetEnvironmentVariable("COMPOSE_NOW_PLUGIN_BUSY_THRESHOLD_PERCENT");
        return double.TryParse(raw, out double value) && value > 0
            ? value
            : 90;
    }

    private static int ReadPositiveInt(string key, int fallback)
    {
        string? raw = Environment.GetEnvironmentVariable(key);
        return int.TryParse(raw, out int value) && value > 0
            ? value
            : fallback;
    }

    private static async Task<PluginNodeSnapshot?> FindNodeAsync(
        IDatabase database,
        string pluginName,
        CancellationToken cancellationToken
    )
    {
        RedisValue[] nodeIds = await database.SetMembersAsync(PluginBrokerKeys.NodesSet);
        var now = DateTimeOffset.UtcNow;

        PluginNodeSnapshot? selected = null;

        foreach (RedisValue nodeId in nodeIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (nodeId.IsNullOrEmpty)
            {
                continue;
            }

            RedisValue value = await database.StringGetAsync(PluginBrokerKeys.Node(nodeId!));
            if (value.IsNullOrEmpty)
            {
                await database.SetRemoveAsync(PluginBrokerKeys.NodesSet, nodeId);
                continue;
            }

            PluginNodeSnapshot? node = JsonSerializer.Deserialize<PluginNodeSnapshot>(
                value!,
                JsonOptions
            );

            if (node is null
                || node.Draining
                || now - node.UpdatedAt > NodeMaxAge
                || !node.Plugins.Contains(pluginName, StringComparer.OrdinalIgnoreCase)
                || !CanAcceptOneMoreSession(node)
                || node.LoadPercent >= BusyThresholdPercent)
            {
                continue;
            }

            if (selected is null
                || node.LoadPercent < selected.LoadPercent
                || node.ActiveSessions < selected.ActiveSessions)
            {
                selected = node;
            }
        }

        return selected;
    }

    private static bool CanAcceptOneMoreSession(PluginNodeSnapshot node)
    {
        if (node.MaxSessions <= 0 || node.ActiveSessions >= node.MaxSessions)
        {
            return false;
        }

        double projectedLoadPercent = (node.ActiveSessions + 1) * 100d / node.MaxSessions;
        return projectedLoadPercent < BusyThresholdPercent;
    }

    private static async Task PublishPendingTaskAsync(
        IDatabase database,
        string pluginName,
        string sessionId
    )
    {
        var now = DateTimeOffset.UtcNow;
        var task = new PluginTaskRequest(
            Guid.NewGuid().ToString("N"),
            pluginName,
            sessionId,
            now,
            now.Add(PendingTaskTtl)
        );

        await database.ListRightPushAsync(
            PluginBrokerKeys.PendingTasks,
            JsonSerializer.Serialize(task, JsonOptions)
        );
        await database.KeyExpireAsync(PluginBrokerKeys.PendingTasks, PendingTaskTtl);
    }

    public void Dispose()
    {
        _redis.Dispose();
    }
}
