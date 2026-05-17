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
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("last_read")]
    public DateTimeOffset LastRead { get; set; } = UserTimeZoneService.Now();
}
