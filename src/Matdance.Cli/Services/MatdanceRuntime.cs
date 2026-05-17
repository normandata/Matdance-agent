using System.Runtime.InteropServices;

namespace Matdance.Cli.Services;

public static class MatdanceRuntime
{
    public static string AppRoot => AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    public static string RuntimeRoot => ResolveRuntimeRoot();
    public static string DependenciesRoot => Path.Combine(RuntimeRoot, "deps");
    public static string PlaywrightBrowsersPath =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH")
        ?? Path.Combine(DependenciesRoot, "playwright-browsers");
    public static string StateRoot => Path.Combine(RuntimeRoot, "state");
    public static string BinRoot => Path.Combine(RuntimeRoot, "bin");

    public static string OsName
    {
        get
        {
            if (OperatingSystem.IsWindows()) return "Windows";
            if (OperatingSystem.IsMacOS()) return "macOS";
            if (OperatingSystem.IsLinux()) return "Linux";
            return RuntimeInformation.OSDescription;
        }
    }

    public static string Architecture => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

    public static string ShellExecutable => OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash";

    public static string ShellInvocation => OperatingSystem.IsWindows()
        ? "cmd.exe /d /s /c"
        : "/bin/bash -c";

    public static string PathStyle => OperatingSystem.IsWindows()
        ? "Windows paths use backslashes, but forward slashes are accepted by Matdance path tools."
        : "Unix paths use forward slashes; match existing file and directory casing exactly.";

    public static void ConfigureProcessEnvironment()
    {
        Environment.SetEnvironmentVariable("MATDANCE_RUNTIME_DIR", RuntimeRoot);
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", PlaywrightBrowsersPath);
    }

    private static string ResolveRuntimeRoot()
    {
        var configured = Environment.GetEnvironmentVariable("MATDANCE_RUNTIME_DIR");
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        var projectRoot = Environment.GetEnvironmentVariable("MATDANCE_PROJECT_ROOT");
        if (!string.IsNullOrWhiteSpace(projectRoot))
            return Path.Combine(Path.GetFullPath(projectRoot), ".matdance");

        var agentsRoot = Environment.GetEnvironmentVariable("MATDANCE_AGENTS_DIR");
        if (!string.IsNullOrWhiteSpace(agentsRoot))
        {
            try
            {
                var fullAgentsRoot = Path.GetFullPath(agentsRoot);
                if (Path.GetFileName(fullAgentsRoot).Equals("agents", StringComparison.OrdinalIgnoreCase))
                {
                    var parent = Directory.GetParent(fullAgentsRoot);
                    if (parent != null)
                        return Path.Combine(parent.FullName, ".matdance");
                }
            }
            catch
            {
                
            }
        }

        return Path.Combine(FindProjectRoot(Directory.GetCurrentDirectory()) ?? FindProjectRoot(AppRoot) ?? AppRoot, ".matdance");
    }

    private static string? FindProjectRoot(string start)
    {
        try
        {
            var dir = new DirectoryInfo(Path.GetFullPath(start));
            if (File.Exists(dir.FullName))
                dir = dir.Parent!;

            while (dir != null)
            {
                var hasSourceProject = File.Exists(Path.Combine(dir.FullName, "src", "Matdance.Cli", "Matdance.Cli.csproj"));
                var hasAgents = Directory.Exists(Path.Combine(dir.FullName, "agents"));
                var hasGit = Directory.Exists(Path.Combine(dir.FullName, ".git"));
                if (hasSourceProject || (hasAgents && hasGit))
                    return dir.FullName;

                dir = dir.Parent;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}
