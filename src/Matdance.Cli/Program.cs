using System.CommandLine;
using Matdance.Cli.Core;
using Matdance.Cli.Interactive;
using Matdance.Cli.Services;
using Matdance.Cli.Web;

MatdanceRuntime.ConfigureProcessEnvironment();

var agentsDirOption = new Option<string?>("--agents-dir", "Specify agents root directory (default: auto-detect or ./agents)");

var rootCommand = new RootCommand("Matdance Agent CLI - Manage agents, sessions, memory and workspace from terminal.");
rootCommand.AddGlobalOption(agentsDirOption);

// ===== Agent Commands =====
var agentCommand = new Command("agent", "Manage agents");

var agentCreate = new Command("create", "Create a new agent");
var createNameArg = new Argument<string>("name", "Agent name");
var createModelOpt = new Option<string?>("--model", "Model ID");
var createBaseUrlOpt = new Option<string?>("--base-url", "API base URL");
var createApiKeyOpt = new Option<string?>("--api-key", "API key");
var createApiTypeOpt = new Option<string?>("--api-type", "API/provider type (openai_chat, deepseek, anthropic)");
agentCreate.AddArgument(createNameArg);
agentCreate.AddOption(createModelOpt);
agentCreate.AddOption(createBaseUrlOpt);
agentCreate.AddOption(createApiKeyOpt);
agentCreate.AddOption(createApiTypeOpt);
agentCreate.SetHandler((string name, string? model, string? baseUrl, string? apiKey, string? apiType, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new AgentService(path).Create(name, model, baseUrl, apiKey, apiType);
}, createNameArg, createModelOpt, createBaseUrlOpt, createApiKeyOpt, createApiTypeOpt, agentsDirOption);

var agentDelete = new Command("delete", "Delete an agent");
var deleteNameArg = new Argument<string>("name", "Agent name");
agentDelete.AddArgument(deleteNameArg);
agentDelete.SetHandler((string name, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new AgentService(path).Delete(name);
}, deleteNameArg, agentsDirOption);

var agentList = new Command("list", "List all agents");
agentList.SetHandler((string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new AgentService(path).List();
}, agentsDirOption);

var agentConfig = new Command("config", "Show agent configuration");
var configNameArg = new Argument<string>("name", "Agent name");
var configEditOpt = new Option<bool>("--edit", "Open config in editor");
agentConfig.AddArgument(configNameArg);
agentConfig.AddOption(configEditOpt);
agentConfig.SetHandler((string name, bool edit, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    var svc = new AgentService(path);
    if (edit) svc.EditConfig(name); else svc.ShowConfig(name);
}, configNameArg, configEditOpt, agentsDirOption);

var agentIdentity = new Command("identity", "Edit agent identity");
var identityNameArg = new Argument<string>("name", "Agent name");
agentIdentity.AddArgument(identityNameArg);
agentIdentity.SetHandler((string name, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new AgentService(path).EditIdentity(name);
}, identityNameArg, agentsDirOption);

var agentUser = new Command("user", "Edit user profile for agent");
var userNameArg = new Argument<string>("name", "Agent name");
agentUser.AddArgument(userNameArg);
agentUser.SetHandler((string name, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new AgentService(path).EditUser(name);
}, userNameArg, agentsDirOption);

agentCommand.AddCommand(agentCreate);
agentCommand.AddCommand(agentDelete);
agentCommand.AddCommand(agentList);
agentCommand.AddCommand(agentConfig);
agentCommand.AddCommand(agentIdentity);
agentCommand.AddCommand(agentUser);

// ===== Session Commands =====
var sessionCommand = new Command("session", "Manage sessions");

var sessionCreate = new Command("create", "Create a new session");
var sCreateAgentArg = new Argument<string>("agent", "Agent name");
var sCreateIdOpt = new Option<string?>("--id", "Session ID (default: timestamp)");
sessionCreate.AddArgument(sCreateAgentArg);
sessionCreate.AddOption(sCreateIdOpt);
sessionCreate.SetHandler((string agent, string? id, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new SessionService(path).Create(agent, id);
}, sCreateAgentArg, sCreateIdOpt, agentsDirOption);

var sessionList = new Command("list", "List sessions for an agent");
var sListAgentArg = new Argument<string>("agent", "Agent name");
sessionList.AddArgument(sListAgentArg);
sessionList.SetHandler((string agent, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new SessionService(path).List(agent);
}, sListAgentArg, agentsDirOption);

