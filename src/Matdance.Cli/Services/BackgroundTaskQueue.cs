using System.Collections.Concurrent;

namespace Matdance.Cli.Services;

public sealed class BackgroundTaskQueue
{
    private readonly ConcurrentDictionary<string, AgentQueueState> _agents = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _resources = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _agentCancellation = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IDisposable> AcquireAsync(CancellationToken ct)
    {
        return await AcquireAgentAsync("__global__", 1, 0, ct);
    }

    public async Task<IDisposable> AcquireAgentAsync(string agent, int maxConcurrency, int reservedSlots, CancellationToken ct)
    {
        var lease = await TryAcquireAgentAsync(agent, maxConcurrency, reservedSlots, Timeout.InfiniteTimeSpan, ct);
        return lease ?? throw new OperationCanceledException(ct);
    }

    public async Task<IDisposable?> TryAcquireAgentAsync(string agent, int maxConcurrency, int reservedSlots, TimeSpan timeout, CancellationToken ct)
    {
        var state = _agents.GetOrAdd(NormalizeAgent(agent), _ => new AgentQueueState());
        var deadline = timeout == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow + timeout;
        while (!ct.IsCancellationRequested)
        {
            lock (state)
            {
                var available = Math.Max(0, Math.Max(1, maxConcurrency) - Math.Max(0, reservedSlots));
                if (state.ActiveBackgroundTasks < available)
                {
                    state.ActiveBackgroundTasks++;
                    return new AgentLease(state);
                }
            }

            if (DateTimeOffset.UtcNow >= deadline)
                return null;

            var delay = timeout == TimeSpan.Zero
                ? TimeSpan.Zero
                : TimeSpan.FromMilliseconds(Math.Min(250, Math.Max(1, (deadline - DateTimeOffset.UtcNow).TotalMilliseconds)));
            if (delay == TimeSpan.Zero)
                return null;

            await Task.Delay(delay, ct);
        }

        ct.ThrowIfCancellationRequested();
        return null;
    }

    public int GetActiveBackgroundCount(string agent)
    {
        if (!_agents.TryGetValue(NormalizeAgent(agent), out var state))
            return 0;

        lock (state)
        {
            return state.ActiveBackgroundTasks;
        }
    }

    public async Task<IDisposable?> TryAcquireResourceAsync(string agent, string resource, TimeSpan timeout, CancellationToken ct)
    {
        var key = NormalizeAgent(agent) + "\u001f" + resource.Trim().ToLowerInvariant();
        var gate = _resources.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        if (await gate.WaitAsync(timeout, ct))
            return new ResourceLease(gate);
        return null;
    }

    public CancellationToken GetAgentCancellationToken(string agent)
    {
        return _agentCancellation.GetOrAdd(NormalizeAgent(agent), _ => new CancellationTokenSource()).Token;
    }

    public void ForgetAgent(string agent)
    {
        var normalized = NormalizeAgent(agent);
        if (_agentCancellation.TryRemove(normalized, out var cts))
        {
            try { cts.Cancel(); } catch { }
            cts.Dispose();
        }

        _agents.TryRemove(normalized, out _);

        var resourcePrefix = normalized + "\u001f";
        foreach (var key in _resources.Keys)
        {
            if (key.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase))
                _resources.TryRemove(key, out _);
        }
    }

    private static string NormalizeAgent(string agent)
        => string.IsNullOrWhiteSpace(agent) ? "__global__" : agent.Trim();

    private sealed class AgentQueueState
    {
        public int ActiveBackgroundTasks { get; set; }
    }

    private sealed class AgentLease : IDisposable
    {
        private readonly AgentQueueState _state;
        private bool _disposed;

        public AgentLease(AgentQueueState state)
        {
            _state = state;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            lock (_state)
            {
                _state.ActiveBackgroundTasks = Math.Max(0, _state.ActiveBackgroundTasks - 1);
            }
        }
    }

    private sealed class ResourceLease : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private bool _disposed;

        public ResourceLease(SemaphoreSlim gate)
        {
            _gate = gate;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gate.Release();
        }
    }
}
