using System.Diagnostics;
using System.Text.Json;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public sealed class RuntimeSupervisorService
{
    public const string ModeFragile = "fragile";
    public const string ModeKeepAlive = "keep-alive";
    public const string ModeAutostartKeepAlive = "autostart-keep-alive";
    public const string ModeKeepAliveNoAutostart = "keep-alive-no-autostart";
    public const string ModePreserve = "preserve";

    private const string HookTaskName = @"\Matdance\RuntimeHook";
    private const string KeepAliveTaskName = @"\Matdance\WebUiKeepAlive";
    private const string AutostartTaskName = @"\Matdance\WebUiAutostart";
    private const string MacHookLabel = "com.matdance.runtime-hook";
    private const string MacKeepAliveLabel = "com.matdance.webui-keepalive";
    private const string MacAutostartLabel = "com.matdance.webui-autostart";
    private const int MaxHeadlessRunsPerInvocation = 25;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly PathService _path;
    private readonly WebUiProcessManager _webUi;

    public RuntimeSupervisorService(PathService path)
    {
        _path = path;
        _webUi = new WebUiProcessManager(path);
    }

    public async Task<WebUiStatus> StartAsync(string mode, string host, int port, CancellationToken ct = default)
    {
        await ConfigureSystemTasksAsync(mode, host, port, ct);
        return await _webUi.StartAsync(host, port);
    }

    public async Task<WebUiStatus> RestartAsync(string mode, string host, int port, CancellationToken ct = default)
    {
        await ConfigureSystemTasksAsync(mode, host, port, ct);
        return await _webUi.RestartAsync(host, port);
    }

    public async Task<WebUiSupervisorStatus> StopAllAsync(string host, int port, CancellationToken ct = default)
    {
        await ConfigureSystemTasksAsync(ModeFragile, host, port, ct);
        await _webUi.StopAsync();
        return await GetSupervisorStatusAsync(ct);
    }

    public async Task ConfigureSystemTasksAsync(string mode, string host, int port, CancellationToken ct = default)
    {
        mode = NormalizeMode(mode);
        if (mode == ModePreserve)
            return;

        if (OperatingSystem.IsWindows())
        {
            await ConfigureWindowsTasksAsync(mode, host, port, ct);
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            await ConfigureMacOsLaunchAgentsAsync(mode, host, port, ct);
            return;
        }

        WriteModeState(mode, host, port);
    }

    private async Task ConfigureWindowsTasksAsync(string mode, string host, int port, CancellationToken ct)
    {
        if (mode == ModeFragile || mode == ModeKeepAliveNoAutostart)
        {
            // keep-alive-no-autostart must not leave persistent OS jobs behind:
            // minute tasks survive reboot/logon and effectively become autostart.
            await DeleteTaskIfExistsAsync(HookTaskName, ct);
            await DeleteTaskIfExistsAsync(KeepAliveTaskName, ct);
            await DeleteTaskIfExistsAsync(AutostartTaskName, ct);
            WriteModeState(mode, host, port);
            return;
        }

        await EnsureMinuteTaskAsync(
            HookTaskName,
            BuildCommand("web-ui supervise --run-due", host, port),
            minutes: 1,
            ct);

        await EnsureMinuteTaskAsync(
            KeepAliveTaskName,
            BuildCommand("web-ui supervise --keep-alive", host, port),
            minutes: 5,
            ct);

        if (mode == ModeAutostartKeepAlive)
        {
            await EnsureLogonTaskAsync(
                AutostartTaskName,
                BuildCommand("web-ui supervise --keep-alive", host, port),
                ct);
        }
        else
        {
            await DeleteTaskIfExistsAsync(AutostartTaskName, ct);
        }

        WriteModeState(mode, host, port);
    }

    private async Task ConfigureMacOsLaunchAgentsAsync(string mode, string host, int port, CancellationToken ct)
    {
        if (mode == ModeFragile || mode == ModeKeepAliveNoAutostart)
        {
            // LaunchAgents with StartInterval can be loaded again by launchd after login.
            // No-autostart mode removes them instead of keeping a persistent timer.
            await DeleteMacLaunchAgentAsync(MacHookLabel, ct);
            await DeleteMacLaunchAgentAsync(MacKeepAliveLabel, ct);
            await DeleteMacLaunchAgentAsync(MacAutostartLabel, ct);
            WriteModeState(mode, host, port);
            return;
        }

        var autostart = mode == ModeAutostartKeepAlive;
        await EnsureMacLaunchAgentAsync(
            MacHookLabel,
            BuildCommandParts("web-ui supervise --run-due", host, port),
            intervalSeconds: 60,
            autostart,
            runAtLoad: !autostart,
            ct);
        await EnsureMacLaunchAgentAsync(
            MacKeepAliveLabel,
            BuildCommandParts("web-ui supervise --keep-alive", host, port),
            intervalSeconds: 300,
            autostart,
            runAtLoad: !autostart,
            ct);

        if (autostart)
        {
            await EnsureMacLaunchAgentAsync(
                MacAutostartLabel,
                BuildCommandParts("web-ui supervise --keep-alive", host, port),
                intervalSeconds: null,
                autostart: true,
                runAtLoad: true,
                ct);
        }
        else
        {
            await DeleteMacLaunchAgentAsync(MacAutostartLabel, ct);
        }

        WriteModeState(mode, host, port);
    }

    public async Task<WebUiSupervisorStatus> GetSupervisorStatusAsync(CancellationToken ct = default)
    {
        var mode = ReadModeState();
        var hook = OperatingSystem.IsWindows()
            ? await QueryTaskAsync(HookTaskName, ct)
            : OperatingSystem.IsMacOS() && await QueryMacLaunchAgentAsync(MacHookLabel, ct);
        var keepAlive = OperatingSystem.IsWindows()
            ? await QueryTaskAsync(KeepAliveTaskName, ct)
            : OperatingSystem.IsMacOS() && await QueryMacLaunchAgentAsync(MacKeepAliveLabel, ct);
        var autostart = OperatingSystem.IsWindows()
            ? await QueryTaskAsync(AutostartTaskName, ct)
            : OperatingSystem.IsMacOS() && IsMacAutostartPlistPresent(MacAutostartLabel) && await QueryMacLaunchAgentAsync(MacAutostartLabel, ct);
        var web = await _webUi.GetStatusAsync();
        return new WebUiSupervisorStatus
        {
            Mode = mode.Mode,
            Host = mode.Host,
            Port = mode.Port,
            HookEnabled = hook,
            KeepAliveEnabled = keepAlive,
            AutostartEnabled = autostart,
            WebUi = web
        };
    }

    public async Task<WebUiSupervisorRunResult> SuperviseAsync(bool keepAlive, bool runDue, string host, int port, CancellationToken ct = default)
    {
        var wasRunning = _webUi.GetStatus().IsRunning;
        var dueRuns = runDue && !wasRunning ? await RunDueTasksOnceAsync(ct) : new HeadlessDueRunResult();
        WebUiStatus? webStatus = null;

        if (keepAlive)
            webStatus = await _webUi.StartAsync(host, port);

        return new WebUiSupervisorRunResult
        {
            WasWebUiRunning = wasRunning,
            KeepAliveRequested = keepAlive,
            RunDueRequested = runDue,
            DueRun = dueRuns,
            WebUi = webStatus ?? await _webUi.GetStatusAsync()
        };
    }

    public async Task<HeadlessDueRunResult> RunDueTasksOnceAsync(CancellationToken ct = default)
    {
        var result = new HeadlessDueRunResult();
        var scheduledTasks = new ScheduledTaskService(_path);
        scheduledTasks.EnsureAllSystemTasks();
        scheduledTasks.RecoverInterruptedRuns(DateTimeOffset.UtcNow);

        var bookmarks = new BookmarkService(_path);
        var memoryOrg = new MemoryOrganizationService(_path, scheduledTasks, bookmarks);
        var skillMaintenance = new SkillMaintenanceService(_path);
        var runner = new ScheduledTaskRunner(_path, scheduledTasks, memoryOrg, skillMaintenance);

        var dueTasks = scheduledTasks.GetDueTasks(DateTimeOffset.UtcNow)
            .OrderByDescending(BackgroundWorkCoordinator.GetScheduledTaskPriority)
            .ThenBy(task => task.NextRunAt ?? DateTimeOffset.MaxValue)
            .ThenBy(task => task.Agent, StringComparer.OrdinalIgnoreCase)
            .ThenBy(task => task.TaskId, StringComparer.OrdinalIgnoreCase)
            .Take(MaxHeadlessRunsPerInvocation)
            .ToList();

        result.DueCount = dueTasks.Count;
        foreach (var due in dueTasks)
        {
            ct.ThrowIfCancellationRequested();
            var scheduledAt = due.NextRunAt;
            var catchUp = scheduledAt.HasValue && scheduledAt.Value < DateTimeOffset.UtcNow - TimeSpan.FromSeconds(90);
            var trigger = catchUp ? "catch_up" : "schedule";
            var reason = catchUp ? "missed while Matdance Web UI was stopped or unavailable" : null;
            var task = scheduledTasks.TryStartRun(due.Agent, due.TaskId, scheduledAt: scheduledAt, trigger: trigger, catchUpReason: reason);
            if (task == null)
            {
                result.Skipped++;
                result.Events.Add($"skipped {due.Agent}/{due.TaskId}: not available or already running");
                continue;
            }

            var run = await runner.ExecuteAsync(task, trigger, scheduledAt, deliver: true, ct, reason);
            scheduledTasks.FinishRun(run);
            result.Ran++;
            result.Events.Add($"{run.Status} {run.Agent}/{run.TaskId}/{run.RunId}");
        }

        result.CompletedAt = UserTimeZoneService.Now();
        return result;
    }

    public static string NormalizeMode(string? mode)
    {
        var normalized = (mode ?? ModeFragile).Trim().ToLowerInvariant().Replace("_", "-");
        return normalized switch
        {
            "long-running" => ModeKeepAlive,
            "durable" => ModeKeepAlive,
            "keepalive" => ModeKeepAlive,
            "autostart" => ModeAutostartKeepAlive,
            "enable-autostart" => ModeAutostartKeepAlive,
            "disable-autostart" => ModeKeepAliveNoAutostart,
            "no-autostart" => ModeKeepAliveNoAutostart,
            "current" => ModePreserve,
            ModePreserve or ModeKeepAlive or ModeAutostartKeepAlive or ModeKeepAliveNoAutostart or ModeFragile => normalized,
            _ => ModeFragile
        };
    }

    private async Task EnsureMinuteTaskAsync(string taskName, string command, int minutes, CancellationToken ct)
    {
        var args = $"/Create /TN {Quote(taskName)} /TR {Quote(command)} /SC MINUTE /MO {Math.Max(1, minutes)} /F";
        await RunSchtasksAsync(args, ct);
    }

    private async Task EnsureLogonTaskAsync(string taskName, string command, CancellationToken ct)
    {
        var args = $"/Create /TN {Quote(taskName)} /TR {Quote(command)} /SC ONLOGON /F";
        await RunSchtasksAsync(args, ct);
    }

    private async Task DeleteTaskIfExistsAsync(string taskName, CancellationToken ct)
    {
        if (!await QueryTaskAsync(taskName, ct))
            return;

        await RunSchtasksAsync($"/Delete /TN {Quote(taskName)} /F", ct);
    }

    private async Task<bool> QueryTaskAsync(string taskName, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        var exitCode = await RunProcessAsync("schtasks.exe", $"/Query /TN {Quote(taskName)}", throwOnFailure: false, ct);
        return exitCode == 0;
    }

    private static async Task RunSchtasksAsync(string arguments, CancellationToken ct)
    {
        var exitCode = await RunProcessAsync("schtasks.exe", arguments, throwOnFailure: false, ct);
        if (exitCode != 0)
            throw new InvalidOperationException("Failed to configure Windows scheduled task. Run Matdance from a normal user session and try again.");
    }

    private static async Task<int> RunProcessAsync(string fileName, string arguments, bool throwOnFailure, CancellationToken ct)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        if (process == null)
            throw new InvalidOperationException("Failed to start " + fileName);

        await process.WaitForExitAsync(ct);
        if (throwOnFailure && process.ExitCode != 0)
            throw new InvalidOperationException(await process.StandardError.ReadToEndAsync(ct));
        return process.ExitCode;
    }

    private static async Task<string> RunProcessCaptureAsync(string fileName, string arguments, CancellationToken ct)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        if (process == null)
            throw new InvalidOperationException("Failed to start " + fileName);

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
        return stdout;
    }

    private string BuildCommand(string subCommand, string host, int port)
    {
        var parts = BuildCommandParts(subCommand, host, port);
        if (OperatingSystem.IsWindows())
            return BuildWindowsHiddenCommand(parts);

        return string.Join(" ", parts.Select(Quote));
    }

    private string[] BuildCommandParts(string subCommand, string host, int port)
    {
        var dllPath = Path.Combine(AppContext.BaseDirectory, "Matdance.Cli.dll");
        var processPath = Environment.ProcessPath;
        var isDotnetHost = DotnetHostResolver.LooksLikeDotnetHost(processPath);

        var parts = new List<string>();
        if (!isDotnetHost && !string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
            parts.Add(processPath);
        else
        {
            parts.Add(DotnetHostResolver.ResolveDotnetHostPath());
            parts.Add(dllPath);
        }

        parts.AddRange(subCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        parts.Add("--host");
        parts.Add(host);
        parts.Add("--port");
        parts.Add(port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        parts.Add("--agents-dir");
        parts.Add(_path.AgentsRoot);
        return parts.ToArray();
    }

    private static string BuildWindowsHiddenCommand(IReadOnlyList<string> parts)
    {
        var script = EnsureWindowsHiddenCommandScript(parts);
        return string.Join(" ", new[] { "wscript.exe", script }.Select(Quote));
    }

    private static string EnsureWindowsHiddenCommandScript(IReadOnlyList<string> parts)
    {
        var root = Path.Combine(MatdanceRuntime.StateRoot, "supervisor");
        Directory.CreateDirectory(root);
        var name = parts.Any(part => part.Equals("--run-due", StringComparison.OrdinalIgnoreCase))
            ? "matdance-runtime-hook.vbs"
            : parts.Any(part => part.Equals("--keep-alive", StringComparison.OrdinalIgnoreCase))
                ? "matdance-webui-keepalive.vbs"
                : "matdance-supervisor-command.vbs";
        var path = Path.Combine(root, name);
        var command = string.Join(" ", parts.Select(QuoteWindowsCommandArgument));
        var escaped = command.Replace("\"", "\"\"", StringComparison.Ordinal);
        File.WriteAllText(path, "exitCode = CreateObject(\"WScript.Shell\").Run(\"" + escaped + "\", 0, True)\r\nWScript.Quit exitCode\r\n");
        return path;
    }

    private static string QuoteWindowsCommandArgument(string value)
        => "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private async Task EnsureMacLaunchAgentAsync(string label, IReadOnlyList<string> arguments, int? intervalSeconds, bool autostart, bool runAtLoad, CancellationToken ct)
    {
        await DeleteMacLaunchAgentAsync(label, ct);
        var path = GetMacPlistPath(label, autostart);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Directory.CreateDirectory(Path.Combine(MatdanceRuntime.RuntimeRoot, "logs"));
        await File.WriteAllTextAsync(path, BuildMacLaunchAgentPlist(label, arguments, intervalSeconds, runAtLoad), ct);
        await RunProcessAsync("chmod", $"644 {Quote(path)}", throwOnFailure: false, ct);

        var domain = await GetMacLaunchctlDomainAsync(ct);
        var service = domain + "/" + label;
        var exit = await RunProcessAsync("launchctl", $"bootstrap {Quote(domain)} {Quote(path)}", throwOnFailure: false, ct);
        if (exit != 0)
            await RunProcessAsync("launchctl", $"load -w {Quote(path)}", throwOnFailure: true, ct);
        await RunProcessAsync("launchctl", $"enable {Quote(service)}", throwOnFailure: false, ct);
        if (runAtLoad || autostart)
            await RunProcessAsync("launchctl", $"kickstart -k {Quote(service)}", throwOnFailure: false, ct);
    }

    private async Task DeleteMacLaunchAgentAsync(string label, CancellationToken ct)
    {
        var domain = await GetMacLaunchctlDomainAsync(ct);
        var service = domain + "/" + label;
        await RunProcessAsync("launchctl", $"disable {Quote(service)}", throwOnFailure: false, ct);
        await RunProcessAsync("launchctl", $"bootout {Quote(service)}", throwOnFailure: false, ct);
        await RunProcessAsync("launchctl", $"remove {Quote(label)}", throwOnFailure: false, ct);

        foreach (var path in GetMacPlistPaths(label))
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }
    }

    private async Task<bool> QueryMacLaunchAgentAsync(string label, CancellationToken ct)
    {
        var domain = await GetMacLaunchctlDomainAsync(ct);
        var exit = await RunProcessAsync("launchctl", $"print {Quote(domain + "/" + label)}", throwOnFailure: false, ct);
        return exit == 0;
    }

    private static bool IsMacAutostartPlistPresent(string label)
        => File.Exists(GetMacPlistPath(label, autostart: true));

    private static IEnumerable<string> GetMacPlistPaths(string label)
    {
        yield return GetMacPlistPath(label, autostart: true);
        yield return GetMacPlistPath(label, autostart: false);
    }

    private static string GetMacPlistPath(string label, bool autostart)
    {
        var root = autostart
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents")
            : Path.Combine(MatdanceRuntime.StateRoot, "launchd");
        return Path.Combine(root, label + ".plist");
    }

    private static string BuildMacLaunchAgentPlist(string label, IReadOnlyList<string> arguments, int? intervalSeconds, bool runAtLoad)
    {
        var logRoot = Path.Combine(MatdanceRuntime.RuntimeRoot, "logs");
        var stdout = Path.Combine(logRoot, label + ".out.log");
        var stderr = Path.Combine(logRoot, label + ".err.log");
        var runAtLoadValue = runAtLoad ? "true" : "false";
        var interval = intervalSeconds.HasValue
            ? $"  <key>StartInterval</key>\n  <integer>{intervalSeconds.Value}</integer>\n"
            : string.Empty;
        var argumentXml = string.Join("\n", arguments.Select(arg => "    <string>" + XmlEscape(arg) + "</string>"));
        return $"""
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key>
  <string>{XmlEscape(label)}</string>
  <key>ProgramArguments</key>
  <array>
{argumentXml}
  </array>
  <key>WorkingDirectory</key>
  <string>{XmlEscape(Directory.GetCurrentDirectory())}</string>
  <key>LimitLoadToSessionType</key>
  <string>Aqua</string>
  <key>RunAtLoad</key>
  <{runAtLoadValue}/>
{interval}  <key>StandardOutPath</key>
  <string>{XmlEscape(stdout)}</string>
  <key>StandardErrorPath</key>
  <string>{XmlEscape(stderr)}</string>
  <key>EnvironmentVariables</key>
  <dict>
    <key>MATDANCE_RUNTIME_DIR</key>
    <string>{XmlEscape(MatdanceRuntime.RuntimeRoot)}</string>
    <key>PLAYWRIGHT_BROWSERS_PATH</key>
    <string>{XmlEscape(MatdanceRuntime.PlaywrightBrowsersPath)}</string>
  </dict>
</dict>
</plist>
""";
    }

    private static async Task<string> GetMacLaunchctlDomainAsync(CancellationToken ct)
    {
        var uid = Environment.GetEnvironmentVariable("UID");
        if (string.IsNullOrWhiteSpace(uid))
            uid = (await RunProcessCaptureAsync("id", "-u", ct)).Trim();
        if (string.IsNullOrWhiteSpace(uid))
            throw new InvalidOperationException("Unable to determine current macOS user id for launchd.");
        return "gui/" + uid;
    }

    private void WriteModeState(string mode, string host, int port)
    {
        Directory.CreateDirectory(MatdanceRuntime.StateRoot);
        var path = GetSupervisorStatePath();
        AtomicFile.WriteAllText(path, JsonSerializer.Serialize(new SupervisorModeState
        {
            Mode = mode,
            Host = host,
            Port = port,
            UpdatedAt = UserTimeZoneService.Now()
        }, JsonOptions));
    }

    private SupervisorModeState ReadModeState()
    {
        var path = GetSupervisorStatePath();
        if (!File.Exists(path))
            return new SupervisorModeState();

        try
        {
            return JsonSerializer.Deserialize<SupervisorModeState>(File.ReadAllText(path), JsonOptions) ?? new SupervisorModeState();
        }
        catch
        {
            return new SupervisorModeState();
        }
    }

    private static string GetSupervisorStatePath() => Path.Combine(MatdanceRuntime.StateRoot, "web-ui-supervisor.json");

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static string XmlEscape(string value)
        => value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);

    private sealed class SupervisorModeState
    {
        public string Mode { get; set; } = ModeFragile;
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 8765;
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}

public sealed class WebUiSupervisorStatus
{
    public string Mode { get; set; } = RuntimeSupervisorService.ModeFragile;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8765;
    public bool HookEnabled { get; set; }
    public bool KeepAliveEnabled { get; set; }
    public bool AutostartEnabled { get; set; }
    public WebUiStatus WebUi { get; set; } = WebUiStatus.Stopped();
}

public sealed class WebUiSupervisorRunResult
{
    public bool WasWebUiRunning { get; set; }
    public bool KeepAliveRequested { get; set; }
    public bool RunDueRequested { get; set; }
    public HeadlessDueRunResult DueRun { get; set; } = new();
    public WebUiStatus WebUi { get; set; } = WebUiStatus.Stopped();
}

public sealed class HeadlessDueRunResult
{
    public int DueCount { get; set; }
    public int Ran { get; set; }
    public int Skipped { get; set; }
    public DateTimeOffset CompletedAt { get; set; } = UserTimeZoneService.Now();
    public List<string> Events { get; set; } = new();
}
