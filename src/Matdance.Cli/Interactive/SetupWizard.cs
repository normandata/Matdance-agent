using Spectre.Console;
using Matdance.Cli.Models;
using Matdance.Cli.Services;

namespace Matdance.Cli.Interactive;

public class SetupWizard
{
    private readonly PathService _path;
    private readonly AgentService _agentService;
    private readonly SessionService _sessionService;

    public SetupWizard(PathService path)
    {
        _path = path;
        _agentService = new AgentService(path);
        _sessionService = new SessionService(path);
    }

    public async Task<(string agentName, string sessionId, SessionData sessionData, SessionState sessionState, AgentConfig config)> RunAsync()
    {
        AnsiConsole.Clear();

        // Fancy header
        AnsiConsole.Write(new FigletText("Matdance").Centered().Color(Color.DodgerBlue1));
        AnsiConsole.Write(new Rule("[dim]Multi-Agent Terminal Dance[/]").RuleStyle("grey").Centered());
        AnsiConsole.WriteLine();

        var agentName = await SelectOrCreateAgentAsync();
        var config = AgentConfig.Load(_path.GetAgentConfigJsonPath(agentName));
        var (sessionId, sessionData) = await SelectOrCreateSessionAsync(agentName);
        var sessionState = SessionState.Load(_path.GetSessionsPath(agentName) + $"/{sessionId}.json");

        // Summary panel
        var summary = new Grid();
        summary.AddColumn(new GridColumn().Width(8));
        summary.AddColumn();
        summary.AddRow("[green]Agent[/]", $"[bold]{agentName.EscapeMarkup()}[/]");
        summary.AddRow("[green]Session[/]", $"[bold]{sessionId.EscapeMarkup()}[/]");
        summary.AddRow("[green]Model[/]", config.ModelId.EscapeMarkup());

        var panel = new Panel(summary)
        {
            Header = new PanelHeader("Ready"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Padding = new Padding(2, 1),
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        return (agentName, sessionId, sessionData, sessionState, config);
    }

    private async Task<string> SelectOrCreateAgentAsync()
    {
        var agents = new List<string>();
        if (Directory.Exists(_path.AgentsRoot))
        {
            agents = Directory.GetDirectories(_path.AgentsRoot)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList()!;
        }

        if (agents.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No agents found. Let's create your first agent.[/]");
            return await CreateAgentInteractiveAsync();
        }

        agents.Add("[green]+ Create new agent[/]");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select an agent[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Green, decoration: Decoration.Bold))
                .AddChoices(agents));

        if (choice.StartsWith("[green]+"))
        {
            return await CreateAgentInteractiveAsync();
        }

        return choice;
    }

    private Task<string> CreateAgentInteractiveAsync()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Create New Agent[/]");
        AnsiConsole.Write(new Rule().RuleStyle("grey"));

        var name = AnsiConsole.Prompt(new TextPrompt<string>("[dim]Agent name:[/]").Validate(n => !string.IsNullOrWhiteSpace(n) ? ValidationResult.Success() : ValidationResult.Error("Name cannot be empty")));
        var provider = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[dim]Provider:[/]")
            .AddChoices(ModelProviderCatalog.ApiTypes()));
        var providerDef = ModelProviderCatalog.FindProvider(provider);
        var modelChoices = providerDef?.Models.Select(item => item.Id).ToList() ?? new List<string>();
        var model = modelChoices.Count > 0
            ? AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[dim]Model:[/]").AddChoices(modelChoices))
            : AnsiConsole.Prompt(new TextPrompt<string>("[dim]Model ID:[/]").DefaultValue("gpt-5.5"));
        var baseUrl = providerDef?.LocksBaseUrl == true
            ? providerDef.BaseUrl
            : AnsiConsole.Prompt(new TextPrompt<string>("[dim]Base URL:[/]").DefaultValue(providerDef?.BaseUrl ?? "https://api.openai.com/v1"));
        var apiKey = AnsiConsole.Prompt(new TextPrompt<string>("[dim]API Key:[/]").Secret());
        var apiType = provider;

        _agentService.Create(name, model, baseUrl, apiKey, apiType);

        AnsiConsole.MarkupLine($"[green]✓ Created agent '{name.EscapeMarkup()}'[/]");
        AnsiConsole.WriteLine();

        return Task.FromResult(name);
    }

    private Task<(string id, SessionData data)> SelectOrCreateSessionAsync(string agentName)
    {
        var sessionsDir = _path.GetSessionsPath(agentName);
        var sessions = new List<(string id, SessionData data)>();

        if (Directory.Exists(sessionsDir))
        {
            foreach (var file in Directory.GetFiles(sessionsDir, "*.json").Where(f => !f.EndsWith(".state.json")))
            {
                var data = SessionData.Load(file);
                sessions.Add((data.SessionId, data));
            }
        }

        var choices = sessions.Select(s => $"{s.id}  │  msgs:{s.data.TotalMessages}  │  {s.data.CreateAt:MM-dd HH:mm}").ToList();
        choices.Add("[green]+ Create new session[/]");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select a session[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Green, decoration: Decoration.Bold))
                .AddChoices(choices));

        if (choice.StartsWith("[green]+"))
        {
            var id = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var data = new SessionData { SessionId = id, CreateAt = UserTimeZoneService.Now(), LastActivity = UserTimeZoneService.Now() };
            var filePath = Path.Combine(sessionsDir, $"{id}.json");
            Directory.CreateDirectory(sessionsDir);
            data.Save(filePath);
            return Task.FromResult((id, data));
        }

        var selectedId = choice.Split('│')[0].Trim();
        var selectedData = sessions.First(s => s.id == selectedId).data;
        return Task.FromResult((selectedId, selectedData));
    }
}
