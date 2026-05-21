using System.Text.Json;
using System.Text.Json.Serialization;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public class BookmarkService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly PathService _path;

    public BookmarkService(PathService path)
    {
        _path = path;
    }

    public SessionBookmark? GetSessionBookmark(string agent, string sessionId)
    {
        var path = GetBookmarkPath(agent, "session", sessionId);
        return File.Exists(path) ? Load<SessionBookmark>(path) : null;
    }

    public SessionBookmark? GetSkillSessionBookmark(string agent, string sessionId)
    {
        var path = GetBookmarkPath(agent, "skill_session", sessionId);
        return File.Exists(path) ? Load<SessionBookmark>(path) : null;
    }

    public void UpdateSessionBookmark(string agent, string sessionId, DateTimeOffset lastIntegratedAt, int version)
    {
        var current = new SessionActivityInspector(_path).Inspect(agent, sessionId);
        UpdateSessionBookmark(agent, sessionId, lastIntegratedAt, version, current);
    }

    public void UpdateSessionBookmark(string agent, string sessionId, DateTimeOffset lastIntegratedAt, int version, SessionActivitySnapshot cursor)
    {
        var path = GetBookmarkPath(agent, "session", sessionId);
        EnsureDirectory(path);
        var bookmark = new SessionBookmark
        {
            Agent = agent,
            SessionId = sessionId,
            LastIntegratedAt = lastIntegratedAt,
            Version = version,
            UpdatedAt = UserTimeZoneService.Now(),
            MessageCount = cursor.MessageCount,
            LastMessageIndex = cursor.LastMessageIndex,
            LastMessageHash = cursor.LastMessageHash,
            StateFileHash = cursor.StateFileHash,
            LatestMessageAt = cursor.LatestMessageAt,
            EffectiveActivityAt = cursor.EffectiveActivity
        };
        Save(path, bookmark);
    }

    public void UpdateSessionBookmark(string agent, SessionBookmark cursor)
    {
        UpdateSessionCursor(agent, "session", cursor);
    }

    public void UpdateSkillSessionBookmark(string agent, SessionBookmark cursor)
    {
        UpdateSessionCursor(agent, "skill_session", cursor);
    }

    public List<SessionBookmark> GetPendingSkillSessionBookmarks(string agent)
    {
        var sessions = new List<SessionBookmark>();
        var inspector = new SessionActivityInspector(_path);
        var sessionsDir = _path.GetSessionsPath(agent);
        if (!Directory.Exists(sessionsDir)) return sessions;

        foreach (var file in Directory.GetFiles(sessionsDir, "*.json"))
        {
                if (file.EndsWith(".state.json", StringComparison.OrdinalIgnoreCase)) continue;
                var sessionId = Path.GetFileNameWithoutExtension(file);
                if (IsScheduledNotificationSession(file))
                    continue;
                var bookmark = GetSkillSessionBookmark(agent, sessionId);
                var current = inspector.Inspect(agent, sessionId);

            if (SessionHasPendingChanges(bookmark, current))
                sessions.Add(BuildPendingSessionBookmark(agent, sessionId, bookmark, current));
        }

        return sessions
            .OrderBy(s => (s.EffectiveActivityAt ?? s.LatestMessageAt ?? DateTimeOffset.MinValue).ToUniversalTime())
            .ThenBy(s => s.SessionId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public TaskBookmark? GetTaskBookmark(string agent, string taskId)
    {
        var path = GetBookmarkPath(agent, "task", taskId);
        return File.Exists(path) ? Load<TaskBookmark>(path) : null;
    }

    public void UpdateTaskBookmark(string agent, string taskId, DateTimeOffset lastIntegratedAt, int version)
    {
        var path = GetBookmarkPath(agent, "task", taskId);
        EnsureDirectory(path);
        var bookmark = new TaskBookmark
        {
            Agent = agent,
            TaskId = taskId,
            LastIntegratedAt = lastIntegratedAt,
            Version = version,
            UpdatedAt = UserTimeZoneService.Now()
        };
        Save(path, bookmark);
    }

    public void UpdateTaskBookmark(string agent, TaskBookmark cursor)
    {
        var integratedAt = cursor.LatestRunFinishedAt == default
            ? UserTimeZoneService.Now()
            : cursor.LatestRunFinishedAt;
        var path = GetBookmarkPath(agent, "task", cursor.TaskId);
        EnsureDirectory(path);
        cursor.Agent = agent;
        cursor.LastIntegratedAt = integratedAt;
        cursor.Version = (cursor.Version > 0 ? cursor.Version : 0) + 1;
        cursor.UpdatedAt = UserTimeZoneService.Now();
        Save(path, cursor);
    }

    public GlobalBookmarkState GetGlobalState(string agent)
    {
        var path = Path.Combine(_path.GetBookmarksPath(agent), "global.json");
        if (!File.Exists(path)) return new GlobalBookmarkState();
        return Load<GlobalBookmarkState>(path) ?? new GlobalBookmarkState();
    }

    public void UpdateGlobalState(string agent, GlobalBookmarkState state)
    {
        var path = Path.Combine(_path.GetBookmarksPath(agent), "global.json");
        EnsureDirectory(path);
        state.UpdatedAt = UserTimeZoneService.Now();
        Save(path, state);
    }

    public PendingBookmarks GetPendingBookmarks(string agent)
    {
        var sessions = new List<SessionBookmark>();
        var tasks = new List<TaskBookmark>();
        var inspector = new SessionActivityInspector(_path);

        var sessionsDir = _path.GetSessionsPath(agent);
        if (Directory.Exists(sessionsDir))
        {
            foreach (var file in Directory.GetFiles(sessionsDir, "*.json"))
            {
                if (file.EndsWith(".state.json", StringComparison.OrdinalIgnoreCase)) continue;
                var sessionId = Path.GetFileNameWithoutExtension(file);
                if (IsScheduledNotificationSession(file))
                    continue;
                var bookmark = GetSessionBookmark(agent, sessionId);
                var current = inspector.Inspect(agent, sessionId);

                if (SessionHasPendingChanges(bookmark, current))
                    sessions.Add(BuildPendingSessionBookmark(agent, sessionId, bookmark, current));
            }
        }

        var tasksDir = Path.Combine(_path.GetScheduledTasksPath(agent), "runs");
        if (Directory.Exists(tasksDir))
        {
            foreach (var taskDir in Directory.GetDirectories(tasksDir))
            {
                var taskId = Path.GetFileName(taskDir);
                if (taskId == ScheduledTaskService.SystemMemoryOrganizationTaskId
                    || taskId == ScheduledTaskService.SystemSkillOrganizationTaskId)
                    continue;

                var bookmark = GetTaskBookmark(agent, taskId);
                var lastIntegrated = bookmark?.LastIntegratedAt ?? DateTimeOffset.MinValue;
                var hasNewRuns = false;
                var latestRunTime = DateTimeOffset.MinValue;
                string? latestRunId = null;

                foreach (var runFile in Directory.GetFiles(taskDir, "*.json"))
                {
                    try
                    {
                        var run = JsonSerializer.Deserialize<ScheduledTaskRun>(File.ReadAllText(runFile), JsonOptions);
                        if (run?.FinishedAt != null && IsAfter(run.FinishedAt.Value, lastIntegrated))
                        {
                            hasNewRuns = true;
                            if (IsAfter(run.FinishedAt.Value, latestRunTime))
                            {
                                latestRunTime = run.FinishedAt.Value;
                                latestRunId = run.RunId;
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                if (hasNewRuns)
                {
                    var pending = bookmark ?? new TaskBookmark
                    {
                        Agent = agent,
                        TaskId = taskId,
                        LastIntegratedAt = DateTimeOffset.MinValue,
                        Version = 0
                    };
                    pending.LatestRunFinishedAt = latestRunTime;
                    pending.LastRunId = latestRunId;
                    tasks.Add(pending);
                }
            }
        }

        return new PendingBookmarks
        {
            Sessions = sessions.OrderByDescending(s => s.LastIntegratedAt.ToUniversalTime()).ToList(),
            Tasks = tasks.OrderByDescending(t => t.LastIntegratedAt.ToUniversalTime()).ToList()
        };
    }

    private string GetBookmarkPath(string agent, string type, string id)
        => Path.Combine(_path.GetBookmarksPath(agent), $"{type}_{id}.json");

    private static bool SessionHasPendingChanges(SessionBookmark? bookmark, SessionActivitySnapshot current)
    {
        if (bookmark == null)
            return current.MessageCount > 0 || current.EffectiveActivity.HasValue;

        if (current.MessageCount != bookmark.MessageCount)
            return true;

        if (!HashesEqual(current.LastMessageHash, bookmark.LastMessageHash))
            return true;

        if (!HashesEqual(current.StateFileHash, bookmark.StateFileHash))
            return true;

        var latest = current.EffectiveActivity ?? current.LatestMessageAt;
        return latest.HasValue && IsAfter(latest.Value, bookmark.LastIntegratedAt);
    }

    private static bool IsScheduledNotificationSession(string file)
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

    private static SessionBookmark BuildPendingSessionBookmark(string agent, string sessionId, SessionBookmark? bookmark, SessionActivitySnapshot current)
    {
        var hasEditedHistory = bookmark != null && current.MessageCount <= bookmark.MessageCount && (
            !HashesEqual(current.LastMessageHash, bookmark.LastMessageHash)
            || !HashesEqual(current.StateFileHash, bookmark.StateFileHash));

        return new SessionBookmark
        {
            Agent = agent,
            SessionId = sessionId,
            LastIntegratedAt = bookmark?.LastIntegratedAt ?? DateTimeOffset.MinValue,
            Version = bookmark?.Version ?? 0,
            UpdatedAt = bookmark?.UpdatedAt ?? DateTimeOffset.MinValue,
            MessageCount = current.MessageCount,
            LastMessageIndex = current.LastMessageIndex,
            LastMessageHash = current.LastMessageHash,
            StateFileHash = current.StateFileHash,
            LatestMessageAt = current.LatestMessageAt,
            EffectiveActivityAt = current.EffectiveActivity,
            NeedsReconcile = hasEditedHistory,
            PreviousMessageCount = bookmark?.MessageCount
        };
    }

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private void UpdateSessionCursor(string agent, string type, SessionBookmark cursor)
    {
        var path = GetBookmarkPath(agent, type, cursor.SessionId);
        EnsureDirectory(path);
        cursor.Agent = agent;
        cursor.LastIntegratedAt = cursor.EffectiveActivityAt ?? cursor.LatestMessageAt ?? UserTimeZoneService.Now();
        cursor.Version = (cursor.Version > 0 ? cursor.Version : 0) + 1;
        cursor.UpdatedAt = UserTimeZoneService.Now();
        Save(path, cursor);
    }

    private static bool HashesEqual(string? current, string? saved)
    {
        var hasCurrent = !string.IsNullOrWhiteSpace(current);
        var hasSaved = !string.IsNullOrWhiteSpace(saved);
        if (!hasCurrent && !hasSaved) return true;
        if (hasCurrent != hasSaved) return false;
        return string.Equals(current, saved, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAfter(DateTimeOffset left, DateTimeOffset right)
    {
        if (right == default || right == DateTimeOffset.MinValue)
            return left != default && left != DateTimeOffset.MinValue;
        return left.ToUniversalTime() > right.ToUniversalTime();
    }

    private static T? Load<T>(string path) where T : class
    {
        try
        {
            var json = File.ReadAllText(path);
            var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            NormalizeBookmarkTimes(value);
            return value;
        }
        catch
        {
            return null;
        }
    }

    private static void Save<T>(string path, T value) where T : class
    {
        NormalizeBookmarkTimes(value);
        AtomicFile.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions));
    }

    private static void NormalizeBookmarkTimes<T>(T? value) where T : class
    {
        switch (value)
        {
            case SessionBookmark session:
                session.LastIntegratedAt = Normalize(session.LastIntegratedAt);
                session.UpdatedAt = Normalize(session.UpdatedAt);
                session.LatestMessageAt = Normalize(session.LatestMessageAt);
                session.EffectiveActivityAt = Normalize(session.EffectiveActivityAt);
                break;
            case TaskBookmark task:
                task.LastIntegratedAt = Normalize(task.LastIntegratedAt);
                task.UpdatedAt = Normalize(task.UpdatedAt);
                task.LatestRunFinishedAt = Normalize(task.LatestRunFinishedAt);
                break;
            case GlobalBookmarkState global:
                global.LastFullRebuild = Normalize(global.LastFullRebuild);
                global.UpdatedAt = Normalize(global.UpdatedAt);
                break;
        }
    }

    private static DateTimeOffset Normalize(DateTimeOffset value)
    {
        return value == default || value == DateTimeOffset.MinValue
            ? value
            : UserTimeZoneService.ToUserTime(value);
    }

    private static DateTimeOffset? Normalize(DateTimeOffset? value)
    {
        return value.HasValue ? Normalize(value.Value) : null;
    }
}

public class SessionBookmark
{
    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("last_integrated_at")]
    public DateTimeOffset LastIntegratedAt { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }

    [JsonPropertyName("last_message_index")]
    public int LastMessageIndex { get; set; } = -1;

    [JsonPropertyName("last_message_hash")]
    public string? LastMessageHash { get; set; }

    [JsonPropertyName("state_file_hash")]
    public string? StateFileHash { get; set; }

    [JsonPropertyName("latest_message_at")]
    public DateTimeOffset? LatestMessageAt { get; set; }

    [JsonPropertyName("effective_activity_at")]
    public DateTimeOffset? EffectiveActivityAt { get; set; }

    [JsonIgnore]
    public bool NeedsReconcile { get; set; }

    [JsonIgnore]
    public int? PreviousMessageCount { get; set; }
}

public class TaskBookmark
{
    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("last_integrated_at")]
    public DateTimeOffset LastIntegratedAt { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("last_run_id")]
    public string? LastRunId { get; set; }

    [JsonPropertyName("latest_run_finished_at")]
    public DateTimeOffset LatestRunFinishedAt { get; set; }
}

public class GlobalBookmarkState
{
    [JsonPropertyName("last_full_rebuild")]
    public DateTimeOffset LastFullRebuild { get; set; } = DateTimeOffset.MinValue;

    [JsonPropertyName("incremental_count_since_rebuild")]
    public int IncrementalCountSinceRebuild { get; set; }

    [JsonPropertyName("memory_org_session_message_batch_hint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MemoryOrgSessionMessageBatchHint { get; set; }

    [JsonPropertyName("memory_org_task_run_batch_hint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MemoryOrgTaskRunBatchHint { get; set; }

    [JsonPropertyName("skill_org_session_message_batch_hint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SkillOrgSessionMessageBatchHint { get; set; }

    [JsonPropertyName("skill_org_read_window_hint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SkillOrgReadWindowHint { get; set; }

    [JsonPropertyName("skill_org_batch_failures")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, int>? SkillOrgBatchFailures { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = UserTimeZoneService.Now();
}

public class PendingBookmarks
{
    [JsonPropertyName("sessions")]
    public List<SessionBookmark> Sessions { get; set; } = new();

    [JsonPropertyName("tasks")]
    public List<TaskBookmark> Tasks { get; set; } = new();
}
