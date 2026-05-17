using System.Text;
using Matdance.Cli.Models;

namespace Matdance.Cli.Core;

public class ContextCompressor
{
    private readonly LlmClient _llm;
    private readonly int _contextWindow;
    private readonly float _threshold;

    public ContextCompressor(AgentConfig config)
    {
        _llm = new LlmClient(config);
        _contextWindow = config.ContextWindow;
        _threshold = config.CompressionThreshold > 0 && config.CompressionThreshold <= 0.95f 
            ? config.CompressionThreshold 
            : 0.7f;
    }

    public bool ShouldCompress(List<ChatMessage> messages)
    {
        var estimated = TokenCounter.EstimateMessages(messages);
        var limit = (int)(_contextWindow * _threshold);
        return estimated > limit;
    }

    public async Task<List<ChatMessage>> CompressAsync(List<ChatMessage> messages, CancellationToken ct = default)
    {
        if (!ShouldCompress(messages))
            return new List<ChatMessage>(messages);

        // 1. Identify protected recent turns (last 3 user-assistant cycles)
        var protectedCount = CalculateProtectedCount(messages);
        var protectedMessages = messages.TakeLast(protectedCount).ToList();
        var compressibleMessages = messages.Take(messages.Count - protectedCount).ToList();

        if (compressibleMessages.Count == 0)
            return new List<ChatMessage>(messages);

        // 2. Group compressible messages into conversation turns
        var turns = GroupIntoTurns(compressibleMessages);

        // 3. Generate summaries for turns (batch by 3 turns per summary to reduce API calls)
        var summarizedMessages = new List<ChatMessage>();
        var batchSize = 3;
        
        for (int i = 0; i < turns.Count; i += batchSize)
        {
            var batch = turns.Skip(i).Take(batchSize).ToList();
            var summary = await SummarizeTurnsAsync(batch, ct);
            summarizedMessages.Add(new ChatMessage 
            { 
                Role = "system", 
                Content = summary,
                IncludeInMainContext = false // Prevent re-compression
            });
            
            // Check if we've compressed enough
            var currentTotal = TokenCounter.EstimateMessages(summarizedMessages) + TokenCounter.EstimateMessages(protectedMessages);
            if (currentTotal < (int)(_contextWindow * _threshold))
                break;
        }

        // 4. If still over limit, fall back to removing oldest summaries
        var result = new List<ChatMessage>();
        result.AddRange(summarizedMessages);
        result.AddRange(protectedMessages);

        while (TokenCounter.EstimateMessages(result) > (int)(_contextWindow * _threshold) && result.Count > protectedMessages.Count + 1)
        {
            result.RemoveAt(0); // Remove oldest summary
        }

        return result;
    }

    private int CalculateProtectedCount(List<ChatMessage> messages)
    {
        // Protect last 3 complete user-assistant turns
        int protectedCount = 0;
        int userCount = 0;
        
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            protectedCount++;
            if (messages[i].Role == "user")
            {
                userCount++;
                if (userCount >= 3)
                    break;
            }
        }
        
        return protectedCount;
    }

    private List<List<ChatMessage>> GroupIntoTurns(List<ChatMessage> messages)
    {
        var turns = new List<List<ChatMessage>>();
        var currentTurn = new List<ChatMessage>();
        
        foreach (var msg in messages)
        {
            if (msg.Role == "user" && currentTurn.Count > 0)
            {
                turns.Add(currentTurn);
                currentTurn = new List<ChatMessage>();
            }
            currentTurn.Add(msg);
        }
        
        if (currentTurn.Count > 0)
            turns.Add(currentTurn);
        
        return turns;
    }

    private async Task<string> SummarizeTurnsAsync(List<List<ChatMessage>> turns, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[COMPRESSED CONTEXT SUMMARY]");
        sb.AppendLine();

        // Extract time range (use message index as proxy since there's no timestamp)
        var turnCount = turns.Count;
        sb.AppendLine($"Turns Compressed: {turnCount} conversation turns");
        sb.AppendLine();

        // Build raw content for summarization
        var rawContent = new StringBuilder();
        foreach (var turn in turns)
        {
            foreach (var msg in turn)
            {
                switch (msg.Role)
                {
                    case "user":
                        rawContent.AppendLine($"[User]: {msg.Content}");
                        break;
                    case "assistant":
                        if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                        {
                            var toolNames = string.Join(", ", msg.ToolCalls.Select(t => t.Function.Name));
                            rawContent.AppendLine($"[AI] (tools: {toolNames}): {msg.Content}");
                        }
                        else
                        {
                            rawContent.AppendLine($"[AI]: {msg.Content}");
                        }
                        break;
                    case "tool":
                        rawContent.AppendLine($"[Tool Result]: {msg.Content}");
                        break;
                }
            }
            rawContent.AppendLine("---");
        }

        // Use LLM to generate structured summary
        var summaryPrompt = "Please summarize the following conversation turns into a structured summary. Focus on:\n\n" +
            "1. Main task/objective being worked on\n" +
            "2. Key actions taken (especially file changes, code edits, tool usage)\n" +
            "3. Current progress/status\n" +
            "4. Pending/next steps\n" +
            "5. Any important decisions or discoveries\n\n" +
            "Format your response as:\n" +
            "- Task: [what was being worked on]\n" +
            "- Actions: [key actions with file paths if applicable]\n" +
            "- Progress: [current status]\n" +
            "- Next: [what needs to happen next]\n" +
            "- Notes: [any important caveats or discoveries]\n\n" +
            "Keep it concise but informative. If file paths or code locations are mentioned, preserve them accurately.\n\n" +
            "Raw conversation:\n" +
            rawContent;

        var messages = new List<ChatMessage>
        {
            ChatMessage.System("You are a context compression assistant. Summarize conversation history concisely while preserving critical information like file paths, code changes, and task status."),
            ChatMessage.User(summaryPrompt)
        };

        try
        {
            var response = await _llm.SendAsync(
                messages,
                new List<ToolDefinition>(),
                _ => { },
                ct,
                enableThinking: false);
            var summary = response.Content?.Trim() ?? "[Summary generation failed]";
            
            sb.AppendLine(summary);
            sb.AppendLine();
            sb.AppendLine("⚠️  [DISCLAIMER] This is a COMPRESSED summary and may not be complete or fully accurate.");
            sb.AppendLine("⚠️  Before trusting any specific details (especially file modifications, code locations, or factual claims),");
            sb.AppendLine("⚠️  please VERIFY by re-reading the actual files or re-checking the facts.");
            sb.AppendLine("⚠️  Do NOT assume code states or file contents based solely on this summary.");
            
            return sb.ToString();
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[Error generating summary: {ex.Message}]");
            sb.AppendLine();
            sb.AppendLine("⚠️  [DISCLAIMER] This is a COMPRESSED summary and may not be complete or fully accurate.");
            sb.AppendLine("⚠️  Please verify all details before acting on them.");
            return sb.ToString();
        }
    }
}
