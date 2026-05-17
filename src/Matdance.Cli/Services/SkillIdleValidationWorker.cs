using System.Text.Json;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public sealed class SkillIdleValidationWorker
{
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private readonly PathService _path;
    private readonly SkillMaintenanceService _skills;
    private readonly ScheduledTaskService _scheduledTasks;
    private readonly AgentActivityService _activity;
    private readonly BackgroundWorkCoordinator _work;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    public SkillIdleValidationWorker(PathService path, SkillMaintenanceService skills, ScheduledTaskService scheduledTasks, AgentActivityService activity, BackgroundTaskQueue queue)
    {
        _path = path;
        _skills = skills;
        _scheduledTasks = scheduledTasks;
        _activity = activity;
        _work = new BackgroundWorkCoordinator(path, queue, activity);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                foreach (var agent in ListAgents())
                {
                    if (ct.IsCancellationRequested) break;
                    await ValidateAgentWhenIdleAsync(agent, ct);
                }
            }
            catch (OperationCanceledException)
            {
                if (ct.IsCancellationRequested) break;
            }
            catch
            {
                // Background validation is opportunistic; retry on the next idle window.
            }

            try { await Task.Delay(PollInterval, ct); }
            catch (OperationCanceledException) { }
        }
    }

    private async Task ValidateAgentWhenIdleAsync(string agent, CancellationToken ct)
    {
        if (!IsIdle(agent))
            return;

        var candidates = _skills.ListUnverifiedSkills(agent);
        foreach (var skill in candidates)
        {
            if (!IsIdle(agent) || HasDueScheduledTask(agent))
                return;

            using var lease = await _work.TryAcquireBudgetAsync(agent, TimeSpan.Zero, ct);
            if (lease == null)
                return;

            using var resourceLease = await _work.TryAcquireResourceAsync(agent, BackgroundWorkCoordinator.SkillsResource, TimeSpan.Zero, ct);
            if (resourceLease == null)
                return;

            if (!IsIdle(agent) || HasDueScheduledTask(agent))
                return;

            using var budgetCts = CreateBudgetOrDueCancellation(agent, ct);
            using var linked = _work.CreateAgentLinkedCancellation(agent, ct, budgetCts.Token);
            try
            {
                var jobId = _skills.StartValidation(agent, skill.Id, linked.Token);
                await WaitForSkillJobAsync(jobId, linked.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task WaitForSkillJobAsync(string jobId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var job = _skills.GetJob(jobId);
            if (job == null || job.Status != "running")
                return;

            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    private bool IsIdle(string agent)
    {
        if (_activity.HasActiveUserTurn(agent))
            return false;

        var latest = LatestSessionActivity(agent);
        var inMemory = _activity.GetLastUserActivity(agent);
        if (inMemory.HasValue && (!latest.HasValue || inMemory.Value > latest.Value))
            latest = inMemory.Value;
        if (!latest.HasValue || latest.Value < _startedAt)
            latest = _startedAt;

        return DateTimeOffset.UtcNow - latest.Value >= IdleThreshold;
    }

    private DateTimeOffset? LatestSessionActivity(string agent)
    {
        var sessionsDir = _path.GetSessionsPath(agent);
        if (!Directory.Exists(sessionsDir))
            return null;

        DateTimeOffset? latest = null;
        foreach (var file in Directory.GetFiles(sessionsDir, "*.json"))
        {
            if (file.EndsWith(".state.json", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var data = JsonSerializer.Deserialize<SessionData>(File.ReadAllText(file), new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (data == null) continue;
                if (!latest.HasValue || data.LastActivity > latest.Value)
                    latest = data.LastActivity;
            }
            catch
            {
                // Skip malformed session files.
            }
        }

        return latest;
    }

    private bool HasDueScheduledTask(string agent)
    {
        return _scheduledTasks.GetDueTasks(DateTimeOffset.UtcNow)
            .Any(task => string.Equals(task.Agent, agent, StringComparison.OrdinalIgnoreCase));
    }

    private BackgroundBudgetCancellation CreateBudgetOrDueCancellation(string agent, CancellationToken hostCt)
    {
        return _work.CreateBudgetCancellation(agent, hostCt, () => HasDueScheduledTask(agent), TimeSpan.FromSeconds(2));
    }

    private IEnumerable<string> ListAgents()
    {
        if (!Directory.Exists(_path.AgentsRoot))
            yield break;

        foreach (var dir in Directory.GetDirectories(_path.AgentsRoot).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            var agent = Path.GetFileName(dir);
            if (!string.IsNullOrWhiteSpace(agent))
                yield return agent;
        }
    }
}
