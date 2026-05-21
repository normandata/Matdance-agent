using System.Text.Json;
using System.Text.Json.Serialization;
using Matdance.Cli.Services;

namespace Matdance.Cli.Models;

public class SessionData
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("display_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayTitle { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = SessionKinds.Chat;

    [JsonPropertyName("is_read_only")]
    public bool IsReadOnly { get; set; }

    [JsonPropertyName("created_by_scheduled_task_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CreatedByScheduledTaskId { get; set; }

    [JsonIgnore]
    public bool IsScheduledNotification => IsReadOnly && string.Equals(Kind, SessionKinds.ScheduledNotification, StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("context_usage")]
    public int ContextUsage { get; set; } = 0;

    [JsonPropertyName("total_messages")]
    public int TotalMessages { get; set; } = 0;

    [JsonPropertyName("tool_messages_count")]
    public int ToolMessagesCount { get; set; } = 0;

    [JsonPropertyName("tokens")]
    public int Tokens { get; set; } = 0;

    [JsonPropertyName("create_at")]
    public DateTimeOffset CreateAt { get; set; } = UserTimeZoneService.Now();

    [JsonPropertyName("last_activity")]
    public DateTimeOffset LastActivity { get; set; } = UserTimeZoneService.Now();

    [JsonPropertyName("is_processing")]
    public bool IsProcessing { get; set; } = false;

    [JsonPropertyName("tasks")]
    public List<SessionTask> Tasks { get; set; } = new();

    [JsonPropertyName("issues")]
    public List<SessionIssue> Issues { get; set; } = new();

    public void Save(string path)
    {
        NormalizeTimeZone();
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        AtomicFile.WriteAllText(path, json);
    }

    public void NormalizeTimeZone()
    {
        if (CreateAt != default && CreateAt != DateTimeOffset.MinValue)
            CreateAt = UserTimeZoneService.ToUserTime(CreateAt);
        if (LastActivity != default && LastActivity != DateTimeOffset.MinValue)
            LastActivity = UserTimeZoneService.ToUserTime(LastActivity);
    }

    public static SessionData Load(string path)
    {
        if (!File.Exists(path))
            return new SessionData();
        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<SessionData>(json) ?? new SessionData();
        data.NormalizeTimeZone();
        return data;
    }
}

public static class SessionKinds
{
    public const string Chat = "chat";
    public const string ScheduledNotification = "scheduled_notification";
}

public class SessionTask
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<TaskStep> Steps { get; set; } = new();

    [JsonPropertyName("issues")]
    public List<SessionIssue> Issues { get; set; } = new();
}

public class TaskStep
{
    [JsonPropertyName("index")]
    public int Index { get; set; } = 1;

    [JsonPropertyName("for_what")]
    public string ForWhat { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";
}

public class SessionIssue
{
    [JsonPropertyName("issues_id")]
    public string IssuesId { get; set; } = string.Empty;

    [JsonPropertyName("why")]
    public string Why { get; set; } = string.Empty;

    [JsonPropertyName("how_to_fix")]
    public string HowToFix { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "in_process";
}
