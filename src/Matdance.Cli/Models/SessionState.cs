using System.Text.Json;
using System.Text.Json.Serialization;
using Matdance.Cli.Services;

namespace Matdance.Cli.Models;

public class SessionState
{
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("active_task")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ActiveTaskInfo? ActiveTask { get; set; }

    [JsonPropertyName("traced_files")]
    public List<TracedFileInfo> TracedFiles { get; set; } = new();

    [JsonPropertyName("file_edit_audits")]
    public List<FileEditAuditInfo> FileEditAudits { get; set; } = new();

    [JsonPropertyName("context_compaction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContextCompactionInfo? ContextCompaction { get; set; }

    public int ClearTraceLocks()
    {
        var count = TracedFiles.Count;
        if (count > 0)
            TracedFiles.Clear();
        return count;
    }

    public string GetStatePath(string sessionJsonPath) => Path.Combine(
        Path.GetDirectoryName(sessionJsonPath)!,
        Path.GetFileNameWithoutExtension(sessionJsonPath) + ".state.json"
    );

    public void Save(string sessionJsonPath)
    {
        NormalizeTimeZone();
        var path = GetStatePath(sessionJsonPath);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        AtomicFile.WriteAllText(path, json);
    }

    public void NormalizeTimeZone()
    {
        foreach (var message in Messages)
        {
            if (message.Timestamp.HasValue)
                message.Timestamp = UserTimeZoneService.ToUserTime(message.Timestamp.Value);
        }

        foreach (var file in TracedFiles)
        {
            if (file.LastRead != default && file.LastRead != DateTimeOffset.MinValue)
                file.LastRead = UserTimeZoneService.ToUserTime(file.LastRead);
        }

        foreach (var audit in FileEditAudits)
        {
            if (audit.Timestamp != default && audit.Timestamp != DateTimeOffset.MinValue)
                audit.Timestamp = UserTimeZoneService.ToUserTime(audit.Timestamp);
        }

        var compaction = ContextCompaction;
        if (compaction != null && compaction.CreatedAt != default && compaction.CreatedAt != DateTimeOffset.MinValue)
            compaction.CreatedAt = UserTimeZoneService.ToUserTime(compaction.CreatedAt);
    }

    public static SessionState Load(string sessionJsonPath)
    {
        var path = Path.Combine(
            Path.GetDirectoryName(sessionJsonPath)!,
            Path.GetFileNameWithoutExtension(sessionJsonPath) + ".state.json"
        );
        if (!File.Exists(path))
            return new SessionState();
        var json = File.ReadAllText(path);
        var state = JsonSerializer.Deserialize<SessionState>(json) ?? new SessionState();
        state.NormalizeTimeZone();
        return state;
    }
}

public class ContextCompactionInfo
{
    [JsonPropertyName("generation")]
    public int Generation { get; set; }

    [JsonPropertyName("compressed_until_message_count")]
    public int CompressedUntilMessageCount { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("last_handoff")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastHandoff { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = UserTimeZoneService.Now();
}

public class ActiveTaskInfo
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<TaskStep> Steps { get; set; } = new();

    [JsonPropertyName("issues")]
    public List<SessionIssue> Issues { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = "in_process";
}

public class TracedFileInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "read";

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "physical";

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("anchor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Anchor { get; set; }

    [JsonPropertyName("anchor_text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AnchorText { get; set; }

    [JsonPropertyName("start_line")]
    public int StartLine { get; set; } = 1;

    [JsonPropertyName("end_line")]
    public int EndLine { get; set; } = 1;

    [JsonPropertyName("center_line")]
    public int CenterLine { get; set; } = 1;

    [JsonPropertyName("max_lines")]
    public int MaxLines { get; set; } = 2000;

    [JsonPropertyName("line_count")]
    public int LineCount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "fresh";

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("content_hash")]
    public string ContentHash { get; set; } = string.Empty;

    [JsonPropertyName("last_read")]
    public DateTimeOffset LastRead { get; set; } = UserTimeZoneService.Now();
}

public class FileEditAuditInfo
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = UserTimeZoneService.Now();

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("before_hash")]
    public string BeforeHash { get; set; } = string.Empty;

    [JsonPropertyName("after_hash")]
    public string AfterHash { get; set; } = string.Empty;

    [JsonPropertyName("diff")]
    public string Diff { get; set; } = string.Empty;

    [JsonPropertyName("write_lock_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WriteLockId { get; set; }
}
