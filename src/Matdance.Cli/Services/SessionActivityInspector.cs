using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public sealed class SessionActivityInspector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PathService _path;

    public SessionActivityInspector(PathService path)
    {
        _path = path;
    }

    public SessionActivitySnapshot Inspect(string agent, string sessionId)
    {
        var sessionFile = _path.GetSessionJsonPath(agent, sessionId);
        var snapshot = InspectFile(sessionFile, sessionId);
        snapshot.Agent = agent;
        return snapshot;
    }

    public static SessionActivitySnapshot InspectFile(string sessionFile, string? sessionId = null)
    {
        var id = string.IsNullOrWhiteSpace(sessionId)
            ? Path.GetFileNameWithoutExtension(sessionFile)
            : sessionId;

        var data = SessionData.Load(sessionFile);
        if (string.IsNullOrWhiteSpace(data.SessionId))
            data.SessionId = id ?? string.Empty;

        var stateFile = Path.Combine(
            Path.GetDirectoryName(sessionFile)!,
            Path.GetFileNameWithoutExtension(sessionFile) + ".state.json");
        var state = SessionState.Load(sessionFile);
        var messages = state.Messages ?? new List<ChatMessage>();
        var count = messages.Count;
        var lastIndex = count - 1;
        var last = lastIndex >= 0 ? messages[lastIndex] : null;
        var latestMessageAt = messages
            .Where(message => message.Timestamp.HasValue)
            .Select(message => UserTimeZoneService.ToUserTime(message.Timestamp!.Value))
            .DefaultIfEmpty(DateTimeOffset.MinValue)
            .Max();

        DateTimeOffset? latest = latestMessageAt == DateTimeOffset.MinValue ? null : latestMessageAt;
        var dataLastActivity = ToDateTimeOffset(data.LastActivity);
        var effective = Max(dataLastActivity, latest);

        return new SessionActivitySnapshot
        {
            Agent = string.Empty,
            SessionId = data.SessionId,
            LastActivity = dataLastActivity,
            EffectiveActivity = effective,
            LatestMessageAt = latest,
            MessageCount = count,
            LastMessageIndex = lastIndex,
            LastMessageHash = last == null ? null : HashMessage(last),
            StateFileHash = File.Exists(stateFile) ? HashFile(stateFile) : null
        };
    }

    public static string HashMessage(ChatMessage message)
    {
        // Keep the hash timezone-neutral; user-visible timestamps are normalized elsewhere.
        var payload = new
        {
            role = message.Role,
            content = message.Content,
            reasoning = message.ReasoningContent,
            toolCallId = message.ToolCallId,
            toolCalls = message.ToolCalls,
            messageType = message.MessageType,
            includeInMainContext = message.IncludeInMainContext,
            importance = message.Importance,
            timestamp = message.Timestamp?.ToUniversalTime().ToString("O")
        };
        return HashText(JsonSerializer.Serialize(payload, JsonOptions));
    }

    public static string HashText(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        var bytes = SHA256.HashData(stream);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static DateTimeOffset? ToDateTimeOffset(DateTime value)
    {
        if (value == default || value == DateTime.MinValue)
            return null;

        return value.Kind switch
        {
            DateTimeKind.Utc => UserTimeZoneService.ToUserTime(new DateTimeOffset(value, TimeSpan.Zero)),
            DateTimeKind.Local => UserTimeZoneService.ToUserTime(new DateTimeOffset(value)),
            _ => new DateTimeOffset(value, UserTimeZoneService.FindZone(null).GetUtcOffset(value))
        };
    }

    private static DateTimeOffset? ToDateTimeOffset(DateTimeOffset value)
    {
        return value == default || value == DateTimeOffset.MinValue ? null : UserTimeZoneService.ToUserTime(value);
    }

    private static DateTimeOffset? Max(DateTimeOffset? a, DateTimeOffset? b)
    {
        if (a == null) return b;
        if (b == null) return a;
        return a.Value.ToUniversalTime() >= b.Value.ToUniversalTime() ? a : b;
    }
}

public sealed class SessionActivitySnapshot
{
    public string Agent { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTimeOffset? LastActivity { get; set; }
    public DateTimeOffset? EffectiveActivity { get; set; }
    public DateTimeOffset? LatestMessageAt { get; set; }
    public int MessageCount { get; set; }
    public int LastMessageIndex { get; set; } = -1;
    public string? LastMessageHash { get; set; }
    public string? StateFileHash { get; set; }
}
