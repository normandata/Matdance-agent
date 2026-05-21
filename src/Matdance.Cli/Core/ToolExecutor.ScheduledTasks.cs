using System.Text.Json;
using System.Text.RegularExpressions;
using Matdance.Cli.Models;
using Matdance.Cli.Services;

namespace Matdance.Cli.Core;

public partial class ToolExecutor
{
    private string ExecuteSessionList(Dictionary<string, JsonElement> args)
    {
        var page = Math.Max(1, IntArg(args, "page", 1));
        var pageSize = Math.Clamp(IntArg(args, "page_size", 10), 1, 30);
        var query = OptionalString(args, "query");
        var sessionsDir = _path.GetSessionsPath(_agentName);
        var sessions = new List<SessionListItem>();

        if (Directory.Exists(sessionsDir))
        {
            foreach (var file in Directory.GetFiles(sessionsDir, "*.json").Where(file => !Path.GetFileName(file).EndsWith(".state.json", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var sessionId = Path.GetFileNameWithoutExtension(file);
                    var data = SessionData.Load(file);
                    if (string.IsNullOrWhiteSpace(data.SessionId))
                        data.SessionId = sessionId;
                    var state = SessionState.Load(file);
                    var lastUserPreview = LatestSessionMessagePreview(state.Messages, "user");
                    var lastAssistantPreview = LatestSessionMessagePreview(state.Messages, "assistant");
                    var effectiveActivity = EffectiveSessionActivity(data, state.Messages);

                    if (!string.IsNullOrWhiteSpace(query) &&
                        !ContainsQuery(data.SessionId, query) &&
                        !ContainsQuery(data.DisplayTitle, query) &&
                        !ContainsQuery(lastUserPreview, query) &&
                        !ContainsQuery(lastAssistantPreview, query))
                        continue;

                    sessions.Add(new SessionListItem
                    {
                        SessionId = data.SessionId,
                        DisplayTitle = data.DisplayTitle,
                        Kind = data.Kind,
                        IsReadOnly = data.IsReadOnly,
                        CreatedByScheduledTaskId = data.CreatedByScheduledTaskId,
                        IsCurrent = !string.IsNullOrWhiteSpace(_sessionId) && string.Equals(data.SessionId, _sessionId, StringComparison.OrdinalIgnoreCase),
                        CreatedAt = data.CreateAt,
                        LastActivity = effectiveActivity,
                        TotalMessages = state.Messages.Count > 0 ? state.Messages.Count : data.TotalMessages,
                        ToolMessages = data.ToolMessagesCount,
                        Tokens = data.Tokens,
                        LastUserPreview = lastUserPreview,
                        LastAssistantPreview = lastAssistantPreview
                    });
                }
                catch
                {
                }
            }
        }

        var ordered = sessions
            .OrderByDescending(item => item.LastActivity)
            .ToList();
        var result = new
        {
            tool = "session_list",
            agent = _agentName,
            currentSessionId = _sessionId,
            defaultDeliveryTarget = new
            {
                type = "created_session",
                meaning = "deliver to the session where the scheduled task was created"
            },
            page,
            pageSize,
            total = ordered.Count,
            items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            reminder = "For a specific old normal chat session target, ask the user to confirm one exact sessionId, then create the task with targets:[{type:'session',sessionId:'...'}]. Do not reuse read-only scheduled notification sessions as normal targets; use targets:[{type:'notification_session'}] when the user wants a new/dedicated notification session titled with the task title."
        };
        return JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
    }

    private string ExecuteScheduledTaskCreate(Dictionary<string, JsonElement> args)
    {
        var service = new ScheduledTaskService(_path);
        var task = service.Create(new ScheduledTaskCreateRequest { Agent = _agentName, Title = RequiredString(args, "title"), Content = RequiredString(args, "content"), TimeZone = OptionalString(args, "timezone"), Schedule = RequiredSchedule(args), Targets = OptionalTargets(args), CreatedFromSession = _sessionId });
        return ScheduledTaskMutationResult("scheduled_task_create", "created", task);
    }

    private string ExecuteScheduledTaskEdit(Dictionary<string, JsonElement> args)
    {
        var service = new ScheduledTaskService(_path);
        var task = service.Edit(new ScheduledTaskEditRequest { Agent = _agentName, TaskId = RequiredString(args, "task_id"), Title = OptionalString(args, "title"), Content = OptionalString(args, "content"), TimeZone = OptionalString(args, "timezone"), Status = OptionalString(args, "status"), Schedule = OptionalSchedule(args), Targets = OptionalTargets(args) });
        return ScheduledTaskMutationResult("scheduled_task_edit", "updated", task);
    }

    private string ExecuteScheduledTaskList(Dictionary<string, JsonElement> args)
    {
        var service = new ScheduledTaskService(_path);
        return JsonSerializer.Serialize(service.List(_agentName, IntArg(args, "page", 1), IntArg(args, "page_size", 10), OptionalString(args, "status")), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
    }

    private string ExecuteScheduledTaskRead(Dictionary<string, JsonElement> args)
    {
        var service = new ScheduledTaskService(_path);
        var taskId = RequiredString(args, "task_id");
        var payload = new { task = service.Read(_agentName, taskId), runs = service.GetRuns(_agentName, taskId, 20) };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
    }

    private async Task<string> ExecuteScheduledTaskDoAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var service = new ScheduledTaskService(_path);
        var taskId = RequiredString(args, "task_id");
        var deliver = BoolArg(args, "deliver", true);
        IDisposable? resourceLease = null;
        IDisposable? budgetLease = null;
        BackgroundBudgetCancellation? budgetCts = null;
        CancellationTokenSource? linked = null;
        var taskInfo = service.Read(_agentName, taskId);
        if (taskInfo.IsSystem)
            return $"[error] System scheduled task {taskId} cannot be manually tested.";
        if (_backgroundWork != null)
        {
            var resource = BackgroundWorkCoordinator.GetScheduledTaskResourceKey(taskInfo);
            if (resource != null)
            {
                resourceLease = await _backgroundWork.TryAcquireResourceAsync(_agentName, resource, BackgroundWorkCoordinator.ResourceRetryTimeout, ct);
                if (resourceLease == null)
                    return $"[busy] Scheduled task {taskId} is waiting for the {resource} resource lock. Try again later.";
            }

            budgetLease = await _backgroundWork.TryAcquireBudgetAsync(_agentName, TimeSpan.Zero, ct);
            if (budgetLease == null)
            {
                resourceLease?.Dispose();
                return $"[busy] Scheduled task {taskId} needs one extra background budget slot. Increase max_concurrency or retry after the current work finishes.";
            }

            budgetCts = _backgroundWork.CreateBudgetCancellation(_agentName, ct);
            linked = _backgroundWork.CreateAgentLinkedCancellation(_agentName, budgetCts.Token);
        }

        var task = service.TryStartRun(_agentName, taskId, manual: true);
        if (task == null)
        {
            linked?.Dispose();
            budgetCts?.Dispose();
            budgetLease?.Dispose();
            resourceLease?.Dispose();
            return $"[error] Scheduled task {taskId} is not available or already running.";
        }

        var bookmarks = new BookmarkService(_path);
        var memoryOrg = new MemoryOrganizationService(_path, service, bookmarks);
        var skillMaintenance = new SkillMaintenanceService(_path);
        var runner = new ScheduledTaskRunner(_path, service, memoryOrg, skillMaintenance, backgroundWork: _backgroundWork);
        try
        {
            var run = await runner.ExecuteAsync(task, "manual", null, deliver, linked?.Token ?? ct);
            service.FinishRun(run);
            var delivery = deliver ? string.Join(", ", run.DeliveryResults.Select(item => item.SessionId + ":" + item.Status)) : "suppressed for test";
            return $"[scheduled_task_do] Run {run.RunId} {run.Status}. Delivery: {delivery}.\n{run.Output ?? run.Error}";
        }
        finally
        {
            linked?.Dispose();
            budgetCts?.Dispose();
            budgetLease?.Dispose();
            resourceLease?.Dispose();
        }
    }

    private string ExecuteScheduledTaskDelete(Dictionary<string, JsonElement> args)
    {
        var service = new ScheduledTaskService(_path);
        var task = service.Delete(_agentName, RequiredString(args, "task_id"));
        return $"[scheduled_task_delete] Deleted {task.TaskId}. History is retained.";
    }

    private static bool TryArg(Dictionary<string, JsonElement> args, string name, out JsonElement element)
    {
        if (args.TryGetValue(name, out element)) return true;
        var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1) return false;
        var camel = parts[0] + string.Concat(parts.Skip(1).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        return args.TryGetValue(camel, out element);
    }

    private static string FormatTaskTime(DateTimeOffset? value, string? timeZone)
    {
        if (!value.HasValue)
            return "none";

        var zone = ScheduledTaskService.FindZone(timeZone);
        return $"{TimeZoneInfo.ConvertTime(value.Value, zone):yyyy-MM-dd HH:mm:ss zzz} ({zone.Id})";
    }

    private string RequiredString(Dictionary<string, JsonElement> args, string name)
    {
        if (!TryArg(args, name, out var element) || element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString())) throw new InvalidOperationException(name + " is required.");
        return element.GetString()!.Trim();
    }

