using System.Text;
using Spectre.Console;
using Matdance.Cli.Models;

namespace Matdance.Cli.Interactive;

public static class ChatRenderer
{
    // ── Colors ──
    private static readonly Color CtxGreen = Color.Green;
    private static readonly Color CtxYellow = Color.Yellow;
    private static readonly Color CtxRed = Color.Red;
    private static readonly Color AccentBlue = Color.DodgerBlue1;
    private static readonly Color AccentOrange = Color.Orange1;
    private static readonly Color DimGrey = Color.Grey;
    private static readonly Color PanelBorder = Color.Grey23;

    public static void RenderHeader(string agentName, string model, string sessionId)
    {
        AnsiConsole.WriteLine();
        var headerText = $"[bold]{agentName.EscapeMarkup()}[/]  [dim]•[/]  [silver]{model.EscapeMarkup()}[/]  [dim]•[/]  [dim]{sessionId.EscapeMarkup()}[/]";
        var rule = new Rule(headerText)
        {
            Style = new Style(AccentBlue),
            Border = BoxBorder.Double
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    public static void RenderUserMessage(string input)
    {
        AnsiConsole.WriteLine();
        var panel = new Panel(new Markup($"[bold deepskyblue2]>[/] [white]{input.EscapeMarkup()}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.DeepSkyBlue4),
            Padding = new Padding(1, 0),
        };
        AnsiConsole.Write(panel);
    }

    public static void RenderAssistantStart()
    {
    }

    public static void RenderAssistantEnd()
    {
    }

    public static void RenderToolResultPanel(string toolName, string? detail, string result)
    {
        var d = string.IsNullOrEmpty(detail) ? "" : $" [dim]{detail.EscapeMarkup()}[/]";
        var header = $"[bold yellow]⚡ {toolName.EscapeMarkup()}[/]{d}";

        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(3).ToList();
        var sb = new StringBuilder();
        sb.AppendLine(header);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 100) trimmed = trimmed[..100] + "...";
            sb.AppendLine($"[grey]{trimmed.EscapeMarkup()}[/]");
        }
        var total = result.Split('\n').Length;
        if (total > 3)
        {
            sb.AppendLine($"[dim]… {total - 3} more lines[/]");
        }

        var panel = new Panel(new Markup(sb.ToString()))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Orange1),
            Padding = new Padding(1, 0),
        };