var sessionShow = new Command("show", "Show session details");
var sShowAgentArg = new Argument<string>("agent", "Agent name");
var sShowIdArg = new Argument<string>("session-id", "Session ID");
sessionShow.AddArgument(sShowAgentArg);
sessionShow.AddArgument(sShowIdArg);
sessionShow.SetHandler((string agent, string sessionId, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new SessionService(path).Show(agent, sessionId);
}, sShowAgentArg, sShowIdArg, agentsDirOption);

var sessionDelete = new Command("delete", "Delete a session");
var sDelAgentArg = new Argument<string>("agent", "Agent name");
var sDelIdArg = new Argument<string>("session-id", "Session ID");
sessionDelete.AddArgument(sDelAgentArg);
sessionDelete.AddArgument(sDelIdArg);
sessionDelete.SetHandler((string agent, string sessionId, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new SessionService(path).Delete(agent, sessionId);
}, sDelAgentArg, sDelIdArg, agentsDirOption);

sessionCommand.AddCommand(sessionCreate);
sessionCommand.AddCommand(sessionList);
sessionCommand.AddCommand(sessionShow);
sessionCommand.AddCommand(sessionDelete);

// ===== Memory Commands =====
var memoryCommand = new Command("memory", "Manage memory");

var memHot = new Command("hot", "Show or edit hot memory");
var memHotAgentArg = new Argument<string>("agent", "Agent name");
var memHotEditOpt = new Option<bool>("--edit", "Open in editor");
memHot.AddArgument(memHotAgentArg);
memHot.AddOption(memHotEditOpt);
memHot.SetHandler((string agent, bool edit, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    var svc = new MemoryService(path);
    if (edit) svc.EditHot(agent); else svc.ShowHot(agent);
}, memHotAgentArg, memHotEditOpt, agentsDirOption);

var memLong = new Command("long", "Manage long term memory");
var memLongAgentArg = new Argument<string>("agent", "Agent name");
var memLongDateArg = new Argument<string?>("date", () => null, "Date (YYYY-MM-DD), omit to list files");
var memLongEditOpt = new Option<bool>("--edit", "Open in editor");
memLong.AddArgument(memLongAgentArg);
memLong.AddArgument(memLongDateArg);
memLong.AddOption(memLongEditOpt);
memLong.SetHandler((string agent, string? date, bool edit, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    var svc = new MemoryService(path);
    if (string.IsNullOrEmpty(date))
    {
        svc.ListLongTerm(agent);
    }
    else if (edit)
    {
        svc.EditLongTerm(agent, date);
    }
    else
    {
        svc.ShowLongTerm(agent, date);
    }
}, memLongAgentArg, memLongDateArg, memLongEditOpt, agentsDirOption);

var memCore = new Command("core", "Show or edit core memory");
var memCoreAgentArg = new Argument<string>("agent", "Agent name");
var memCoreEditOpt = new Option<bool>("--edit", "Open in editor");
memCore.AddArgument(memCoreAgentArg);
memCore.AddOption(memCoreEditOpt);
memCore.SetHandler((string agent, bool edit, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    var svc = new MemoryService(path);
    if (edit) svc.EditCore(agent); else svc.ShowCore(agent);
}, memCoreAgentArg, memCoreEditOpt, agentsDirOption);

var memVector = new Command("vector", "Show vector memory base");
var memVecAgentArg = new Argument<string>("agent", "Agent name");
memVector.AddArgument(memVecAgentArg);
memVector.SetHandler((string agent, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new MemoryService(path).ShowVector(agent);
}, memVecAgentArg, agentsDirOption);

var memSearch = new Command("search", "Search vector memory");
var memSearchAgentArg = new Argument<string>("agent", "Agent name");
var memSearchQueryArg = new Argument<string>("query", "Search query");
var memSearchTakeOpt = new Option<int>("--take", () => 5, "Number of search results");
memSearch.AddArgument(memSearchAgentArg);
memSearch.AddArgument(memSearchQueryArg);
memSearch.AddOption(memSearchTakeOpt);
memSearch.SetHandler((string agent, string query, int take, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    var result = new VectorMemoryService(path).Search(agent, query, take);
    Console.WriteLine($"[memory_search] {result.Algorithm} entries={result.EntryCount} candidates={result.CandidateCount} visited={result.VisitedNodes}");
    if (result.Items.Count == 0)
    {
        Console.WriteLine("No relevant memories found.");
        return;
    }

    foreach (var item in result.Items)
    {
        var entry = item.Entry;
        Console.WriteLine();
        Console.WriteLine($"--- {entry.Kind}: {entry.Title} #{entry.ChunkIndex} score={item.Score:0.000} ---");
        Console.WriteLine($"{entry.SourcePath}:{entry.StartLine}");
        Console.WriteLine($"rerank cosine={item.Cosine:0.000}, lexical={item.Lexical:0.000}, hamming={item.HammingSimilarity:0.000}");
        Console.WriteLine(entry.Text.Length > 900 ? entry.Text[..900] + "\n...[truncated]" : entry.Text);
    }
}, memSearchAgentArg, memSearchQueryArg, memSearchTakeOpt, agentsDirOption);