    private string? OptionalString(Dictionary<string, JsonElement> args, string name)
    {
        return TryArg(args, name, out var element) && element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    private List<string>? OptionalStringArray(Dictionary<string, JsonElement> args, string name)
    {
        if (!TryArg(args, name, out var element) || element.ValueKind != JsonValueKind.Array)
            return null;
        return element.EnumerateArray().Select(e => e.GetString()?.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()!;
    }

    private static int IntArg(Dictionary<string, JsonElement> args, string name, int fallback)
    {
        return TryArg(args, name, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value) ? value : fallback;
    }

    private static bool BoolArg(Dictionary<string, JsonElement> args, string name, bool fallback)
    {
        if (!TryArg(args, name, out var element)) return fallback;
        if (element.ValueKind == JsonValueKind.True) return true;
        if (element.ValueKind == JsonValueKind.False) return false;
        return element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var value) ? value : fallback;
    }

    private static ScheduledTaskSchedule RequiredSchedule(Dictionary<string, JsonElement> args)
    {
        return OptionalSchedule(args) ?? throw new InvalidOperationException("schedule is required.");
    }

    private static ScheduledTaskSchedule? OptionalSchedule(Dictionary<string, JsonElement> args)
    {
        if (!TryArg(args, "schedule", out var element) || element.ValueKind != JsonValueKind.Object) return null;
        var schedule = (ScheduledTaskSchedule?)JsonSerializer.Deserialize(element.GetRawText(), typeof(ScheduledTaskSchedule), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (schedule == null) return null;

        schedule.Type = OptionalObjectString(element, "type", "scheduleType", "schedule_type") ?? schedule.Type;
        schedule.RunAt ??= OptionalObjectString(element, "runAt", "run_at");
        schedule.StartAt ??= OptionalObjectString(element, "startAt", "start_at");
        schedule.EndAt ??= OptionalObjectString(element, "endAt", "end_at");
        schedule.Time ??= OptionalObjectString(element, "time");
        schedule.StartTime ??= OptionalObjectString(element, "startTime", "start_time");
        schedule.WindowStart ??= OptionalObjectString(element, "windowStart", "window_start");
        schedule.WindowEnd ??= OptionalObjectString(element, "windowEnd", "window_end");
        schedule.StartDate ??= OptionalObjectString(element, "startDate", "start_date");
        schedule.EndDate ??= OptionalObjectString(element, "endDate", "end_date");
        schedule.IntervalMinutes ??= OptionalObjectInt(element, "intervalMinutes", "interval_minutes");
        schedule.MaxRuns ??= OptionalObjectInt(element, "maxRuns", "max_runs");
        schedule.CountPerDay ??= OptionalObjectInt(element, "countPerDay", "count_per_day");
        schedule.Times ??= OptionalObjectStringArray(element, "times");
        return schedule;
    }

    private static List<ScheduledTaskTarget>? OptionalTargets(Dictionary<string, JsonElement> args)
    {
        if (!TryArg(args, "targets", out var element) || element.ValueKind != JsonValueKind.Array) return null;
        var targets = new List<ScheduledTaskTarget>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            targets.Add(new ScheduledTaskTarget
            {
                Type = OptionalObjectString(item, "type") ?? ScheduledTaskTargetTypes.CreatedSession,
                SessionId = OptionalObjectString(item, "sessionId", "session_id")
            });
        }
        return targets;
    }

    private static string? OptionalObjectString(JsonElement obj, params string[] names)
    {
        if (TryObjectProperty(obj, names, out var value))
        {
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString();
            if (value.ValueKind == JsonValueKind.Number || value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
                return value.ToString();
        }
        return null;
    }

    private static int? OptionalObjectInt(JsonElement obj, params string[] names)
    {
        if (!TryObjectProperty(obj, names, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;
        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number) ? number : null;
    }

    private static List<string>? OptionalObjectStringArray(JsonElement obj, params string[] names)
    {
        if (!TryObjectProperty(obj, names, out var value) || value.ValueKind != JsonValueKind.Array)
            return null;
        return value.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString()?.Trim() : item.ToString()?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList()!;
    }

    private static bool TryObjectProperty(JsonElement obj, string[] names, out JsonElement value)
    {
        foreach (var name in names)
            if (obj.TryGetProperty(name, out value))
                return true;
        foreach (var property in obj.EnumerateObject())
        {
            if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static DateTimeOffset EffectiveSessionActivity(SessionData data, List<ChatMessage> messages)
    {
        var lastMessage = messages
            .Where(message => message.Timestamp.HasValue)
            .Select(message => UserTimeZoneService.ToUserTime(message.Timestamp!.Value))
            .DefaultIfEmpty(DateTimeOffset.MinValue)
            .Max();
        var dataLast = data.LastActivity == default || data.LastActivity == DateTimeOffset.MinValue
            ? DateTimeOffset.MinValue
            : UserTimeZoneService.ToUserTime(data.LastActivity);
        return lastMessage.ToUniversalTime() > dataLast.ToUniversalTime() ? lastMessage : dataLast;
    }

    private static string? LatestSessionMessagePreview(List<ChatMessage> messages, string role)
    {
        var message = messages
            .LastOrDefault(item =>
                string.Equals(item.Role, role, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(item.Content) &&
                !string.Equals(item.MessageType, "context_summary", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.MessageType, "context_handoff", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.MessageType, "scheduled_task_notice", StringComparison.OrdinalIgnoreCase));
        return message == null ? null : TrimPreview(message.Content, 160);
    }

    private static bool ContainsQuery(string? value, string query)
        => !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static string TrimPreview(string value, int max)
    {
        var text = Regex.Replace(value, @"\s+", " ").Trim();
        return text.Length <= max ? text : text[..max] + "...";
    }

    private static string ScheduledTaskMutationResult(string tool, string status, ScheduledTaskItem task)
    {
        var result = new
        {
            tool,
            status,
            createdOrUpdated = true,
            taskId = task.TaskId,
            title = task.Title,
            taskStatus = task.Status,
            runState = task.RunState,
            timeZone = task.TimeZone,
            nextRunAt = FormatTaskTime(task.NextRunAt, task.TimeZone),
            hasFutureRun = task.NextRunAt.HasValue,
            schedule = task.Schedule,
            targets = task.Targets,
            verification = "persisted_to_scheduled_tasks"
        };
        return JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
    }

    private sealed class SessionListItem
    {
        public string SessionId { get; set; } = string.Empty;
        public string? DisplayTitle { get; set; }
        public string Kind { get; set; } = SessionKinds.Chat;
        public bool IsReadOnly { get; set; }
        public string? CreatedByScheduledTaskId { get; set; }
        public bool IsCurrent { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastActivity { get; set; }
        public int TotalMessages { get; set; }
        public int ToolMessages { get; set; }
        public int Tokens { get; set; }
        public string? LastUserPreview { get; set; }
        public string? LastAssistantPreview { get; set; }
    }
}
