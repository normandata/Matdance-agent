using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Matdance.Cli.Services;

public sealed class DependencyInstallerService
{
    public async Task InstallAsync(DependencySource source, Action<string>? log = null, CancellationToken ct = default)
    {
        MatdanceRuntime.ConfigureProcessEnvironment();
        Directory.CreateDirectory(MatdanceRuntime.DependenciesRoot);
        Directory.CreateDirectory(MatdanceRuntime.PlaywrightBrowsersPath);

        log?.Invoke($"Dependency root: {MatdanceRuntime.DependenciesRoot}");
        log?.Invoke($"Playwright browsers: {MatdanceRuntime.PlaywrightBrowsersPath}");
        log?.Invoke($"Download source: {(source == DependencySource.Cn ? "CN optimized" : "Global")}");

        await InstallPlaywrightChromiumAsync(source, log, ct);
    }

    public bool HasPlaywrightChromium()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH"),
            MatdanceRuntime.PlaywrightBrowsersPath,
            Path.Combine(MatdanceRuntime.DependenciesRoot, "playwright-browsers")
        };

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Any(HasPlaywrightChromiumAt);
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

        EnsureExecutable(node, log);

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

    private static void EnsureExecutable(string path, Action<string>? log)
    {
        if (OperatingSystem.IsWindows())
            return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = "+x " + Quote(path),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            log?.Invoke("chmod failed, continuing: " + ex.Message);
        }
    }

    private static async Task<int> RunProcessAsync(ProcessStartInfo psi, Action<string>? log, CancellationToken ct)
    {
        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) log?.Invoke(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) log?.Invoke(e.Data); };

        if (!proc.Start())
            throw new InvalidOperationException("Failed to start dependency installer.");

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        try
        {
            await proc.WaitForExitAsync(ct);
            return proc.ExitCode;
        }
        catch (OperationCanceledException)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw;
        }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
}

public enum DependencySource
{
    Global,
    Cn
}