memoryCommand.AddCommand(memHot);
memoryCommand.AddCommand(memLong);
memoryCommand.AddCommand(memCore);
memoryCommand.AddCommand(memVector);
memoryCommand.AddCommand(memSearch);

// ===== Workspace Commands =====
var workspaceCommand = new Command("workspace", "Manage workspace");

var wsOpen = new Command("open", "Open workspace in file explorer");
var wsOpenAgentArg = new Argument<string>("agent", "Agent name");
wsOpen.AddArgument(wsOpenAgentArg);
wsOpen.SetHandler((string agent, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new WorkspaceService(path).Open(agent);
}, wsOpenAgentArg, agentsDirOption);

var wsTree = new Command("tree", "Show workspace directory tree");
var wsTreeAgentArg = new Argument<string>("agent", "Agent name");
wsTree.AddArgument(wsTreeAgentArg);
wsTree.SetHandler((string agent, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    new WorkspaceService(path).Tree(agent);
}, wsTreeAgentArg, agentsDirOption);

workspaceCommand.AddCommand(wsOpen);
workspaceCommand.AddCommand(wsTree);


// ===== Web UI Command =====
var webCommand = new Command("web", "Start the local C# HTML web interface");
var webHostOpt = new Option<string>("--host", () => "localhost", "Host to bind (default: localhost)");
var webPortOpt = new Option<int>("--port", () => 8765, "Port to bind (default: 8765)");
webCommand.AddOption(webHostOpt);
webCommand.AddOption(webPortOpt);
webCommand.SetHandler(async (string host, int port, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    var webProcess = new WebUiProcessManager(path);
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    webProcess.RegisterCurrentProcess(host, port);
    try
    {
        await new WebServer(path, host, port).RunAsync(cts.Token);
    }
    finally
    {
        webProcess.ClearCurrentProcess();
    }
}, webHostOpt, webPortOpt, agentsDirOption);

