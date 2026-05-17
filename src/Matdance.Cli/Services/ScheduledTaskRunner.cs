using Matdance.Cli.Core;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public class ScheduledTaskRunner
{
    private readonly PathService _path;
    private readonly ScheduledTaskService _tasks;
    private readonly MemoryOrganizationService? _memoryOrg;
    private readonly SkillMaintenanceService? _skillMaintenance;
    private readonly AgentActivityService? _activity;
    private readonly BackgroundEventService _events;
    private readonly Func<string, string, CancellationToken, Task<IDisposable?>>? _acquireSessionTurn;
    private readonly BackgroundWorkCoordinator? _backgroundWork;

    public ScheduledTaskRunner(
        PathService path,
        ScheduledTaskService tasks,
        MemoryOrganizationService? memoryOrg = null,
        SkillMaintenanceService? skillMaintenance = null,
        AgentActivityService? activity = null,
        Func<string, string, CancellationToken, Task<IDisposable?>>? acquireSessionTurn = null,
        BackgroundWorkCoordinator? backgroundWork = null)
    {
        _path = path;
        _tasks = tasks;
        _memoryOrg = memoryOrg;
        _skillMaintenance = skillMaintenance;
        _activity = activity;
        _acquireSessionTurn = acquireSessionTurn;
        _backgroundWork = backgroundWork;
        _events = new BackgroundEventService(path);
    }

    public async Task<ScheduledTaskRun> ExecuteAsync(ScheduledTaskItem task, string trigger = "schedule", DateTimeOffset? scheduledAt = null, bool deliver = true, CancellationToken ct = default, string? catchUpReason = null)
    {
        var run = new ScheduledTaskRun
        {
            RunId = string.IsNullOrWhiteSpace(task.ActiveRunId) ? "run_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "_" + Guid.NewGuid().ToString("N")[..8] : task.ActiveRunId,
            TaskId = task.TaskId,
            Agent = task.Agent,
            Trigger = trigger,
            ScheduledAt = scheduledAt,
            CatchUpReason = catchUpReason,
            StartedAt = task.ActiveRunStartedAt ?? UserTimeZoneService.Now(),
            LastHeartbeatAt = task.ActiveRunLastHeartbeatAt ?? UserTimeZoneService.Now(),
            HeartbeatKind = task.ActiveRunHeartbeatKind,
            HeartbeatMessage = task.ActiveRunHeartbeatMessage,
            Status = ScheduledTaskRunStatuses.Running
        };

        var manualTrigger = string.Equals(trigger, "manual", StringComparison.OrdinalIgnoreCase);
        _events.Record(task.Agent, "scheduled_task", run.RunId, task.TaskId, "started", $"{trigger}: {task.Title}", "wait_for_completion");
        Heartbeat(run, "started", $"{trigger}: {task.Title}", addEvent: true);

        try
        {
            if (task.IsSystem && task.TaskId == ScheduledTaskService.SystemMemoryOrganizationTaskId && _memoryOrg != null)
            {
                if (!manualTrigger) await WaitForAgentQuietAsync(task, run, ct);
                await RunMemoryOrganizationAsync(task, run, ct);
            }
            else if (task.IsSystem && task.TaskId == ScheduledTaskService.SystemSkillOrganizationTaskId && _skillMaintenance != null)
            {
                if (!manualTrigger) await WaitForAgentQuietAsync(task, run, ct);
                await RunSkillOrganizationAsync(task, run, ct);
            }
            else
            {
                if (!manualTrigger) await WaitForAgentQuietAsync(task, run, ct);
                await WaitForMainAgentAsync(task, run, ct);
                await RunGeneralScheduledTaskAsync(task, run, ct);
            }

            if (deliver)
            {
                Heartbeat(run, "delivery_start", "Delivering scheduled task notice.", addEvent: true);
                await DeliverAsync(task, run, ct);
                Heartbeat(run, "delivery_done", "Scheduled task notice delivery finished.", addEvent: true);
            }
        }
        catch (OperationCanceledException ex)
        {
            run.Status = ScheduledTaskRunStatuses.Canceled;
            run.ErrorType = "canceled";
            run.Error = ex.Message;
            run.Events.Add(new ScheduledTaskRunEvent { Type = "canceled", Message = ex.ToString(), Status = "canceled" });
        }
        catch (Exception ex)
        {
            run.Status = ScheduledTaskRunStatuses.Failed;
            run.ErrorType = ex.GetType().Name;
            run.Error = ex.Message;
            run.Diagnostic = BuildExceptionDiagnostic(ex, run);
            run.Events.Add(new ScheduledTaskRunEvent { Type = "error", Message = ex.ToString(), Status = "failed" });
        }
        finally
        {
            run.FinishedAt = UserTimeZoneService.Now();
            Heartbeat(run, "finished", run.Output ?? run.Error ?? run.Diagnostic ?? run.Status, addEvent: run.Status != ScheduledTaskRunStatuses.Running, status: run.Status);
            _events.Record(task.Agent, "scheduled_task", run.RunId, task.TaskId, run.Status, run.Output ?? run.Error ?? task.Title, run.Status == ScheduledTaskRunStatuses.Succeeded ? "review_result" : "retry_manual");
        }

        return run;
    }

    private async Task RunMemoryOrganizationAsync(ScheduledTaskItem task, ScheduledTaskRun run, CancellationToken ct)
    {
        var jobId = _memoryOrg!.StartOrganization(task.Agent, new MemoryLimits(), ct: ct);
        run.Events.Add(new ScheduledTaskRunEvent { Type = "memory_organization", Message = $"Started memory organization job {jobId}", Status = "running" });
        Heartbeat(run, "subagent_started", $"Started memory organization job {jobId}.");

        OrganizationJob? job = null;
        var lastStage = string.Empty;
        while ((job = _memoryOrg.GetJob(jobId)) != null && job.Status == "running")
        {
            var stage = $"{job.Status}|{job.Progress}|{job.Stage}|{job.Error}";
            if (!string.Equals(stage, lastStage, StringComparison.Ordinal))
            {
                lastStage = stage;
                Heartbeat(run, "subagent_progress", $"Memory organization {job.Progress}%: {job.Stage}", addEvent: true);
            }
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        if (job != null && job.Status == "completed")
        {
            run.Output = $"Memory organization completed. Progress: {job.Progress}%. Stage: {job.Stage}";
            run.Status = ScheduledTaskRunStatuses.Succeeded;
        }
        else if (job != null && job.Status == "canceled")
        {
            run.Error = job.Error ?? "Memory organization was canceled.";
            run.ErrorType = "canceled";
            run.Status = ScheduledTaskRunStatuses.Canceled;
        }
        else
        {
            run.Error = job?.Error ?? "Memory organization failed or was not found.";
            run.Status = ScheduledTaskRunStatuses.Failed;
        }
    }

    private async Task RunSkillOrganizationAsync(ScheduledTaskItem task, ScheduledTaskRun run, CancellationToken ct)
    {
        var jobId = _skillMaintenance!.StartOrganization(task.Agent, ct);
        run.Events.Add(new ScheduledTaskRunEvent { Type = "skill_organization", Message = $"Started skill organization job {jobId}", Status = "running" });
        Heartbeat(run, "subagent_started", $"Started skill organization job {jobId}.");

        SkillJob? job = null;
        var lastStage = string.Empty;
        while ((job = _skillMaintenance.GetJob(jobId)) != null && job.Status == "running")
        {
            var stage = $"{job.Status}|{job.Progress}|{job.Stage}|{job.Error}";
            if (!string.Equals(stage, lastStage, StringComparison.Ordinal))
            {
                lastStage = stage;
                Heartbeat(run, "subagent_progress", $"Skill organization {job.Progress}%: {job.Stage}", addEvent: true);
            }
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        if (job != null && job.Status == "completed")
        {
            run.Output = $"Skill organization completed. Progress: {job.Progress}%. Stage: {job.Stage}. {job.ResultSummary}";
            run.Status = ScheduledTaskRunStatuses.Succeeded;
        }
        else if (job != null && job.Status == "canceled")
        {
            run.Error = job.Error ?? "Skill organization was canceled.";
            run.ErrorType = "canceled";
            run.Status = ScheduledTaskRunStatuses.Canceled;
        }
        else
        {
            run.Error = job?.Error ?? "Skill organization failed, was canceled, or was not found.";
            run.Status = ScheduledTaskRunStatuses.Failed;
        }
    }

    private async Task RunGeneralScheduledTaskAsync(ScheduledTaskItem task, ScheduledTaskRun run, CancellationToken ct)
    {
        var config = AgentConfig.Load(_path.GetAgentConfigJsonPath(task.Agent));
        var llm = new LlmClient(config);
        var messages = PromptBuilder.BuildScheduledTaskMessages(task.Agent, _path, task);
        run.ContextSnapshot = messages[0].Content;
        var state = new SessionState();
        var executor = new ToolExecutor(task.Agent, _path, state, allowInteractiveConfirmation: false, backgroundWork: _backgroundWork);
        var tools = ToolRegistry.GetAll();
        var thinkingToolNoticeSent = false;

        for (var loop = 1; loop <= 200; loop++)
        {
            Heartbeat(run, "llm_request", $"Sending scheduled subagent model request {loop}.", addEvent: true);
            var assistant = await llm.SendAsync(messages, tools, _ => { }, ct, async (attempt, delay, error, token) =>
            {
                var message = $"Model/API retry {attempt}; waiting {delay.TotalSeconds:F0}s. {error.GetType().Name}: {error.Message}";
                run.Events.Add(new ScheduledTaskRunEvent { Type = "llm_retry_wait", Message = message, Status = "retry" });
                Heartbeat(run, "llm_retry_wait", message);
                await WaitForMainAgentAsync(task, run, token);
            }, enableThinking: false);
            Heartbeat(run, "llm_response", $"Received scheduled subagent model response {loop}.", addEvent: true);
            if ((assistant.ToolCalls == null || assistant.ToolCalls.Count == 0) && !thinkingToolNoticeSent && LlmResponseGuard.HasTextualToolRequestInThinking(assistant))
            {
                thinkingToolNoticeSent = true;
                messages.Add(ChatMessage.User(LlmResponseGuard.ThinkingTextToolRequestNotice));
                run.Events.Add(new ScheduledTaskRunEvent { Type = "thinking_tool_blocked", Message = LlmResponseGuard.ThinkingTextToolRequestNotice, Status = "blocked" });
                continue;
            }

            if (string.IsNullOrWhiteSpace(assistant.Content) && (assistant.ToolCalls == null || assistant.ToolCalls.Count == 0))
                assistant.Content = "(no response)";

            messages.Add(assistant);
            run.Events.Add(new ScheduledTaskRunEvent { Type = "assistant", Message = Trim(assistant.Content, 4000), Status = assistant.ToolCalls == null ? "final" : "tool_calls" });
            _tasks.HeartbeatRun(run, "assistant", Trim(assistant.Content, 700));

            if (assistant.ToolCalls == null || assistant.ToolCalls.Count == 0)
            {
                run.Output = assistant.Content;
                run.Status = ScheduledTaskRunStatuses.Succeeded;
                break;
            }

            foreach (var toolCall in assistant.ToolCalls)
            {
                Heartbeat(run, "tool_call_start", toolCall.Function.Name, addEvent: true);
                var result = await executor.ExecuteAsync(toolCall);
                messages.Add(ChatMessage.Tool(toolCall.Id, result));
                run.Events.Add(new ScheduledTaskRunEvent { Type = "tool_call", ToolName = toolCall.Function.Name, ToolArguments = Trim(toolCall.Function.Arguments, 2000), ToolResult = Trim(result, 4000), Status = IsToolError(result) ? "error" : "done" });
                Heartbeat(run, "tool_call_done", $"{toolCall.Function.Name}: {(IsToolError(result) ? "error" : "done")}");
            }
        }

        if (run.Status == ScheduledTaskRunStatuses.Running)
        {
            run.Status = ScheduledTaskRunStatuses.Failed;
            run.ErrorType = "max_rounds";
            run.Error = "Stopped after 200 scheduled subagent rounds.";
        }
    }

    private async Task DeliverAsync(ScheduledTaskItem task, ScheduledTaskRun run, CancellationToken ct)
    {
        var sessions = _tasks.ResolveTargetSessions(task);
        if (sessions.Count == 0)
        {
            run.DeliveryResults.Add(new ScheduledTaskDeliveryResult { Status = "skipped", Error = "No available target sessions." });
            return;
        }
        foreach (var sessionId in sessions)
        {
            var delivery = new ScheduledTaskDeliveryResult { Target = new ScheduledTaskTarget { Type = ScheduledTaskTargetTypes.Session, SessionId = sessionId }, SessionId = sessionId };
            try
            {
                Heartbeat(run, "delivery_session", $"Delivering notice to session {sessionId}.");
                IDisposable? lease = null;
                if (_acquireSessionTurn != null)
                    lease = await _acquireSessionTurn(task.Agent, sessionId, ct);
                using (lease)
                {
                    var sessionFile = _path.GetSessionJsonPath(task.Agent, sessionId);
                    var data = SessionData.Load(sessionFile);
                    var state = SessionState.Load(sessionFile);
                    state.Messages.Add(ChatMessage.ScheduledNotice(BuildNotice(task, run)));
                    data.TotalMessages++;
                    data.LastActivity = UserTimeZoneService.Now();
                    data.Save(sessionFile);
                    state.Save(sessionFile);
                }
                delivery.Status = "delivered";
            }
            catch (Exception ex)
            {
                delivery.Status = "failed";
                delivery.Error = ex.Message;
            }
            run.DeliveryResults.Add(delivery);
        }
    }

    private async Task WaitForMainAgentAsync(ScheduledTaskItem task, ScheduledTaskRun run, CancellationToken ct)
    {
        if (_acquireSessionTurn == null) return;
        foreach (var sessionId in _tasks.ResolveTargetSessions(task))
        {
            Heartbeat(run, "waiting_session_turn", $"Waiting for main agent session turn: {sessionId}.");
            using var lease = await _acquireSessionTurn(task.Agent, sessionId, ct);
        }
    }

    private async Task WaitForAgentQuietAsync(ScheduledTaskItem task, ScheduledTaskRun run, CancellationToken ct)
    {
        if (_activity == null) return;
        while (_activity.GetActiveUserTurnCount(task.Agent) >= GetMaxConcurrency(task.Agent))
        {
            Heartbeat(run, "waiting_user_activity", "Waiting for active user work to release agent concurrency budget.");
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    private int GetMaxConcurrency(string agent)
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

    private void Heartbeat(ScheduledTaskRun run, string kind, string message, bool addEvent = false, string status = "running")
    {
        if (addEvent)
            run.Events.Add(new ScheduledTaskRunEvent { Type = kind, Message = Trim(message, 1000), Status = status });
        _tasks.HeartbeatRun(run, kind, message);
    }

    private static string BuildNotice(ScheduledTaskItem task, ScheduledTaskRun run)
    {
        var ok = run.Status == ScheduledTaskRunStatuses.Succeeded;
        var zone = ScheduledTaskService.FindZone(task.TimeZone);
        var localFinished = run.FinishedAt.HasValue ? TimeZoneInfo.ConvertTime(run.FinishedAt.Value, zone) : (DateTimeOffset?)null;
        var localScheduled = run.Trigger == "manual" ? null : (run.ScheduledAt.HasValue ? TimeZoneInfo.ConvertTime(run.ScheduledAt.Value, zone) : (DateTimeOffset?)null);
        var timeStr = localFinished?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? run.FinishedAt?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "N/A";
        var scheduledStr = localScheduled?.ToString("yyyy-MM-dd HH:mm:ss zzz");
        var catchUp = string.IsNullOrWhiteSpace(run.CatchUpReason) ? string.Empty : $"\nCatch-up reason: {run.CatchUpReason}";
        return $"## Scheduled Task Notice\n\nTask: {task.Title}\nStatus: {(ok ? "Succeeded" : "Failed")}\nRun ID: {run.RunId}\n{(scheduledStr == null ? string.Empty : $"Scheduled at: {scheduledStr}\n")}Completed at: {timeStr}{catchUp}\n\n{(ok ? run.Output : run.Error)}\n\n---\nThis is a low-priority notice. It does not enter the main agent reasoning context by default.";
    }

    private static string BuildExceptionDiagnostic(Exception ex, ScheduledTaskRun run)
    {
        var heartbeat = string.IsNullOrWhiteSpace(run.HeartbeatKind)
            ? "no heartbeat was recorded"
            : $"{run.HeartbeatKind}: {run.HeartbeatMessage}";
        return $"Scheduled task failed with {ex.GetType().Name}: {ex.Message}. Last heartbeat: {heartbeat}. Network/model retry attempts are recorded as llm_retry_wait events when the upstream client classifies them as retryable.";
    }

    private static bool IsToolError(string result)
    {
        var text = result.TrimStart();
        return text.StartsWith("[error]", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("[blocked]", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("[timeout]", StringComparison.OrdinalIgnoreCase);
    }

    private static string Trim(string? value, int max)
    {
        value ??= string.Empty;
        return value.Length <= max ? value : value[..max] + "\n...[truncated]";
    }
}
