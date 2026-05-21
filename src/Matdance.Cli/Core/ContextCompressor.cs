using System.Text;
using Matdance.Cli.Models;
using Matdance.Cli.Services;

namespace Matdance.Cli.Core;

public class ContextCompressor
{
    public const string SummaryMessageType = "context_summary";
    public const string HandoffMessageType = "context_handoff";

    private const int ProtectedUserTurns = 3;
    private const int MaxSummaryChars = 30_000;
    private const int MaxSegmentCount = 18;
    private const int ToolResultPreviewChars = 4_000;
    private const int ProtectedToolPreviewChars = 2_000;
    private readonly AgentConfig _config;
    private readonly LlmClient _llm;
    private readonly float _threshold;

    public ContextCompressor(AgentConfig config)
    {
        _config = config;
        _llm = new LlmClient(config);
        _threshold = config.CompressionThreshold > 0 && config.CompressionThreshold <= 0.95f
            ? config.CompressionThreshold
            : 0.7f;
    }

    public int CompressionLimit => Math.Max(1, (int)(_config.ContextWindow * _threshold));

    public bool ShouldCompress(List<ChatMessage> messages)
        => TokenCounter.EstimateMessages(messages) > CompressionLimit;

    public bool ShouldCompressRequest(List<ChatMessage> messages, List<ToolDefinition>? tools = null)
        => EstimateRequestTokens(messages, tools) > CompressionLimit;

    public int EstimateRequestTokens(List<ChatMessage> messages, List<ToolDefinition>? tools = null)
        => TokenCounter.EstimateRequest(messages, tools);

    public async Task<List<ChatMessage>> CompressAsync(List<ChatMessage> messages, CancellationToken ct = default)
    {
        if (!ShouldCompress(messages))
            return new List<ChatMessage>(messages.Select(CloneMessage));

        var result = await CompressConversationAsync(messages, includeHandoff: false, ct);
        return result.Messages;
    }

    public async Task<ContextCompressionResult> CompressRequestAsync(
        List<ChatMessage> requestMessages,
        List<ToolDefinition>? tools,
        CancellationToken ct = default,
        bool force = false)
    {
        var estimated = EstimateRequestTokens(requestMessages, tools);
        if (!force && estimated <= CompressionLimit)
        {
            return new ContextCompressionResult
            {
                Messages = new List<ChatMessage>(requestMessages.Select(CloneMessage)),
                Compressed = false,
                EstimatedTokens = estimated,
                Limit = CompressionLimit
            };
        }

        var fixedSystemMessages = requestMessages
            .Where(message => IsFixedSystemMessage(message))
            .Select(CloneMessage)
            .ToList();
        var conversationMessages = requestMessages
            .Where(message => !IsFixedSystemMessage(message) && !IsOneShotHandoff(message))
            .Select(CloneMessage)
            .ToList();

        if (conversationMessages.Count == 0)
        {
            return new ContextCompressionResult
            {
                Messages = new List<ChatMessage>(requestMessages.Select(CloneMessage)),
                Compressed = false,
                EstimatedTokens = estimated,
                Limit = CompressionLimit
            };
        }

        var compressed = await CompressConversationAsync(conversationMessages, includeHandoff: true, ct);
        var result = new List<ChatMessage>();
        result.AddRange(fixedSystemMessages);
        result.AddRange(compressed.Messages);

        result = DegradeProtectedToolResultsIfNeeded(result, tools);

        return new ContextCompressionResult
        {
            Messages = result,
            Compressed = true,
            UsedHandoff = result.Any(IsOneShotHandoff),
            EstimatedTokens = EstimateRequestTokens(result, tools),
            Limit = CompressionLimit,
            SummaryContent = compressed.SummaryContent,
            HandoffContent = compressed.HandoffContent,
            CompressedSourceMessageCount = compressed.CompressedSourceMessageCount,
            ProtectedMessageCount = compressed.ProtectedMessageCount
        };
    }

    public static bool IsOneShotHandoff(ChatMessage message)
        => string.Equals(message.MessageType, HandoffMessageType, StringComparison.OrdinalIgnoreCase);

