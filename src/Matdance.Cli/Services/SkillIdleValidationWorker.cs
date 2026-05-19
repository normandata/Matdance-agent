using System.Text.Json;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public sealed class SkillIdleValidationWorker
{
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private const int MaxQueueItems = 1000;

    private readonly PathService _path;
    private readonly SkillMaintenanceService _skills;
    private readonly ScheduledTaskService _scheduledTasks;
    private readonly AgentActivityService _activity;
    private readonly BackgroundWorkCoordinator _work;
    private readonly SkillValidationSettingsService _settings = new();
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    private static string QueuePath => Path.Combine(MatdanceRuntime.StateRoot, "skill-validation-queue.json");

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
                await RunOneSchedulerTickAsync(ct);
            }
            catch (OperationCanceledException)
            {
                if (ct.IsCancellationRequested) break;
            }
            catch
            {
                // Background validation is opportunistic; retry on the next scheduler tick.
            }

            try { await Task.Delay(PollInterval, ct); }
            catch (OperationCanceledException) { }
        }
    }

    private async Task RunOneSchedulerTickAsync(CancellationToken ct)
    {
        var settings = _settings.Load();
        if (!settings.AutoSkillValidationEnabled)
            return;

        var state = LoadQueueState();
        RefreshQueue(state);
        var now = DateTimeOffset.UtcNow;
        var interval = TimeSpan.FromHours(settings.AutoSkillValidationIntervalHours);

        if (!state.NextRunAt.HasValue)
        {
            state.NextRunAt = now + interval;
            SaveQueueState(state);
            return;
        }

        if (state.Pending.Count == 0)
        {
            if (state.NextRunAt.Value <= now)
                state.NextRunAt = now + interval;
            SaveQueueState(state);
            return;
        }

        if (state.RateLimitedUntil.HasValue && state.RateLimitedUntil.Value > now)
        {
            SaveQueueState(state);
            return;
        }

        if (state.NextRunAt.Value > now)
        {
            SaveQueueState(state);
            return;
        }

        var outcome = await ProcessDueQueueWindowAsync(state, settings.AutoSkillValidationBatchSize, interval, ct);
        if (outcome.Processed > 0 || outcome.RateLimited)
        {
            now = DateTimeOffset.UtcNow;
            state.LastRunAt = now;
            state.NextRunAt = now + interval;
        }

        SaveQueueState(state);
    }

    private async Task<QueueWindowOutcome> ProcessDueQueueWindowAsync(AutoValidationQueueState state, int batchSize, TimeSpan interval, CancellationToken ct)
    {
        var processed = 0;
        var rateLimited = false;

        while (processed < batchSize && state.Pending.Count > 0 && !ct.IsCancellationRequested)
        {
            var item = state.Pending[0];
            if (!IsStillAutomaticCandidate(item))
            {
                state.Pending.RemoveAt(0);
                SaveQueueState(state);
                continue;
            }

            if (!IsIdle(item.Agent) || HasDueScheduledTask(item.Agent))
                break;

            using var budgetLease = await _work.TryAcquireBudgetAsync(item.Agent, TimeSpan.Zero, ct);
            if (budgetLease == null)
                break;

            using var resourceLease = await _work.TryAcquireResourceAsync(item.Agent, BackgroundWorkCoordinator.SkillsResource, TimeSpan.Zero, ct);
            if (resourceLease == null)
                break;

            if (!IsIdle(item.Agent) || HasDueScheduledTask(item.Agent))
                break;

            using var budgetCts = CreateBudgetOrDueCancellation(item.Agent, ct);
            using var linked = _work.CreateAgentLinkedCancellation(item.Agent, ct, budgetCts.Token);
            try
            {
                var jobId = _skills.StartValidation(item.Agent, item.SkillId, linked.Token, automatic: true);
                await WaitForSkillJobAsync(jobId, linked.Token);
                processed++;

                var job = _skills.GetJob(jobId);
                item.Attempts++;
                item.LastAttemptAt = DateTimeOffset.UtcNow;

                if (IsRateLimitedJob(job))
                {
                    state.RateLimitedUntil = DateTimeOffset.UtcNow + interval;
                    rateLimited = true;
                    break;
                }

                if (!IsStillAutomaticCandidate(item) || string.Equals(job?.Status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    state.Pending.RemoveAt(0);
                }
                else
                {
                    state.Pending.RemoveAt(0);
                    state.Pending.Add(item);
                }

                SaveQueueState(state);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return new QueueWindowOutcome(processed, rateLimited);
    }

    private void RefreshQueue(AutoValidationQueueState state)
    {
        var currentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kept = new List<AutoValidationQueueItem>();

        foreach (var item in state.Pending)
        {
            if (!IsStillAutomaticCandidate(item))
                continue;

            var key = QueueKey(item.Agent, item.SkillId, item.Fingerprint);
            if (currentKeys.Add(key))
                kept.Add(item);
        }

        state.Pending = kept;

        foreach (var agent in ListAgents())
        {
            foreach (var skill in _skills.ListAutomaticValidationCandidates(agent))
            {
                var skillDir = _path.GetSkillPath(agent, skill.Id);
                if (!Directory.Exists(skillDir) || !SkillValidationState.NeedsAutomaticValidation(skillDir))
                    continue;

                var fingerprint = SkillValidationState.ComputeFingerprint(skillDir);
                var key = QueueKey(agent, skill.Id, fingerprint);
                if (!currentKeys.Add(key))
                    continue;

                state.Pending.Add(new AutoValidationQueueItem
                {
                    Agent = agent,
                    SkillId = skill.Id,
                    Fingerprint = fingerprint,
                    EnqueuedAt = DateTimeOffset.UtcNow
                });

                if (state.Pending.Count >= MaxQueueItems)
                    return;
            }
        }
    }

    private bool IsStillAutomaticCandidate(AutoValidationQueueItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Agent) || string.IsNullOrWhiteSpace(item.SkillId) || string.IsNullOrWhiteSpace(item.Fingerprint))
            return false;

        var skillDir = _path.GetSkillPath(item.Agent, item.SkillId);
        if (!Directory.Exists(skillDir))
            return false;

        if (!SkillValidationState.NeedsAutomaticValidation(skillDir))
            return false;

        var fingerprint = SkillValidationState.ComputeFingerprint(skillDir);
        return string.Equals(fingerprint, item.Fingerprint, StringComparison.OrdinalIgnoreCase);
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

    private static AutoValidationQueueState LoadQueueState()
    {
        if (!File.Exists(QueuePath))
            return new AutoValidationQueueState();

        try
        {
            var state = JsonSerializer.Deserialize<AutoValidationQueueState>(File.ReadAllText(QueuePath), JsonOptions) ?? new AutoValidationQueueState();
            state.Pending ??= new List<AutoValidationQueueItem>();
            return state;
        }
        catch
        {
            return new AutoValidationQueueState();
        }
    }

    private static void SaveQueueState(AutoValidationQueueState state)
    {
        Directory.CreateDirectory(MatdanceRuntime.StateRoot);
        state.Pending = state.Pending
            .Where(item => !string.IsNullOrWhiteSpace(item.Agent) && !string.IsNullOrWhiteSpace(item.SkillId) && !string.IsNullOrWhiteSpace(item.Fingerprint))
            .Take(MaxQueueItems)
            .ToList();
        AtomicFile.WriteAllText(QueuePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    private static bool IsRateLimitedJob(SkillJob? job)
    {
        var text = string.Join("\n", job?.Error, job?.Stage, job?.ResultSummary);
        return text.Contains("429", StringComparison.OrdinalIgnoreCase)
            || text.Contains("rate_limit", StringComparison.OrdinalIgnoreCase)
            || text.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || text.Contains("quota", StringComparison.OrdinalIgnoreCase)
            || text.Contains("high demand", StringComparison.OrdinalIgnoreCase);
    }

    private static string QueueKey(string agent, string skillId, string fingerprint)
        => agent.Trim() + "\n" + skillId.Trim() + "\n" + fingerprint.Trim();

    private sealed record QueueWindowOutcome(int Processed, bool RateLimited);

    private sealed class AutoValidationQueueState
    {
        public DateTimeOffset? LastRunAt { get; set; }
        public DateTimeOffset? NextRunAt { get; set; }
        public DateTimeOffset? RateLimitedUntil { get; set; }
        public List<AutoValidationQueueItem> Pending { get; set; } = new();
    }

    private sealed class AutoValidationQueueItem
    {
        public string Agent { get; set; } = string.Empty;
        public string SkillId { get; set; } = string.Empty;
        public string Fingerprint { get; set; } = string.Empty;
        public DateTimeOffset EnqueuedAt { get; set; }
        public DateTimeOffset? LastAttemptAt { get; set; }
        public int Attempts { get; set; }
    }
}
