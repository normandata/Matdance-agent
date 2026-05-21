using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public class ScheduledTaskService
{
    public const string SystemMemoryOrganizationTaskId = "sched_system_memory_org";
    public const string SystemSkillOrganizationTaskId = "sched_system_skill_org";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly object Gate = new();
    private static readonly TimeSpan RunningRecoveryAge = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan RunHeartbeatTimeout = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan StalledBackoff = TimeSpan.FromMinutes(30);
    private const int MaxCatchUpRunsPerTask = 8;
    private const int MaxDueRunsPerAgentPerPoll = 25;

    private readonly PathService _path;
    private readonly BackgroundEventService _events;

    public ScheduledTaskService(PathService path)
    {
        _path = path;
        _events = new BackgroundEventService(path);
    }

    public ScheduledTaskItem Create(ScheduledTaskCreateRequest request)
    {
        var agent = RequireAgent(request.Agent);
        ValidateSchedule(request.Schedule);
        var now = DateTimeOffset.UtcNow;
        var userNow = UserTimeZoneService.Now();
        var task = new ScheduledTaskItem
        {
            TaskId = NewId("sched"),
            Agent = agent,
            Title = Require(request.Title, "title"),
            Content = Require(request.Content, "content"),
            TimeZone = NormalizeTimeZone(request.TimeZone),
            Schedule = request.Schedule ?? new ScheduledTaskSchedule(),
            CreatedFromSession = Blank(request.CreatedFromSession),
            CreatedAt = userNow,
            UpdatedAt = userNow
        };
        task.NextRunAt = ComputeNextRunAtOrThrow(task, now);
        task.Targets = NormalizeTargets(agent, request.Targets, task.CreatedFromSession, task.Title, task.TaskId);
        lock (Gate)
        {
            var tasks = LoadCore(agent);
            tasks.Add(task);
            SaveCore(agent, tasks);
        }
        return Clone(task);
    }

    public ScheduledTaskItem Edit(ScheduledTaskEditRequest request)
    {
        var agent = RequireAgent(request.Agent);
        if (request.Schedule != null) ValidateSchedule(request.Schedule);
        lock (Gate)
        {
            var tasks = LoadCore(agent);
            var task = FindVisibleTask(tasks, request.TaskId) ?? throw new InvalidOperationException("Scheduled task not found.");
            if (task.IsSystem) throw new InvalidOperationException("System tasks cannot be modified.");
            if (!string.IsNullOrWhiteSpace(request.Title)) task.Title = request.Title.Trim();
            if (!string.IsNullOrWhiteSpace(request.Content)) task.Content = request.Content.Trim();
            if (!string.IsNullOrWhiteSpace(request.TimeZone)) task.TimeZone = NormalizeTimeZone(request.TimeZone);
            if (!string.IsNullOrWhiteSpace(request.Status)) task.Status = request.Status.Trim().ToLowerInvariant();
            if (request.Schedule != null) task.Schedule = request.Schedule;
            task.UpdatedAt = UserTimeZoneService.Now();
            task.NextRunAt = task.Status == ScheduledTaskStatuses.Enabled
                ? ComputeNextRunAtOrThrow(task, DateTimeOffset.UtcNow)
                : null;
            if (request.Targets != null)
                task.Targets = NormalizeTargets(agent, request.Targets, task.CreatedFromSession, task.Title, task.TaskId);
            else
                RefreshNotificationSessionTitles(agent, task);
            SaveCore(agent, tasks);
            return Clone(task);
        }
    }

    public ScheduledTaskItem Delete(string agent, string taskId)
    {
        agent = RequireAgent(agent);
        lock (Gate)
        {
            var tasks = LoadCore(agent);
            var task = FindVisibleTask(tasks, taskId) ?? throw new InvalidOperationException("Scheduled task not found.");
            if (task.IsSystem) throw new InvalidOperationException("System tasks cannot be deleted.");
            task.Status = ScheduledTaskStatuses.Deleted;
            task.RunState = ScheduledTaskRunStates.Idle;
            task.NextRunAt = null;
            ClearActiveRun(task);
            ClearQueuedManualRun(task);
            task.UpdatedAt = UserTimeZoneService.Now();
            SaveCore(agent, tasks);
            return Clone(task);
        }
    }

    public ScheduledTaskPage List(string agent, int page = 1, int pageSize = 10, string? status = null)
    {
        agent = RequireAgent(agent);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        List<ScheduledTaskItem> snapshot;
        lock (Gate)
        {
            snapshot = LoadCore(agent).Select(Clone).ToList();
        }

        IEnumerable<ScheduledTaskItem> items = snapshot;
        if (string.IsNullOrWhiteSpace(status))
            items = items.Where(item => IsVisibleStatus(item.Status));
        else
            items = items.Where(item => !IsDeletedStatus(item.Status) && string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase));

        var ordered = items
            .OrderBy(item => item.StalledUntil != null && item.StalledUntil > DateTimeOffset.UtcNow)
            .ThenBy(item => item.NextRunAt ?? DateTimeOffset.MaxValue)
            .ThenByDescending(item => item.CreatedAt)
            .ToList();
        return new ScheduledTaskPage { Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), Page = page, PageSize = pageSize, Total = ordered.Count };
    }

    public ScheduledTaskItem Read(string agent, string taskId)
    {
        agent = RequireAgent(agent);
        lock (Gate)
        {
            var task = FindVisibleTask(LoadCore(agent), taskId) ?? throw new InvalidOperationException("Scheduled task not found.");
            return Clone(task);
        }
    }

    public IReadOnlyList<ScheduledTaskRun> GetRuns(string agent, string taskId, int take = 20)
    {
        agent = RequireAgent(agent);
        var task = Read(agent, taskId);
        var dir = _path.GetScheduledTaskRunsPath(agent, taskId);
        if (!Directory.Exists(dir)) return Array.Empty<ScheduledTaskRun>();
        return Directory.GetFiles(dir, "*.json")
            .Select(file =>
            {
                try { return JsonSerializer.Deserialize<ScheduledTaskRun>(File.ReadAllText(file), JsonOptions); }
                catch { return null; }
            })
            .Where(run => run != null)
            .Cast<ScheduledTaskRun>()
            .Select(run =>
            {
                NormalizeRunTimes(run, task.TimeZone);
                return run;
            })
            .OrderByDescending(run => run.StartedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToList();
    }

    public IReadOnlyList<ScheduledTaskItem> GetDueTasks(DateTimeOffset now)
    {
        var due = new List<ScheduledTaskItem>();
        if (!Directory.Exists(_path.AgentsRoot)) return due;
        lock (Gate)
        {
            foreach (var dir in Directory.GetDirectories(_path.AgentsRoot))
            {
                var agent = Path.GetFileName(dir);
                if (string.IsNullOrWhiteSpace(agent)) continue;
                var agentDueCount = 0;
                var tasks = LoadCore(agent);
                foreach (var item in tasks.Where(item =>
                    string.Equals(item.Status, ScheduledTaskStatuses.Enabled, StringComparison.OrdinalIgnoreCase) &&
                    item.RunState != ScheduledTaskRunStates.Running &&
                    item.NextRunAt != null &&
                    item.NextRunAt <= now &&
                    (item.StalledUntil == null || item.StalledUntil <= now)))
                {
                    foreach (var occurrence in BuildDueOccurrences(item, now, MaxCatchUpRunsPerTask))
                    {
                        due.Add(occurrence);
                        agentDueCount++;
                        if (agentDueCount >= MaxDueRunsPerAgentPerPoll)
                            break;
                    }
                    if (agentDueCount >= MaxDueRunsPerAgentPerPoll)
                        break;
                }
            }
        }
        return due.OrderBy(item => item.NextRunAt).ToList();
    }

    public IReadOnlyList<ScheduledTaskItem> GetQueuedManualRuns()
    {
        var queued = new List<ScheduledTaskItem>();
        if (!Directory.Exists(_path.AgentsRoot)) return queued;
        lock (Gate)
        {
            foreach (var dir in Directory.GetDirectories(_path.AgentsRoot))
            {
                var agent = Path.GetFileName(dir);
                if (string.IsNullOrWhiteSpace(agent)) continue;
                queued.AddRange(LoadCore(agent)
                    .Where(item =>
                        item.ManualRunQueued &&
                        !item.IsSystem &&
                        item.RunState != ScheduledTaskRunStates.Running &&
                        IsVisibleStatus(item.Status))
                    .Select(Clone));
            }
        }
        return queued
            .OrderBy(item => item.ManualRunQueuedAt ?? DateTimeOffset.MinValue)
            .ThenBy(item => item.Agent, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.TaskId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public ScheduledTaskItem QueueManualRun(string agent, string taskId, bool deliver = true, string? reason = null)
    {
        agent = RequireAgent(agent);
        lock (Gate)
        {
            var tasks = LoadCore(agent);
            var task = FindVisibleTask(tasks, taskId) ?? throw new InvalidOperationException("Scheduled task not found.");
            if (task.IsSystem)
                throw new InvalidOperationException("System scheduled tasks cannot be manually tested.");
            if (task.RunState == ScheduledTaskRunStates.Running)
                throw new InvalidOperationException("Scheduled task is already running.");

            var now = UserTimeZoneService.Now();
            task.ManualRunQueued = true;
            task.ManualRunQueuedAt ??= now;
            task.ManualRunDeliver = deliver;
            task.ManualRunReason = Trim(string.IsNullOrWhiteSpace(reason) ? "Manual test queued by the main agent." : reason.Trim(), 500);
            task.UpdatedAt = now;
            SaveCore(agent, tasks);
            _events.Record(agent, "scheduled_task", NewId("manual_test"), task.TaskId, "manual_test_queued", "Manual test queued at highest scheduled-task priority.", "wait_for_idle");
            return Clone(task);
        }
    }

    public void RecoverInterruptedRuns(DateTimeOffset now)
    {
        if (!Directory.Exists(_path.AgentsRoot)) return;
        lock (Gate)
        {
            foreach (var dir in Directory.GetDirectories(_path.AgentsRoot))
            {
                var agent = Path.GetFileName(dir);
                if (string.IsNullOrWhiteSpace(agent)) continue;
                var tasks = LoadCore(agent);
                if (RecoverInterruptedRunsCore(agent, tasks, now))
                    SaveCore(agent, tasks);
            }
        }
    }

    public ScheduledTaskItem? TryStartRun(string agent, string taskId, bool manual = false, DateTimeOffset? scheduledAt = null, string? trigger = null, string? catchUpReason = null)
    {
        agent = RequireAgent(agent);
        lock (Gate)
        {
            var tasks = LoadCore(agent);
            var task = FindVisibleTask(tasks, taskId);
            if (task == null || task.RunState == ScheduledTaskRunStates.Running) return null;
            if (manual && task.IsSystem)
                throw new InvalidOperationException("System scheduled tasks cannot be manually tested.");

            var nowUtc = DateTimeOffset.UtcNow;
            if (!manual)
            {
                var dueAt = scheduledAt ?? task.NextRunAt;
                if (task.Status != ScheduledTaskStatuses.Enabled || task.NextRunAt == null || dueAt == null || task.NextRunAt > nowUtc)
                    return null;
                if (task.StalledUntil != null && task.StalledUntil > nowUtc)
                    return null;
                if (HasRunForSchedule(agent, task.TaskId, dueAt.Value))
                {
                    task.RunCount++;
                    task.NextRunAt = ComputeNextRunAt(task, dueAt.Value);
                    task.UpdatedAt = UserTimeZoneService.Now();
                    if (task.Status == ScheduledTaskStatuses.Enabled && task.NextRunAt == null)
                        task.Status = ScheduledTaskStatuses.Completed;
                    SaveCore(agent, tasks);
                    return null;
                }
            }

            var userNow = UserTimeZoneService.Now();
            var runId = NewId("run");
            task.RunState = ScheduledTaskRunStates.Running;
            task.ActiveRunId = runId;
            task.ActiveRunStartedAt = userNow;
            task.ActiveRunLastHeartbeatAt = userNow;
            task.ActiveRunHeartbeatKind = "started";
            task.ActiveRunHeartbeatMessage = "Execution lease acquired and persisted.";
            task.StalledUntil = null;
            if (manual)
                ClearQueuedManualRun(task);
            task.UpdatedAt = userNow;

            var run = new ScheduledTaskRun
            {
                RunId = runId,
                TaskId = task.TaskId,
                Agent = task.Agent,
                Trigger = manual ? "manual" : (string.IsNullOrWhiteSpace(trigger) ? "schedule" : trigger),
                ScheduledAt = scheduledAt,
                CatchUpReason = catchUpReason,
                StartedAt = userNow,
                LastHeartbeatAt = userNow,
                HeartbeatKind = "started",
                HeartbeatMessage = "Execution lease acquired and persisted.",
                Status = ScheduledTaskRunStatuses.Running
            };
            run.Events.Add(new ScheduledTaskRunEvent { Type = "started", Message = run.HeartbeatMessage, Status = "running" });
            SaveCore(agent, tasks);
            SaveRun(run, task.TimeZone);
            return Clone(task);
        }
    }

    public bool HeartbeatRun(ScheduledTaskRun run, string kind, string? message)
    {
        if (string.IsNullOrWhiteSpace(run.Agent) || string.IsNullOrWhiteSpace(run.TaskId) || string.IsNullOrWhiteSpace(run.RunId))
            return false;

        lock (Gate)
        {
            if (!AgentExists(run.Agent))
                return false;

            var tasks = LoadCore(run.Agent);
            var task = FindVisibleTask(tasks, run.TaskId);
            var now = UserTimeZoneService.Now();
            var trimmedMessage = Trim(message ?? string.Empty, 700);
            run.LastHeartbeatAt = now;
            run.HeartbeatKind = kind;
            run.HeartbeatMessage = trimmedMessage;
            if (task != null && string.Equals(task.ActiveRunId, run.RunId, StringComparison.OrdinalIgnoreCase))
            {
                task.ActiveRunLastHeartbeatAt = now;
                task.ActiveRunHeartbeatKind = kind;
                task.ActiveRunHeartbeatMessage = trimmedMessage;
                task.UpdatedAt = now;
                SaveCore(run.Agent, tasks);
            }
            SaveRun(run, task?.TimeZone);
            return true;
        }
    }

    public bool TryGetRunStallDiagnostic(string agent, string taskId, string? runId, DateTimeOffset now, out string diagnostic)
    {
        diagnostic = string.Empty;
        if (string.IsNullOrWhiteSpace(runId)) return false;
        agent = RequireAgent(agent);
        lock (Gate)
        {
            var task = FindVisibleTask(LoadCore(agent), taskId);
            if (task == null || task.RunState != ScheduledTaskRunStates.Running || !string.Equals(task.ActiveRunId, runId, StringComparison.OrdinalIgnoreCase))
                return false;

            var last = task.ActiveRunLastHeartbeatAt ?? task.ActiveRunStartedAt ?? task.UpdatedAt;
            if (now - last <= RunHeartbeatTimeout)
                return false;

            diagnostic = BuildStallDiagnostic(task, now, last);
            return true;
        }
    }

    public void FinishRun(ScheduledTaskRun run)
    {
        lock (Gate)
        {
            if (!AgentExists(run.Agent))
                return;

            if (string.IsNullOrWhiteSpace(run.RunId))
                run.RunId = NewId("run");
            run.FinishedAt ??= UserTimeZoneService.Now();
            var tasks = LoadCore(run.Agent);
            var task = FindVisibleTask(tasks, run.TaskId) ?? tasks.FirstOrDefault(item => item.TaskId == run.TaskId);
            if (task != null)
            {
                var manualRun = string.Equals(run.Trigger, "manual", StringComparison.OrdinalIgnoreCase);
                task.RunState = ScheduledTaskRunStates.Idle;
                ClearActiveRun(task);
                task.UpdatedAt = UserTimeZoneService.Now();

                if (!manualRun)
                {
                    task.LastRunAt = run.FinishedAt;
                    task.LastRunStatus = run.Status;
                    task.LastRunSummary = Trim(run.Output ?? run.Error ?? run.Diagnostic ?? string.Empty, 500);

                    if (run.Status == ScheduledTaskRunStatuses.Stalled)
                    {
                        task.StalledUntil = DateTimeOffset.UtcNow.Add(StalledBackoff);
                        task.FailureCount++;
                    }
                    else if (run.Status == ScheduledTaskRunStatuses.Failed)
                    {
                        task.FailureCount++;
                        task.StalledUntil = null;
                    }
                    else if (run.Status == ScheduledTaskRunStatuses.Succeeded)
                    {
                        task.StalledUntil = null;
                    }

                    if (ConsumesScheduledOccurrence(run.Status))
                    {
                        task.RunCount++;
                        var nextAfter = run.ScheduledAt ?? DateTimeOffset.UtcNow;
                        task.NextRunAt = ComputeNextRunAt(task, nextAfter);
                        if (task.Status == ScheduledTaskStatuses.Enabled && task.NextRunAt == null)
                            task.Status = ScheduledTaskStatuses.Completed;
                    }
                }

                SaveCore(run.Agent, tasks);
            }
            SaveRun(run, task?.TimeZone);
        }
    }

    public ScheduledTaskItem RetryNow(string agent, string taskId)
    {
        agent = RequireAgent(agent);
        lock (Gate)
        {
            var tasks = LoadCore(agent);
            var task = FindVisibleTask(tasks, taskId) ?? throw new InvalidOperationException("Scheduled task not found.");
            if (task.RunState == ScheduledTaskRunStates.Running)
                throw new InvalidOperationException("Scheduled task is already running.");

            task.Status = ScheduledTaskStatuses.Enabled;
            task.RunState = ScheduledTaskRunStates.Idle;
            ClearActiveRun(task);
            task.StalledUntil = null;
            task.LastRunStatus = null;
            task.NextRunAt = DateTimeOffset.UtcNow.AddSeconds(-1);
            task.UpdatedAt = UserTimeZoneService.Now();
            SaveCore(agent, tasks);
            _events.Record(agent, "scheduled_task", NewId("retry"), task.TaskId, "retry_queued", "Manual retry queued.", "wait_for_completion");
            return Clone(task);
        }
    }

    public ScheduledTaskItem RepairAndRetry(string agent, string taskId, string? reason = null)
    {
        agent = RequireAgent(agent);
        lock (Gate)
        {
            var tasks = LoadCore(agent);
            var old = FindVisibleTask(tasks, taskId) ?? throw new InvalidOperationException("Scheduled task not found.");
            if (old.RunState == ScheduledTaskRunStates.Running)
                throw new InvalidOperationException("Scheduled task is already running.");

            var now = UserTimeZoneService.Now();
            var notes = new List<string>();
            var originalTaskId = old.TaskId;
            var repairReason = string.IsNullOrWhiteSpace(reason) ? "Manual repair and retry requested." : reason.Trim();
            var repaired = Clone(old);

            old.Status = ScheduledTaskStatuses.Replaced;
            old.RunState = ScheduledTaskRunStates.Idle;
            old.NextRunAt = null;
            old.RepairReason = repairReason;
            old.UpdatedAt = now;
            ClearActiveRun(old);
            ClearQueuedManualRun(old);

            repaired.TaskId = originalTaskId;
            repaired.Status = ScheduledTaskStatuses.Enabled;
            repaired.RunState = ScheduledTaskRunStates.Idle;
            repaired.CreatedAt = now;
            repaired.UpdatedAt = now;
            repaired.LastRunAt = null;
            repaired.LastRunStatus = null;
            repaired.LastRunSummary = null;
            repaired.FailureCount = 0;
            repaired.RepairedFromTaskId = originalTaskId;
            repaired.RepairReason = repairReason;
            repaired.StalledUntil = null;
            ClearActiveRun(repaired);
            ClearQueuedManualRun(repaired);
            RepairTaskShape(repaired, DateTimeOffset.UtcNow, notes);
            repaired.NextRunAt = DateTimeOffset.UtcNow.AddSeconds(-1);

            tasks.Add(repaired);
            SaveCore(agent, tasks);
            var message = "Repair clone queued. " + string.Join(" ", notes);
            _events.Record(agent, "scheduled_task", NewId("repair"), repaired.TaskId, "repair_retry_queued", message.Trim(), "wait_for_completion");
            return Clone(repaired);
        }
    }

    public IReadOnlyList<string> ResolveTargetSessions(ScheduledTaskItem task)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sessionsDir = _path.GetSessionsPath(task.Agent);
        if (!Directory.Exists(sessionsDir)) return result.ToList();
        foreach (var target in task.Targets)
        {
            var targetType = string.IsNullOrWhiteSpace(target.Type)
                ? ScheduledTaskTargetTypes.CreatedSession
                : target.Type.Trim().ToLowerInvariant();
            if (targetType == ScheduledTaskTargetTypes.AllAgentSessions)
            {
                foreach (var file in Directory.GetFiles(sessionsDir, "*.json").Where(file => !Path.GetFileName(file).EndsWith(".state.json", StringComparison.OrdinalIgnoreCase)))
                {
                    if (IsScheduledNotificationSessionFile(file))
                        continue;
                    result.Add(Path.GetFileNameWithoutExtension(file));
                }
                continue;
            }
            var sessionId = targetType == ScheduledTaskTargetTypes.CreatedSession ? task.CreatedFromSession : target.SessionId;
            if (string.IsNullOrWhiteSpace(sessionId))
                continue;

            var sessionFile = Path.Combine(sessionsDir, sessionId + ".json");
            if (!File.Exists(sessionFile))
                continue;

            if (targetType != ScheduledTaskTargetTypes.NotificationSession && IsScheduledNotificationSessionFile(sessionFile))
                continue;

            if (targetType == ScheduledTaskTargetTypes.NotificationSession && !IsScheduledNotificationSessionFile(sessionFile))
                continue;

            if (!string.IsNullOrWhiteSpace(sessionId))
                result.Add(sessionId);
        }
        return result.ToList();
    }

    public void EnsureSystemTask(string agent)
    {
        EnsureSystemTasks(agent);
    }

    public void EnsureSystemTasks(string agent)
    {
        agent = RequireAgent(agent);
        lock (Gate)
        {
            var tasks = LoadCore(agent);
            EnsureSystemTaskCore(tasks, agent, SystemMemoryOrganizationTaskId, "System Memory Organization", "Automatically organize this agent's memory files, including hot_memory, core_memory, user.md, identity.md, and long-term memory archives.");
            EnsureSystemTaskCore(tasks, agent, SystemSkillOrganizationTaskId, "System Skill Organization", "Automatically analyze recent conversations, extract or update reusable skills, and keep the skill library learning over time.");
            SaveCore(agent, tasks);
        }
    }

    public void EnsureAllSystemTasks()
    {
        if (!Directory.Exists(_path.AgentsRoot)) return;
        foreach (var dir in Directory.GetDirectories(_path.AgentsRoot))
        {
            var agent = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(agent)) continue;
            try { EnsureSystemTasks(agent); }
            catch { }
        }
    }

    private static void EnsureSystemTaskCore(List<ScheduledTaskItem> tasks, string agent, string taskId, string title, string content)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = tasks.FirstOrDefault(t => t.TaskId == taskId && IsVisibleStatus(t.Status));
        if (existing != null)
        {
            existing.IsSystem = true;
            existing.Title = title;
            existing.Content = content;
            existing.Agent = agent;
            existing.Status = ScheduledTaskStatuses.Enabled;
            existing.TimeZone = NormalizeTimeZone(existing.TimeZone);
            existing.Schedule ??= new ScheduledTaskSchedule();
            existing.Schedule.Type = ScheduledTaskScheduleTypes.Interval;
            if (string.IsNullOrWhiteSpace(existing.Schedule.StartAt))
                existing.Schedule.StartAt = FormatLocalDateTime(now, existing.TimeZone);
            if (existing.Schedule.IntervalMinutes == null || existing.Schedule.IntervalMinutes < 1)
                existing.Schedule.IntervalMinutes = 180;
            existing.UpdatedAt = UserTimeZoneService.Now();
            if (existing.NextRunAt == null && existing.RunState != ScheduledTaskRunStates.Running)
                existing.NextRunAt = ComputeNextRunAt(existing, now);
            return;
        }

        var timeZone = NormalizeTimeZone(null);
        var task = new ScheduledTaskItem
        {
            TaskId = taskId,
            Agent = agent,
            Title = title,
            Content = content,
            Status = ScheduledTaskStatuses.Enabled,
            TimeZone = timeZone,
            IsSystem = true,
            Schedule = new ScheduledTaskSchedule
            {
                Type = ScheduledTaskScheduleTypes.Interval,
                StartAt = FormatLocalDateTime(now, timeZone),
                IntervalMinutes = 180
            },
            Targets = new List<ScheduledTaskTarget>(),
            CreatedAt = UserTimeZoneService.Now(),
            UpdatedAt = UserTimeZoneService.Now()
        };
        task.NextRunAt = ComputeNextRunAt(task, now);
        tasks.Add(task);
    }

    private List<ScheduledTaskItem> BuildDueOccurrences(ScheduledTaskItem task, DateTimeOffset now, int max)
    {
        var result = new List<ScheduledTaskItem>();
        var cursor = task.NextRunAt;
        if (cursor == null) return result;
        if (ShouldCollapseCatchUpOccurrences(task) && cursor.Value < now - TimeSpan.FromSeconds(90))
        {
            var collapsed = Clone(task);
            collapsed.NextRunAt = LatestDueOccurrenceForCollapse(task, now) ?? cursor;
            result.Add(collapsed);
            return result;
        }
        var schedule = task.Schedule ?? new ScheduledTaskSchedule();
        var remainingByMaxRuns = schedule.MaxRuns == null ? max : Math.Max(0, schedule.MaxRuns.Value - task.RunCount);
        var produced = 0;
        var temp = Clone(task);
        while (cursor != null && cursor <= now && result.Count < max && produced < remainingByMaxRuns)
        {
            var due = Clone(task);
            due.NextRunAt = cursor;
            result.Add(due);
            produced++;
            temp.RunCount = task.RunCount + produced;
            cursor = ComputeNextRunAt(temp, cursor.Value);
            temp.NextRunAt = cursor;
        }
        return result;
    }

    private static bool ShouldCollapseCatchUpOccurrences(ScheduledTaskItem task)
    {
        if (!task.IsSystem) return false;
        return string.Equals(task.TaskId, SystemMemoryOrganizationTaskId, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(task.TaskId, SystemSkillOrganizationTaskId, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset? LatestDueOccurrenceForCollapse(ScheduledTaskItem task, DateTimeOffset now)
    {
        var schedule = task.Schedule ?? new ScheduledTaskSchedule();
        var type = (schedule.Type ?? ScheduledTaskScheduleTypes.Once).Trim().ToLowerInvariant();
        if (type == ScheduledTaskScheduleTypes.Interval || type == ScheduledTaskScheduleTypes.AfterCount)
        {
            var zone = FindZone(task.TimeZone);
            var start = ParseDateTime(schedule.StartAt, zone);
            var interval = TimeSpan.FromMinutes(Math.Max(1, schedule.IntervalMinutes ?? 1));
            var end = string.IsNullOrWhiteSpace(schedule.EndAt) ? (DateTimeOffset?)null : ParseDateTime(schedule.EndAt, zone);
            var limit = end.HasValue && end.Value < now ? end.Value : now;
            if (start > limit) return task.NextRunAt;

            var ticks = Math.Max(0, (limit - start).Ticks);
            var steps = ticks / interval.Ticks;
            var candidate = start.AddTicks(steps * interval.Ticks);
            if (candidate > limit)
                candidate = candidate.AddTicks(-interval.Ticks);
            if (task.NextRunAt.HasValue && candidate < task.NextRunAt.Value)
                candidate = task.NextRunAt.Value;
            return candidate <= now ? candidate : task.NextRunAt;
        }

        var cursor = task.NextRunAt;
        var latest = cursor;
        var temp = Clone(task);
        var produced = 0;
        var guard = 0;
        var remainingByMaxRuns = schedule.MaxRuns == null ? int.MaxValue : Math.Max(0, schedule.MaxRuns.Value - task.RunCount);
        while (cursor != null && cursor <= now && produced < remainingByMaxRuns && guard++ < 10000)
        {
            latest = cursor;
            produced++;
            temp.RunCount = task.RunCount + produced;
            cursor = ComputeNextRunAt(temp, cursor.Value);
            temp.NextRunAt = cursor;
        }
        return latest;
    }

    private bool RecoverInterruptedRunsCore(string agent, List<ScheduledTaskItem> tasks, DateTimeOffset now)
    {
        var changed = false;
        foreach (var task in tasks.Where(item => item.RunState == ScheduledTaskRunStates.Running && IsVisibleStatus(item.Status)))
        {
            var last = task.ActiveRunLastHeartbeatAt ?? task.ActiveRunStartedAt ?? task.UpdatedAt;
            var hasActiveRun = !string.IsNullOrWhiteSpace(task.ActiveRunId);
            var timedOut = now - last > RunHeartbeatTimeout;
            var legacyInterrupted = !hasActiveRun && task.UpdatedAt <= now - RunningRecoveryAge;
            if (!timedOut && !legacyInterrupted)
                continue;

            var run = hasActiveRun ? LoadRun(agent, task.TaskId, task.ActiveRunId!) : null;
            run ??= new ScheduledTaskRun
            {
                RunId = hasActiveRun ? task.ActiveRunId! : NewId("run_recovered"),
                TaskId = task.TaskId,
                Agent = task.Agent,
                Trigger = "schedule",
                ScheduledAt = task.NextRunAt,
                StartedAt = task.ActiveRunStartedAt ?? task.UpdatedAt
            };

            var status = timedOut ? ScheduledTaskRunStatuses.Stalled : ScheduledTaskRunStatuses.Interrupted;
            var diagnostic = timedOut
                ? BuildStallDiagnostic(task, now, last)
                : "Recovered a running task left by an earlier process before transactional heartbeat data was available.";
            run.Status = status;
            run.FinishedAt = UserTimeZoneService.Now();
            run.ErrorType = timedOut ? "heartbeat_timeout" : "process_interrupted";
            run.Error = diagnostic;
            run.Diagnostic = diagnostic;
            run.LastHeartbeatAt = task.ActiveRunLastHeartbeatAt;
            run.HeartbeatKind = task.ActiveRunHeartbeatKind;
            run.HeartbeatMessage = task.ActiveRunHeartbeatMessage;
            run.Events.Add(new ScheduledTaskRunEvent { Type = status, Message = diagnostic, Status = status });
            SaveRun(run, task.TimeZone);

            task.RunState = ScheduledTaskRunStates.Idle;
            ClearActiveRun(task);
            task.LastRunAt = run.FinishedAt;
            task.LastRunStatus = status;
            task.LastRunSummary = Trim(diagnostic, 500);
            task.UpdatedAt = UserTimeZoneService.Now();
            if (timedOut)
            {
                task.StalledUntil = DateTimeOffset.UtcNow.Add(StalledBackoff);
                task.FailureCount++;
            }
            _events.Record(agent, "scheduled_task", run.RunId, task.TaskId, status, diagnostic, "retry_manual");
            changed = true;
        }
        return changed;
    }

    private ScheduledTaskRun? LoadRun(string agent, string taskId, string runId)
    {
        var file = Path.Combine(_path.GetScheduledTaskRunsPath(agent, taskId), _path.NormalizePathSegment(runId, "Run id") + ".json");
        if (!File.Exists(file)) return null;
        try { return JsonSerializer.Deserialize<ScheduledTaskRun>(File.ReadAllText(file), JsonOptions); }
        catch { return null; }
    }

    private bool HasRunForSchedule(string agent, string taskId, DateTimeOffset scheduledAt)
    {
        var dir = _path.GetScheduledTaskRunsPath(agent, taskId);
        if (!Directory.Exists(dir)) return false;
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            ScheduledTaskRun? run = null;
            try { run = JsonSerializer.Deserialize<ScheduledTaskRun>(File.ReadAllText(file), JsonOptions); }
            catch { continue; }
            if (run?.ScheduledAt == null) continue;
            if (!ConsumesScheduledOccurrence(run.Status)) continue;
            if (string.Equals(run.Trigger, "manual", StringComparison.OrdinalIgnoreCase)) continue;
            if (Math.Abs((run.ScheduledAt.Value.ToUniversalTime() - scheduledAt.ToUniversalTime()).TotalSeconds) < 1) return true;
        }
        return false;
    }

    public static string DescribeSchedule(ScheduledTaskItem task)
    {
        var schedule = task.Schedule;
        if (schedule.Type == ScheduledTaskScheduleTypes.Once) return "once at " + schedule.RunAt;
        if (schedule.Type == ScheduledTaskScheduleTypes.Daily) return "daily at " + schedule.Time;
        if (schedule.Type == ScheduledTaskScheduleTypes.DailyCount) return "daily from " + (schedule.StartTime ?? schedule.Time);
        if (schedule.Type == ScheduledTaskScheduleTypes.DailyWindow) return "daily window " + schedule.WindowStart + "-" + schedule.WindowEnd;
        if (schedule.Type == ScheduledTaskScheduleTypes.DailyTimes) return "daily at " + string.Join(", ", schedule.Times ?? new List<string>());
        return schedule.Type;
    }

    private List<ScheduledTaskItem> Load(string agent) { lock (Gate) return LoadCore(agent).Select(Clone).ToList(); }

    private List<ScheduledTaskItem> LoadCore(string agent)
    {
        var file = _path.GetScheduledTasksJsonPath(agent);
        if (!File.Exists(file)) return new List<ScheduledTaskItem>();
        var tasks = JsonSerializer.Deserialize<List<ScheduledTaskItem>>(File.ReadAllText(file), JsonOptions) ?? new List<ScheduledTaskItem>();
        foreach (var task in tasks)
            NormalizeTaskTimes(task);
        return tasks;
    }

    private void SaveCore(string agent, List<ScheduledTaskItem> tasks)
    {
        if (!AgentExists(agent))
            return;

        foreach (var task in tasks)
            NormalizeTaskTimes(task);
        var file = _path.GetScheduledTasksJsonPath(agent);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        AtomicFile.WriteAllText(file, JsonSerializer.Serialize(tasks, JsonOptions));
    }

    private void SaveRun(ScheduledTaskRun run, string? taskTimeZone)
    {
        if (!AgentExists(run.Agent))
            return;

        NormalizeRunTimes(run, taskTimeZone);
        var dir = _path.GetScheduledTaskRunsPath(run.Agent, run.TaskId);
        Directory.CreateDirectory(dir);
        AtomicFile.WriteAllText(Path.Combine(dir, _path.NormalizePathSegment(run.RunId, "Run id") + ".json"), JsonSerializer.Serialize(run, JsonOptions));
    }

    public static DateTimeOffset? ComputeNextRunAt(ScheduledTaskItem task, DateTimeOffset afterUtc)
    {
        if (task.Status != ScheduledTaskStatuses.Enabled) return null;
        var schedule = task.Schedule ?? new ScheduledTaskSchedule();
        var zone = FindZone(task.TimeZone);
        var type = (schedule.Type ?? ScheduledTaskScheduleTypes.Once).Trim().ToLowerInvariant();
        if (type == ScheduledTaskScheduleTypes.Once)
        {
            if (task.RunCount > 0) return null;
            var once = ParseDateTime(schedule.RunAt, zone);
            return once > afterUtc ? once : null;
        }
        if (type == ScheduledTaskScheduleTypes.Interval || type == ScheduledTaskScheduleTypes.AfterCount)
        {
            if (schedule.MaxRuns != null && task.RunCount >= schedule.MaxRuns.Value) return null;
            var start = ParseDateTime(schedule.StartAt, zone);
            var interval = TimeSpan.FromMinutes(Math.Max(1, schedule.IntervalMinutes ?? 1));
            var next = start > afterUtc ? start : start.AddTicks(((afterUtc - start).Ticks / interval.Ticks + 1) * interval.Ticks);
            if (!string.IsNullOrWhiteSpace(schedule.EndAt) && next > ParseDateTime(schedule.EndAt, zone)) return null;
            return next;
        }
        var times = DailyTimes(schedule, type);
        var localAfter = TimeZoneInfo.ConvertTime(afterUtc, zone).DateTime;
        var firstDate = ParseDate(schedule.StartDate) ?? localAfter.Date;
        var lastDate = ParseDate(schedule.EndDate);
        var date = localAfter.Date > firstDate ? localAfter.Date : firstDate;
        for (var day = 0; day < 370; day++)
        {
            if (lastDate != null && date > lastDate.Value) return null;
            foreach (var time in times.OrderBy(item => item))
            {
                var candidate = ToUtc(date.Add(time), zone);
                if (candidate > afterUtc) return candidate;
            }
            date = date.AddDays(1);
        }
        return null;
    }

    private static List<TimeSpan> DailyTimes(ScheduledTaskSchedule schedule, string type)
    {
        if (type == ScheduledTaskScheduleTypes.DailyTimes && schedule.Times != null && schedule.Times.Count > 0)
            return schedule.Times.Select(item => ParseTime(item)).ToList();
        if (type == ScheduledTaskScheduleTypes.DailyWindow)
        {
            var list = new List<TimeSpan>();
            var start = ParseTime(schedule.WindowStart);
            var end = ParseTime(schedule.WindowEnd);
            var step = TimeSpan.FromMinutes(Math.Max(1, schedule.IntervalMinutes ?? 1));
            for (var item = start; item <= end; item = item.Add(step))
                list.Add(item);
            return list;
        }
        if (type == ScheduledTaskScheduleTypes.DailyCount)
        {
            var list = new List<TimeSpan>();
            var start = ParseTime(schedule.StartTime ?? schedule.Time);
            var step = TimeSpan.FromMinutes(Math.Max(1, schedule.IntervalMinutes ?? 1));
            var count = Math.Max(1, schedule.CountPerDay ?? 1);
            for (var index = 0; index < count; index++)
            {
                var item = start.Add(TimeSpan.FromTicks(step.Ticks * index));
                if (item < TimeSpan.FromDays(1))
                    list.Add(item);
            }
            return list;
        }
        return new List<TimeSpan> { ParseTime(schedule.Time) };
    }

    private List<ScheduledTaskTarget> NormalizeTargets(string agent, List<ScheduledTaskTarget>? targets, string? createdSession, string notificationTitle, string taskId)
    {
        if (targets == null || targets.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(createdSession))
                return new List<ScheduledTaskTarget>();
            if (IsScheduledNotificationSessionFile(_path.GetSessionJsonPath(agent, createdSession)))
                throw new InvalidOperationException($"Source session '{createdSession}' is a read-only scheduled-task notification session. Use notification_session or choose a normal chat session.");
            return new List<ScheduledTaskTarget> { new() { Type = ScheduledTaskTargetTypes.CreatedSession } };
        }

        var normalized = new List<ScheduledTaskTarget>();
        foreach (var target in targets)
        {
            var type = string.IsNullOrWhiteSpace(target.Type)
                ? ScheduledTaskTargetTypes.CreatedSession
                : target.Type.Trim().ToLowerInvariant();
            if (type != ScheduledTaskTargetTypes.CreatedSession &&
                type != ScheduledTaskTargetTypes.Session &&
                type != ScheduledTaskTargetTypes.AllAgentSessions &&
                type != ScheduledTaskTargetTypes.NotificationSession)
                throw new InvalidOperationException($"Invalid target type '{target.Type}'.");

            if (type == ScheduledTaskTargetTypes.Session && string.IsNullOrWhiteSpace(target.SessionId))
                throw new InvalidOperationException("targets[].sessionId is required when target type is session.");
            if (type == ScheduledTaskTargetTypes.CreatedSession && string.IsNullOrWhiteSpace(createdSession))
                throw new InvalidOperationException("created_session target requires a source session. Use an explicit session target or no targets.");

            var sessionId = string.IsNullOrWhiteSpace(target.SessionId) ? null : _path.NormalizeSessionId(target.SessionId.Trim());
            if (type == ScheduledTaskTargetTypes.Session && !File.Exists(_path.GetSessionJsonPath(agent, sessionId!)))
                throw new InvalidOperationException($"Target session '{target.SessionId}' was not found. Use session_list and ask the user to confirm an existing sessionId.");
            if (type == ScheduledTaskTargetTypes.CreatedSession && !string.IsNullOrWhiteSpace(createdSession) && !File.Exists(_path.GetSessionJsonPath(agent, createdSession)))
                throw new InvalidOperationException($"Source session '{createdSession}' was not found. Use an explicit existing session target or no targets.");
            if (type == ScheduledTaskTargetTypes.Session && IsScheduledNotificationSessionFile(_path.GetSessionJsonPath(agent, sessionId!)))
                throw new InvalidOperationException($"Target session '{target.SessionId}' is a read-only scheduled-task notification session. Use notification_session to create or maintain a task's dedicated notification session.");
            if (type == ScheduledTaskTargetTypes.CreatedSession && !string.IsNullOrWhiteSpace(createdSession) && IsScheduledNotificationSessionFile(_path.GetSessionJsonPath(agent, createdSession)))
                throw new InvalidOperationException($"Source session '{createdSession}' is a read-only scheduled-task notification session. Use notification_session or choose a normal chat session.");
            if (type == ScheduledTaskTargetTypes.NotificationSession)
            {
                sessionId = string.IsNullOrWhiteSpace(sessionId)
                    ? CreateNotificationSession(agent, notificationTitle, taskId)
                    : EnsureNotificationSession(agent, sessionId, notificationTitle, taskId);
            }

            normalized.Add(new ScheduledTaskTarget
            {
                Type = type,
                SessionId = sessionId
            });
        }
        return normalized;
    }

    private string CreateNotificationSession(string agent, string title, string taskId)
    {
        var now = UserTimeZoneService.Now();
        string sessionId;
        string sessionFile;
        do
        {
            sessionId = "notice_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "_" + Guid.NewGuid().ToString("N")[..8];
            sessionFile = _path.GetSessionJsonPath(agent, sessionId);
        }
        while (File.Exists(sessionFile));

        var data = new SessionData
        {
            SessionId = sessionId,
            DisplayTitle = Trim(title, 120),
            Kind = SessionKinds.ScheduledNotification,
            IsReadOnly = true,
            CreatedByScheduledTaskId = taskId,
            CreateAt = now,
            LastActivity = now
        };
        data.Save(sessionFile);
        new SessionState().Save(sessionFile);
        return sessionId;
    }

    private string EnsureNotificationSession(string agent, string sessionId, string title, string taskId)
    {
        var sessionFile = _path.GetSessionJsonPath(agent, sessionId);
        if (!File.Exists(sessionFile))
            throw new InvalidOperationException($"Notification session '{sessionId}' was not found.");

        var data = SessionData.Load(sessionFile);
        if (!data.IsScheduledNotification)
            throw new InvalidOperationException($"Session '{sessionId}' is not a scheduled-task notification session.");

        data.DisplayTitle = Trim(title, 120);
        data.Kind = SessionKinds.ScheduledNotification;
        data.IsReadOnly = true;
        data.CreatedByScheduledTaskId = string.IsNullOrWhiteSpace(data.CreatedByScheduledTaskId) ? taskId : data.CreatedByScheduledTaskId;
        data.Save(sessionFile);
        return sessionId;
    }

    private void RefreshNotificationSessionTitles(string agent, ScheduledTaskItem task)
    {
        foreach (var target in task.Targets.Where(target => string.Equals(target.Type, ScheduledTaskTargetTypes.NotificationSession, StringComparison.OrdinalIgnoreCase)))
        {
            if (!string.IsNullOrWhiteSpace(target.SessionId))
                EnsureNotificationSession(agent, target.SessionId, task.Title, task.TaskId);
        }
    }

    private static bool IsScheduledNotificationSessionFile(string file)
    {
        try
        {
            return SessionData.Load(file).IsScheduledNotification;
        }
        catch
        {
            return false;
        }
    }

    private bool AgentExists(string agent) => Directory.Exists(_path.GetAgentPath(agent));

    private string RequireAgent(string agent)
    {
        agent = Require(agent, "agent");
        if (!AgentExists(agent)) throw new InvalidOperationException("Agent does not exist.");
        return agent;
    }

    private static string Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(name + " is required.");
        return value.Trim();
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max] + "...";

    private static void ValidateSchedule(ScheduledTaskSchedule? schedule)
    {
        if (schedule == null) throw new InvalidOperationException("schedule is required.");
        var type = (schedule.Type ?? string.Empty).Trim().ToLowerInvariant();
        var validTypes = new[] { ScheduledTaskScheduleTypes.Once, ScheduledTaskScheduleTypes.Interval, ScheduledTaskScheduleTypes.AfterCount, ScheduledTaskScheduleTypes.Daily, ScheduledTaskScheduleTypes.DailyCount, ScheduledTaskScheduleTypes.DailyWindow, ScheduledTaskScheduleTypes.DailyTimes };
        if (string.IsNullOrWhiteSpace(type) || !validTypes.Contains(type))
            throw new InvalidOperationException($"Invalid schedule type '{schedule.Type}'. Must be one of: {string.Join(", ", validTypes)}. Example: {{\"type\":\"daily\",\"time\":\"09:30\"}}");
        schedule.Type = type;

        switch (type)
        {
            case ScheduledTaskScheduleTypes.Once:
                Require(schedule.RunAt, "schedule.runAt");
                break;

            case ScheduledTaskScheduleTypes.Daily:
                Require(schedule.Time, "schedule.time");
                _ = ParseTime(schedule.Time);
                break;

            case ScheduledTaskScheduleTypes.DailyTimes:
                if (schedule.Times == null || schedule.Times.Count == 0)
                    throw new InvalidOperationException("schedule.times is required for daily_times.");
                foreach (var time in schedule.Times)
                    _ = ParseTime(time);
                break;

            case ScheduledTaskScheduleTypes.DailyWindow:
            {
                Require(schedule.WindowStart, "schedule.windowStart");
                Require(schedule.WindowEnd, "schedule.windowEnd");
                var start = ParseTime(schedule.WindowStart);
                var end = ParseTime(schedule.WindowEnd);
                if (end < start)
                    throw new InvalidOperationException("daily_window does not support overnight windows. Choose a same-day window or split it into separate tasks.");
                if (schedule.IntervalMinutes == null || schedule.IntervalMinutes < 1)
                    throw new InvalidOperationException("schedule.intervalMinutes must be at least 1 for daily_window.");
                break;
            }

            case ScheduledTaskScheduleTypes.DailyCount:
                Require(schedule.StartTime ?? schedule.Time, "schedule.startTime");
                if (schedule.CountPerDay == null || schedule.CountPerDay < 1)
                    throw new InvalidOperationException("schedule.countPerDay must be at least 1 for daily_count.");
                if (schedule.CountPerDay > 1 && (schedule.IntervalMinutes == null || schedule.IntervalMinutes < 1))
                    throw new InvalidOperationException("schedule.intervalMinutes must be at least 1 when daily_count runs more than once per day.");
                _ = ParseTime(schedule.StartTime ?? schedule.Time);
                break;

            case ScheduledTaskScheduleTypes.Interval:
                Require(schedule.StartAt, "schedule.startAt");
                if (schedule.IntervalMinutes == null || schedule.IntervalMinutes < 1)
                    throw new InvalidOperationException("schedule.intervalMinutes must be at least 1 for interval.");
                break;

            case ScheduledTaskScheduleTypes.AfterCount:
                Require(schedule.StartAt, "schedule.startAt");
                if (schedule.IntervalMinutes == null || schedule.IntervalMinutes < 1)
                    throw new InvalidOperationException("schedule.intervalMinutes must be at least 1 for after_count.");
                if (schedule.MaxRuns == null || schedule.MaxRuns < 1)
                    throw new InvalidOperationException("schedule.maxRuns must be at least 1 for after_count.");
                break;
        }
    }

    private static DateTimeOffset? ComputeNextRunAtOrThrow(ScheduledTaskItem task, DateTimeOffset afterUtc)
    {
        var next = ComputeNextRunAt(task, afterUtc);
        if (task.Status == ScheduledTaskStatuses.Enabled && next == null)
            throw new InvalidOperationException("Scheduled task must have a future run time. Choose a future time or a future time window.");
        return next;
    }

    private static void RepairTaskShape(ScheduledTaskItem task, DateTimeOffset nowUtc, List<string> notes)
    {
        task.Agent = Require(task.Agent, "agent");
        if (string.IsNullOrWhiteSpace(task.Title))
        {
            task.Title = "Repaired scheduled task";
            notes.Add("Missing title was replaced.");
        }
        if (string.IsNullOrWhiteSpace(task.Content))
        {
            task.Content = "Continue the repaired scheduled task.";
            notes.Add("Missing content was replaced.");
        }
        task.TimeZone = NormalizeTimeZone(task.TimeZone);
        task.Targets ??= new List<ScheduledTaskTarget>();
        task.Schedule ??= new ScheduledTaskSchedule();
        try
        {
            ValidateSchedule(task.Schedule);
            _ = ComputeNextRunAt(Clone(task), nowUtc);
        }
        catch
        {
            task.Schedule = new ScheduledTaskSchedule
            {
                Type = ScheduledTaskScheduleTypes.Interval,
                StartAt = FormatLocalDateTime(nowUtc, task.TimeZone),
                IntervalMinutes = 180
            };
            task.RunCount = 0;
            notes.Add("Invalid schedule was replaced with a 180-minute interval.");
        }

        var type = (task.Schedule.Type ?? string.Empty).Trim().ToLowerInvariant();
        if ((type == ScheduledTaskScheduleTypes.Interval || type == ScheduledTaskScheduleTypes.AfterCount) &&
            (task.Schedule.IntervalMinutes == null || task.Schedule.IntervalMinutes < 1))
        {
            task.Schedule.IntervalMinutes = 180;
            notes.Add("Invalid interval was repaired.");
        }
    }

    private static string FormatLocalDateTime(DateTimeOffset utc, string timeZone)
    {
        var zone = FindZone(timeZone);
        return TimeZoneInfo.ConvertTime(utc, zone).DateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static void NormalizeTaskTimes(ScheduledTaskItem task)
    {
        task.TimeZone = NormalizeTimeZone(task.TimeZone);
        task.CreatedAt = NormalizeUserTime(task.CreatedAt);
        task.UpdatedAt = NormalizeUserTime(task.UpdatedAt);
        task.LastRunAt = NormalizeUserTime(task.LastRunAt);
        task.ActiveRunStartedAt = NormalizeUserTime(task.ActiveRunStartedAt);
        task.ActiveRunLastHeartbeatAt = NormalizeUserTime(task.ActiveRunLastHeartbeatAt);
        task.StalledUntil = NormalizeUserTime(task.StalledUntil);
        if (task.NextRunAt.HasValue)
            task.NextRunAt = TimeZoneInfo.ConvertTime(task.NextRunAt.Value, FindZone(task.TimeZone));
    }

    private static void NormalizeRunTimes(ScheduledTaskRun run, string? taskTimeZone)
    {
        if (run.ScheduledAt.HasValue)
            run.ScheduledAt = TimeZoneInfo.ConvertTime(run.ScheduledAt.Value, FindZone(taskTimeZone));
        run.StartedAt = NormalizeUserTime(run.StartedAt);
        run.FinishedAt = NormalizeUserTime(run.FinishedAt);
        run.LastHeartbeatAt = NormalizeUserTime(run.LastHeartbeatAt);
        foreach (var item in run.Events)
            item.Timestamp = NormalizeUserTime(item.Timestamp);
    }

    private static DateTimeOffset NormalizeUserTime(DateTimeOffset value)
    {
        return value == default || value == DateTimeOffset.MinValue ? value : UserTimeZoneService.ToUserTime(value);
    }

    private static DateTimeOffset? NormalizeUserTime(DateTimeOffset? value)
    {
        return value.HasValue ? NormalizeUserTime(value.Value) : null;
    }

    private static bool ConsumesScheduledOccurrence(string? status)
    {
        return string.Equals(status, ScheduledTaskRunStatuses.Succeeded, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, ScheduledTaskRunStatuses.Failed, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDeletedStatus(string? status)
    {
        return string.Equals(status, ScheduledTaskStatuses.Deleted, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVisibleStatus(string? status)
    {
        return !string.Equals(status, ScheduledTaskStatuses.Deleted, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(status, ScheduledTaskStatuses.Replaced, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(status, ScheduledTaskStatuses.Quarantined, StringComparison.OrdinalIgnoreCase);
    }

    private static ScheduledTaskItem? FindVisibleTask(List<ScheduledTaskItem> tasks, string taskId)
    {
        return tasks.FirstOrDefault(item => item.TaskId == taskId && IsVisibleStatus(item.Status));
    }

    private static void ClearActiveRun(ScheduledTaskItem task)
    {
        task.ActiveRunId = null;
        task.ActiveRunStartedAt = null;
        task.ActiveRunLastHeartbeatAt = null;
        task.ActiveRunHeartbeatKind = null;
        task.ActiveRunHeartbeatMessage = null;
    }

    private static void ClearQueuedManualRun(ScheduledTaskItem task)
    {
        task.ManualRunQueued = false;
        task.ManualRunQueuedAt = null;
        task.ManualRunDeliver = true;
        task.ManualRunReason = null;
    }

    private static string BuildStallDiagnostic(ScheduledTaskItem task, DateTimeOffset now, DateTimeOffset lastHeartbeat)
    {
        var kind = string.IsNullOrWhiteSpace(task.ActiveRunHeartbeatKind) ? "unknown" : task.ActiveRunHeartbeatKind;
        var message = string.IsNullOrWhiteSpace(task.ActiveRunHeartbeatMessage) ? "no heartbeat message" : task.ActiveRunHeartbeatMessage;
        var elapsed = now - lastHeartbeat;
        return $"No scheduled-task activity heartbeat for {elapsed.TotalMinutes:F1} minutes after execution started. Last heartbeat: {kind}; {message}. If the last heartbeat is llm_request or llm_retry_wait, this points to model/API/network access; otherwise it points to a stalled host stage or subagent.";
    }

    private static string NewId(string prefix) => prefix + "_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "_" + Guid.NewGuid().ToString("N")[..8];
    private static ScheduledTaskItem Clone(ScheduledTaskItem task) => JsonSerializer.Deserialize<ScheduledTaskItem>(JsonSerializer.Serialize(task, JsonOptions), JsonOptions)!;
    private static string NormalizeTimeZone(string? id) => UserTimeZoneService.NormalizeTimeZoneId(id);
    public static TimeZoneInfo FindZone(string? id) => UserTimeZoneService.FindZone(id);
    private static DateTimeOffset ParseDateTime(string? value, TimeZoneInfo zone)
    {
        var text = Require(value, "datetime");
        if (Regex.IsMatch(text, @"(?:[zZ]|[+-]\d{2}:?\d{2})$") && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            return dto.ToUniversalTime();
        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var local))
            throw new InvalidOperationException("Invalid date time.");
        return ToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone);
    }
    private static TimeSpan ParseTime(string? value)
    {
        var text = Require(value, "time");
        if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var time)) return time;
        throw new InvalidOperationException("Invalid time.");
    }
    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)) return date.Date;
        throw new InvalidOperationException("Invalid date.");
    }
    private static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo zone) => new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
}
