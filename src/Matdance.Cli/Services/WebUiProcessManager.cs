using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Matdance.Cli.Services;

public sealed class WebUiProcessManager
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly PathService _path;
    private readonly string _statePath;

    public WebUiProcessManager(PathService path)
    {
        _path = path;
        _statePath = Path.Combine(MatdanceRuntime.StateRoot, "web-ui.json");
    }

    public WebUiStatus GetStatus()
    {
        var state = ReadState();
        if (state == null)
            return WebUiStatus.Stopped();

        var process = TryGetManagedProcess(state);
        if (process == null)
        {
            TryDeleteState();
            return WebUiStatus.Stopped();
        }

        return WebUiStatus.Running(state.ProcessId, state.Host, state.Port, state.StartedAt, state.Url);
    }

    public async Task<WebUiStatus> GetStatusAsync()
    {
        var state = ReadState();
        if (state == null)
            return WebUiStatus.Stopped();

        if (TryGetManagedProcess(state) == null)
        {
            TryDeleteState();
            return WebUiStatus.Stopped();
        }

        return await WaitForReadyAsync(state, TimeSpan.FromSeconds(3));
    }

    public void RegisterCurrentProcess(string host, int port)
    {
        Directory.CreateDirectory(MatdanceRuntime.StateRoot);
        var state = new WebUiState
        {
            ProcessId = Environment.ProcessId,
            Host = host,
            Port = port,
            Url = $"http://{host}:{port}",
            StartedAt = UserTimeZoneService.Now(),
            AgentsRoot = _path.AgentsRoot,
            ProcessPath = Environment.ProcessPath ?? string.Empty,
            ProcessName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? string.Empty)
        };
        AtomicFile.WriteAllText(_statePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    public void ClearCurrentProcess()
    {
        var state = ReadState();
        if (state?.ProcessId == Environment.ProcessId)
            TryDeleteState();
    }

    public async Task<WebUiStatus> StartAsync(string host = "localhost", int port = 8765)
    {
        var current = GetStatus();
        if (current.IsRunning)
        {
            if (string.Equals(current.Host, host, StringComparison.OrdinalIgnoreCase) && current.Port == port)
                return await GetStatusAsync();

            await StopAsync();
        }

        Directory.CreateDirectory(MatdanceRuntime.StateRoot);
        var startInfo = BuildStartInfo(host, port);
        var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start Matdance Web UI process.");

        var state = new WebUiState
        {
            ProcessId = process.Id,
            Host = host,
            Port = port,
            Url = $"http://{host}:{port}",
            StartedAt = UserTimeZoneService.Now(),
            AgentsRoot = _path.AgentsRoot,
            ProcessPath = ResolveProcessPathHint(startInfo.FileName),
            ProcessName = Path.GetFileNameWithoutExtension(startInfo.FileName)
        };
        AtomicFile.WriteAllText(_statePath, JsonSerializer.Serialize(state, JsonOptions));

        return await WaitForReadyAsync(state, TimeSpan.FromSeconds(20));
    }

    public async Task StopAsync()
    {
        var state = ReadState();
        if (state == null)
            return;

        var process = TryGetManagedProcess(state);
        if (process != null)
        {
            try
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            catch (InvalidOperationException) { }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to stop Matdance Web UI: " + ex.Message, ex);
            }
        }

        TryDeleteState();
    }

    public async Task<WebUiStatus> RestartAsync(string host = "localhost", int port = 8765)
    {
        await StopAsync();
        return await StartAsync(host, port);
    }

    private async Task<WebUiStatus> WaitForReadyAsync(WebUiState state, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var status = WebUiStatus.Running(state.ProcessId, state.Host, state.Port, state.StartedAt, state.Url);
        string? lastError = null;

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (TryGetManagedProcess(state) == null)
            {
                TryDeleteState();
                return WebUiStatus.Stopped() with { Message = lastError ?? "Web UI process exited during startup." };
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, BuildLocalProbeUrl(state.Host, state.Port) + "/api/runtime-status");
                var token = WebAuthService.TryReadConfiguredToken();
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await http.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    lastError = "Web UI requires authentication; local token was not available.";
                    await Task.Delay(500);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                var runtime = await response.Content.ReadFromJsonAsync<RuntimeStatus>(JsonOptions);
                if (runtime != null)
                {
                    status = WebUiStatus.Running(state.ProcessId, state.Host, state.Port, state.StartedAt, state.Url) with
                    {
                        BackendReady = runtime.Backend,
                        BrowserReady = runtime.Browser,
                        BrowserDependenciesInstalled = runtime.Browser || runtime.BrowserDependencies,
                        Message = runtime.Browser
                            ? "Backend and browser runtime are ready."
                            : runtime.BrowserDependencies
                                ? "Backend is ready; browser runtime is still warming up."
                                : "Backend is ready; browser dependencies are missing. Run dependency installation first."
                    };

                    if (status.BackendReady)
                        return status;
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }

            await Task.Delay(500);
        }

        return status with
        {
            Message = lastError == null
                ? "Web UI process started, but readiness check timed out."
                : "Web UI process started, but readiness check timed out: " + lastError
        };
    }

    private ProcessStartInfo BuildStartInfo(string host, int port)
    {
        var appRoot = PrepareShadowAppRoot(out var playwrightDriverRoot);
        var dllPath = Path.Combine(appRoot, "Matdance.Cli.dll");
        var args = $"web --host {Quote(host)} --port {port} --agents-dir {Quote(_path.AgentsRoot)}";

        string fileName;
        string arguments;
        var processPath = Environment.ProcessPath;
        if (!DotnetHostResolver.LooksLikeDotnetHost(processPath) && !string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
        {
            fileName = processPath;
            arguments = args;
        }
        else
        {
            fileName = DotnetHostResolver.ResolveDotnetHostPath();
            arguments = $"{Quote(dllPath)} {args}";
        }

        var psi = new ProcessStartInfo
        {
            WorkingDirectory = Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        // On Unix, spawn through 'exec' in a shell so we can redirect stdout/stderr
        // to a log file. This prevents the child from inheriting the terminal (which
        // floods the console and can block further terminal use) and avoids SIGPIPE
        // when the parent CLI exits.
        if (!OperatingSystem.IsWindows())
        {
            var logDir = Path.Combine(MatdanceRuntime.RuntimeRoot, "logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "web-ui.log");
            psi.FileName = "/bin/sh";
            psi.Arguments = $"-c \"exec {EscapeShellArg(fileName)} {arguments.Replace("\"", "\\\"")} >> {EscapeShellArg(logPath)} 2>&1\"";
        }
        else
        {
            psi.FileName = fileName;
            psi.Arguments = arguments;
        }

        psi.Environment["PLAYWRIGHT_BROWSERS_PATH"] = MatdanceRuntime.PlaywrightBrowsersPath;
        psi.Environment["MATDANCE_RUNTIME_DIR"] = MatdanceRuntime.RuntimeRoot;
        psi.Environment["PLAYWRIGHT_DRIVER_SEARCH_PATH"] = playwrightDriverRoot;
        if (WebAuthService.IsRemoteBinding(host))
        {
            WebAuthService.LoadOrCreate(host);
            psi.Environment["MATDANCE_ALLOW_REMOTE_WEB"] = "1";
        }
        return psi;
    }

    private static string PrepareShadowAppRoot(out string playwrightDriverRoot)
    {
        var sourceRoot = Path.GetFullPath(AppContext.BaseDirectory);
        playwrightDriverRoot = EnsurePlaywrightDriverRoot(sourceRoot);
        var shadowParent = Path.Combine(MatdanceRuntime.RuntimeRoot, "web-ui-shadow");
        Directory.CreateDirectory(shadowParent);

        if (PathSafety.IsUnderRoot(sourceRoot, shadowParent))
            return sourceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        CleanupOldShadowRoots(shadowParent);

        var shadowRoot = Path.Combine(
            shadowParent,
            DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)
                + "-"
                + Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "-"
                + Guid.NewGuid().ToString("N")[..8]);
        CopyDirectoryForShadowRun(sourceRoot, shadowRoot);
        return shadowRoot;
    }

    private static void CopyDirectoryForShadowRun(string sourceRoot, string shadowRoot)
    {
        Directory.CreateDirectory(shadowRoot);
        foreach (var sourceDirectory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceRoot, sourceDirectory);
            if (ShouldSkipShadowPath(relative))
                continue;

            Directory.CreateDirectory(Path.Combine(shadowRoot, relative));
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceRoot, sourceFile);
            if (ShouldSkipShadowPath(relative))
                continue;

            var targetFile = Path.Combine(shadowRoot, relative);
            var targetDirectory = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
                Directory.CreateDirectory(targetDirectory);
            File.Copy(sourceFile, targetFile, overwrite: false);
        }
    }

    private static bool ShouldSkipShadowPath(string relativePath)
    {
        var first = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).FirstOrDefault();
        return first != null
            && (first.Equals(".matdance", StringComparison.OrdinalIgnoreCase)
                || first.Equals(".playwright", StringComparison.OrdinalIgnoreCase));
    }

    private static string EnsurePlaywrightDriverRoot(string sourceRoot)
    {
        var sourceDriver = Path.Combine(sourceRoot, ".playwright");
        if (!Directory.Exists(sourceDriver))
            return sourceRoot;

        var driverRoot = Path.Combine(MatdanceRuntime.DependenciesRoot, "playwright-driver");
        var targetDriver = Path.Combine(driverRoot, ".playwright");
        if (Directory.Exists(targetDriver))
        {
            UnixExecutablePermissions.EnsurePlaywrightDriverExecutables(targetDriver);
            return driverRoot;
        }

        Directory.CreateDirectory(driverRoot);
        CopyDirectoryForShadowRun(sourceDriver, targetDriver);
        UnixExecutablePermissions.EnsurePlaywrightDriverExecutables(targetDriver);
        return driverRoot;
    }

    private static void CleanupOldShadowRoots(string shadowParent)
    {
        try
        {
            foreach (var directory in Directory.EnumerateDirectories(shadowParent))
            {
                try
                {
                    var info = new DirectoryInfo(directory);
                    if (DateTimeOffset.UtcNow - new DateTimeOffset(info.CreationTimeUtc, TimeSpan.Zero) > TimeSpan.FromDays(2))
                        Directory.Delete(directory, recursive: true);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private static string EscapeShellArg(string arg)
    {
        // Wrap in single quotes; escape embedded single quotes by ending the
        // quoted string, adding an escaped quote, then resuming.
        return "'" + arg.Replace("'", "'\\''") + "'";
    }

    private static string BuildLocalProbeUrl(string host, int port)
    {
        var value = (host ?? string.Empty).Trim().Trim('[', ']');
        if (string.IsNullOrWhiteSpace(value)
            || value.Equals("0.0.0.0", StringComparison.Ordinal)
            || value.Equals("::", StringComparison.Ordinal)
            || value.Equals("*", StringComparison.Ordinal)
            || value.Equals("+", StringComparison.Ordinal))
        {
            return $"http://127.0.0.1:{port}";
        }

        if (value.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || value.Equals("127.0.0.1", StringComparison.Ordinal)
            || value.Equals("::1", StringComparison.Ordinal))
        {
            return value.Contains(':', StringComparison.Ordinal)
                ? $"http://[{value}]:{port}"
                : $"http://{value}:{port}";
        }

        return value.Contains(':', StringComparison.Ordinal)
            ? $"http://[{value}]:{port}"
            : $"http://{value}:{port}";
    }

    private WebUiState? ReadState()
    {
        if (!File.Exists(_statePath))
            return null;

        try
        {
            return JsonSerializer.Deserialize<WebUiState>(File.ReadAllText(_statePath), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void TryDeleteState()
    {
        try
        {
            if (File.Exists(_statePath))
                File.Delete(_statePath);
        }
        catch { }
    }

    private static Process? TryGetProcess(int processId)
    {
        if (processId <= 0)
            return null;

        try
        {
            var process = Process.GetProcessById(processId);
            return process.HasExited ? null : process;
        }
        catch
        {
            return null;
        }
    }

    private static Process? TryGetManagedProcess(WebUiState state)
    {
        var process = TryGetProcess(state.ProcessId);
        if (process == null)
            return null;

        if (state.ProcessId == Environment.ProcessId)
            return process;

        var startTimeMatches = ProcessStartTimeMatches(process, state.StartedAt);
        var hasIdentity = !string.IsNullOrWhiteSpace(state.ProcessPath) || !string.IsNullOrWhiteSpace(state.ProcessName);
        if (!hasIdentity)
        {
            if (!startTimeMatches)
                return null;

            var legacyName = SafeProcessName(process);
            return legacyName.Equals("Matdance.Cli", StringComparison.OrdinalIgnoreCase)
                || legacyName.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
                    ? process
                    : null;
        }

        if (!startTimeMatches)
            return null;

        if (!string.IsNullOrWhiteSpace(state.ProcessPath) && ProcessPathMatches(process, state.ProcessPath))
            return process;

        if (!string.IsNullOrWhiteSpace(state.ProcessName)
            && SafeProcessName(process).Equals(state.ProcessName, StringComparison.OrdinalIgnoreCase))
            return process;

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            var expectedName = state.ProcessName ?? string.Empty;
            var actualName = SafeProcessName(process);
            if (expectedName.Equals("sh", StringComparison.OrdinalIgnoreCase)
                && (actualName.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
                    || actualName.Equals("Matdance.Cli", StringComparison.OrdinalIgnoreCase)
                    || actualName.Equals("Matdance.Cli.dll", StringComparison.OrdinalIgnoreCase)))
            {
                return process;
            }
        }

        return null;
    }

    private static bool ProcessStartTimeMatches(Process process, DateTimeOffset expected)
    {
        try
        {
            var actual = new DateTimeOffset(process.StartTime);
            return Math.Abs((actual - expected).TotalSeconds) <= 120;
        }
        catch
        {
            return false;
        }
    }

    private static bool ProcessPathMatches(Process process, string expectedPath)
    {
        try
        {
            var actual = process.MainModule?.FileName;
            return !string.IsNullOrWhiteSpace(actual)
                && Path.GetFullPath(actual).Equals(Path.GetFullPath(expectedPath), PathSafety.PathComparison);
        }
        catch
        {
            return false;
        }
    }

    private static string SafeProcessName(Process process)
    {
        try { return process.ProcessName; }
        catch { return string.Empty; }
    }

    private static string ResolveProcessPathHint(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        try
        {
            return Path.IsPathRooted(fileName) && File.Exists(fileName)
                ? Path.GetFullPath(fileName)
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private sealed class WebUiState
    {
        public int ProcessId { get; set; }
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 8765;
        public string Url { get; set; } = "http://localhost:8765";
        public DateTimeOffset StartedAt { get; set; }
        public string AgentsRoot { get; set; } = string.Empty;
        public string ProcessPath { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
    }

    private sealed class RuntimeStatus
    {
        public bool Backend { get; set; }
        public bool Browser { get; set; }
        public bool BrowserDependencies { get; set; }
    }
}

public sealed record WebUiStatus
{
    public bool IsRunning { get; init; }
    public int? ProcessId { get; init; }
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 8765;
    public string Url { get; init; } = "http://localhost:8765";
    public DateTimeOffset? StartedAt { get; init; }
    public bool BackendReady { get; init; }
    public bool BrowserReady { get; init; }
    public bool BrowserDependenciesInstalled { get; init; }
    public string? Message { get; init; }
    public TimeSpan? Uptime => IsRunning && StartedAt != null ? UserTimeZoneService.Now() - StartedAt.Value : null;

    public static WebUiStatus Stopped() => new() { IsRunning = false };

    public static WebUiStatus Running(int processId, string host, int port, DateTimeOffset startedAt, string url) => new()
    {
        IsRunning = true,
        ProcessId = processId,
        Host = host,
        Port = port,
        Url = url,
        StartedAt = startedAt
    };
}
