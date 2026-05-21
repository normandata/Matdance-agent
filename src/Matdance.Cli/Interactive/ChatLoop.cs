using System.Text;
using Spectre.Console;
using Matdance.Cli.Core;
using Matdance.Cli.Models;
using Matdance.Cli.Services;

namespace Matdance.Cli.Interactive;

public class ChatLoop
{
    private readonly string _agentName;
    private readonly string _sessionId;
    private readonly SessionData _sessionData;
    private readonly SessionState _sessionState;
    private readonly AgentConfig _config;
    private readonly PathService _path;
    private readonly LlmClient _llm;
    private bool _shuttingDown = false;

    public ChatLoop(string agentName, string sessionId, SessionData sessionData, SessionState sessionState, AgentConfig config, PathService path)
    {
        _agentName = agentName;
        _sessionId = sessionId;
        _sessionData = sessionData;
        _sessionState = sessionState;
        _config = config;
        _path = path;
        _llm = new LlmClient(config);
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _shuttingDown = true;
            SaveState();
            AnsiConsole.MarkupLine("\n[dim]State saved. Exiting...[/]");
            Environment.Exit(0);
        };

        AnsiConsole.Clear();
        ChatRenderer.RenderHeader(_agentName, _config.ModelId, _sessionId);
        ChatRenderer.RenderHistory(_sessionState.Messages);

        ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
        while (!ct.IsCancellationRequested && !_shuttingDown)
        {
            AnsiConsole.Markup("[bold dodgerblue1]➜[/] ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
                continue;
            }

            var trimmed = input.Trim();

            if (trimmed == "/exit")
            {
                if (_sessionState.ActiveTask != null && _sessionState.ActiveTask.Status == "in_process")
                {
                    AnsiConsole.MarkupLine("[yellow]Active task in progress. Finish it first, or use /task to check.[/]");
                    continue;
                }
                SaveState();
                AnsiConsole.MarkupLine("[dim]Saved. Goodbye.[/]");
                break;
            }

            if (trimmed == "/clear")
            {
                _sessionState.Messages.Clear();
                _sessionState.ContextCompaction = null;
                SaveState();
                AnsiConsole.MarkupLine("[dim]Context cleared.[/]");
                ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
                continue;
            }

            if (trimmed == "/task")
            {
                if (_sessionState.ActiveTask == null)
                    AnsiConsole.MarkupLine("[dim]No active task.[/]");
                else
                    ChatRenderer.RenderTaskTable(_sessionState.ActiveTask);
                ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
                continue;
            }

            if (trimmed == "/files")
            {
                if (_sessionState.TracedFiles.Count == 0)
                {
                    AnsiConsole.MarkupLine("[dim]No traced files.[/]");
                }
                else
                {
                    ChatRenderer.RenderTracedFiles(_sessionState.TracedFiles);
                }
                ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
                continue;
            }

            if (trimmed == "/prompt")
            {
                var systemPrompt = PromptBuilder.BuildSystemContent(_agentName, _path);
                systemPrompt += "\n" + PromptBuilder.BuildActiveTaskSection(_sessionState);
                systemPrompt += "\n" + PromptBuilder.BuildReadingFilesSection(_sessionState);
                ChatRenderer.RenderPromptPreview(systemPrompt, _sessionState.Messages);
                ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
                continue;
            }

            if (trimmed == "/status")
            {
                ChatRenderer.RenderStatusPage(_sessionData, _sessionState, _config);
                ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
                continue;
            }

            if (trimmed == "/history")
            {
                ChatRenderer.RenderHistory(_sessionState.Messages, full: true);
                ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
                continue;
            }

            if (trimmed == "/compact")
            {
                var before = _sessionState.Messages.Count;
                var mainContextMessages = _sessionState.Messages.Where(PromptBuilder.ShouldIncludeInMainContext).ToList();
                var compressor = new ContextCompressor(_config);
                var compressed = await compressor.CompressAsync(mainContextMessages, ct);
                
                // Replace main context messages with compressed, keep excluded messages
                var excludedMessages = _sessionState.Messages.Where(m => m.IncludeInMainContext == false).ToList();
                _sessionState.Messages = excludedMessages.Concat(compressed).ToList();
                _sessionState.ContextCompaction = null;
                
                var after = _sessionState.Messages.Count;
                SaveState();
                AnsiConsole.MarkupLine($"[dim]Compacted: {before} → {after} messages (threshold: {_config.CompressionThreshold:P0}).[/]");
                ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
                continue;
            }

            await ProcessUserInputAsync(input, ct);
        }
    }

    private async Task ProcessUserInputAsync(string input, CancellationToken ct)
    {
        _sessionState.ClearTraceLocks();
        _sessionData.TotalMessages++;
        ChatRenderer.RenderUserMessage(input);

        // Build the full request first, then compress against the real prompt budget.
        var rawMainContextCount = _sessionState.Messages.Count(PromptBuilder.ShouldIncludeInMainContext);
        var mainContextMessages = BuildMainContextWithCompaction(out var baseCompressedCount, out var usedStoredSummary);
        var compressor = new ContextCompressor(_config);

        // Build request messages but don't add to persistent history yet
        var messages = PromptBuilder.BuildRequestMessages(_agentName, _path, _config, _sessionState, mainContextMessages);
        var privacyRevocationNotice = new SecuritySettingsService().ConsumePrivacyAccessRevokedNotice();
        if (!string.IsNullOrWhiteSpace(privacyRevocationNotice))
        {
            messages.Insert(Math.Min(1, messages.Count), ChatMessage.System(privacyRevocationNotice));
        }
        var userMsg = ChatMessage.User(input);
        messages.Add(userMsg);

        var tools = ToolRegistry.GetAll();
        var initialCompression = await EnsureRequestBudgetAsync(messages, tools, compressor, ct);
        RecordContextCompaction(initialCompression, rawMainContextCount, baseCompressedCount, usedStoredSummary);
        bool userMsgSaved = false;

        try
        {
            bool hasToolCalls = true;
            int maxLoops = 10;
            int loop = 0;
            int transientResponseRetries = 0;
            bool thinkingToolNoticeSent = false;
            const int maxTransientResponseRetries = 1;

            while (hasToolCalls && loop < maxLoops)
            {
                loop++;
                PromptBuilder.UpsertLiveFileLocksSnapshot(messages, _sessionState);
                await EnsureRequestBudgetAsync(messages, tools, compressor, ct);
                var assistantMsg = await CallLlmWithContextRetryAsync(messages, tools, compressor, ct, enableThinking: false);
                messages.RemoveAll(ContextCompressor.IsOneShotHandoff);
                var hasCurrentToolCalls = assistantMsg.ToolCalls != null && assistantMsg.ToolCalls.Count > 0;
                if (!hasCurrentToolCalls && !thinkingToolNoticeSent && LlmResponseGuard.HasTextualToolRequestInThinking(assistantMsg))
                {
                    thinkingToolNoticeSent = true;
                    messages.Add(ChatMessage.User(LlmResponseGuard.ThinkingTextToolRequestNotice));
                    continue;
                }
                if (LlmResponseGuard.IsTransientAssistantFailure(assistantMsg) && !hasCurrentToolCalls)
                {
                    if (transientResponseRetries < maxTransientResponseRetries)
                    {
                        transientResponseRetries++;
                        AnsiConsole.MarkupLine("[dim yellow][[retry]] Upstream returned no usable answer; retrying once...[/]");
                        continue;
                    }

                    if (LlmResponseGuard.IsUpstreamRejection(assistantMsg))
                    {
                        LlmResponseGuard.MarkAsExcluded(assistantMsg);
                        assistantMsg.Content = "Upstream model gateway rejected this turn before Matdance received a usable answer. Please retry.";
                    }
                    else
                    {
                        LlmResponseGuard.MarkAsNoResponse(assistantMsg);
                        assistantMsg.Content = "The model returned an empty response. Please retry.";
                    }
                    ChatRenderer.RenderAssistantStart();
                    AnsiConsole.WriteLine(assistantMsg.Content.EscapeMarkup());
                    ChatRenderer.RenderAssistantEnd();
                }

                // Calculate tokens for THIS request round (assign, not accumulate)
                var promptTokens = TokenCounter.EstimateMessages(messages);
                var responseTokens = TokenCounter.Estimate(assistantMsg.Content) + TokenCounter.Estimate(assistantMsg.ReasoningContent ?? string.Empty);
                _sessionData.Tokens = promptTokens + responseTokens;

                // Persist user message on first successful LLM response
                if (!userMsgSaved)
                {
                    _sessionState.Messages.Add(userMsg);
                    userMsgSaved = true;
                }

                assistantMsg.Timestamp = UserTimeZoneService.Now();
                _sessionState.Messages.Add(assistantMsg);
                messages.Add(assistantMsg);

                if (assistantMsg.ToolCalls == null || assistantMsg.ToolCalls.Count == 0)
                {
                    hasToolCalls = false;
                    ChatRenderer.RenderAssistantEnd();
                }
                else
                {
                    foreach (var tc in assistantMsg.ToolCalls)
                    {
                        var args = TryGetToolArgsPreview(tc.Function.Arguments);
                        var result = await RunToolWithAnimationAsync(tc, args);
                        ChatRenderer.RenderToolResultPanel(tc.Function.Name, args, result);
                        var toolMsg = ChatMessage.Tool(tc.Id, result);
                        _sessionState.Messages.Add(toolMsg);
                        messages.Add(toolMsg);
                        _sessionData.ToolMessagesCount++;
                    }
                }
            }

            _sessionData.ContextUsage = Math.Min(100, (int)((double)_sessionData.Tokens / _config.ContextWindow * 100));
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red][[error]] {ex.Message.EscapeMarkup()}[/]");
            // User message was NOT added to persistent history, so no pollution
        }

        _sessionData.LastActivity = UserTimeZoneService.Now();
        _sessionState.ClearTraceLocks();
        SaveState();
        ChatRenderer.RenderStatusBar(_sessionData, _sessionState, _config);
    }

    private async Task<ContextCompressionResult> EnsureRequestBudgetAsync(
        List<ChatMessage> messages,
        List<ToolDefinition> tools,
        ContextCompressor compressor,
        CancellationToken ct,
        bool force = false)
    {
        var result = await compressor.CompressRequestAsync(messages, tools, ct, force);
        if (!result.Compressed)
            return result;

        messages.Clear();
        messages.AddRange(result.Messages);
        AnsiConsole.MarkupLine($"[dim yellow][[context]] compressed request to {result.EstimatedTokens:N0}/{result.Limit:N0} estimated tokens.[/]");
        return result;
    }

    private List<ChatMessage> BuildMainContextWithCompaction(out int baseCompressedCount, out bool usedStoredSummary)
    {
        var raw = _sessionState.Messages.Where(PromptBuilder.ShouldIncludeInMainContext).ToList();
        baseCompressedCount = 0;
        usedStoredSummary = false;

        var compaction = _sessionState.ContextCompaction;
        if (compaction == null || string.IsNullOrWhiteSpace(compaction.Summary))
            return raw;

        var point = Math.Clamp(compaction.CompressedUntilMessageCount, 0, raw.Count);
        if (point <= 0)
            return raw;

        baseCompressedCount = point;
        usedStoredSummary = true;
        var result = new List<ChatMessage> { ContextCompressor.SummaryMessage(compaction.Summary) };
        result.AddRange(raw.Skip(point));
        return result;
    }

    private void RecordContextCompaction(
        ContextCompressionResult result,
        int rawMainContextCount,
        int baseCompressedCount,
        bool usedStoredSummary)
    {
        if (!result.Compressed || string.IsNullOrWhiteSpace(result.SummaryContent))
            return;

        var covered = result.CompressedSourceMessageCount;
        if (covered <= 0)
            return;

        var rawCovered = usedStoredSummary && covered > 0
            ? baseCompressedCount + Math.Max(0, covered - 1)
            : covered;
        rawCovered = Math.Clamp(rawCovered, 0, rawMainContextCount);
        if (rawCovered <= 0)
            return;

        _sessionState.ContextCompaction = new ContextCompactionInfo
        {
            Generation = (_sessionState.ContextCompaction?.Generation ?? 0) + 1,
            CompressedUntilMessageCount = rawCovered,
            Summary = result.SummaryContent,
            LastHandoff = result.HandoffContent,
            CreatedAt = UserTimeZoneService.Now()
        };
    }

    private async Task<ChatMessage> CallLlmWithContextRetryAsync(
        List<ChatMessage> messages,
        List<ToolDefinition> tools,
        ContextCompressor compressor,
        CancellationToken ct,
        bool enableThinking)
    {
        try
        {
            return await CallLlmAsync(messages, tools, ct, enableThinking);
        }
        catch (Exception ex) when (LlmClient.IsContextLimitError(ex) && !ct.IsCancellationRequested)
        {
            CalibrateContextWindowAfterLimitError(messages, tools);
            AnsiConsole.MarkupLine("[dim yellow][[context]] upstream reported a context limit; recalibrating and retrying with forced compression...[/]");
            compressor = new ContextCompressor(_config);
            await EnsureRequestBudgetAsync(messages, tools, compressor, ct, force: true);
            return await CallLlmAsync(messages, tools, ct, enableThinking);
        }
    }

    private void CalibrateContextWindowAfterLimitError(List<ChatMessage> messages, List<ToolDefinition> tools)
    {
        var observed = ContextCompressor.EstimateObservedContextWindowAfterLimitError(_config, messages, tools);
        if (observed >= _config.ContextWindow)
            return;

        _config.ContextWindow = observed;
        try
        {
            _config.Save(_path.GetAgentConfigJsonPath(_agentName));
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[dim yellow][[context]] failed to save calibrated context window: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private async Task<ChatMessage> CallLlmAsync(List<ChatMessage> messages, List<ToolDefinition> tools, CancellationToken ct, bool enableThinking)
    {
        var sb = new StringBuilder();
        bool started = false;
        var streamFilter = new LlmResponseGuard.StreamingFilter();
        void EmitChunk(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!started)
            {
                started = true;
                ChatRenderer.RenderAssistantStart();
            }

            sb.Append(text);
            AnsiConsole.Write(text.EscapeMarkup());
        }

        var msg = await _llm.SendAsync(
            messages,
            tools,
            onStreamChunk: chunk => streamFilter.OnChunk(chunk, EmitChunk),
            ct,
            enableThinking: enableThinking);

        if (LlmResponseGuard.IsTransientAssistantFailure(msg))
        {
            return msg;
        }

        streamFilter.FlushIfAllowed(EmitChunk);

        if (!started)
        {
            ChatRenderer.RenderAssistantStart();
        }

        if (string.IsNullOrEmpty(msg.Content) && sb.Length > 0 && (msg.ToolCalls == null || msg.ToolCalls.Count == 0))
        {
            msg.Content = sb.ToString();
        }

        if (string.IsNullOrEmpty(msg.Content) && (msg.ToolCalls == null || msg.ToolCalls.Count == 0))
        {
            LlmResponseGuard.MarkAsNoResponse(msg);
            msg.Content = "(no response)";
        }

        if (msg.ToolCalls == null || msg.ToolCalls.Count == 0)
        {
            if (!started && !string.IsNullOrEmpty(msg.Content))
            {
                AnsiConsole.WriteLine(msg.Content.EscapeMarkup());
            }
            AnsiConsole.WriteLine();
        }
        else
        {
            AnsiConsole.WriteLine();
        }

        return msg;
    }

    private async Task<string> RunToolWithAnimationAsync(ToolCall tc, string? argsPreview = null)
    {
        var executor = new ToolExecutor(_agentName, _path, _sessionState, sessionId: _sessionId);
        string result = "";
        var displayName = tc.Function.Name.ReplaceLineEndings(" ").EscapeMarkup();
        if (!string.IsNullOrEmpty(argsPreview))
            displayName += $" ({argsPreview.ReplaceLineEndings(" ").EscapeMarkup()})";

        await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(new ChatRenderer.MatrixSpinner())
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync(displayName, async ctx =>
            {
                result = await executor.ExecuteAsync(tc);
            });
        return result;
    }

    private static string? TryGetToolArgsPreview(string arguments)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(arguments);
            var root = doc.RootElement;
            var parts = new List<string>();
            foreach (var prop in root.EnumerateObject())
            {
                var val = prop.Value.ToString();
                if (val.Length > 30) val = val[..30] + "...";
                parts.Add($"{prop.Name}={val}");
            }
            return string.Join(", ", parts);
        }
        catch
        {
            return null;
        }
    }

    private void SaveState()
    {
        try
        {
            var sessionFile = _path.GetSessionJsonPath(_agentName, _sessionId);
            _sessionData.Save(sessionFile);
            _sessionState.Save(sessionFile);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red][[save error]] {ex.Message.EscapeMarkup()}[/]");
        }
    }
}