// ===== Managed Web UI Commands =====
var webUiCommand = new Command("web-ui", "Start, stop or restart the managed background Web UI process");
var webUiStart = new Command("start", "Start Web UI in the background");
var webUiStop = new Command("stop", "Stop the background Web UI process");
var webUiStopAll = new Command("stop-all", "Stop Web UI and disable hook, keep-alive, and boot supervisor tasks");
var webUiRestart = new Command("restart", "Restart Web UI in the background");
var webUiStatus = new Command("status", "Show background Web UI status");
var webUiSupervise = new Command("supervise", "Run the Web UI supervisor hook once");
var webUiSupervisor = new Command("supervisor", "Manage system-level Web UI supervisor tasks");
var webUiSupervisorEnable = new Command("enable", "Enable system-level keep-alive tasks");
var webUiSupervisorDisable = new Command("disable", "Disable system-level supervisor tasks");
var webUiSupervisorStatus = new Command("status", "Show system-level supervisor task status");
var webUiStartHostOpt = new Option<string>("--host", () => "localhost", "Host to bind (default: localhost)");
var webUiStartPortOpt = new Option<int>("--port", () => 8765, "Port to bind (default: 8765)");
var webUiStartModeOpt = new Option<string>("--mode", () => RuntimeSupervisorService.ModePreserve, "Run mode: preserve, fragile, keep-alive, autostart-keep-alive, keep-alive-no-autostart");
var webUiStartPublicOpt = new Option<bool>("--public", "Expose Web UI on 0.0.0.0 with single-token authentication");
var webUiRestartHostOpt = new Option<string>("--host", () => "localhost", "Host to bind (default: localhost)");
var webUiRestartPortOpt = new Option<int>("--port", () => 8765, "Port to bind (default: 8765)");
var webUiRestartModeOpt = new Option<string>("--mode", () => RuntimeSupervisorService.ModePreserve, "Run mode: preserve, fragile, keep-alive, autostart-keep-alive, keep-alive-no-autostart");
var webUiRestartPublicOpt = new Option<bool>("--public", "Expose Web UI on 0.0.0.0 with single-token authentication");
var webUiSuperviseHostOpt = new Option<string>("--host", () => "localhost", "Host to bind (default: localhost)");
var webUiSupervisePortOpt = new Option<int>("--port", () => 8765, "Port to bind (default: 8765)");
var webUiSuperviseKeepAliveOpt = new Option<bool>("--keep-alive", () => false, "Start Web UI if it is not running");
var webUiSuperviseRunDueOpt = new Option<bool>("--run-due", () => false, "Run due scheduled tasks headlessly when Web UI is not available");
var webUiSupervisePublicOpt = new Option<bool>("--public", "Expose keep-alive Web UI on 0.0.0.0 with single-token authentication");
var webUiSupervisorEnableHostOpt = new Option<string>("--host", () => "localhost", "Host to bind (default: localhost)");
var webUiSupervisorEnablePortOpt = new Option<int>("--port", () => 8765, "Port to bind (default: 8765)");
var webUiSupervisorAutostartOpt = new Option<bool>("--autostart", () => false, "Also start and keep Web UI alive after user logon");
var webUiSupervisorPublicOpt = new Option<bool>("--public", "Configure keep-alive/autostart for 0.0.0.0 with single-token authentication");
webUiStart.AddOption(webUiStartHostOpt);
webUiStart.AddOption(webUiStartPortOpt);
webUiStart.AddOption(webUiStartModeOpt);
webUiStart.AddOption(webUiStartPublicOpt);
webUiRestart.AddOption(webUiRestartHostOpt);
webUiRestart.AddOption(webUiRestartPortOpt);
webUiRestart.AddOption(webUiRestartModeOpt);
webUiRestart.AddOption(webUiRestartPublicOpt);
webUiSupervise.AddOption(webUiSuperviseHostOpt);
webUiSupervise.AddOption(webUiSupervisePortOpt);
webUiSupervise.AddOption(webUiSuperviseKeepAliveOpt);
webUiSupervise.AddOption(webUiSuperviseRunDueOpt);
webUiSupervise.AddOption(webUiSupervisePublicOpt);
webUiSupervisorEnable.AddOption(webUiSupervisorEnableHostOpt);
webUiSupervisorEnable.AddOption(webUiSupervisorEnablePortOpt);
webUiSupervisorEnable.AddOption(webUiSupervisorAutostartOpt);
webUiSupervisorEnable.AddOption(webUiSupervisorPublicOpt);
webUiStart.SetHandler(async (string host, int port, string mode, bool publicExposure, string? agentsDir) =>
{
    host = PrepareWebUiHost(host, publicExposure);
    var path = new PathService(agentsDir);
    var status = await new RuntimeSupervisorService(path).StartAsync(mode, host, port);
    Console.WriteLine(FormatWebUiStatus(status, "start requested"));
    PrintWebAuthStartupHint(host);
}, webUiStartHostOpt, webUiStartPortOpt, webUiStartModeOpt, webUiStartPublicOpt, agentsDirOption);
webUiStop.SetHandler(async (string? agentsDir) =>
{
    await new WebUiProcessManager(new PathService(agentsDir)).StopAsync();
    Console.WriteLine("[web-ui] stopped");
}, agentsDirOption);
webUiStopAll.SetHandler(async (string? agentsDir) =>
{
    var status = await new RuntimeSupervisorService(new PathService(agentsDir)).StopAllAsync("localhost", 8765);
    Console.WriteLine($"[web-ui] stopped all: hook={status.HookEnabled} keepAlive={status.KeepAliveEnabled} autostart={status.AutostartEnabled} web={FormatWebUiStatus(status.WebUi, "status")}");
}, agentsDirOption);
webUiRestart.SetHandler(async (string host, int port, string mode, bool publicExposure, string? agentsDir) =>
{
    host = PrepareWebUiHost(host, publicExposure);
    var path = new PathService(agentsDir);
    var status = await new RuntimeSupervisorService(path).RestartAsync(mode, host, port);
    Console.WriteLine(FormatWebUiStatus(status, "restart requested"));
    PrintWebAuthStartupHint(host);
}, webUiRestartHostOpt, webUiRestartPortOpt, webUiRestartModeOpt, webUiRestartPublicOpt, agentsDirOption);
webUiStatus.SetHandler(async (string? agentsDir) =>
{
    var status = await new WebUiProcessManager(new PathService(agentsDir)).GetStatusAsync();
    Console.WriteLine(FormatWebUiStatus(status, "status"));
}, agentsDirOption);
webUiSupervise.SetHandler(async (string host, int port, bool keepAlive, bool runDue, bool publicExposure, string? agentsDir) =>
{
    host = PrepareWebUiHost(host, publicExposure);
    var result = await new RuntimeSupervisorService(new PathService(agentsDir)).SuperviseAsync(keepAlive, runDue, host, port);
    Console.WriteLine($"[web-ui] supervise keepAlive={result.KeepAliveRequested} runDue={result.RunDueRequested} due={result.DueRun.DueCount} ran={result.DueRun.Ran} skipped={result.DueRun.Skipped} web={FormatWebUiStatus(result.WebUi, "supervise")}");
    if (keepAlive) PrintWebAuthStartupHint(host);
}, webUiSuperviseHostOpt, webUiSupervisePortOpt, webUiSuperviseKeepAliveOpt, webUiSuperviseRunDueOpt, webUiSupervisePublicOpt, agentsDirOption);
webUiSupervisorEnable.SetHandler(async (string host, int port, bool autostart, bool publicExposure, string? agentsDir) =>
{
    host = PrepareWebUiHost(host, publicExposure);
    var mode = autostart ? RuntimeSupervisorService.ModeAutostartKeepAlive : RuntimeSupervisorService.ModeKeepAliveNoAutostart;
    await new RuntimeSupervisorService(new PathService(agentsDir)).ConfigureSystemTasksAsync(mode, host, port);
    Console.WriteLine(autostart ? "[web-ui] supervisor enabled with autostart" : "[web-ui] autostart disabled; persistent boot/logon tasks removed");
    PrintWebAuthStartupHint(host);
}, webUiSupervisorEnableHostOpt, webUiSupervisorEnablePortOpt, webUiSupervisorAutostartOpt, webUiSupervisorPublicOpt, agentsDirOption);
webUiSupervisorDisable.SetHandler(async (string? agentsDir) =>
{
    await new RuntimeSupervisorService(new PathService(agentsDir)).ConfigureSystemTasksAsync(RuntimeSupervisorService.ModeFragile, "localhost", 8765);
    Console.WriteLine("[web-ui] supervisor disabled");
}, agentsDirOption);
webUiSupervisorStatus.SetHandler(async (string? agentsDir) =>
{
    var status = await new RuntimeSupervisorService(new PathService(agentsDir)).GetSupervisorStatusAsync();
    Console.WriteLine($"[web-ui] supervisor mode={status.Mode} hook={status.HookEnabled} keepAlive={status.KeepAliveEnabled} autostart={status.AutostartEnabled} web={FormatWebUiStatus(status.WebUi, "status")}");
}, agentsDirOption);
webUiCommand.AddCommand(webUiStart);
webUiCommand.AddCommand(webUiStop);
webUiCommand.AddCommand(webUiStopAll);
webUiCommand.AddCommand(webUiRestart);
webUiCommand.AddCommand(webUiStatus);
webUiCommand.AddCommand(webUiSupervise);
webUiSupervisor.AddCommand(webUiSupervisorEnable);
webUiSupervisor.AddCommand(webUiSupervisorDisable);
webUiSupervisor.AddCommand(webUiSupervisorStatus);
webUiCommand.AddCommand(webUiSupervisor);