        AnsiConsole.WriteLine();
        AnsiConsole.Write(panel);
    }

    public static void RenderHistory(List<ChatMessage> messages, bool full = false)
    {
        if (messages.Count == 0) return;

        var recent = messages
            .Where(m => m.Role == "user" || m.Role == "assistant")
            .ToList();

        if (!full)
        {
            recent = recent.TakeLast(4).ToList();
        }

        if (recent.Count == 0) return;

        AnsiConsole.WriteLine();
        var rule = new Rule("[dim]Recent Context[/]")
        {
            Style = new Style(DimGrey),
            Border = BoxBorder.None
        };
        AnsiConsole.Write(rule);

        foreach (var msg in recent)
        {
            switch (msg.Role)
            {
                case "user":
                    var up = msg.Content.Length > 80 ? msg.Content[..80].EscapeMarkup() + "…" : msg.Content.EscapeMarkup();
                    AnsiConsole.MarkupLine($"[dim]  >[/] [deepskyblue2]{up}[/]");
                    break;
                case "assistant":
                    var ap = msg.Content.Length > 80 ? msg.Content[..80].EscapeMarkup() + "…" : msg.Content.EscapeMarkup();
                    var tc = msg.ToolCalls != null && msg.ToolCalls.Count > 0
                        ? $" [dim]({msg.ToolCalls.Count} tools)[/]"
                        : "";
                    AnsiConsole.MarkupLine($"[dim]  <[/] [silver]{ap}[/]{tc}");
                    break;
            }
        }
        AnsiConsole.WriteLine();
    }

    public static void RenderStatusBar(SessionData data, SessionState state, AgentConfig config)
    {
        var ctxColor = data.ContextUsage < 50 ? CtxGreen : data.ContextUsage < 80 ? CtxYellow : CtxRed;
        var taskInfo = state.ActiveTask?.Status == "in_process"
            ? $"[yellow]{(state.ActiveTask.Title ?? "untitled").EscapeMarkup()}[/]"
            : "[dim]idle[/]";
        var taskIcon = state.ActiveTask?.Status == "in_process" ? "▶" : "○";

        // Unicode block progress bar
        var barWidth = 12;
        var filled = (int)Math.Round(data.ContextUsage / 100.0 * barWidth);
        var bar = $"[[{new string('█', filled)}{new string('░', barWidth - filled)}]]";

        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(3));
        grid.AddColumn(new GridColumn().Width(24));
        grid.AddColumn(new GridColumn().Width(3));
        grid.AddColumn();
        grid.AddColumn(new GridColumn().Width(12));

        grid.AddRow(
            new Markup($"[dim]{taskIcon}[/]"),
            new Markup(taskInfo),
            new Markup("[dim]│[/]"),
            new Markup($"[dim]ctx[/] [{ctxColor.ToMarkup()}]{bar}[/] [bold {ctxColor.ToMarkup()}]{data.ContextUsage}%[/] [dim]({data.Tokens:N0} / {config.ContextWindow:N0})[/]"),
            new Markup($"[dim]msgs {data.TotalMessages}[/]")
        );

        var panel = new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(PanelBorder),
            Padding = new Padding(1, 0),
        };

        AnsiConsole.WriteLine();
        AnsiConsole.Write(panel);
    }

    public static void RenderPromptPreview(string systemPrompt, List<ChatMessage> history)
    {
        AnsiConsole.WriteLine();

        var sysPanel = new Panel(new Markup($"[dim]{systemPrompt.EscapeMarkup()}[/]"))
        {
            Header = new PanelHeader("System Prompt"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey35),
            Padding = new Padding(1, 0),
        };
        AnsiConsole.Write(sysPanel);

        AnsiConsole.WriteLine();
        var histTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey35)
            .AddColumn("Role", c => c.Width(12))
            .AddColumn("Content");

        foreach (var msg in history.TakeLast(10))
        {
            var color = msg.Role switch
            {
                "user" => "deepskyblue2",
                "assistant" => "white",
                "tool" => "grey",
                _ => "white"
            };
            var preview = msg.Content.Length > 120 ? msg.Content[..120].EscapeMarkup() + "…" : msg.Content.EscapeMarkup();
            histTable.AddRow($"[{color}]{msg.Role}[/]", $"[dim]{preview}[/]");
        }
        AnsiConsole.Write(histTable);
        AnsiConsole.WriteLine();
    }

    public static void RenderStatusPage(SessionData data, SessionState state, AgentConfig config)
    {
        AnsiConsole.WriteLine();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(AccentBlue)
            .AddColumn("Property", c => c.Width(16).RightAligned())
            .AddColumn("Value");

        table.AddRow("[bold]Agent[/]", config.Name.EscapeMarkup());
        table.AddRow("[bold]Model[/]", config.ModelId.EscapeMarkup());
        table.AddRow("[bold]API[/]", $"{config.ApiType.EscapeMarkup()} @ {config.BaseUrl.EscapeMarkup()}");

        var ctxColor = data.ContextUsage < 50 ? "green" : data.ContextUsage < 80 ? "yellow" : "red";
        table.AddRow("[bold]Context[/]", $"[{ctxColor}]{data.ContextUsage}%[/] used ({data.Tokens:N0} / {config.ContextWindow:N0})");
        table.AddRow("[bold]Messages[/]", data.TotalMessages.ToString());
        table.AddRow("[bold]Tool Calls[/]", data.ToolMessagesCount.ToString());
        table.AddRow("[bold]Traced Files[/]", state.TracedFiles.Count.ToString());
        table.AddRow("[bold]Active Task[/]", state.ActiveTask?.Title.EscapeMarkup() ?? "[dim]none[/]");
        table.AddRow("[bold]Task Status[/]", state.ActiveTask?.Status.EscapeMarkup() ?? "[dim]N/A[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public static void RenderTaskTable(ActiveTaskInfo task)
    {
        AnsiConsole.WriteLine();
        var statusColor = task.Status == "in_process" ? "yellow" : task.Status == "done" ? "green" : "grey";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Orange1)
            .AddColumn("#", c => c.Width(4).Centered())
            .AddColumn("Status", c => c.Width(12))
            .AddColumn("Description");

        table.Title = new TableTitle($"[bold]{(task.Title ?? "untitled").EscapeMarkup()}[/] [dim]({task.Status})[/]");

        foreach (var s in task.Steps)
        {
            var color = s.Status == "in_process" ? "yellow" : s.Status == "done" ? "green" : "grey";
            var marker = s.Status == "done" ? "✓" : s.Status == "in_process" ? "▶" : "○";
            table.AddRow(
                $"[dim]{marker}[/]",
                $"[{color}]{s.Status.EscapeMarkup()}[/]",
                $"[dim]{s.ForWhat.EscapeMarkup()}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public static void RenderTracedFiles(List<TracedFileInfo> files)
    {
        AnsiConsole.WriteLine();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.DodgerBlue1)
            .AddColumn("Kind")
            .AddColumn("ID")
            .AddColumn("Path")
            .AddColumn("Range", c => c.Width(14))
            .AddColumn("Size", c => c.Width(10).RightAligned())
            .AddColumn("Last Read", c => c.Width(12));

        foreach (var f in files)
        {
            table.AddRow(
                $"[dim]{f.Kind.EscapeMarkup()}[/]",
                $"[dim]{f.Id.EscapeMarkup()}[/]",
                $"[dim]{f.Path.EscapeMarkup()}[/]",
                $"[dim]L{f.StartLine}-L{f.EndLine}[/]",
                $"[dim]{f.Content.Length:N0}[/]",
                $"[dim]{f.LastRead:HH:mm:ss}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public class MatrixSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
        public override bool IsUnicode => true;
        public override IReadOnlyList<string> Frames => new[]
        {
            "▖", "▘", "▝", "▗",
            "◢", "◣", "◤", "◥",
            "▪", "■", "□", "■",
        };
    }
}
