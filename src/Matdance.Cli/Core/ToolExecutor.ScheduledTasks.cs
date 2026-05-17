using System.Text.Json;
using Matdance.Cli.Models;
using Matdance.Cli.Services;

namespace Matdance.Cli.Core;

public partial class ToolExecutor
{
    private string ExecuteScheduledTaskCreate(Dictionary<string, JsonElement> args)
    {
        var service = new ScheduledTaskService(_path);
        var task = service.Create(new ScheduledTaskCreateRequest { Agent = _agentName, Title = RequiredString(args, "title"), Content = RequiredString(args, "content"), TimeZone = OptionalString(args, "timezone"), Schedule = RequiredSchedule(args), Targets = OptionalTargets(args), CreatedFromSession = _sessionId });
        return $"[scheduled_task_create] Created {task.TaskId}. Next run: {FormatTaskTime(task.NextRunAt, task.TimeZone)}.";
    }

    private string ExecuteScheduledTaskEdit(Dictionary<string, JsonElement> args)
    {
        var service = new ScheduledTaskService(_path);
        var task = service.Edit(new ScheduledTaskEditRequest { Agent = _agentName, TaskId = RequiredString(args, "task_id"), Title = OptionalString(args, "title"), Content = OptionalString(args, "content"), TimeZone = OptionalString(args, "timezone"), Status = OptionalString(args, "status"), Schedule = OptionalSchedule(args), Targets = OptionalTargets(args) });
        return $"[scheduled_task_edit] Updated {task.TaskId}. Status: {task.Status}. Next run: {FormatTaskTime(task.NextRunAt, task.TimeZone)}.";
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

    private async Task<string> ExecuteScheduledTaskDoAsync(Dictionary<string, JsonElement> args)
    {
        var service = new ScheduledTaskService(_path);
        var taskId = RequiredString(args, "task_id");
        var deliver = BoolArg(args, "deliver", false);
        IDisposable? resourceLease = null;
        IDisposable? budgetLease = null;
        BackgroundBudgetCancellation? budgetCts = null;
        CancellationTokenSource? linked = null;
        if (_backgroundWork != null)
        {
            var taskInfo = service.Read(_agentName, taskId);
            var resource = BackgroundWorkCoordinator.GetScheduledTaskResourceKey(taskInfo);
            if (resource != null)
            {
                resourceLease = await _backgroundWork.TryAcquireResourceAsync(_agentName, resource, BackgroundWorkCoordinator.ResourceRetryTimeout, CancellationToken.None);
                if (resourceLease == null)
                    return $"[busy] Scheduled task {taskId} is waiting for the {resource} resource lock. Try again later.";
            }

            budgetLease = await _backgroundWork.TryAcquireBudgetAsync(_agentName, TimeSpan.Zero, CancellationToken.None);
            if (budgetLease == null)
            {
                resourceLease?.Dispose();
                return $"[busy] Scheduled task {taskId} needs one extra background budget slot. Increase max_concurrency or retry after the current work finishes.";
            }

            budgetCts = _backgroundWork.CreateBudgetCancellation(_agentName, CancellationToken.None);
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
            var run = await runner.ExecuteAsync(task, "manual", DateTimeOffset.UtcNow, deliver, linked?.Token ?? CancellationToken.None);
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
        return (ScheduledTaskSchedule?)JsonSerializer.Deserialize(element.GetRawText(), typeof(ScheduledTaskSchedule), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static List<ScheduledTaskTarget>? OptionalTargets(Dictionary<string, JsonElement> args)
    {
        if (!TryArg(args, "targets", out var element) || element.ValueKind != JsonValueKind.Array) return null;
        var targets = (ScheduledTaskTarget[]?)JsonSerializer.Deserialize(element.GetRawText(), typeof(ScheduledTaskTarget[]), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return targets?.ToList();
    }
}