// ===== Dependency Commands =====
var depsCommand = new Command("deps", "Install local runtime dependencies under the Matdance install root");
var depsInstall = new Command("install", "Install Playwright browser dependencies");
var depsSourceOpt = new Option<string>("--source", () => "auto", "Download source: auto, global, or cn");
depsInstall.AddOption(depsSourceOpt);
depsInstall.SetHandler(async (string source) =>
{
    var selected = source.Trim().ToLowerInvariant() switch
    {
        "cn" or "china" => DependencySource.Cn,
        "global" or "official" => DependencySource.Global,
        _ => DependencySource.Auto
    };
    await new DependencyInstallerService().InstallAsync(selected, Console.WriteLine);
}, depsSourceOpt);
depsCommand.AddCommand(depsInstall);

// ===== Launcher Registration =====
var installEntryCommand = new Command("install-entry", "Register the `matdance` launcher in PATH");
var userOnlyOpt = new Option<bool>("--user", "Register only for the current user");
installEntryCommand.AddOption(userOnlyOpt);
installEntryCommand.SetHandler((bool userOnly, string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    var result = new LauncherRegistrationService().Register(preferSystem: !userOnly, agentsRoot: path.AgentsRoot);
    Console.WriteLine(result);
}, userOnlyOpt, agentsDirOption);

