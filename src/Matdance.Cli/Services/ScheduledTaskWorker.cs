using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public class ScheduledTaskWorker
{
    private readonly ScheduledTaskService _tasks;
    private readonly ScheduledTaskRunner _runner;
    private readonly BackgroundWorkCoordinator _work;
    private readonly BackgroundEventService _events;

    public ScheduledTaskWorker(ScheduledTaskService tasks, ScheduledTaskRunner runner, BackgroundTaskQueue queue, AgentActivityService? activity = null, PathService? path = null)
    {
        _tasks = tasks;
        _runner = runner;
        var pathService = path ?? new PathService();
        _work = new BackgroundWorkCoordinator(pathService, queue, activity);
        _events = new BackgroundEventService(pathService);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _events.RecoverStaleJobsForAllAgents(DateTimeOffset.UtcNow - ScheduledTaskService.RunHeartbeatTimeout, "Recovered an unfinished background job left by an earlier Matdance process.");
        _tasks.RecoverInterruptedRuns(DateTimeOffset.UtcNow);
        while (!ct.IsCancellationRequested)
        {
            var launched = 0;
            try
            {
                var now = DateTimeOffset.UtcNow;
                _tasks.RecoverInterruptedRuns(now);
                launched = await LaunchDueTasksAsync(now, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Agent deletion can race with a due-task snapshot. The next poll reloads from disk.
            }

            try { await Task.Delay(launched > 0 ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(15), ct); }
            catch (OperationCanceledException) { }
        }
    }

    private async Task<int> LaunchDueTasksAsync(DateTimeOffset now, CancellationToken ct)
    {
        var dueTasks = _tasks.GetDueTasks(now)
            .OrderByDescending(BackgroundWorkCoordinator.GetScheduledTaskPriority)
            .ThenBy(task => task.NextRunAt ?? DateTimeOffset.MaxValue)
            .ThenBy(task => task.Agent, StringComparer.OrdinalIgnoreCase)
            .ThenBy(task => task.TaskId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var launched = 0;
        var resourceBlocked = new List<ScheduledTaskItem>();
        foreach (var due in dueTasks)
        {
            if (ct.IsCancellationRequested) break;

            var resourceLease = await TryAcquireTaskResourceAsync(due, TimeSpan.Zero, ct);
            if (resourceLease == null && BackgroundWorkCoordinator.GetScheduledTaskResourceKey(due) != null)
            {
                resourceBlocked.Add(due);
                continue;
            }

            if (await TryLaunchDueTaskAsync(due, resourceLease, ct))
                launched++;
        }

        if (launched == 0 && resourceBlocked.Count > 0 && !ct.IsCancellationRequested)
        {
            foreach (var due in resourceBlocked)
            {
                if (ct.IsCancellationRequested)
                    break;

                var resourceLease = await TryAcquireTaskResourceAsync(due, BackgroundWorkCoordinator.ResourceRetryTimeout, ct);
                if (resourceLease == null)
                    continue;

                if (await TryLaunchDueTaskAsync(due, resourceLease, ct))
                {
                    launched++;
                    break;
                }
            }
        }

        return launched;
    }

    private async Task<bool> TryLaunchDueTaskAsync(ScheduledTaskItem due, IDisposable? resourceLease, CancellationToken ct)
    {
        var budgetLease = await _work.TryAcquireBudgetAsync(due.Agent, TimeSpan.Zero, ct);
        if (budgetLease == null)
        {
            resourceLease?.Dispose();
            return false;
        }

        var scheduledAt = due.NextRunAt;
        var catchUp = scheduledAt.HasValue && scheduledAt.Value < DateTimeOffset.UtcNow - TimeSpan.FromSeconds(90);
        var trigger = catchUp ? "catch_up" : "schedule";
        var reason = catchUp ? "missed while Matdance was stopped, asleep, or busy" : null;
        var task = _tasks.TryStartRun(due.Agent, due.TaskId, scheduledAt: scheduledAt, trigger: trigger, catchUpReason: reason);
        if (task == null)
        {
            resourceLease?.Dispose();
            budgetLease.Dispose();
            return false;
        }

        StartRun(task, due, scheduledAt, trigger, reason, budgetLease, resourceLease, ct);
        return true;
    }

    private void StartRun(ScheduledTaskItem task, ScheduledTaskItem due, DateTimeOffset? scheduledAt, string trigger, string? catchUpReason, IDisposable budgetLease, IDisposable? resourceLease, CancellationToken hostCt)
    {
        _ = Task.Run(async () =>
        {
            using (budgetLease)
            using (resourceLease)
            using (var stallCts = new CancellationTokenSource())
            using (var budgetCts = CreateBudgetCancellation(task.Agent, hostCt))
            using (var linked = _work.CreateAgentLinkedCancellation(task.Agent, hostCt, budgetCts.Token, stallCts.Token))
            {
                var stalled = false;
                var stallDiagnostic = string.Empty;
                var monitor = MonitorRunHeartbeatAsync(task, stallCts, hostCt, diagnostic =>
                {
                    stalled = true;
                    stallDiagnostic = diagnostic;
                });
                try
                {
                    var run = await _runner.ExecuteAsync(task, trigger, scheduledAt, true, linked.Token, catchUpReason);
                    if (stalled)
                        MarkRunStalled(run, stallDiagnostic);
                    _tasks.FinishRun(run);
                }
                catch (OperationCanceledException) when (stalled)
                {
                    _tasks.FinishRun(BuildStalledRun(task, due, scheduledAt, trigger, catchUpReason, stallDiagnostic));
                }
                catch (OperationCanceledException)
                {
                    _tasks.FinishRun(BuildCanceledRun(task, due, scheduledAt, trigger, catchUpReason));
                }
                catch (Exception ex)
                {
                    _tasks.FinishRun(BuildFailedRun(task, due, scheduledAt, trigger, catchUpReason, ex));
                }
                finally
                {
                    try { stallCts.Cancel(); } catch { }
                    try { await monitor; } catch { }
                }
            }
        }, CancellationToken.None);
    }

    private async Task MonitorRunHeartbeatAsync(ScheduledTaskItem task, CancellationTokenSource stallCts, CancellationToken hostCt, Action<string> onStalled)
    {
        while (!hostCt.IsCancellationRequested && !stallCts.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(5), hostCt); }
            catch (OperationCanceledException) { return; }

            try
            {
                if (_tasks.TryGetRunStallDiagnostic(task.Agent, task.TaskId, task.ActiveRunId, DateTimeOffset.UtcNow, out var diagnostic))
                {
                    onStalled(diagnostic);
                    try { stallCts.Cancel(); } catch { }
                    return;
                }
            }
            catch
            {
                return;
            }
        }
    }

    private async Task<IDisposable?> TryAcquireTaskResourceAsync(ScheduledTaskItem task, TimeSpan timeout, CancellationToken ct)
    {
        var resource = BackgroundWorkCoordinator.GetScheduledTaskResourceKey(task);
        if (resource == null)
            return null;

        return await _work.TryAcquireResourceAsync(task.Agent, resource, timeout, ct);
    }

    private static ScheduledTaskRun BuildCanceledRun(ScheduledTaskItem task, ScheduledTaskItem due, DateTimeOffset? scheduledAt, string trigger, string? catchUpReason)
    {
        return new ScheduledTaskRun
        {
            RunId = string.IsNullOrWhiteSpace(task.ActiveRunId) ? "run_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : task.ActiveRunId,
            TaskId = due.TaskId,
            Agent = due.Agent,
            Trigger = trigger,
            ScheduledAt = scheduledAt,
            CatchUpReason = catchUpReason,
            StartedAt = task.ActiveRunStartedAt ?? UserTimeZoneService.Now(),
            FinishedAt = UserTimeZoneService.Now(),
            Status = ScheduledTaskRunStatuses.Canceled,
            ErrorType = "budget_preempted",
            Error = "Canceled because user activity or higher-priority work consumed the agent concurrency budget."
        };
    }

    private static ScheduledTaskRun BuildStalledRun(ScheduledTaskItem task, ScheduledTaskItem due, DateTimeOffset? scheduledAt, string trigger, string? catchUpReason, string diagnostic)
    {
        return new ScheduledTaskRun
        {
            RunId = string.IsNullOrWhiteSpace(task.ActiveRunId) ? "run_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : task.ActiveRunId,
            TaskId = due.TaskId,
            Agent = due.Agent,
            Trigger = trigger,
            ScheduledAt = scheduledAt,
            CatchUpReason = catchUpReason,
            StartedAt = task.ActiveRunStartedAt ?? UserTimeZoneService.Now(),
            FinishedAt = UserTimeZoneService.Now(),
            Status = ScheduledTaskRunStatuses.Stalled,
            ErrorType = "heartbeat_timeout",
            Error = diagnostic,
            Diagnostic = diagnostic
        };
    }

    private static ScheduledTaskRun BuildFailedRun(ScheduledTaskItem task, ScheduledTaskItem due, DateTimeOffset? scheduledAt, string trigger, string? catchUpReason, Exception ex)
    {
        return new ScheduledTaskRun
        {
            RunId = string.IsNullOrWhiteSpace(task.ActiveRunId) ? "run_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : task.ActiveRunId,
            TaskId = due.TaskId,
            Agent = due.Agent,
            Trigger = trigger,
            ScheduledAt = scheduledAt,
            CatchUpReason = catchUpReason,
            StartedAt = task.ActiveRunStartedAt ?? UserTimeZoneService.Now(),
            FinishedAt = UserTimeZoneService.Now(),
            Status = ScheduledTaskRunStatuses.Failed,
            ErrorType = ex.GetType().Name,
            Error = ex.Message
        };
    }

    private BackgroundBudgetCancellation CreateBudgetCancellation(string agent, CancellationToken hostCt)
        => _work.CreateBudgetCancellation(agent, hostCt);

    private static void MarkRunStalled(ScheduledTaskRun run, string diagnostic)
    {
        run.Status = ScheduledTaskRunStatuses.Stalled;
        run.ErrorType = "heartbeat_timeout";
        run.Error = diagnostic;
        run.Diagnostic = diagnostic;
        run.Events.Add(new ScheduledTaskRunEvent { Type = "stalled", Message = diagnostic, Status = ScheduledTaskRunStatuses.Stalled });
    }
}
