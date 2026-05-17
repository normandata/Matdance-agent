using System.Diagnostics;

namespace Matdance.Cli.Services;

internal static class UnixExecutablePermissions
{
    public static void EnsurePlaywrightDriverExecutables(string playwrightRoot, Action<string>? log = null)
    {
        if (OperatingSystem.IsWindows() || !Directory.Exists(playwrightRoot))
            return;

        foreach (var path in EnumeratePlaywrightExecutableCandidates(playwrightRoot).Distinct(StringComparer.Ordinal))
            EnsureExecutable(path, log);
    }

    private static IEnumerable<string> EnumeratePlaywrightExecutableCandidates(string playwrightRoot)
    {
        string[] files;
        try
        {
            files = Directory.EnumerateFiles(playwrightRoot, "*", SearchOption.AllDirectories).ToArray();
        }
        catch
        {
            yield break;
        }

        foreach (var file in files)
        {
            var name = Path.GetFileName(file);
            if (name.Equals("node", StringComparison.Ordinal)
                || name.Equals("cli.js", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }

    private static void EnsureExecutable(string path, Action<string>? log)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList = { "+x", path }
            });

            if (proc == null)
                return;

            if (!proc.WaitForExit(5000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                log?.Invoke("chmod timed out, continuing: " + path);
            }
            else if (proc.ExitCode != 0)
            {
                log?.Invoke("chmod failed, continuing: " + path);
            }
        }
        catch (Exception ex)
        {
            log?.Invoke("chmod failed, continuing: " + ex.Message);
        }
    }
}