var stopAllCommand = new Command("stop-all", "Stop Web UI and disable hook, keep-alive, and boot supervisor tasks");
stopAllCommand.SetHandler(async (string? agentsDir) =>
{
    var status = await new RuntimeSupervisorService(new PathService(agentsDir)).StopAllAsync("localhost", 8765);
    Console.WriteLine($"[matdance] stopped all: hook={status.HookEnabled} keepAlive={status.KeepAliveEnabled} autostart={status.AutostartEnabled} web={FormatWebUiStatus(status.WebUi, "status")}");
}, agentsDirOption);

// ===== Terminal Chat Command =====
var chatCommand = new Command("chat", "Start the legacy interactive terminal chat");
chatCommand.SetHandler(async (string? agentsDir) =>
{
    var path = new PathService(agentsDir);
    await RunTerminalChatAsync(path);
}, agentsDirOption);

// Register all
rootCommand.AddCommand(agentCommand);
rootCommand.AddCommand(sessionCommand);
rootCommand.AddCommand(memoryCommand);
rootCommand.AddCommand(workspaceCommand);
rootCommand.AddCommand(webCommand);
rootCommand.AddCommand(webUiCommand);
rootCommand.AddCommand(depsCommand);
rootCommand.AddCommand(installEntryCommand);
rootCommand.AddCommand(stopAllCommand);
rootCommand.AddCommand(chatCommand);

// ===== Interactive mode when no args =====
if (args.Length == 0)
{
    try
    {
        var path = new PathService();
        await new MainMenu(path).RunAsync();
        return 0;
    }
    catch (Exception ex)
    {
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), "matdance_error.log");
        File.WriteAllText(logPath, $"[{UserTimeZoneService.Now():yyyy-MM-dd HH:mm:ss zzz}] {ex.GetType().FullName}: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}");
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine($"Full details written to: {logPath}");
        return 1;
    }
}

return await rootCommand.InvokeAsync(args);

static async Task RunTerminalChatAsync(PathService path)
{
    var taskService = new ScheduledTaskService(path);
    taskService.EnsureAllSystemTasks();
    var wizard = new SetupWizard(path);
    var (agentName, sessionId, sessionData, sessionState, config) = await wizard.RunAsync();
    var loop = new ChatLoop(agentName, sessionId, sessionData, sessionState, config, path);
    await loop.RunAsync();
}

static string PrepareWebUiHost(string host, bool publicExposure)
{
    if (publicExposure)
        host = "0.0.0.0";

    if (WebAuthService.IsRemoteBinding(host))
    {
        Environment.SetEnvironmentVariable("MATDANCE_ALLOW_REMOTE_WEB", "1");
        WebAuthService.LoadOrCreate(host);
    }

    return host;
}

static void PrintWebAuthStartupHint(string host)
{
    if (!WebAuthService.IsRemoteBinding(host))
        return;

    var auth = WebAuthService.LoadOrCreate(host);
    Console.WriteLine($"[web-auth] single-token authentication enabled source={auth.Source}");
    if (!string.IsNullOrWhiteSpace(auth.TokenForLocalDisplay))
        Console.WriteLine($"[web-auth] token: {auth.TokenForLocalDisplay}");
    Console.WriteLine($"[web-auth] token file: {WebAuthService.StatePath}");
}

static string FormatWebUiStatus(WebUiStatus status, string fallback)
{
    if (!status.IsRunning)
        return "[web-ui] stopped";

    var backend = status.BackendReady ? "backend=ready" : "backend=starting";
    var browser = status.BrowserReady
        ? "browser=ready"
        : status.BrowserDependenciesInstalled
            ? "browser=warming"
            : "browser=deps-missing";
    var message = string.IsNullOrWhiteSpace(status.Message) ? "" : " - " + status.Message;
    return $"[web-ui] running at {status.Url} pid={status.ProcessId} uptime={status.Uptime} {backend} {browser}{message}";
}
