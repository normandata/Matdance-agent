namespace Matdance.Cli.Core;

public static class TokenCounter
{

    public static int Estimate(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int chineseCount = text.Count(c => c >= 0x4E00 && c <= 0x9FFF);
        int otherCount = text.Length - chineseCount;
        return (int)Math.Ceiling(chineseCount / 1.5) + (int)Math.Ceiling(otherCount / 3.5);
    }

    public static int Estimate(IEnumerable<string> texts)
    {
        return texts.Sum(Estimate);
    }

    public static int EstimateMessages(IEnumerable<Models.ChatMessage> messages)
    {
        int total = 0;
        foreach (var msg in messages)
        {
            total += Estimate(msg.Content);
            total += Estimate(msg.ReasoningContent ?? string.Empty);
            if (msg.Attachments != null)
            {
                foreach (var attachment in msg.Attachments)
                {
                    total += Estimate(attachment.Name)
                        + Estimate(attachment.Kind)
                        + Estimate(attachment.RelativePath)
                        + Estimate(attachment.Summary);
                }
            }
            if (msg.ToolCalls != null)
            {
                foreach (var tc in msg.ToolCalls)
                {
                    total += Estimate(tc.Function.Name) + Estimate(tc.Function.Arguments);
                }
            }
        }
        return total;
    }

    public static int EstimateTools(IEnumerable<ToolDefinition>? tools)
    {
        if (tools == null) return 0;

        var total = 0;
        foreach (var tool in tools)
        {
            total += Estimate(tool.Name);
            total += Estimate(tool.Description);
            total += Estimate(tool.Parameters.ToJsonString());
        }

        return total;
    }

    public static int EstimateRequest(IEnumerable<Models.ChatMessage> messages, IEnumerable<ToolDefinition>? tools = null)
    {
        return EstimateMessages(messages) + EstimateTools(tools);
    }
}
