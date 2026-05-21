using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Matdance.Cli.Models;
using Matdance.Cli.Services;

namespace Matdance.Cli.Core;

public class LlmClient
{
    private readonly HttpClient _http = new();
    private readonly AgentConfig _config;
    private static readonly ConcurrentDictionary<string, bool> EndpointsWithoutClientHeaders = new(StringComparer.OrdinalIgnoreCase);
    private const bool ThinkingTemporarilyDisabled = true;
    private const string ClientUserAgent = "Matdance/1.1.21-preview";
    private const string ClientName = "Matdance";
    private const int MaxLoggedRequestChars = 20_000;
    private const int ImagePayloadRetryCutoffAttempt = 3;
    private static readonly TimeSpan StreamTransportIdleTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StreamUsefulOutputTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan StreamResponseTimeout = TimeSpan.FromMinutes(3);

    public LlmClient(AgentConfig config)
    {
        _config = config;
        _http.Timeout = TimeSpan.FromMinutes(3);
    }

    public async Task<ChatMessage> SendAsync(
        List<ChatMessage> messages,
        List<ToolDefinition>? tools = null,
        Action<string>? onStreamChunk = null,
        CancellationToken ct = default,
        Func<int, TimeSpan, Exception, CancellationToken, Task>? beforeRetryDelay = null,
        Action<string>? onReasoningChunk = null,
        bool? enableThinking = null)
    {
        if (IsAnthropicApi())
        {
            return await SendAnthropicAsync(messages, tools, onStreamChunk, ct, beforeRetryDelay, onReasoningChunk, enableThinking);
        }
        return await SendOpenAiAsync(messages, tools, onStreamChunk, ct, beforeRetryDelay, onReasoningChunk, enableThinking);
    }

    public static bool IsContextLimitError(Exception ex)
    {
        var message = ex.Message ?? string.Empty;
        if (ex is HttpRequestException httpEx)
        {
            var status = httpEx.StatusCode.HasValue ? (int)httpEx.StatusCode.Value : 0;
            if (status == 413)
                return true;
        }

        var needles = new[]
        {
            "context_length_exceeded",
            "context length",
            "maximum context",
            "max context",
            "context window",
            "too many tokens",
            "token limit",
            "tokens exceed",
            "input is too long",
            "prompt is too long",
            "request too large"
        };

        return needles.Any(needle => message.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ChatMessage> SendOpenAiAsync(
        List<ChatMessage> messages,
        List<ToolDefinition>? tools,
        Action<string>? onStreamChunk,
        CancellationToken ct,
        Func<int, TimeSpan, Exception, CancellationToken, Task>? beforeRetryDelay,
        Action<string>? onReasoningChunk,
        bool? enableThinking)
    {
        var url = GetOpenAiChatCompletionsUrl();
        var thinkingMode = GetThinkingParameterMode();
        var supportsThinking = SupportsThinkingModel();
        var forceMimoThinking = IsMimoModel();
        var thinkingEnabled = supportsThinking && (forceMimoThinking || (!ThinkingTemporarilyDisabled && (enableThinking ?? true)));
        var requestTemperature = GetRequestTemperature(thinkingMode, thinkingEnabled);
        var includeReasoningContent = forceMimoThinking
            || (!ThinkingTemporarilyDisabled && (thinkingEnabled || messages.Any(message => !string.IsNullOrWhiteSpace(message.ReasoningContent))));
        var hasImageAttachments = messages.Any(HasImageAttachment);
        var cachedVisionSupport = hasImageAttachments ? ModelCapabilityCacheService.GetVisionSupport(_config) : null;
        var includeImageAttachments = hasImageAttachments && cachedVisionSupport != false;
        var payloadMessages = hasImageAttachments && !includeImageAttachments
            ? BuildImageUnavailableMessages(messages, "Matdance previously detected that this provider/model should be treated as text-only for image attachments. Continue without image pixels and tell the user plainly if the task required visual inspection.")
            : messages;
        string? ambiguousImagePayloadFailure = null;
        var headerKey = GetHeaderCompatibilityKey();
        var includeClientHeaders = !EndpointsWithoutClientHeaders.ContainsKey(headerKey);

        var maxAttempts = ReconnectRetryPolicy.TotalAttempts;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var payload = CreateOpenAiPayload(
                payloadMessages,
                tools,
                includeReasoningContent,
                thinkingMode,
                supportsThinking,
                thinkingEnabled,
                requestTemperature,
                includeImageAttachments,
                forceAssistantReasoningContent: forceMimoThinking,
                stream: onStreamChunk != null);
            var json = payload.ToJsonString();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                if (includeClientHeaders)
                    ApplyCommonHeaders(request);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

                if (onStreamChunk != null)
                {
                    request.Headers.TryAddWithoutValidation("Accept", "text/event-stream");
                    var response = await StreamOpenAiAsync(request, onStreamChunk, onReasoningChunk, collectReasoning: includeReasoningContent, ct: ct);
                    RecordImagePayloadOutcome(hasImageAttachments, includeImageAttachments, ambiguousImagePayloadFailure);
                    return response;
                }
                else
                {
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    var response = await SendOpenAiNonStreamingAsync(request, collectReasoning: includeReasoningContent, ct: ct);
                    RecordImagePayloadOutcome(hasImageAttachments, includeImageAttachments, ambiguousImagePayloadFailure);
                    return response;
                }
            }
            catch (HttpRequestException ex) when (includeImageAttachments && IsImagePayloadRejected(ex) && !ct.IsCancellationRequested)
            {
                ModelCapabilityCacheService.RecordVisionUnsupported(_config, ex.Message);
                includeImageAttachments = false;
                payloadMessages = BuildImageUnavailableMessages(messages, "The previous request included image attachments, but the current model/API rejected multimodal image input. Continue without image pixels. Tell the user plainly that this model cannot view the image attachment in this turn; use only file names, paths, metadata, or user-provided descriptions, and do not pretend to have seen the image.");
                LogRequestError(json, ex, $"Attempt {attempt}/{maxAttempts} rejected image content. Retrying once without image payload and with a model-visible attachment limitation notice.");
                continue;
            }
            catch (Exception ex) when (includeImageAttachments && IsRetryable(ex) && attempt <= ImagePayloadRetryCutoffAttempt && !ct.IsCancellationRequested)
            {
                ambiguousImagePayloadFailure = ex.Message;
                includeImageAttachments = false;
                payloadMessages = BuildImageUnavailableMessages(messages, "The previous request included image attachments, but the image payload failed before Matdance received a usable answer. Continue without image pixels for stability. Tell the user plainly if the task required actual visual inspection; use only file names, paths, metadata, or user-provided descriptions, and do not pretend to have seen the image.");
                LogRequestError(json, ex, $"Attempt {attempt}/{maxAttempts} failed while image payload was attached. Retrying immediately without image payload before entering the ordinary LLM retry chain.");
                continue;
            }
            catch (Exception ex) when (IsRetryable(ex) && attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                if (includeClientHeaders && IsNoResponseFailure(ex))
                {
                    includeClientHeaders = false;
                    EndpointsWithoutClientHeaders[headerKey] = true;
                    LogRequestError(json, ex, $"Attempt {attempt}/{maxAttempts} failed with no response. Retrying without Matdance client headers.");
                    continue;
                }

                var retryStep = ReconnectRetryPolicy.GetStepAfterFailure(attempt) ?? throw new InvalidOperationException("Reconnect retry budget was exhausted.");
                var delay = retryStep.Delay;
                LogRequestError(json, ex, $"Attempt {attempt}/{maxAttempts} failed. {ReconnectRetryPolicy.Describe(retryStep)}");
                if (beforeRetryDelay != null)
                {
                    await beforeRetryDelay(attempt, delay, ex, ct);
                }
                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }

        // Should never reach here; last attempt throws on failure
        throw new InvalidOperationException("Max retries exceeded");
    }

    private static bool IsRetryable(Exception ex)
    {
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode != null)
            {
                var status = (int)httpEx.StatusCode.Value;
                return status == 408 || status == 409 || status == 429 || status >= 500;
            }

            return true;
        }