    public static ChatMessage SummaryMessage(string content)
    {
        return new ChatMessage
        {
            Role = "system",
            MessageType = SummaryMessageType,
            IncludeInMainContext = true,
            Timestamp = UserTimeZoneService.Now(),
            Content = content
        };
    }

    public static int EstimateObservedContextWindowAfterLimitError(
        AgentConfig config,
        List<ChatMessage> requestMessages,
        List<ToolDefinition>? tools)
    {
        var promptTokens = TokenCounter.EstimateRequest(requestMessages, tools);
        var observed = (int)Math.Floor((promptTokens + Math.Max(0, config.MaxOutputToken)) * 0.9);
        var floor = Math.Max(8192, Math.Min(config.ContextWindow, config.MaxOutputToken + 4096));
        return Math.Clamp(observed, floor, Math.Max(floor, config.ContextWindow - 1));
    }

    private async Task<ContextCompressionResult> CompressConversationAsync(
        List<ChatMessage> messages,
        bool includeHandoff,
        CancellationToken ct)
    {
        var protectedCount = CalculateProtectedCount(messages);
        var protectedMessages = messages.TakeLast(protectedCount).Select(CloneMessage).ToList();
        var compressibleMessages = messages.Take(messages.Count - protectedCount).Select(CloneMessage).ToList();

        if (compressibleMessages.Count == 0)
        {
            return new ContextCompressionResult
            {
                Messages = messages.Select(CloneMessage).ToList(),
                Compressed = false,
                EstimatedTokens = TokenCounter.EstimateMessages(messages),
                Limit = CompressionLimit
            };
        }

        var segmentSummaries = new List<string>();
        var segments = BuildTokenBalancedSegments(compressibleMessages);
        for (var i = 0; i < segments.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var summary = await SummarizeSegmentWithFallbackAsync(segments[i], i + 1, segments.Count, ct);
            segmentSummaries.Add(summary);
        }

        var finalSummary = await MergeSummariesWithFallbackAsync(segmentSummaries, ct);
        var result = new List<ChatMessage>
        {
            SummaryMessage(finalSummary)
        };

        string? handoffContent = null;
        if (includeHandoff)
        {
            var handoff = await CreateHandoffWithFallbackAsync(protectedMessages, ct);
            if (!string.IsNullOrWhiteSpace(handoff))
            {
                handoffContent = handoff;
                result.Add(new ChatMessage
                {
                    Role = "system",
                    MessageType = HandoffMessageType,
                    IncludeInMainContext = false,
                    Timestamp = UserTimeZoneService.Now(),
                    Content = handoff
                });
            }
        }

        result.AddRange(protectedMessages);
        return new ContextCompressionResult
        {
            Messages = result,
            Compressed = true,
            UsedHandoff = includeHandoff,
            EstimatedTokens = TokenCounter.EstimateMessages(result),
            Limit = CompressionLimit,
            SummaryContent = finalSummary,
            HandoffContent = handoffContent,
            CompressedSourceMessageCount = compressibleMessages.Count,
            ProtectedMessageCount = protectedMessages.Count
        };
    }

    private int CalculateProtectedCount(List<ChatMessage> messages)
    {
        var protectedCount = 0;
        var userCount = 0;

        for (var i = messages.Count - 1; i >= 0; i--)
        {
            protectedCount++;
            if (NormalizeRole(messages[i].Role) == "user")
            {
                userCount++;
                if (userCount >= ProtectedUserTurns)
                    break;
            }
        }

        return protectedCount;
    }

    private List<List<ChatMessage>> BuildTokenBalancedSegments(List<ChatMessage> messages)
    {
        var units = GroupIntoProtocolSafeUnits(messages);
        var target = CalculateSegmentTargetTokens();
        var segments = new List<List<ChatMessage>>();
        var current = new List<ChatMessage>();
        var currentTokens = 0;

        foreach (var unit in units)
        {
            var unitTokens = TokenCounter.EstimateMessages(unit);
            if (current.Count > 0 && currentTokens + unitTokens > target)
            {
                segments.Add(current);
                current = new List<ChatMessage>();
                currentTokens = 0;
            }

            current.AddRange(unit.Select(CloneMessage));
            currentTokens += unitTokens;
        }

        if (current.Count > 0)
            segments.Add(current);

        while (segments.Count > MaxSegmentCount)
        {
            var last = segments[^1];
            segments.RemoveAt(segments.Count - 1);
            segments[^1].AddRange(last);
        }

        return segments;
    }

