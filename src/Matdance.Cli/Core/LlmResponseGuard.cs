using System.Text;
using System.Text.RegularExpressions;
using Matdance.Cli.Models;

namespace Matdance.Cli.Core;

public static class LlmResponseGuard
{
    private const string UpstreamHighRiskRejection = "The request was rejected because it was considered high risk";
    public const string ThinkingTextToolRequestNotice = "Runtime notice: Plain-text or pseudo tool requests embedded in /think, thinking, or reasoning_content are not executable. Use the real assistant tool-call channel instead. Protocol-level tool_calls returned by the API alongside reasoning_content are supported.";

    private static readonly string[] UpstreamRejectionMessages =
    {
        UpstreamHighRiskRejection
    };

    private static readonly string[] EmptyResponseMessages =
    {
        "(no response)"
    };

    public static bool IsUpstreamRejection(ChatMessage message)
    {
        return message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            && (message.ToolCalls == null || message.ToolCalls.Count == 0)
            && IsUpstreamRejectionContent(message.Content);
    }

    public static bool IsUpstreamRejectionContent(string? content)
    {
        var text = Normalize(content);
        return UpstreamRejectionMessages.Any(item => text.Equals(item, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsNoResponse(ChatMessage message)
    {
        return message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            && (message.ToolCalls == null || message.ToolCalls.Count == 0)
            && (string.IsNullOrWhiteSpace(message.Content) || IsNoResponseContent(message.Content));
    }

    public static bool IsNoResponseContent(string? content)
    {
        var text = Normalize(content);
        return string.IsNullOrEmpty(text)
            || EmptyResponseMessages.Any(item => text.Equals(item, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsTransientAssistantFailure(ChatMessage message)
    {
        return IsUpstreamRejection(message) || IsNoResponse(message);
    }

    public static bool IsSuppressedContent(string? content)
    {
        return IsUpstreamRejectionContent(content) || IsNoResponseContent(content);
    }

    public static bool IsPossibleUpstreamRejectionPrefix(string? content)
    {
        var text = Normalize(content);
        return !string.IsNullOrEmpty(text)
            && UpstreamRejectionMessages.Any(item => item.StartsWith(text, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsPossibleSuppressedPrefix(string? content)
    {
        var text = Normalize(content);
        return !string.IsNullOrEmpty(text)
            && UpstreamRejectionMessages.Concat(EmptyResponseMessages)
                .Any(item => item.StartsWith(text, StringComparison.OrdinalIgnoreCase));
    }

    public static void MarkAsExcluded(ChatMessage message)
    {
        message.MessageType = "upstream_rejection";
        message.IncludeInMainContext = false;
        message.Importance = "transient_error";
    }

    public static void MarkAsNoResponse(ChatMessage message)
    {
        message.MessageType = "no_response";
        message.IncludeInMainContext = false;
        message.Importance = "transient_error";
    }

    public static bool HasTextualToolRequestInThinking(ChatMessage message)
    {
        if (message.ToolCalls is { Count: > 0 })
        {
            return false;
        }

        var reasoning = message.ReasoningContent ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reasoning) && HasThinkingMarker(message.Content))
        {
            reasoning = message.Content ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(reasoning))
        {
            return false;
        }

        return LooksLikeToolRequest(reasoning);
    }

    private static bool HasThinkingMarker(string? content)
    {
        return Regex.IsMatch(content ?? string.Empty, @"(?is)(^|\s)(/think\b|<think\b|thinking\s*:)", RegexOptions.CultureInvariant);
    }

    private static bool LooksLikeToolRequest(string text)
    {
        if (Regex.IsMatch(text, @"(?is)(tool_calls?|function_call|<tool_call\b|</tool_call>|tool_use|调用工具|工具调用)", RegexOptions.CultureInvariant))
        {
            return true;
        }

        return Regex.IsMatch(text, @"(?is)\b(bash|file_(?:read|write|search|trace_open|trace_show|trace_close|write_locks|write_lock_close)|browser_[a-z_]+|save_cookie|apply_cookie|list_cookie_by_site|task_manager|memory_store|skill_create|skill_editor|skill_delete|image_generation(?:_[a-z_]+)?|text_to_speech)\s*\(", RegexOptions.CultureInvariant);
    }

    private static string Normalize(string? content)
    {
        return (content ?? string.Empty).ReplaceLineEndings(" ").Trim().TrimEnd('.', '!', '?');
    }

    public sealed class StreamingFilter
    {
        private readonly StringBuilder _buffer = new();
        private bool _passthrough;

        public void OnChunk(string chunk, Action<string> emit)
        {
            if (string.IsNullOrEmpty(chunk))
            {
                return;
            }

            if (_passthrough)
            {
                emit(chunk);
                return;
            }

            _buffer.Append(chunk);
            var pending = _buffer.ToString();
            if (IsPossibleSuppressedPrefix(pending))
            {
                return;
            }

            _passthrough = true;
            _buffer.Clear();
            emit(pending);
        }

        public void FlushIfAllowed(Action<string> emit)
        {
            if (_passthrough || _buffer.Length == 0)
            {
                return;
            }

            var pending = _buffer.ToString();
            _buffer.Clear();
            _passthrough = true;
            if (!IsSuppressedContent(pending))
            {
                emit(pending);
            }
        }
    }
}
