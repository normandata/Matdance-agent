namespace Matdance.Cli.Services;

internal static class DotnetHostResolver
{
    public static string ResolveDotnetHostPath()
    {
        var processPath = Environment.ProcessPath;
        if (LooksLikeDotnetHost(processPath) && File.Exists(processPath))
            return processPath!;

        var found = FindOnPath(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        return found ?? "dotnet";
    }

    public static bool LooksLikeDotnetHost(string? path)
        => !string.IsNullOrWhiteSpace(path)
            && Path.GetFileNameWithoutExtension(path).Equals("dotnet", StringComparison.OrdinalIgnoreCase);

    private static string? FindOnPath(string fileName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH")
            ?? Environment.GetEnvironmentVariable("Path");
        if (string.IsNullOrWhiteSpace(pathValue))
            return null;

        foreach (var entry in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate;
            try
            {
                candidate = Path.Combine(Environment.ExpandEnvironmentVariables(entry.Trim().Trim('"')), fileName);
            }
            catch
            {
                continue;
            }

            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
        }

        return null;
    }
}