    private int CalculateSegmentTargetTokens()
    {
        var averageThird = Math.Max(2048, _config.ContextWindow / 3);
        var compressionBudget = Math.Max(4096, _config.ContextWindow - _config.MaxOutputToken - 4096);
        return Math.Max(2048, Math.Min(averageThird, (int)(compressionBudget * 0.6)));
    }

    private static List<List<ChatMessage>> GroupIntoProtocolSafeUnits(List<ChatMessage> messages)
    {
        var units = new List<List<ChatMessage>>();
        var current = new List<ChatMessage>();

        for (var i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            var role = NormalizeRole(msg.Role);
            if (role == "user" && current.Count > 0)
            {
                units.Add(current);
                current = new List<ChatMessage>();
            }

            current.Add(CloneMessage(msg));

            if (role == "assistant" && msg.ToolCalls is { Count: > 0 })
            {
                var expectedIds = msg.ToolCalls.Select(call => call.Id).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet(StringComparer.Ordinal);
                while (i + 1 < messages.Count && NormalizeRole(messages[i + 1].Role) == "tool")
                {
                    var toolMessage = messages[i + 1];
                    if (!string.IsNullOrWhiteSpace(toolMessage.ToolCallId) && expectedIds.Count > 0 && !expectedIds.Contains(toolMessage.ToolCallId))
                        break;

                    i++;
                    current.Add(CloneMessage(toolMessage));
                }
            }
        }

        if (current.Count > 0)
            units.Add(current);

        return units;
    }

    private async Task<string> SummarizeSegmentWithFallbackAsync(
        List<ChatMessage> segment,
        int index,
        int total,
        CancellationToken ct,
        int depth = 0)
    {
        var rawContent = BuildRawConversation(segment, ToolResultPreviewChars);
        var prompt = "Summarize this compressed context segment for a future Matdance agent.\n\n" +
            "Preserve important task state, decisions, file paths, tool calls, tool outcomes, user preferences, and unresolved risks. " +
            "You may dilute unimportant small talk, repeated text, and low-value logs. Do not invent facts. " +
            "Use concise bullets.\n\n" +
            $"Segment: {index}/{total}\n\n" +
            rawContent;

        var messages = new List<ChatMessage>
        {
            ChatMessage.System("You are a context compression subagent. Produce compact, faithful summaries. Historical drift is tolerable; recent actionable state and verified details are not."),
            ChatMessage.User(prompt)
        };

        try
        {
            var response = await _llm.SendAsync(messages, new List<ToolDefinition>(), _ => { }, ct, enableThinking: false);
            var content = response.Content?.Trim();
            if (!string.IsNullOrWhiteSpace(content))
                return $"## Segment {index}/{total}\n{TrimChars(content, MaxSummaryChars / Math.Max(1, total))}";
        }
        catch (Exception ex) when (LlmClient.IsContextLimitError(ex) && depth < 4 && segment.Count > 1)
        {
            var splitAt = Math.Max(1, segment.Count / 2);
            var left = segment.Take(splitAt).Select(CloneMessage).ToList();
            var right = segment.Skip(splitAt).Select(CloneMessage).ToList();
            var leftSummary = await SummarizeSegmentWithFallbackAsync(left, index, total, ct, depth + 1);
            var rightSummary = await SummarizeSegmentWithFallbackAsync(right, index, total, ct, depth + 1);
            return leftSummary + "\n\n" + rightSummary;
        }
        catch
        {
            // Fall through to local degradation.
        }

        return BuildLocalSegmentSummary(segment, index, total);
    }