        return ex is IOException
            || ex is TaskCanceledException
            || ex is TimeoutException;
    }

    private async Task<ChatMessage> SendOpenAiNonStreamingAsync(HttpRequestMessage request, bool collectReasoning, CancellationToken ct)
    {
        var response = await _http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"LLM API error: {(int)response.StatusCode} - {responseBody}", null, response.StatusCode);
        }

        var doc = JsonDocument.Parse(responseBody);
        var choice = doc.RootElement.GetProperty("choices")[0];
        var msg = choice.GetProperty("message");
        var role = msg.GetProperty("role").GetString() ?? "assistant";
        var content = msg.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
        var reasoningContent = collectReasoning ? ExtractReasoningText(msg) : null;
        var toolCalls = ParseToolCalls(msg);
        return new ChatMessage { Role = role, Content = content, ReasoningContent = reasoningContent, ToolCalls = toolCalls, Timestamp = UserTimeZoneService.Now() };
    }

    private static JsonArray ToOpenAiMessagesNode(IEnumerable<ChatMessage> messages, bool includeReasoningContent, bool includeImageAttachments = false, bool forceAssistantReasoningContent = false)
    {
        var result = new JsonArray();
        var list = messages.ToList();
        var lastImageAttachmentMessageIndex = includeImageAttachments
            ? list.FindLastIndex(message =>
                NormalizeRole(message.Role).Equals("user", StringComparison.OrdinalIgnoreCase)
                && HasImageAttachment(message))
            : -1;
        for (var i = 0; i < list.Count; i++)
        {
            var message = list[i];
            var role = NormalizeRole(message.Role);
            var attachImages = i == lastImageAttachmentMessageIndex;

            if (role.Equals("tool", StringComparison.OrdinalIgnoreCase))
            {
                // OpenAI-compatible APIs reject orphan tool messages. Tool results
                // are emitted only as part of a complete assistant tool-call block.
                continue;
            }

            if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && message.ToolCalls is { Count: > 0 })
            {
                if (TryCollectCompleteToolResultBlock(list, i, out var toolMessages, out var nextIndex))
                {
                    result.Add(CreateOpenAiMessageObject(message, includeReasoningContent, includeToolCalls: true, includeImageAttachments: false, forceAssistantReasoningContent));
                    foreach (var toolMessage in toolMessages)
                    {
                        result.Add(CreateOpenAiMessageObject(toolMessage, includeReasoningContent, includeToolCalls: false, includeImageAttachments: false, forceAssistantReasoningContent));
                    }
                    i = nextIndex - 1;
                    continue;
                }

                result.Add(CreateHistoricalAssistantWithoutToolCalls(message, includeReasoningContent, forceAssistantReasoningContent));
                continue;
            }

            result.Add(CreateOpenAiMessageObject(message, includeReasoningContent, includeToolCalls: false, includeImageAttachments: attachImages, forceAssistantReasoningContent));
        }

        return result;
    }

    private static bool TryCollectCompleteToolResultBlock(IReadOnlyList<ChatMessage> messages, int assistantIndex, out List<ChatMessage> toolMessages, out int nextIndex)
    {
        toolMessages = new List<ChatMessage>();
        nextIndex = assistantIndex + 1;
        var toolCalls = messages[assistantIndex].ToolCalls ?? new List<ToolCall>();
        var expectedIds = toolCalls
            .Select(item => item.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        if (toolCalls.Count == 0 || expectedIds.Count != toolCalls.Count)
        {
            return false;
        }

        while (nextIndex < messages.Count && NormalizeRole(messages[nextIndex].Role).Equals("tool", StringComparison.OrdinalIgnoreCase))
        {
            toolMessages.Add(messages[nextIndex]);
            nextIndex++;
        }

        if (toolMessages.Count == 0)
        {
            return false;
        }

        var actualIds = toolMessages
            .Select(item => item.ToolCallId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.Ordinal);

        return expectedIds.SetEquals(actualIds)
            && toolMessages.All(item => !string.IsNullOrWhiteSpace(item.ToolCallId) && expectedIds.Contains(item.ToolCallId));
    }

    private static JsonObject CreateHistoricalAssistantWithoutToolCalls(ChatMessage message, bool includeReasoningContent, bool forceAssistantReasoningContent)
    {
        var clone = new ChatMessage
        {
            Role = "assistant",
            Content = string.IsNullOrWhiteSpace(message.Content)
                ? "(historical tool call omitted because matching tool result messages were unavailable)"
                : message.Content,
            ReasoningContent = message.ReasoningContent
        };
        return CreateOpenAiMessageObject(clone, includeReasoningContent, includeToolCalls: false, includeImageAttachments: false, forceAssistantReasoningContent);
    }

    private static JsonObject CreateOpenAiMessageObject(ChatMessage message, bool includeReasoningContent, bool includeToolCalls, bool includeImageAttachments, bool forceAssistantReasoningContent)
    {
        var role = NormalizeRole(message.Role);
        var item = new JsonObject
        {
            ["role"] = role
        };

        var contentText = BuildMessageTextForModel(message, includeImageAttachments);
        if (includeImageAttachments && role.Equals("user", StringComparison.OrdinalIgnoreCase))
        {
            item["content"] = BuildUserMultimodalContent(message, contentText);
        }
        else
        {
            item["content"] = contentText;
        }

        if (includeToolCalls && role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && message.ToolCalls is { Count: > 0 })
        {
            item["tool_calls"] = JsonSerializer.SerializeToNode(message.ToolCalls);
        }

        if (includeReasoningContent
            && role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            && (forceAssistantReasoningContent || !string.IsNullOrWhiteSpace(message.ReasoningContent) || (includeToolCalls && message.ToolCalls is { Count: > 0 })))
        {
            item["reasoning_content"] = message.ReasoningContent ?? string.Empty;
        }

        if (role.Equals("tool", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(message.ToolCallId))
        {
            item["tool_call_id"] = message.ToolCallId;
        }

        return item;
    }

    private static string BuildMessageTextForModel(ChatMessage message, bool imagePayloadIncluded)
    {
        var content = message.Content ?? string.Empty;
        if (message.Attachments is not { Count: > 0 })
            return content;

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(content))
        {
            sb.AppendLine(content.TrimEnd());
            sb.AppendLine();
        }

        sb.AppendLine("Attached files for this message:");
        foreach (var attachment in message.Attachments)
        {
            var imageState = attachment.Kind.Equals("image", StringComparison.OrdinalIgnoreCase)
                ? imagePayloadIncluded ? "image payload included in this request" : "image payload not included; use metadata/path only"
                : "use file tools when content inspection is needed";
            sb.AppendLine($"- {attachment.Name} ({attachment.Kind}, {attachment.MimeType}, {FormatBytes(attachment.Size)}): {attachment.Summary}");
            sb.AppendLine($"  workspace path: {attachment.Path}");
            sb.AppendLine($"  relative path: {attachment.RelativePath}");
            sb.AppendLine($"  access note: {imageState}");
        }

        return sb.ToString().TrimEnd();
    }

    private static JsonArray BuildUserMultimodalContent(ChatMessage message, string contentText)
    {
        var content = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "text",
                ["text"] = contentText
            }
        };

        foreach (var attachment in message.Attachments ?? new List<ChatAttachment>())
        {
            if (!attachment.Kind.Equals("image", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrWhiteSpace(attachment.Path) || !File.Exists(attachment.Path))
                continue;

            try
            {
                var bytes = File.ReadAllBytes(attachment.Path);
                var mime = string.IsNullOrWhiteSpace(attachment.MimeType) ? "image/png" : attachment.MimeType;
                content.Add(new JsonObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JsonObject
                    {
                        ["url"] = $"data:{mime};base64,{Convert.ToBase64String(bytes)}"
                    }
                });
            }
            catch
            {
                // The text metadata still tells the model the attachment exists.
            }
        }

        return content;
    }

    private static bool HasImageAttachment(ChatMessage message)
        => message.Attachments?.Any(attachment => attachment.Kind.Equals("image", StringComparison.OrdinalIgnoreCase)) == true;

    private void RecordImagePayloadOutcome(bool hasImageAttachments, bool includeImageAttachments, string? ambiguousImagePayloadFailure)
    {
        if (!hasImageAttachments)
            return;

        if (includeImageAttachments)
        {
            ModelCapabilityCacheService.RecordVisionSupported(_config);
            return;
        }

        if (!string.IsNullOrWhiteSpace(ambiguousImagePayloadFailure))
        {
            ModelCapabilityCacheService.RecordAmbiguousImageFailureWithTextOnlySuccess(_config, ambiguousImagePayloadFailure);
        }
    }

    private static List<ChatMessage> BuildImageUnavailableMessages(List<ChatMessage> messages, string notice)
    {
        var result = new List<ChatMessage>(messages.Count + 1);
        var inserted = false;
        for (var i = 0; i < messages.Count; i++)
        {
            result.Add(messages[i]);
            if (!inserted && NormalizeRole(messages[i].Role).Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(ChatMessage.System(notice));
                inserted = true;
            }
        }

        if (!inserted)
        {
            result.Insert(0, ChatMessage.System(notice));
        }

        return result;
    }

    private static bool IsImagePayloadRejected(HttpRequestException ex)
    {
        var text = ex.Message.ToLowerInvariant();
        return text.Contains("image", StringComparison.Ordinal)
            || text.Contains("multimodal", StringComparison.Ordinal)
            || text.Contains("multi-modal", StringComparison.Ordinal)
            || text.Contains("vision", StringComparison.Ordinal)
            || text.Contains("image_url", StringComparison.Ordinal)
            || text.Contains("input_image", StringComparison.Ordinal)
            || text.Contains("non-text", StringComparison.Ordinal)
            || text.Contains("non text", StringComparison.Ordinal)
            || text.Contains("content part", StringComparison.Ordinal)
            || text.Contains("content parts", StringComparison.Ordinal)
            || text.Contains("content type", StringComparison.Ordinal)
            || text.Contains("content_type", StringComparison.Ordinal);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return bytes + " B";
        if (bytes < 1024 * 1024)
            return (bytes / 1024d).ToString("0.#") + " KB";
        return (bytes / 1024d / 1024d).ToString("0.#") + " MB";
    }

    private static string NormalizeRole(string? role)
    {
        return string.IsNullOrWhiteSpace(role) ? "user" : role;
    }

    private void ApplyThinkingOptions(JsonObject payload, ThinkingParameterMode mode, bool supportsThinking, bool thinkingEnabled)
    {
        if (!supportsThinking)
        {
            return;
        }

        if (mode == ThinkingParameterMode.KimiPreservedObject)
        {
            payload["thinking"] = thinkingEnabled
                ? new JsonObject
                {
                    ["type"] = "enabled",
                    ["keep"] = "all"
                }
                : new JsonObject
                {
                    ["type"] = "disabled"
                };
        }
        else if (mode == ThinkingParameterMode.TypeObject)
        {
            payload["thinking"] = new JsonObject { ["type"] = thinkingEnabled ? "enabled" : "disabled" };
            if (thinkingEnabled)
            {
                payload["reasoning_effort"] = "high";
            }
        }
        else if (thinkingEnabled)
        {
            payload["reasoning_effort"] = "high";
        }
    }

    private float GetRequestTemperature(ThinkingParameterMode mode, bool thinkingEnabled)
    {
        if (mode == ThinkingParameterMode.KimiPreservedObject)
        {
            return thinkingEnabled ? 1.0f : 0.6f;
        }

        return _config.Temperature;
    }

    private async Task<ChatMessage> StreamOpenAiAsync(HttpRequestMessage request, Action<string> onChunk, Action<string>? onReasoningChunk, bool collectReasoning, CancellationToken ct)
    {
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"LLM API error: {(int)response.StatusCode} - {err}", null, response.StatusCode);
        }

        var contentBuilder = new StringBuilder();
        var reasoningBuilder = new StringBuilder();
        var toolCallDict = new Dictionary<int, ToolCallAccumulator>();

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var startedAt = DateTimeOffset.UtcNow;
            var lastUsefulOutputAt = startedAt;

            while (!ct.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await ReadStreamingLineAsync(reader, ct, startedAt, lastUsefulOutputAt, "OpenAI-compatible");
                }
                catch (IOException)
                {
                    // Stream closed prematurely, use what we have
                    break;
                }
                if (line == null)
                    break;

                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
                var data = line[6..];
                if (data == "[DONE]") break;

                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var choice = doc.RootElement.GetProperty("choices")[0];
                    var delta = choice.GetProperty("delta");

                    if (delta.TryGetProperty("content", out var contentNode) && contentNode.ValueKind != JsonValueKind.Null)
                    {
                        var chunk = contentNode.GetString() ?? "";
                        if (chunk.Length > 0)
                        {
                            lastUsefulOutputAt = DateTimeOffset.UtcNow;
                            contentBuilder.Append(chunk);
                            onChunk(chunk);
                        }
                    }

                    var reasoningChunk = collectReasoning ? ExtractReasoningText(delta) : null;
                    if (!string.IsNullOrEmpty(reasoningChunk))
                    {
                        lastUsefulOutputAt = DateTimeOffset.UtcNow;
                        reasoningBuilder.Append(reasoningChunk);
                        onReasoningChunk?.Invoke(reasoningChunk);
                    }

                    if (delta.TryGetProperty("tool_calls", out var tcArray) && tcArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tc in tcArray.EnumerateArray())
                        {
                            if (tc.ValueKind != JsonValueKind.Object)
                                continue;

                            var idx = TryGetInt(tc, "index") ?? toolCallDict.Count;
                            if (!toolCallDict.TryGetValue(idx, out var acc))
                            {
                                acc = new ToolCallAccumulator();
                                toolCallDict[idx] = acc;
                            }

                            var id = TryGetString(tc, "id");
                            if (!string.IsNullOrEmpty(id))
                            {
                                lastUsefulOutputAt = DateTimeOffset.UtcNow;
                                acc.Id = id;
                            }

                            var type = TryGetString(tc, "type");
                            if (!string.IsNullOrEmpty(type))
                            {
                                lastUsefulOutputAt = DateTimeOffset.UtcNow;
                                acc.Type = type;
                            }

                            if (tc.TryGetProperty("function", out var fnNode) && fnNode.ValueKind == JsonValueKind.Object)
                            {
                                var name = TryGetString(fnNode, "name");
                                if (!string.IsNullOrEmpty(name))
                                {
                                    lastUsefulOutputAt = DateTimeOffset.UtcNow;
                                    acc.Name += name;
                                }

                                var arguments = TryGetString(fnNode, "arguments");
                                if (!string.IsNullOrEmpty(arguments))
                                {
                                    lastUsefulOutputAt = DateTimeOffset.UtcNow;
                                    acc.Arguments += arguments;
                                }
                            }
                        }
                    }
                }
                catch { /* ignore malformed SSE lines */ }
            }
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            // For IOException during stream reading, just return what we have
            if (ex is IOException)
            {
                // Fall through to return partial content
            }
            else
            {
                throw;
            }
        }

        var toolCalls = toolCallDict.Count > 0
            ? toolCallDict.OrderBy(kv => kv.Key).Select(kv => new ToolCall
            {
                Id = kv.Value.Id,
                Type = kv.Value.Type,
                Function = new ToolFunction
                {
                    Name = kv.Value.Name,
                    Arguments = kv.Value.Arguments
                }
            }).ToList()
            : null;

        return new ChatMessage
        {
            Role = "assistant",
            Content = contentBuilder.ToString(),
            ReasoningContent = reasoningBuilder.Length > 0 ? reasoningBuilder.ToString() : null,
            ToolCalls = toolCalls,
            Timestamp = UserTimeZoneService.Now()
        };
    }

    private static void LogRequestError(string requestJson, Exception ex, string? context = null)
    {
        try
        {
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "matdance_request.log");
            var sb = new StringBuilder();
            sb.AppendLine($"[{UserTimeZoneService.Now():yyyy-MM-dd HH:mm:ss zzz}] Exception: {ex.GetType().FullName}");
            if (!string.IsNullOrEmpty(context))
                sb.AppendLine($"Context: {context}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine();
            sb.AppendLine("Request JSON:");
            sb.AppendLine(TrimForLog(requestJson));
            sb.AppendLine();
            sb.AppendLine("StackTrace:");
            sb.AppendLine(ex.StackTrace);
            File.AppendAllText(logPath, sb.ToString());
            File.AppendAllText(logPath, Environment.NewLine + new string('-', 80) + Environment.NewLine);
        }
        catch { /* ignore logging errors */ }
    }

    private static string TrimForLog(string value)
    {
        if (value.Length <= MaxLoggedRequestChars)
            return value;

        return value[..MaxLoggedRequestChars] + $"\n...[truncated {value.Length - MaxLoggedRequestChars} chars]";
    }

    private async Task<ChatMessage> SendAnthropicAsync(
        List<ChatMessage> messages,
        List<ToolDefinition>? tools,
        Action<string>? onStreamChunk,
        CancellationToken ct,
        Func<int, TimeSpan, Exception, CancellationToken, Task>? beforeRetryDelay,
        Action<string>? onReasoningChunk,
        bool? enableThinking)
    {
        var endpointCandidates = GetAnthropicMessagesUrlCandidates();
        var endpointIndex = 0;
        var hasImageAttachments = messages.Any(HasImageAttachment);
        var cachedVisionSupport = hasImageAttachments ? ModelCapabilityCacheService.GetVisionSupport(_config) : null;
        var includeImageAttachments = hasImageAttachments && cachedVisionSupport != false;
        var payloadMessages = hasImageAttachments && !includeImageAttachments
            ? BuildImageUnavailableMessages(messages, "Matdance previously detected that this Anthropic provider/model should be treated as text-only for image attachments. Continue without image pixels and tell the user plainly if the task required visual inspection.")
            : messages;
        var collectReasoning = !ThinkingTemporarilyDisabled && (enableThinking ?? true);
        string? ambiguousImagePayloadFailure = null;
        var headerKey = GetHeaderCompatibilityKey();
        var includeClientHeaders = !EndpointsWithoutClientHeaders.ContainsKey(headerKey);

        var maxAttempts = ReconnectRetryPolicy.TotalAttempts;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var payload = CreateAnthropicPayload(
                payloadMessages,
                tools,
                includeImageAttachments,
                stream: onStreamChunk != null);
            var json = payload.ToJsonString();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpointCandidates[endpointIndex])
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                if (includeClientHeaders)
                    ApplyCommonHeaders(request);
                request.Headers.TryAddWithoutValidation("x-api-key", _config.ApiKey);
                if (UsesBearerAuthForAnthropicCompatibility())
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
                request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");

                if (onStreamChunk != null)
                {
                    request.Headers.TryAddWithoutValidation("Accept", "text/event-stream");
                    var response = await StreamAnthropicAsync(request, onStreamChunk, collectReasoning ? onReasoningChunk : null, collectReasoning, ct);
                    RecordImagePayloadOutcome(hasImageAttachments, includeImageAttachments, ambiguousImagePayloadFailure);
                    return response;
                }
                else
                {
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    var response = await SendAnthropicNonStreamingAsync(request, collectReasoning, ct);
                    RecordImagePayloadOutcome(hasImageAttachments, includeImageAttachments, ambiguousImagePayloadFailure);
                    return response;
                }
            }
            catch (HttpRequestException ex) when (IsAnthropicEndpointNotFound(ex) && endpointIndex + 1 < endpointCandidates.Count && !ct.IsCancellationRequested)
            {
                var failedEndpoint = endpointCandidates[endpointIndex];
                endpointIndex++;
                LogRequestError(json, ex, $"Attempt {attempt}/{maxAttempts} could not find Anthropic-compatible endpoint {failedEndpoint}. Retrying with {endpointCandidates[endpointIndex]}.");
                continue;
            }
            catch (HttpRequestException ex) when (IsAnthropicEndpointNotFound(ex) && !ct.IsCancellationRequested)
            {
                ModelCapabilityCacheService.RecordAnthropicMessagesEndpointNotFound(_config, ex.Message);
                throw;
            }
            catch (HttpRequestException ex) when (includeImageAttachments && IsImagePayloadRejected(ex) && !ct.IsCancellationRequested)
            {
                ModelCapabilityCacheService.RecordVisionUnsupported(_config, ex.Message);
                includeImageAttachments = false;
                payloadMessages = BuildImageUnavailableMessages(messages, "The previous request included image attachments, but Anthropic rejected multimodal image input for this provider/model. Continue without image pixels. Tell the user plainly that this model cannot view the image attachment in this turn; use only file names, paths, metadata, or user-provided descriptions, and do not pretend to have seen the image.");
                LogRequestError(json, ex, $"Attempt {attempt}/{maxAttempts} rejected image content. Retrying once without image payload and with a model-visible attachment limitation notice.");
                continue;
            }
            catch (Exception ex) when (includeImageAttachments && IsRetryable(ex) && attempt <= ImagePayloadRetryCutoffAttempt && !ct.IsCancellationRequested)
            {
                ambiguousImagePayloadFailure = ex.Message;
                includeImageAttachments = false;
                payloadMessages = BuildImageUnavailableMessages(messages, "The previous request included image attachments, but the image payload failed before Matdance received a usable Anthropic answer. Continue without image pixels for stability. Tell the user plainly if the task required actual visual inspection; use only file names, paths, metadata, or user-provided descriptions, and do not pretend to have seen the image.");
                LogRequestError(json, ex, $"Attempt {attempt}/{maxAttempts} failed while image payload was attached. Retrying immediately without image payload before entering the ordinary LLM retry chain.");
                continue;
            }
            catch (Exception ex) when (IsRetryable(ex) && attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                if (includeClientHeaders && IsNoResponseFailure(ex))
                {
                    includeClientHeaders = false;
                    EndpointsWithoutClientHeaders[headerKey] = true;
                    LogRequestError(json, ex, $"Attempt {attempt}/{maxAttempts} failed with no response. Retrying without Matdance client headers.");
                    continue;
                }

                var retryStep = ReconnectRetryPolicy.GetStepAfterFailure(attempt) ?? throw new InvalidOperationException("Reconnect retry budget was exhausted.");
                var delay = retryStep.Delay;
                LogRequestError(json, ex, $"Attempt {attempt}/{maxAttempts} failed. {ReconnectRetryPolicy.Describe(retryStep)}");
                if (beforeRetryDelay != null)
                {
                    await beforeRetryDelay(attempt, delay, ex, ct);
                }
                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }

        throw new InvalidOperationException("Max retries exceeded");
    }

    private JsonObject CreateAnthropicPayload(
        IReadOnlyList<ChatMessage> messages,
        List<ToolDefinition>? tools,
        bool includeImageAttachments,
        bool stream)
    {
        var payload = new JsonObject
        {
            ["model"] = _config.ModelId,
            ["max_tokens"] = _config.MaxOutputToken,
            ["temperature"] = _config.Temperature,
            ["messages"] = ToAnthropicMessagesNode(messages, includeImageAttachments),
            ["stream"] = stream
        };

        var systemContent = BuildAnthropicSystemContent(messages);
        if (!string.IsNullOrWhiteSpace(systemContent))
        {
            payload["system"] = systemContent;
        }

        if (tools != null && tools.Count > 0)
        {
            var toolsArray = new JsonArray();
            foreach (var tool in tools)
            {
                toolsArray.Add(new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["input_schema"] = tool.Parameters.DeepClone()
                });
            }

            payload["tools"] = toolsArray;
            payload["tool_choice"] = new JsonObject { ["type"] = "auto" };
        }

        return payload;
    }

    private static string BuildAnthropicSystemContent(IEnumerable<ChatMessage> messages)
    {
        return string.Join("\n\n", messages
            .Where(message => NormalizeRole(message.Role).Equals("system", StringComparison.OrdinalIgnoreCase))
            .Select(message => BuildMessageTextForModel(message, imagePayloadIncluded: false))
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static JsonArray ToAnthropicMessagesNode(IReadOnlyList<ChatMessage> messages, bool includeImageAttachments)
    {
        var result = new JsonArray();
        var lastImageAttachmentMessageIndex = includeImageAttachments
            ? messages.ToList().FindLastIndex(message =>
                NormalizeRole(message.Role).Equals("user", StringComparison.OrdinalIgnoreCase)
                && HasImageAttachment(message))
            : -1;

        for (var i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            var role = NormalizeRole(message.Role);

            if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
                continue;

            if (role.Equals("tool", StringComparison.OrdinalIgnoreCase))
                continue;

            if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && message.ToolCalls is { Count: > 0 })
            {
                if (TryCollectCompleteToolResultBlock(messages, i, out var toolMessages, out var nextIndex))
                {
                    AppendAnthropicMessage(result, "assistant", BuildAnthropicAssistantContent(message, includeToolCalls: true));
                    AppendAnthropicMessage(result, "user", BuildAnthropicToolResultContent(toolMessages));
                    i = nextIndex - 1;
                    continue;
                }

                AppendAnthropicMessage(result, "assistant", BuildAnthropicAssistantContent(
                    new ChatMessage
                    {
                        Role = "assistant",
                        Content = string.IsNullOrWhiteSpace(message.Content)
                            ? "(historical tool call omitted because matching tool result messages were unavailable)"
                            : message.Content
                    },
                    includeToolCalls: false));
                continue;
            }

            if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                AppendAnthropicMessage(result, "assistant", BuildAnthropicAssistantContent(message, includeToolCalls: false));
                continue;
            }

            AppendAnthropicMessage(result, "user", BuildAnthropicUserContent(
                message,
                includeImageAttachments && i == lastImageAttachmentMessageIndex));
        }

        if (result.Count == 0)
        {
            AppendAnthropicMessage(result, "user", new JsonArray(CreateAnthropicTextBlock("Continue.")));
        }

        return result;
    }

    private static void AppendAnthropicMessage(JsonArray messages, string role, JsonArray content)
    {
        if (content.Count == 0)
        {
            content.Add(CreateAnthropicTextBlock("(empty message)"));
        }

        var last = messages.Count > 0 ? messages[^1] as JsonObject : null;
        if (last != null
            && string.Equals(last["role"]?.GetValue<string>(), role, StringComparison.OrdinalIgnoreCase)
            && last["content"] is JsonArray previousContent)
        {
            foreach (var item in content)
            {
                previousContent.Add(item?.DeepClone());
            }
            return;
        }

        messages.Add(new JsonObject
        {
            ["role"] = role,
            ["content"] = content
        });
    }

    private static JsonArray BuildAnthropicUserContent(ChatMessage message, bool includeImageAttachments)
    {
        var content = new JsonArray();
        var text = BuildMessageTextForModel(message, includeImageAttachments);
        if (!string.IsNullOrWhiteSpace(text))
        {
            content.Add(CreateAnthropicTextBlock(text));
        }

        if (!includeImageAttachments)
        {
            return content;
        }

        foreach (var attachment in message.Attachments ?? new List<ChatAttachment>())
        {
            if (!attachment.Kind.Equals("image", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrWhiteSpace(attachment.Path) || !File.Exists(attachment.Path))
                continue;

            try
            {
                var bytes = File.ReadAllBytes(attachment.Path);
                var mime = string.IsNullOrWhiteSpace(attachment.MimeType) ? "image/png" : attachment.MimeType;
                content.Add(new JsonObject
                {
                    ["type"] = "image",
                    ["source"] = new JsonObject
                    {
                        ["type"] = "base64",
                        ["media_type"] = mime,
                        ["data"] = Convert.ToBase64String(bytes)
                    }
                });
            }
            catch
            {
                // The text metadata still tells the model the attachment exists.
            }
        }

        return content;
    }

    private static JsonArray BuildAnthropicAssistantContent(ChatMessage message, bool includeToolCalls)
    {
        var content = new JsonArray();
        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            content.Add(CreateAnthropicTextBlock(message.Content));
        }

        if (includeToolCalls && message.ToolCalls is { Count: > 0 })
        {
            foreach (var toolCall in message.ToolCalls)
            {
                if (string.IsNullOrWhiteSpace(toolCall.Id) || string.IsNullOrWhiteSpace(toolCall.Function.Name))
                    continue;

                content.Add(new JsonObject
                {
                    ["type"] = "tool_use",
                    ["id"] = toolCall.Id,
                    ["name"] = toolCall.Function.Name,
                    ["input"] = ParseAnthropicToolInput(toolCall.Function.Arguments)
                });
            }
        }

        return content;
    }

    private static JsonArray BuildAnthropicToolResultContent(IEnumerable<ChatMessage> toolMessages)
    {
        var content = new JsonArray();
        foreach (var message in toolMessages)
        {
            if (string.IsNullOrWhiteSpace(message.ToolCallId))
                continue;

            content.Add(new JsonObject
            {
                ["type"] = "tool_result",
                ["tool_use_id"] = message.ToolCallId,
                ["content"] = message.Content ?? string.Empty
            });
        }

        return content;
    }

    private static JsonObject CreateAnthropicTextBlock(string text)
        => new()
        {
            ["type"] = "text",
            ["text"] = text
        };

    private static JsonNode ParseAnthropicToolInput(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return new JsonObject();

        try
        {
            var parsed = JsonNode.Parse(arguments);
            if (parsed is JsonObject obj)
                return obj;

            return new JsonObject { ["value"] = parsed?.DeepClone() };
        }
        catch
        {
            return new JsonObject { ["_raw_arguments"] = arguments };
        }
    }

    private async Task<ChatMessage> SendAnthropicNonStreamingAsync(HttpRequestMessage request, bool collectReasoning, CancellationToken ct)
    {
        var response = await _http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Anthropic-compatible API error: {(int)response.StatusCode} at {SanitizeEndpointForError(request.RequestUri)} - {responseBody}", null, response.StatusCode);
        }

        RecordAnthropicEndpointSuccess(request);
        using var doc = JsonDocument.Parse(responseBody);
        return ParseAnthropicResponseMessage(doc.RootElement, collectReasoning);
    }

    private async Task<ChatMessage> StreamAnthropicAsync(HttpRequestMessage request, Action<string> onChunk, Action<string>? onReasoningChunk, bool collectReasoning, CancellationToken ct)
    {
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Anthropic-compatible API error: {(int)response.StatusCode} at {SanitizeEndpointForError(request.RequestUri)} - {err}", null, response.StatusCode);
        }

        RecordAnthropicEndpointSuccess(request);
        var contentBuilder = new StringBuilder();
        var reasoningBuilder = new StringBuilder();
        var toolCallDict = new Dictionary<int, ToolCallAccumulator>();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var startedAt = DateTimeOffset.UtcNow;
        var lastUsefulOutputAt = startedAt;

        while (!ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await ReadStreamingLineAsync(reader, ct, startedAt, lastUsefulOutputAt, "Anthropic-compatible");
            }
            catch (IOException)
            {
                break;
            }
            if (line == null)
                break;

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var data = line[6..];
            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                var type = TryGetString(root, "type") ?? string.Empty;

                if (type.Equals("error", StringComparison.OrdinalIgnoreCase))
                {
                    throw new HttpRequestException($"Anthropic stream error: {root.GetRawText()}");
                }

                if (type.Equals("content_block_start", StringComparison.OrdinalIgnoreCase)
                    && root.TryGetProperty("content_block", out var block)
                    && block.ValueKind == JsonValueKind.Object)
                {
                    var index = TryGetInt(root, "index") ?? toolCallDict.Count;
                    var blockType = TryGetString(block, "type") ?? string.Empty;
                    if (blockType.Equals("text", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = TryGetString(block, "text");
                        if (!string.IsNullOrEmpty(text))
                        {
                            lastUsefulOutputAt = DateTimeOffset.UtcNow;
                            contentBuilder.Append(text);
                            onChunk(text);
                        }
                    }
                    else if (blockType.Equals("tool_use", StringComparison.OrdinalIgnoreCase))
                    {
                        lastUsefulOutputAt = DateTimeOffset.UtcNow;
                        var acc = new ToolCallAccumulator
                        {
                            Id = TryGetString(block, "id") ?? string.Empty,
                            Type = "function",
                            Name = TryGetString(block, "name") ?? string.Empty,
                            Arguments = string.Empty
                        };
                        toolCallDict[index] = acc;
                    }
                    else if (collectReasoning && blockType.Equals("thinking", StringComparison.OrdinalIgnoreCase))
                    {
                        var thinking = TryGetString(block, "thinking");
                        if (!string.IsNullOrEmpty(thinking))
                        {
                            lastUsefulOutputAt = DateTimeOffset.UtcNow;
                            reasoningBuilder.Append(thinking);
                            onReasoningChunk?.Invoke(thinking);
                        }
                    }
                }
                else if (type.Equals("content_block_delta", StringComparison.OrdinalIgnoreCase)
                    && root.TryGetProperty("delta", out var delta)
                    && delta.ValueKind == JsonValueKind.Object)
                {
                    var index = TryGetInt(root, "index") ?? 0;
                    var deltaType = TryGetString(delta, "type") ?? string.Empty;
                    if (deltaType.Equals("text_delta", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = TryGetString(delta, "text") ?? string.Empty;
                        if (text.Length > 0)
                        {
                            lastUsefulOutputAt = DateTimeOffset.UtcNow;
                            contentBuilder.Append(text);
                            onChunk(text);
                        }
                    }
                    else if (deltaType.Equals("input_json_delta", StringComparison.OrdinalIgnoreCase))
                    {
                        lastUsefulOutputAt = DateTimeOffset.UtcNow;
                        if (!toolCallDict.TryGetValue(index, out var acc))
                        {
                            acc = new ToolCallAccumulator();
                            toolCallDict[index] = acc;
                        }

                        acc.Arguments += TryGetString(delta, "partial_json") ?? string.Empty;
                    }
                    else if (collectReasoning && deltaType.Equals("thinking_delta", StringComparison.OrdinalIgnoreCase))
                    {
                        var thinking = TryGetString(delta, "thinking") ?? string.Empty;
                        if (thinking.Length > 0)
                        {
                            lastUsefulOutputAt = DateTimeOffset.UtcNow;
                            reasoningBuilder.Append(thinking);
                            onReasoningChunk?.Invoke(thinking);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed SSE keep-alive or intermediary lines.
            }
        }

        var toolCalls = toolCallDict.Count > 0
            ? toolCallDict.OrderBy(kv => kv.Key)
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Id) && !string.IsNullOrWhiteSpace(kv.Value.Name))
                .Select(kv => new ToolCall
                {
                    Id = kv.Value.Id,
                    Type = "function",
                    Function = new ToolFunction
                    {
                        Name = kv.Value.Name,
                        Arguments = string.IsNullOrWhiteSpace(kv.Value.Arguments) ? "{}" : kv.Value.Arguments
                    }
                })
                .ToList()
            : null;

        return new ChatMessage
        {
            Role = "assistant",
            Content = contentBuilder.ToString(),
            ReasoningContent = collectReasoning && reasoningBuilder.Length > 0 ? reasoningBuilder.ToString() : null,
            ToolCalls = toolCalls is { Count: > 0 } ? toolCalls : null,
            Timestamp = UserTimeZoneService.Now()
        };
    }

    private static async Task<string?> ReadStreamingLineAsync(StreamReader reader, CancellationToken ct)
    {
        try
        {
            return await reader.ReadLineAsync(ct).AsTask().WaitAsync(StreamTransportIdleTimeout, ct);
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException($"LLM stream produced no transport data for {StreamTransportIdleTimeout.TotalSeconds:F0}s.", ex);
        }
    }

    private static async Task<string?> ReadStreamingLineAsync(
        StreamReader reader,
        CancellationToken ct,
        DateTimeOffset startedAt,
        DateTimeOffset lastUsefulOutputAt,
        string apiKind)
    {
        var elapsedTotal = DateTimeOffset.UtcNow - startedAt;
        if (elapsedTotal > StreamResponseTimeout)
        {
            throw new TimeoutException(
                $"LLM stream total response time exceeded {StreamResponseTimeout.TotalMinutes:F0}min " +
                $"({elapsedTotal.TotalSeconds:F0}s elapsed, {apiKind}).");
        }

        var elapsedSinceUseful = DateTimeOffset.UtcNow - lastUsefulOutputAt;
        if (elapsedSinceUseful > StreamUsefulOutputTimeout)
        {
            throw new TimeoutException(
                $"LLM stream produced no useful output for {StreamUsefulOutputTimeout.TotalSeconds:F0}s " +
                $"({elapsedSinceUseful.TotalSeconds:F0}s elapsed, {apiKind}).");
        }

        try
        {
            return await reader.ReadLineAsync(ct).AsTask().WaitAsync(StreamTransportIdleTimeout, ct);
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException(
                $"LLM stream produced no transport data for {StreamTransportIdleTimeout.TotalSeconds:F0}s ({apiKind}).", ex);
        }
    }

    private static ChatMessage ParseAnthropicResponseMessage(JsonElement root, bool collectReasoning)
    {
        var contentBuilder = new StringBuilder();
        var reasoningBuilder = new StringBuilder();
        var toolCalls = new List<ToolCall>();

        if (root.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in contentArr.EnumerateArray())
            {
                if (block.ValueKind != JsonValueKind.Object)
                    continue;

                var type = TryGetString(block, "type") ?? string.Empty;
                if (type.Equals("text", StringComparison.OrdinalIgnoreCase))
                {
                    contentBuilder.Append(TryGetString(block, "text") ?? string.Empty);
                }
                else if (type.Equals("tool_use", StringComparison.OrdinalIgnoreCase))
                {
                    toolCalls.Add(new ToolCall
                    {
                        Id = TryGetString(block, "id") ?? string.Empty,
                        Type = "function",
                        Function = new ToolFunction
                        {
                            Name = TryGetString(block, "name") ?? string.Empty,
                            Arguments = block.TryGetProperty("input", out var input) && input.ValueKind != JsonValueKind.Null
                                ? input.GetRawText()
                                : "{}"
                        }
                    });
                }
                else if (collectReasoning && type.Equals("thinking", StringComparison.OrdinalIgnoreCase))
                {
                    reasoningBuilder.Append(TryGetString(block, "thinking") ?? string.Empty);
                }
            }
        }

        return new ChatMessage
        {
            Role = "assistant",
            Content = contentBuilder.ToString(),
            ReasoningContent = collectReasoning && reasoningBuilder.Length > 0 ? reasoningBuilder.ToString() : null,
            ToolCalls = toolCalls.Count > 0 ? toolCalls : null,
            Timestamp = UserTimeZoneService.Now()
        };
    }

    private JsonObject CreateOpenAiPayload(
        IReadOnlyList<ChatMessage> messages,
        List<ToolDefinition>? tools,
        bool includeReasoningContent,
        ThinkingParameterMode thinkingMode,
        bool supportsThinking,
        bool thinkingEnabled,
        float requestTemperature,
        bool includeImageAttachments,
        bool forceAssistantReasoningContent,
        bool stream)
    {
        var payload = new JsonObject
        {
            ["model"] = _config.ModelId,
            ["messages"] = ToOpenAiMessagesNode(messages, includeReasoningContent, includeImageAttachments, forceAssistantReasoningContent),
            ["temperature"] = requestTemperature,
            ["max_tokens"] = _config.MaxOutputToken,
            ["stream"] = stream
        };

        ApplyThinkingOptions(payload, thinkingMode, supportsThinking, thinkingEnabled);

        if (tools != null && tools.Count > 0)
        {
            var toolsArray = new JsonArray();
            foreach (var t in tools)
            {
                var toolObj = new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = JsonSerializer.SerializeToNode(t, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })
                };
                toolsArray.Add(toolObj);
            }
            payload["tools"] = toolsArray;
            payload["tool_choice"] = "auto";
        }

        return payload;
    }

    private bool IsAnthropicApi()
    {
        return _config.ApiType.Contains("anthropic", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDeepSeekApi()
    {
        return _config.ApiType.Contains("deepseek", StringComparison.OrdinalIgnoreCase)
            || _config.BaseUrl.Contains("deepseek.com", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsZaiApi()
    {
        return _config.ApiType.Contains("zai_glm", StringComparison.OrdinalIgnoreCase)
            || _config.ApiType.Contains("z.ai", StringComparison.OrdinalIgnoreCase)
            || _config.BaseUrl.Contains("api.z.ai", StringComparison.OrdinalIgnoreCase)
            || _config.BaseUrl.Contains("bigmodel.cn", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsXiaomiMimoApi()
    {
        return _config.ApiType.Contains("xiaomi_mimo", StringComparison.OrdinalIgnoreCase)
            || _config.ApiType.Contains("mimo", StringComparison.OrdinalIgnoreCase)
            || _config.BaseUrl.Contains("xiaomimimo.com", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsMimoModel()
    {
        return _config.ModelId.Contains("mimo", StringComparison.OrdinalIgnoreCase);
    }

    private ThinkingParameterMode GetThinkingParameterMode()
    {
        if (IsKimiLikeApiOrModel())
            return ThinkingParameterMode.KimiPreservedObject;

        if (IsDeepSeekApi() || IsZaiApi() || IsXiaomiMimoApi() || IsMimoModel())
            return ThinkingParameterMode.TypeObject;

        return ThinkingParameterMode.ReasoningEffort;
    }

    private bool SupportsThinkingModel()
    {
        return ModelProviderCatalog.FindModel(_config.ApiType, _config.ModelId)?.SupportsThinking == true
            || (IsDeepSeekApi() && IsDeepSeekThinkingModel(_config.ModelId))
            || IsMimoModel()
            || IsKimiLikeApiOrModel();
    }

    public static bool IsKimiLike(AgentConfig config)
    {
        return IsKimiLike(config.ModelId, config.BaseUrl, config.ApiType);
    }

    public static bool IsKimiLike(string? modelId, string? baseUrl = null, string? apiType = null)
    {
        return ContainsKimiOrMoonshot(modelId)
            || ContainsKimiOrMoonshot(baseUrl)
            || ContainsKimiOrMoonshot(apiType);
    }

    private bool IsKimiLikeApiOrModel()
    {
        return IsKimiLike(_config);
    }

    private static bool ContainsKimiOrMoonshot(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().ToLowerInvariant();
        return normalized.Contains("kimi", StringComparison.Ordinal)
            || normalized.Contains("moonshot", StringComparison.Ordinal);
    }

    private static bool IsDeepSeekThinkingModel(string model)
    {
        return model.Equals("deepseek-reasoner", StringComparison.OrdinalIgnoreCase)
            || model.Equals("deepseek-v4-pro", StringComparison.OrdinalIgnoreCase)
            || model.Equals("deepseek-v4-flash", StringComparison.OrdinalIgnoreCase);
    }

    private string GetOpenAiChatCompletionsUrl()
    {
        var baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
            ? AgentConfig.DefaultBaseUrl
            : _config.BaseUrl;
        return baseUrl.TrimEnd('/') + "/chat/completions";
    }

    private List<string> GetAnthropicMessagesUrlCandidates()
    {
        var baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
            ? "https://api.anthropic.com/v1"
            : _config.BaseUrl;
        var trimmed = baseUrl.TrimEnd('/');
        var candidates = new List<string>();

        var cached = ModelCapabilityCacheService.GetAnthropicMessagesUrl(_config);
        if (!string.IsNullOrWhiteSpace(cached))
            AddUniqueEndpoint(candidates, cached);

        if (trimmed.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
        {
            AddUniqueEndpoint(candidates, trimmed);
            return candidates;
        }

        if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            AddUniqueEndpoint(candidates, trimmed + "/messages");
            AddUniqueEndpoint(candidates, trimmed[..^3] + "/messages");
            return candidates;
        }

        AddUniqueEndpoint(candidates, trimmed + "/v1/messages");
        AddUniqueEndpoint(candidates, trimmed + "/messages");
        return candidates;
    }

    private static void AddUniqueEndpoint(List<string> endpoints, string endpoint)
    {
        var trimmed = endpoint.Trim().TrimEnd('/');
        if (!endpoints.Any(existing => existing.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            endpoints.Add(trimmed);
    }

    private bool UsesBearerAuthForAnthropicCompatibility()
    {
        if (!Uri.TryCreate(_config.BaseUrl, UriKind.Absolute, out var uri))
            return false;

        return uri.Host.Equals("qianfan.baidubce.com", StringComparison.OrdinalIgnoreCase);
    }

    private void RecordAnthropicEndpointSuccess(HttpRequestMessage request)
    {
        var endpoint = request.RequestUri?.GetLeftPart(UriPartial.Path);
        if (!string.IsNullOrWhiteSpace(endpoint))
            ModelCapabilityCacheService.RecordAnthropicMessagesEndpointSuccess(_config, endpoint);
    }

    private static string SanitizeEndpointForError(Uri? uri)
    {
        if (uri == null)
            return "(unknown endpoint)";

        return uri.GetLeftPart(UriPartial.Path);
    }

    private static bool IsAnthropicEndpointNotFound(HttpRequestException ex)
    {
        if ((int?)ex.StatusCode != 404)
            return false;

        var message = ex.Message ?? string.Empty;
        return message.Contains("Resource not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ResourceNotFound", StringComparison.OrdinalIgnoreCase)
            || message.Contains("404", StringComparison.OrdinalIgnoreCase);
    }

    private string GetHeaderCompatibilityKey()
    {
        return (_config.ApiType + "|" + (_config.BaseUrl ?? "")).Trim().ToLowerInvariant();
    }

    private static bool IsNoResponseFailure(Exception ex)
    {
        if (ex is TaskCanceledException or TimeoutException or IOException)
            return true;

        if (ex is HttpRequestException httpEx)
        {
            var message = httpEx.Message ?? "";
            if (message.Contains("(no response)", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("no response", StringComparison.OrdinalIgnoreCase))
                return true;

            // Errors raised after receiving an HTTP status are not header-compatibility failures.
            if (message.StartsWith("LLM API error:", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("Anthropic API error:", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("Anthropic-compatible API error:", StringComparison.OrdinalIgnoreCase))
                return false;

            return httpEx.StatusCode == null;
        }

        return false;
    }

    private static void ApplyCommonHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("User-Agent", ClientUserAgent);
        request.Headers.TryAddWithoutValidation("X-Client-Name", ClientName);
        request.Headers.TryAddWithoutValidation("X-Title", ClientName);
        request.Headers.TryAddWithoutValidation("X-Matdance-Channel", ClientName);
    }

    private static List<ToolCall>? ParseToolCalls(JsonElement msg)
    {
        if (!msg.TryGetProperty("tool_calls", out var tcArray)) return null;
        if (tcArray.ValueKind != JsonValueKind.Array) return null;

        var list = new List<ToolCall>();
        foreach (var tc in tcArray.EnumerateArray())
        {
            if (tc.ValueKind != JsonValueKind.Object) continue;
            if (!tc.TryGetProperty("function", out var fn) || fn.ValueKind != JsonValueKind.Object) continue;

            list.Add(new ToolCall
            {
                Id = TryGetString(tc, "id") ?? "",
                Type = TryGetString(tc, "type") ?? "function",
                Function = new ToolFunction
                {
                    Name = TryGetString(fn, "name") ?? "",
                    Arguments = TryGetString(fn, "arguments") ?? "{}"
                }
            });
        }
        return list.Count > 0 ? list : null;
    }

    private static string? ExtractReasoningText(JsonElement element)
    {
        if (TryReasoningProperty(element, "reasoning_content", out var text)) return text;
        if (TryReasoningProperty(element, "thinking", out text)) return text;
        if (TryReasoningProperty(element, "reasoning", out text)) return text;
        return null;
    }

    private static bool TryReasoningProperty(JsonElement element, string propertyName, out string? text)
    {
        text = null;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return false;
        }

        text = property.ValueKind == JsonValueKind.String ? property.GetString() : property.GetRawText();
        return !string.IsNullOrEmpty(text);
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.GetRawText();
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
            return number;

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out number))
            return number;

        return null;
    }

    private class ToolCallAccumulator
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "function";
        public string Name { get; set; } = "";
        public string Arguments { get; set; } = "";
    }

    private enum ThinkingParameterMode
    {
        ReasoningEffort,
        TypeObject,
        KimiPreservedObject
    }
}

public class ToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("parameters")]
    public JsonObject Parameters { get; set; } = new();
}
