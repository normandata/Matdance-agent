using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public sealed class BackgroundWorkCoordinator
{
    public const string MemoryResource = "memory";
    public const string SkillsResource = "skills";
    public static readonly TimeSpan ResourceRetryTimeout = TimeSpan.FromSeconds(30);

    private readonly PathService _path;
    private readonly BackgroundTaskQueue _queue;
    private readonly AgentActivityService? _activity;

    public BackgroundWorkCoordinator(PathService path, BackgroundTaskQueue queue, AgentActivityService? activity = null)
    {
        _path = path;
        _queue = queue;
        _activity = activity;
    }

    public int GetMaxConcurrency(string agent)
    {
        try
        {
            var config = AgentConfig.Load(_path.GetAgentConfigJsonPath(agent));
            return Math.Clamp(config.MaxConcurrency, 1, 16);
        }
        catch
        {
            return 1;
        }
    }

    public int GetReservedUserSlots(string agent)
    {
        return _activity?.GetActiveUserTurnCount(agent) ?? 0;
    }

    public int GetActiveBackgroundSlots(string agent)
    {
        return _queue.GetActiveBackgroundCount(agent);
    }

    public Task<IDisposable?> TryAcquireBudgetAsync(string agent, TimeSpan timeout, CancellationToken ct)
    {
        return _queue.TryAcquireAgentAsync(agent, GetMaxConcurrency(agent), GetReservedUserSlots(agent), timeout, ct);
    }

    public IDisposable BeginForegroundWork(string agent)
    {
        return _activity?.BeginUserTurn(agent) ?? NoopLease.Instance;
    }

    public async Task<IDisposable> AcquireBudgetAsync(string agent, CancellationToken ct)
    {
        var lease = await TryAcquireBudgetAsync(agent, Timeout.InfiniteTimeSpan, ct);
        return lease ?? throw new OperationCanceledException(ct);
    }

    public Task<IDisposable?> TryAcquireResourceAsync(string agent, string resource, TimeSpan timeout, CancellationToken ct)
    {
        return _queue.TryAcquireResourceAsync(agent, resource, timeout, ct);
    }

    public CancellationToken GetAgentCancellationToken(string agent)
    {
        return _queue.GetAgentCancellationToken(agent);
    }

    public CancellationTokenSource CreateAgentLinkedCancellation(string agent, params CancellationToken[] tokens)
    {
        var linkedTokens = tokens
            .Append(GetAgentCancellationToken(agent))
            .Where(token => token.CanBeCanceled)
            .ToArray();

        return linkedTokens.Length == 0
            ? new CancellationTokenSource()
            : CancellationTokenSource.CreateLinkedTokenSource(linkedTokens);
    }

    public void ForgetAgent(string agent)
    {
        _queue.ForgetAgent(agent);
        _activity?.RemoveAgent(agent);
    }

    public bool IsBudgetOverdrawn(string agent)
    {
        return GetReservedUserSlots(agent) + GetActiveBackgroundSlots(agent) > GetMaxConcurrency(agent);
    }

    public BackgroundBudgetCancellation CreateBudgetCancellation(string agent, CancellationToken hostCt, Func<bool>? shouldCancel = null, TimeSpan? pollInterval = null)
    {
        var cts = new CancellationTokenSource();
        var lease = new BackgroundBudgetCancellation(cts);
        var token = lease.Token;
        var interval = pollInterval ?? TimeSpan.FromSeconds(1);
        _ = Task.Run(async () =>
        {
            while (!hostCt.IsCancellationRequested && !token.IsCancellationRequested)
            {
                if (IsBudgetOverdrawn(agent) || shouldCancel?.Invoke() == true)
                {
                    lease.Cancel();
                    return;
                }

                try { await Task.Delay(interval, token); }
                catch (OperationCanceledException) { return; }
            }
        }, CancellationToken.None);
        return lease;
    }

    public static string? GetScheduledTaskResourceKey(ScheduledTaskItem task)
    {
        if (task.TaskId == ScheduledTaskService.SystemMemoryOrganizationTaskId)
            return MemoryResource;
        if (task.TaskId == ScheduledTaskService.SystemSkillOrganizationTaskId)
            return SkillsResource;
        return null;
    }

    public static int GetScheduledTaskPriority(ScheduledTaskItem task)
    {
        if (string.Equals(task.LastRunStatus, ScheduledTaskRunStatuses.Stalled, StringComparison.OrdinalIgnoreCase))
            return 10;
        if (task.TaskId == ScheduledTaskService.SystemMemoryOrganizationTaskId) return 300;
        if (task.TaskId == ScheduledTaskService.SystemSkillOrganizationTaskId) return 250;
        if (task.IsSystem) return 200;
        return 100;
    }

    private sealed class NoopLease : IDisposable
    {
        public static readonly NoopLease Instance = new();
        public void Dispose() { }
    }
}

public sealed class BackgroundBudgetCancellation : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    public BackgroundBudgetCancellation(CancellationTokenSource cts)
    {
        _cts = cts;
    }

    public CancellationToken Token => _cts.Token;

    public void Cancel()
    {
        try { _cts.Cancel(); } catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cancel();
        _cts.Dispose();
    }
}