    private async Task<string> MergeSummariesWithFallbackAsync(List<string> segmentSummaries, CancellationToken ct)
    {
        var source = string.Join("\n\n---\n\n", segmentSummaries);
        var prompt = "Merge these compressed segment summaries into one conflict-resolved context summary for the next Matdance request.\n\n" +
            "Rules:\n" +
            "- Keep the final summary under 30000 characters.\n" +
            "- Prefer newer segment facts when summaries conflict.\n" +
            "- Preserve current task state, durable user preferences, file paths, code changes, tool outcomes, and pending next steps.\n" +
            "- Mark uncertain or degraded details as uncertain.\n\n" +
            source;

        var messages = new List<ChatMessage>
        {
            ChatMessage.System("You merge compressed context summaries without adding new facts."),
            ChatMessage.User(prompt)
        };

        try
        {
            var response = await _llm.SendAsync(messages, new List<ToolDefinition>(), _ => { }, ct, enableThinking: false);
            var content = response.Content?.Trim();
            if (!string.IsNullOrWhiteSpace(content))
                return BuildSummaryEnvelope(TrimChars(content, MaxSummaryChars));
        }
        catch
        {
            // Fall through to deterministic merge.
        }

        return BuildSummaryEnvelope(TrimChars(source, MaxSummaryChars));
    }

    private async Task<string> CreateHandoffWithFallbackAsync(List<ChatMessage> protectedMessages, CancellationToken ct)
    {
        if (protectedMessages.Count == 0)
            return string.Empty;

        var raw = BuildRawConversation(protectedMessages, ProtectedToolPreviewChars);
        var prompt = "You are the active Matdance agent immediately before context compression. " +
            "Write a one-use handoff for the next request. Be accurate and operational. " +
            "Focus on what is happening right now, details that must not drift, and the next concrete action. " +
            "Do not say the handoff is incomplete unless it truly is.\n\n" +
            "Format:\n" +
            "## Handoff\n" +
            "- Current user intent:\n" +
            "- Current task state:\n" +
            "- Last verified facts:\n" +
            "- Recent tool/file state:\n" +
            "- Pending next action:\n" +
            "- Risks / uncertain compressed details:\n\n" +
            "Recent protected context:\n" +
            raw;

        var messages = new List<ChatMessage>
        {
            ChatMessage.System("You are Matdance preparing a one-time context handoff. This handoff will appear once and then be discarded."),
            ChatMessage.User(prompt)
        };

        try
        {
            var response = await _llm.SendAsync(messages, new List<ToolDefinition>(), _ => { }, ct, enableThinking: false);
            var content = response.Content?.Trim();
            if (!string.IsNullOrWhiteSpace(content))
                return TrimChars(content, 6_000);
        }
        catch
        {
            // Fall through to local handoff.
        }

        return "## Handoff\n" +
            "- Current user intent: Continue from the latest protected messages.\n" +
            "- Current task state: See the recent uncompressed user/assistant/tool messages below.\n" +
            "- Last verified facts: Recent tool results and live file state remain authoritative.\n" +
            "- Recent tool/file state: Preserved in the following protected messages when available.\n" +
            "- Pending next action: Continue the current task without repeating completed work.\n" +
            "- Risks / uncertain compressed details: Older details came from compressed summaries and may need verification.";
    }

    private static string BuildSummaryEnvelope(string content)
    {
        return "[COMPRESSED CONTEXT SUMMARY]\n\n" +
            content.Trim() +
            "\n\n[NOTE] This is a lossy compressed summary of older conversation history. Verify old file states, exact code, and factual details before relying on them.";
    }

