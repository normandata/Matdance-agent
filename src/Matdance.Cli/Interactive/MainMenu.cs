using System.Globalization;
using Matdance.Cli.Services;
using Spectre.Console;

namespace Matdance.Cli.Interactive;

public sealed class MainMenu
{
    private const string DefaultHost = "localhost";
    private const int DefaultPort = 8765;

    private readonly PathService _path;
    private readonly WebUiProcessManager _web;
    private readonly RuntimeSupervisorService _supervisor;
    private readonly DependencyInstallerService _dependencies = new();
    private readonly LauncherRegistrationService _launcher = new();
    private MenuLanguage _language;

    public MainMenu(PathService path)
    {
        _path = path;
        _web = new WebUiProcessManager(path);
        _supervisor = new RuntimeSupervisorService(path);
        _language = DetectSystemLanguage();
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            await RenderHeaderAsync(ct);
            var text = Text();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]" + text.SelectAction.EscapeMarkup() + "[/]")
                    .PageSize(8)
                    .AddChoices(
                        text.InstallDeps,
                        text.WebRuntimeMenu,
                        text.RegisterEntry,
                        text.SwitchEnglish,
                        text.SwitchChinese,
                        text.Exit));

            var waitAfterAction = true;
            try
            {
                if (choice.StartsWith("1.", StringComparison.Ordinal))
                    await InstallDependenciesAsync(ct);
                else if (choice.StartsWith("2.", StringComparison.Ordinal))
                {
                    await RunWebRuntimeMenuAsync(ct);
                    waitAfterAction = false;
                }
                else if (choice.StartsWith("3.", StringComparison.Ordinal))
                    RegisterLauncher();
                else if (choice.StartsWith("4.", StringComparison.Ordinal))
                {
                    _language = MenuLanguage.English;
                    waitAfterAction = false;
                }
                else if (choice.StartsWith("5.", StringComparison.Ordinal))
                {
                    _language = MenuLanguage.Chinese;
                    waitAfterAction = false;
                }
                else
                    return;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] " + ex.Message.EscapeMarkup());
            }

            if (waitAfterAction)
                WaitForContinue();
        }
    }

    private async Task RunWebRuntimeMenuAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await RenderHeaderAsync(ct);
            var text = Text();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]" + text.WebRuntimeTitle.EscapeMarkup() + "[/]")
                    .PageSize(13)
                    .AddChoices(
                        text.WebStatus,
                        text.StartFragile,
                        text.StartKeepAlive,
                        text.RestartPreserve,
                        text.StopWeb,
                        text.EnableAutostartKeepAlive,
                        text.DisableAutostartKeepAlive,
                        text.DisableSupervisor,
                        text.RunSupervisorHook,
                        text.StopAll,
                        text.Back));

            try
            {
                if (choice.StartsWith("1.", StringComparison.Ordinal))
                    await ShowRuntimeStatusAsync(ct);
                else if (choice.StartsWith("2.", StringComparison.Ordinal))
                    await StartWebAsync(RuntimeSupervisorService.ModeFragile);
                else if (choice.StartsWith("3.", StringComparison.Ordinal))
                    await StartWebAsync(RuntimeSupervisorService.ModeKeepAliveNoAutostart);
                else if (choice.StartsWith("4.", StringComparison.Ordinal))
                    await RestartWebAsync(RuntimeSupervisorService.ModePreserve);
                else if (choice.StartsWith("5.", StringComparison.Ordinal))
                    await StopWebAsync();
                else if (choice.StartsWith("6.", StringComparison.Ordinal))
                    await EnableSupervisorAsync(RuntimeSupervisorService.ModeAutostartKeepAlive, ct);
                else if (choice.StartsWith("7.", StringComparison.Ordinal))
                    await EnableSupervisorAsync(RuntimeSupervisorService.ModeKeepAliveNoAutostart, ct);
                else if (choice.StartsWith("8.", StringComparison.Ordinal))
                    await EnableSupervisorAsync(RuntimeSupervisorService.ModeFragile, ct);
                else if (choice.StartsWith("9.", StringComparison.Ordinal))
                    await RunSupervisorHookAsync(ct);
                else if (choice.StartsWith("10.", StringComparison.Ordinal))
                    await StopAllAsync(ct);
                else
                    return;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] " + ex.Message.EscapeMarkup());
            }

            WaitForContinue();
        }
    }

    private async Task RenderHeaderAsync(CancellationToken ct)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Matdance").Centered().Color(Color.DodgerBlue1));

        var text = Text();
        var supervisor = await SafeSupervisorStatusAsync(ct);
        var status = supervisor?.WebUi ?? _web.GetStatus();
        var statusText = status.IsRunning
            ? $"[green]{text.WebRunning.EscapeMarkup()}[/]  {status.Url.EscapeMarkup()}  pid:{status.ProcessId}  uptime:{FormatDuration(status.Uptime)}"
            : "[yellow]" + text.WebStopped.EscapeMarkup() + "[/]";
        var runtimeText = supervisor == null
            ? text.SupervisorUnknown
            : $"{text.SupervisorMode}: {ModeLabel(supervisor.Mode)} | hook:{OnOff(supervisor.HookEnabled)} keep:{OnOff(supervisor.KeepAliveEnabled)} boot:{OnOff(supervisor.AutostartEnabled)}";
        var meta = $"{statusText}\n[dim]{runtimeText.EscapeMarkup()}[/]\n[dim]OS: {MatdanceRuntime.OsName} {MatdanceRuntime.Architecture} | {text.ShellTool.EscapeMarkup()}: {MatdanceRuntime.ShellInvocation} | Agents: {_path.AgentsRoot.EscapeMarkup()} | {text.Language.EscapeMarkup()}: {text.LanguageName.EscapeMarkup()}[/]";

        AnsiConsole.Write(new Panel(meta)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(status.IsRunning ? Color.Green : Color.Yellow),
            Padding = new Padding(2, 1)
        });
        AnsiConsole.WriteLine();
    }

    private async Task<WebUiSupervisorStatus?> SafeSupervisorStatusAsync(CancellationToken ct)
    {
        try
        {
            return await _supervisor.GetSupervisorStatusAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task InstallDependenciesAsync(CancellationToken ct)
    {
        var text = Text();
        var source = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]" + text.DownloadSource.EscapeMarkup() + "[/]")
                .AddChoices(text.CnSource, text.GlobalSource));

        var selected = source.Contains("CN", StringComparison.OrdinalIgnoreCase)
            ? DependencySource.Cn
            : DependencySource.Global;

        AnsiConsole.MarkupLine("[cyan]" + text.InstallingDeps.EscapeMarkup() + "[/]");
        await _dependencies.InstallAsync(selected, line =>
        {
            AnsiConsole.MarkupLine("[dim]" + TrimLogLine(line).EscapeMarkup() + "[/]");
        }, ct);

        AnsiConsole.MarkupLine("[green]" + text.DependenciesInstalled.EscapeMarkup() + "[/]");
    }

    private async Task ShowRuntimeStatusAsync(CancellationToken ct)
    {
        var status = await _supervisor.GetSupervisorStatusAsync(ct);
        RenderWebStatus(status.WebUi, Text().StatusLabel);
        AnsiConsole.MarkupLine("[cyan]" + $"{Text().SupervisorMode}: {ModeLabel(status.Mode)}".EscapeMarkup() + "[/]");
        AnsiConsole.MarkupLine("[dim]" + $"hook:{OnOff(status.HookEnabled)} keep:{OnOff(status.KeepAliveEnabled)} boot:{OnOff(status.AutostartEnabled)}".EscapeMarkup() + "[/]");
    }

    private async Task StartWebAsync(string mode)
    {
        var status = await _supervisor.StartAsync(mode, DefaultHost, DefaultPort);
        RenderWebStatus(status, Text().Started);
    }

    private async Task StopWebAsync()
    {
        await _web.StopAsync();
        AnsiConsole.MarkupLine("[green]" + Text().WebStoppedSentence.EscapeMarkup() + "[/]");
    }

    private async Task RestartWebAsync(string mode)
    {
        var status = await _supervisor.RestartAsync(mode, DefaultHost, DefaultPort);
        RenderWebStatus(status, Text().Restarted);
    }

    private async Task EnableSupervisorAsync(string mode, CancellationToken ct)
    {
        await _supervisor.ConfigureSystemTasksAsync(mode, DefaultHost, DefaultPort, ct);
        var status = await _supervisor.GetSupervisorStatusAsync(ct);
        AnsiConsole.MarkupLine("[green]" + string.Format(Text().SupervisorUpdated, ModeLabel(status.Mode)).EscapeMarkup() + "[/]");
        AnsiConsole.MarkupLine("[dim]" + $"hook:{OnOff(status.HookEnabled)} keep:{OnOff(status.KeepAliveEnabled)} boot:{OnOff(status.AutostartEnabled)}".EscapeMarkup() + "[/]");
    }

    private async Task RunSupervisorHookAsync(CancellationToken ct)
    {
        var result = await _supervisor.SuperviseAsync(keepAlive: false, runDue: true, DefaultHost, DefaultPort, ct);
        AnsiConsole.MarkupLine("[green]" + string.Format(Text().SupervisorHookDone, result.DueRun.DueCount, result.DueRun.Ran, result.DueRun.Skipped).EscapeMarkup() + "[/]");
        RenderWebStatus(result.WebUi, Text().StatusLabel);
    }

    private async Task StopAllAsync(CancellationToken ct)
    {
        var status = await _supervisor.StopAllAsync(DefaultHost, DefaultPort, ct);
        AnsiConsole.MarkupLine("[green]" + Text().StoppedAll.EscapeMarkup() + "[/]");
        AnsiConsole.MarkupLine("[dim]" + $"hook:{OnOff(status.HookEnabled)} keep:{OnOff(status.KeepAliveEnabled)} boot:{OnOff(status.AutostartEnabled)}".EscapeMarkup() + "[/]");
    }

    private void RegisterLauncher()
    {
        var result = _launcher.Register(preferSystem: true, agentsRoot: _path.AgentsRoot);
        AnsiConsole.MarkupLine("[green]" + result.EscapeMarkup() + "[/]");
    }

    private void RenderWebStatus(WebUiStatus status, string action)
    {
        var text = Text();
        if (!status.IsRunning)
        {
            AnsiConsole.MarkupLine("[yellow]" + string.Format(text.WebStatusUnknown, action).EscapeMarkup() + "[/]");
            return;
        }

        var backend = status.BackendReady ? "[green]" + text.BackendReady.EscapeMarkup() + "[/]" : "[yellow]" + text.BackendStarting.EscapeMarkup() + "[/]";
        var browser = status.BrowserReady
            ? "[green]" + text.BrowserReady.EscapeMarkup() + "[/]"
            : status.BrowserDependenciesInstalled
                ? "[yellow]" + text.BrowserWarming.EscapeMarkup() + "[/]"
                : "[red]" + text.BrowserDepsMissing.EscapeMarkup() + "[/]";
        AnsiConsole.MarkupLine($"[green]Web UI {action.EscapeMarkup()}:[/] {status.Url.EscapeMarkup()}  {backend}  {browser}");
        if (!string.IsNullOrWhiteSpace(status.Message))
            AnsiConsole.MarkupLine("[dim]" + status.Message.EscapeMarkup() + "[/]");
    }

    private void WaitForContinue()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]" + Text().PressEnter.EscapeMarkup() + "[/]");
        Console.ReadLine();
    }

    private static string FormatDuration(TimeSpan? value)
    {
        if (value == null)
            return "0s";

        var span = value.Value;
        if (span.TotalDays >= 1)
            return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes}m";
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes}m {span.Seconds}s";
        if (span.TotalMinutes >= 1)
            return $"{(int)span.TotalMinutes}m {span.Seconds}s";
        return $"{Math.Max(0, span.Seconds)}s";
    }

    private static string TrimLogLine(string value)
    {
        var singleLine = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return singleLine.Length > 220 ? singleLine[..220] + "..." : singleLine;
    }

    private static string OnOff(bool value) => value ? "on" : "off";

    private string ModeLabel(string mode)
    {
        var text = Text();
        return RuntimeSupervisorService.NormalizeMode(mode) switch
        {
            RuntimeSupervisorService.ModeFragile => text.ModeFragile,
            RuntimeSupervisorService.ModeKeepAlive => text.ModeKeepAlive,
            RuntimeSupervisorService.ModeAutostartKeepAlive => text.ModeAutostartKeepAlive,
            RuntimeSupervisorService.ModeKeepAliveNoAutostart => text.ModeKeepAliveNoAutostart,
            RuntimeSupervisorService.ModePreserve => text.ModePreserve,
            _ => mode
        };
    }

    private static MenuLanguage DetectSystemLanguage()
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        if (string.IsNullOrWhiteSpace(culture))
            culture = CultureInfo.CurrentCulture.Name;

        return culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? MenuLanguage.Chinese
            : MenuLanguage.English;
    }

    private MenuText Text() => _language == MenuLanguage.Chinese ? MenuText.Zh : MenuText.En;

    private enum MenuLanguage
    {
        English,
        Chinese
    }

    private sealed record MenuText(
        string SelectAction,
        string InstallDeps,
        string WebRuntimeMenu,
        string RegisterEntry,
        string SwitchEnglish,
        string SwitchChinese,
        string Exit,
        string WebRuntimeTitle,
        string WebStatus,
        string StartFragile,
        string StartKeepAlive,
        string RestartPreserve,
        string StopWeb,
        string EnableAutostartKeepAlive,
        string DisableAutostartKeepAlive,
        string DisableSupervisor,
        string RunSupervisorHook,
        string StopAll,
        string Back,
        string PressEnter,
        string WebRunning,
        string WebStopped,
        string WebStoppedSentence,
        string ShellTool,
        string Language,
        string LanguageName,
        string DownloadSource,
        string CnSource,
        string GlobalSource,
        string InstallingDeps,
        string DependenciesInstalled,
        string Started,
        string Restarted,
        string StatusLabel,
        string WebStatusUnknown,
        string BackendReady,
        string BackendStarting,
        string BrowserReady,
        string BrowserWarming,
        string BrowserDepsMissing,
        string SupervisorMode,
        string SupervisorUnknown,
        string SupervisorUpdated,
        string SupervisorHookDone,
        string StoppedAll,
        string ModeFragile,
        string ModeKeepAlive,
        string ModeAutostartKeepAlive,
        string ModeKeepAliveNoAutostart,
        string ModePreserve)
    {
        public static readonly MenuText Zh = new(
            "选择操作",
            "1. 下载依赖",
            "2. Web UI / 运行守护",
            "3. 注册 matdance 入口",
            "4. English",
            "5. 中文",
            "0. 退出",
            "Web UI / 运行守护",
            "1. 查看运行状态",
            "2. 启动 Web UI（可能断连，无守护）",
            "3. 启动 Web UI（长期运行，不开机自启）",
            "4. 重启 Web UI（保留当前守护模式）",
            "5. 停止 Web UI",
            "6. 启用开机自启并长期运行",
            "7. 禁用开机自启并长期运行",
            "8. 禁用运行守护（恢复可能断连模式）",
            "9. 手动运行一次守护 hook",
            "10. 关闭全部（Web UI + hook/keep/boot）",
            "0. 返回主菜单",
            "按 Enter 继续...",
            "Web UI 运行中",
            "Web UI 已停止",
            "Web UI 已停止。",
            "Shell 工具",
            "语言",
            "中文",
            "下载源",
            "CN 下载优化",
            "Global 官方源",
            "正在安装依赖...",
            "依赖安装完成。",
            "已启动",
            "已重启",
            "状态",
            "Web UI {0} 请求已发出，但无法确认进程状态。",
            "后端就绪",
            "后端启动中",
            "浏览器就绪",
            "浏览器预热中",
            "浏览器依赖缺失",
            "守护模式",
            "守护状态未知",
            "运行守护已切换到 {0}",
            "守护 hook 完成：due={0}, ran={1}, skipped={2}",
            "Web UI 和所有守护任务已关闭。",
            "可能断连",
            "长期运行",
            "开机自启 + 长期运行",
            "不开机自启 + 长期运行",
            "保留当前模式");

        public static readonly MenuText En = new(
            "Select an action",
            "1. Install dependencies",
            "2. Web UI / Runtime supervisor",
            "3. Register matdance entry",
            "4. English",
            "5. 中文",
            "0. Exit",
            "Web UI / Runtime supervisor",
            "1. Show runtime status",
            "2. Start Web UI (fragile, no supervisor)",
            "3. Start Web UI (keep alive, no autostart)",
            "4. Restart Web UI (preserve supervisor mode)",
            "5. Stop Web UI",
            "6. Enable autostart + keep alive",
            "7. Disable autostart + keep alive",
            "8. Disable supervisor (fragile mode)",
            "9. Run supervisor hook once",
            "10. Stop all (Web UI + hook/keep/boot)",
            "0. Back to main menu",
            "Press Enter to continue...",
            "Web UI running",
            "Web UI stopped",
            "Web UI stopped.",
            "Shell tool",
            "Language",
            "English",
            "Download source",
            "CN mirror",
            "Global official source",
            "Installing dependencies...",
            "Dependencies installed.",
            "started",
            "restarted",
            "status",
            "Web UI {0} requested, but process status could not be confirmed.",
            "backend ready",
            "backend starting",
            "browser ready",
            "browser warming",
            "browser deps missing",
            "Supervisor mode",
            "Supervisor status unknown",
            "Runtime supervisor switched to {0}",
            "Supervisor hook done: due={0}, ran={1}, skipped={2}",
            "Web UI and all supervisor tasks stopped.",
            "fragile",
            "keep alive",
            "autostart + keep alive",
            "no autostart + keep alive",
            "preserve current mode");
    }
}
