using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Matdance.Cli.Services;

public sealed class DependencyInstallerService
{
    private static readonly object BrowserDependencyCacheLock = new();
    private static DateTimeOffset _browserDependencyCacheAt;
    private static string _browserDependencyCacheKey = string.Empty;
    private static bool _browserDependencyCacheValue;

    public async Task InstallAsync(DependencySource source, Action<string>? log = null, CancellationToken ct = default)
    {
        MatdanceRuntime.ConfigureProcessEnvironment();
        Directory.CreateDirectory(MatdanceRuntime.DependenciesRoot);
        Directory.CreateDirectory(MatdanceRuntime.PlaywrightBrowsersPath);
        var effectiveSource = ResolveSource(source);

        log?.Invoke($"Dependency root: {MatdanceRuntime.DependenciesRoot}");
        log?.Invoke($"Playwright browsers: {MatdanceRuntime.PlaywrightBrowsersPath}");
        log?.Invoke($"Download source: {DescribeSource(source, effectiveSource)}");

        await InstallPlaywrightChromiumAsync(effectiveSource, log, ct);
    }

    public static DependencySource ResolveSource(DependencySource source)
        => source == DependencySource.Auto ? InferDefaultSource() : source;

    public bool HasPlaywrightChromium()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH"),
            MatdanceRuntime.PlaywrightBrowsersPath,
            Path.Combine(MatdanceRuntime.DependenciesRoot, "playwright-browsers")
        }
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Select(path => Path.GetFullPath(path!))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

        var cacheKey = string.Join("|", candidates);
        lock (BrowserDependencyCacheLock)
        {
            if (cacheKey.Equals(_browserDependencyCacheKey, StringComparison.Ordinal)
                && DateTimeOffset.UtcNow - _browserDependencyCacheAt < TimeSpan.FromSeconds(5))
            {
                return _browserDependencyCacheValue;
            }
        }

        var value = candidates.Any(HasPlaywrightChromiumAt);
        lock (BrowserDependencyCacheLock)
        {
            _browserDependencyCacheKey = cacheKey;
            _browserDependencyCacheAt = DateTimeOffset.UtcNow;
            _browserDependencyCacheValue = value;
        }

        return value;
    }

    private static bool HasPlaywrightChromiumAt(string browsersPath)
    {
        try
        {
            if (!Directory.Exists(browsersPath))
                return false;

            return Directory.EnumerateDirectories(browsersPath, "chromium-*")
                    .Concat(Directory.EnumerateDirectories(browsersPath, "chromium_headless_shell-*"))
                    .Any(IsCompletePlaywrightBrowserDirectory);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCompletePlaywrightBrowserDirectory(string directory)
    {
        try
        {
            if (File.Exists(Path.Combine(directory, "INSTALLATION_COMPLETE")))
                return true;

            if (OperatingSystem.IsWindows())
            {
                return Directory.EnumerateFiles(directory, "chrome.exe", SearchOption.AllDirectories).Any()
                    || Directory.EnumerateFiles(directory, "chrome-headless-shell.exe", SearchOption.AllDirectories).Any();
            }

            return Directory.EnumerateFiles(directory, "chrome", SearchOption.AllDirectories).Any()
                || Directory.EnumerateFiles(directory, "chrome-headless-shell", SearchOption.AllDirectories).Any()
                || Directory.EnumerateFiles(directory, "Chromium", SearchOption.AllDirectories).Any();
        }
        catch
        {
            return false;
        }
    }

    private async Task InstallPlaywrightChromiumAsync(DependencySource source, Action<string>? log, CancellationToken ct)
    {
        var driverRoot = Path.Combine(AppContext.BaseDirectory, ".playwright");
        var packageCli = Path.Combine(driverRoot, "package", "cli.js");
        var node = GetBundledNodePath(driverRoot);

        if (!File.Exists(packageCli))
            throw new InvalidOperationException("Playwright CLI asset not found. Build or publish Matdance before installing dependencies.");
        if (!File.Exists(node))
            throw new InvalidOperationException("Playwright bundled Node runtime not found for " + MatdanceRuntime.OsName + " " + MatdanceRuntime.Architecture + ".");

        UnixExecutablePermissions.EnsurePlaywrightDriverExecutables(driverRoot, log);

        if (source == DependencySource.Cn)
        {
            const string mirror = "https://npmmirror.com/mirrors/playwright";
            log?.Invoke("Installing Playwright Chromium from CN mirror...");
            var cnExitCode = await RunProcessAsync(CreatePlaywrightInstallStartInfo(node, packageCli, mirror), log, ct);
            if (cnExitCode == 0)
                return;

            log?.Invoke($"CN mirror install failed with exit code {cnExitCode}. Falling back to Playwright global source...");
        }

        log?.Invoke("Installing Playwright Chromium from Playwright global source...");
        var exitCode = await RunProcessAsync(CreatePlaywrightInstallStartInfo(node, packageCli, mirror: null), log, ct);
        if (exitCode != 0)
            throw new InvalidOperationException("Playwright Chromium install failed with exit code " + exitCode + ".");
    }

    private static ProcessStartInfo CreatePlaywrightInstallStartInfo(string node, string packageCli, string? mirror)
    {
        var psi = new ProcessStartInfo
        {
            FileName = node,
            Arguments = $"{Quote(packageCli)} install chromium",
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["PLAYWRIGHT_BROWSERS_PATH"] = MatdanceRuntime.PlaywrightBrowsersPath;
        psi.Environment["CI"] = "1";
        if (!string.IsNullOrWhiteSpace(mirror))
        {
            psi.Environment["PLAYWRIGHT_DOWNLOAD_HOST"] = mirror;
            psi.Environment["PLAYWRIGHT_CHROMIUM_DOWNLOAD_HOST"] = mirror;
        }
        else
        {
            psi.Environment.Remove("PLAYWRIGHT_DOWNLOAD_HOST");
            psi.Environment.Remove("PLAYWRIGHT_CHROMIUM_DOWNLOAD_HOST");
        }

        return psi;
    }

    private static string GetBundledNodePath(string driverRoot)
    {
        var nodeRoot = Path.Combine(driverRoot, "node");
        string platform;
        if (OperatingSystem.IsWindows())
        {
            platform = "win32_x64";
        }
        else if (OperatingSystem.IsMacOS())
        {
            platform = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "darwin-arm64" : "darwin-x64";
        }
        else if (OperatingSystem.IsLinux())
        {
            platform = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        }
        else
        {
            platform = string.Empty;
        }

        var fileName = OperatingSystem.IsWindows() ? "node.exe" : "node";
        return Path.Combine(nodeRoot, platform, fileName);
    }

    private static async Task<int> RunProcessAsync(ProcessStartInfo psi, Action<string>? log, CancellationToken ct)
    {
        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

        if (!proc.Start())
            throw new InvalidOperationException("Failed to start dependency installer.");

        var outputTask = PumpProcessOutputAsync(proc.StandardOutput, log, ct);
        var errorTask = PumpProcessOutputAsync(proc.StandardError, log, ct);
        try
        {
            await proc.WaitForExitAsync(ct);
            await Task.WhenAll(outputTask, errorTask);
            return proc.ExitCode;
        }
        catch (OperationCanceledException)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            try { await Task.WhenAll(outputTask, errorTask); } catch { }
            throw;
        }
    }

    private static async Task PumpProcessOutputAsync(StreamReader reader, Action<string>? log, CancellationToken ct)
    {
        var readBuffer = new char[1024];
        var pending = new StringBuilder();
        string? lastEmitted = null;

        while (true)
        {
            var read = await reader.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), ct);
            if (read == 0)
                break;

            for (var i = 0; i < read; i++)
            {
                var ch = readBuffer[i];
                if (ch == '\r' || ch == '\n')
                {
                    EmitProcessOutput(pending, log, ref lastEmitted);
                    pending.Clear();
                    continue;
                }

                pending.Append(ch);
            }
        }

        EmitProcessOutput(pending, log, ref lastEmitted);
    }

    private static void EmitProcessOutput(StringBuilder pending, Action<string>? log, ref string? lastEmitted)
    {
        var text = pending.ToString().TrimEnd();
        if (string.IsNullOrWhiteSpace(text) || string.Equals(text, lastEmitted, StringComparison.Ordinal))
            return;

        lastEmitted = text;
        log?.Invoke(text);
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static DependencySource InferDefaultSource()
    {
        if (IsMainlandChinaEnvironment())
            return DependencySource.Cn;

        return DependencySource.Global;
    }

    private static bool IsMainlandChinaEnvironment()
    {
        var cultureHints = new[]
        {
            CultureInfo.CurrentCulture.Name,
            CultureInfo.CurrentUICulture.Name,
            CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            SafeCurrentRegionName(),
            UserTimeZoneService.GetDefaultTimeZoneId()
        };

        foreach (var hint in cultureHints)
        {
            if (string.IsNullOrWhiteSpace(hint))
                continue;

            var value = hint.Trim();
            if (value.Equals("CN", StringComparison.OrdinalIgnoreCase)
                || value.EndsWith("-CN", StringComparison.OrdinalIgnoreCase)
                || value.Equals("zh-CN", StringComparison.OrdinalIgnoreCase)
                || value.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase)
                || value.Contains("China Standard Time", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Asia/Shanghai", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Asia/Chongqing", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Asia/Harbin", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Asia/Urumqi", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? SafeCurrentRegionName()
    {
        try
        {
            return RegionInfo.CurrentRegion.TwoLetterISORegionName;
        }
        catch
        {
            return null;
        }
    }

    private static string DescribeSource(DependencySource requested, DependencySource effective)
    {
        var effectiveText = effective == DependencySource.Cn ? "CN optimized" : "Global official";
        return requested == DependencySource.Auto ? $"Auto -> {effectiveText}" : effectiveText;
    }
}

public enum DependencySource
{
    Auto,
    Global,
    Cn
}