    private static string BuildRawConversation(List<ChatMessage> messages, int toolResultLimit)
    {
        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            switch (NormalizeRole(msg.Role))
            {
                case "system":
                    sb.AppendLine($"[system/{msg.MessageType ?? "context"}]: {TrimChars(msg.Content, 3000)}");
                    break;
                case "user":
                    sb.AppendLine($"[user]: {TrimChars(msg.Content, 6000)}");
                    AppendAttachmentSummary(sb, msg);
                    break;
                case "assistant":
                    if (msg.ToolCalls is { Count: > 0 })
                    {
                        var toolNames = string.Join(", ", msg.ToolCalls.Select(t => t.Function.Name));
                        sb.AppendLine($"[assistant tools={toolNames}]: {TrimChars(msg.Content, 4000)}");
                        foreach (var call in msg.ToolCalls)
                        {
                            sb.AppendLine($"  tool_call {call.Id}: {call.Function.Name} {TrimChars(call.Function.Arguments, 2000)}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"[assistant]: {TrimChars(msg.Content, 6000)}");
                    }
                    break;
                case "tool":
                    sb.AppendLine($"[tool result {msg.ToolCallId ?? "unknown"}]: {TrimChars(msg.Content, toolResultLimit)}");
                    break;
            }
            sb.AppendLine("---");
        }

        return sb.ToString();
    }

    private static void AppendAttachmentSummary(StringBuilder sb, ChatMessage msg)
    {
        if (msg.Attachments is not { Count: > 0 })
            return;

        foreach (var attachment in msg.Attachments)
        {
            sb.AppendLine($"  attachment: {attachment.Name} ({attachment.Kind}, {attachment.MimeType}) {attachment.RelativePath} {TrimChars(attachment.Summary, 500)}");
        }
    }

    private static string BuildLocalSegmentSummary(List<ChatMessage> segment, int index, int total)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Segment {index}/{total} (degraded local summary)");
        foreach (var msg in segment)
        {
            switch (NormalizeRole(msg.Role))
            {
                case "user":
                    sb.AppendLine($"- User: {TrimChars(msg.Content, 500)}");
                    break;
                case "assistant":
                    var tools = msg.ToolCalls is { Count: > 0 }
                        ? $" tools={string.Join(", ", msg.ToolCalls.Select(t => t.Function.Name))}"
                        : "";
                    sb.AppendLine($"- Assistant{tools}: {TrimChars(msg.Content, 500)}");
                    break;
                case "tool":
                    sb.AppendLine($"- Tool {msg.ToolCallId ?? "unknown"}: {TrimChars(msg.Content, 500)}");
                    break;
                case "system":
                    sb.AppendLine($"- System/{msg.MessageType ?? "context"}: {TrimChars(msg.Content, 500)}");
                    break;
            }
        }

        return sb.ToString();
    }

    private List<ChatMessage> DegradeProtectedToolResultsIfNeeded(List<ChatMessage> messages, List<ToolDefinition>? tools)
    {
        var result = messages.Select(CloneMessage).ToList();
        var limit = CompressionLimit;
        if (EstimateRequestTokens(result, tools) <= limit)
            return result;

        foreach (var tool in result.Where(message => NormalizeRole(message.Role) == "tool").ToList())
        {
            if (tool.Content.Length <= ProtectedToolPreviewChars)
                continue;

            tool.Content = "[compressed protected tool result preview]\n" + TrimChars(tool.Content, ProtectedToolPreviewChars);
            if (EstimateRequestTokens(result, tools) <= limit)
                break;
        }

        var summary = result.FirstOrDefault(message => string.Equals(message.MessageType, SummaryMessageType, StringComparison.OrdinalIgnoreCase));
        if (summary != null && EstimateRequestTokens(result, tools) > limit)
        {
            summary.Content = TrimChars(summary.Content, Math.Max(4000, MaxSummaryChars / 2));
        }

        return result;
    }

    private static bool IsFixedSystemMessage(ChatMessage message)
    {
        return NormalizeRole(message.Role) == "system"
            && !string.Equals(message.MessageType, SummaryMessageType, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(message.MessageType, HandoffMessageType, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRole(string? role) => (role ?? "user").Trim().ToLowerInvariant();

    private static ChatMessage CloneMessage(ChatMessage msg)
    {
        return new ChatMessage
        {
            Role = msg.Role,
            Content = msg.Content,
            ReasoningContent = msg.ReasoningContent,
            ToolCalls = msg.ToolCalls,
            ToolCallId = msg.ToolCallId,
            MessageType = msg.MessageType,
            IncludeInMainContext = msg.IncludeInMainContext,
            Importance = msg.Importance,
            Timestamp = msg.Timestamp,
            Audio = msg.Audio,
            Attachments = msg.Attachments
        };
    }

    private static string TrimChars(string? value, int maxChars)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (value.Length <= maxChars)
            return value;
        return value[..Math.Max(0, maxChars)] + "\n...[truncated]";
    }
}

public sealed class ContextCompressionResult
{
    public List<ChatMessage> Messages { get; init; } = new();
    public bool Compressed { get; init; }
    public bool UsedHandoff { get; init; }
    public int EstimatedTokens { get; init; }
    public int Limit { get; init; }
    public string? SummaryContent { get; init; }
    public string? HandoffContent { get; init; }
    public int CompressedSourceMessageCount { get; init; }
    public int ProtectedMessageCount { get; init; }
}
