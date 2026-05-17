using System.Text.Json.Serialization;
using Matdance.Cli.Services;

namespace Matdance.Cli.Models;

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user"; // system, user, assistant, tool

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("reasoning_content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningContent { get; set; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("message_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageType { get; set; }

    [JsonPropertyName("include_in_main_context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeInMainContext { get; set; }

    [JsonPropertyName("importance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Importance { get; set; }

    [JsonPropertyName("timestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? Timestamp { get; set; }

    [JsonPropertyName("audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AudioAttachment? Audio { get; set; }

    [JsonPropertyName("attachments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ChatAttachment>? Attachments { get; set; }

    public static ChatMessage System(string content) => new() { Role = "system", Content = content, Timestamp = UserTimeZoneService.Now() };
    public static ChatMessage System(string content, DateTimeOffset timestamp) => new() { Role = "system", Content = content, Timestamp = timestamp };
    public static ChatMessage User(string content) => new() { Role = "user", Content = content, Timestamp = UserTimeZoneService.Now() };
    public static ChatMessage User(string content, List<ChatAttachment>? attachments) => new() { Role = "user", Content = content, Attachments = attachments, Timestamp = UserTimeZoneService.Now() };
    public static ChatMessage Assistant(string content, List<ToolCall>? toolCalls = null) => new() { Role = "assistant", Content = content, ToolCalls = toolCalls, Timestamp = UserTimeZoneService.Now() };
    public static ChatMessage Tool(string toolCallId, string content) => new() { Role = "tool", ToolCallId = toolCallId, Content = content, Timestamp = UserTimeZoneService.Now() };
    public static ChatMessage ScheduledNotice(string content) => new() { Role = "assistant", Content = content, MessageType = "scheduled_task_notice", IncludeInMainContext = false, Importance = "notification", Timestamp = UserTimeZoneService.Now() };
}

public class ChatAttachment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "document";

    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = "application/octet-stream";

    [JsonPropertyName("extension")]
    public string Extension { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("relative_path")]
    public string RelativePath { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}

public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public ToolFunction Function { get; set; } = new();
}

public class ToolFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}
