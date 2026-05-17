using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace Matdance.Cli.Services;

public sealed class LauncherRegistrationService
{
    public string Register(bool preferSystem = true, string? agentsRoot = null)
    {
        Directory.CreateDirectory(MatdanceRuntime.BinRoot);

        if (OperatingSystem.IsWindows())
            return RegisterWindows(preferSystem, agentsRoot);

        return RegisterUnix(preferSystem, agentsRoot);
    }

    [SupportedOSPlatform("windows")]
    private static string RegisterWindows(bool preferSystem, string? agentsRoot)
    {
        var cmdPath = Path.Combine(MatdanceRuntime.BinRoot, "matdance.cmd");
        var psPath = Path.Combine(MatdanceRuntime.BinRoot, "matdance.ps1");
        var invocation = BuildInvocation("%*", agentsRoot);

        WriteWindowsLaunchers(cmdPath, psPath, invocation);

        var target = EnvironmentVariableTarget.User;
        if (preferSystem && IsWindowsAdministrator())
            target = EnvironmentVariableTarget.Machine;

        var pathNote = PromoteInPath(MatdanceRuntime.BinRoot, target);
        var repairResult = RepairShadowedWindowsLaunchers(invocation, MatdanceRuntime.BinRoot);
        var scope = target == EnvironmentVariableTarget.Machine ? "machine PATH" : "user PATH";
        var repairNote = WindowsRepairNote(repairResult.Repaired, repairResult.Blocked);
        return $"Registered `matdance` via {cmdPath} in {scope}. {pathNote}.{repairNote} Open a new terminal before using it.";
    }

    private static string RegisterUnix(bool preferSystem, string? agentsRoot)
    {
        var scriptPath = Path.Combine(MatdanceRuntime.BinRoot, "matdance");
        var invocation = BuildInvocation("\"$@\"", agentsRoot);
        File.WriteAllText(scriptPath, "#!/usr/bin/env sh\n" + invocation.Sh + "\n");
        Chmod(scriptPath);

        var systemTarget = "/usr/local/bin/matdance";
        if (preferSystem && CanWriteDirectory("/usr/local/bin"))
        {
            ReplaceSymlink(scriptPath, systemTarget);
            var (repaired, blocked) = RepairShadowedUnixLaunchers(scriptPath, systemTarget);
            return UnixRegistrationMessage(systemTarget, repaired, blocked);
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var userBin = Path.Combine(home, ".local", "bin");
        Directory.CreateDirectory(userBin);
        var userTarget = Path.Combine(userBin, "matdance");
        ReplaceSymlink(scriptPath, userTarget);
        var repairResult = RepairShadowedUnixLaunchers(scriptPath, userTarget);
        return UnixRegistrationMessage(userTarget, repairResult.Repaired, repairResult.Blocked)
            + $" Add {userBin} to PATH if your shell does not already include it.";
    }

    private static (string Cmd, string PowerShell, string Sh) BuildInvocation(string argsToken, string? agentsRoot)
    {
        var normalizedAgentsRoot = string.IsNullOrWhiteSpace(agentsRoot) ? null : Path.GetFullPath(agentsRoot);
        var sourceProject = FindSourceProject();
        if (!string.IsNullOrWhiteSpace(sourceProject))
        {
            var workingDirectory = FindSourceWorkingDirectory(sourceProject) ?? Directory.GetCurrentDirectory();
            var configuration = SourceRunConfigurationArgument();
            var sourceScripts = BuildSourceWrapperInvocation(workingDirectory, configuration.Name, argsToken, normalizedAgentsRoot);
            if (sourceScripts != null)
                return sourceScripts.Value;

            return (
                WindowsCmdScript(workingDirectory, normalizedAgentsRoot, $"dotnet run{configuration.Cmd} --project {QuoteForCmd(sourceProject)} -- {argsToken}"),
                PowerShellScript(workingDirectory, normalizedAgentsRoot, $"& dotnet run{configuration.PowerShell} --project {QuoteForPowerShell(sourceProject)} -- @args"),
                ShScript(workingDirectory, normalizedAgentsRoot, $"exec dotnet run{configuration.Sh} --project {QuoteForSh(sourceProject)} -- {argsToken}"));
        }

        var processPath = Environment.ProcessPath ?? string.Empty;
        var dllPath = Path.Combine(AppContext.BaseDirectory, "Matdance.Cli.dll");
        var isDotnetHost = Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        var appRoot = AppContext.BaseDirectory;

        if (!isDotnetHost && File.Exists(processPath))
        {
            return (
                WindowsCmdScript(appRoot, normalizedAgentsRoot, $"{QuoteForCmd(processPath)} {argsToken}"),
                PowerShellScript(appRoot, normalizedAgentsRoot, $"& {QuoteForPowerShell(processPath)} @args"),
                ShScript(appRoot, normalizedAgentsRoot, $"exec {QuoteForSh(processPath)} {argsToken}"));
        }

        return (
            WindowsCmdScript(appRoot, normalizedAgentsRoot, $"dotnet {QuoteForCmd(dllPath)} {argsToken}"),
            PowerShellScript(appRoot, normalizedAgentsRoot, $"& dotnet {QuoteForPowerShell(dllPath)} @args"),
            ShScript(appRoot, normalizedAgentsRoot, $"exec dotnet {QuoteForSh(dllPath)} {argsToken}"));
    }

    private static string WindowsCmdScript(string workingDirectory, string? agentsRoot, string invocation)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(agentsRoot))
            lines.Add($"set \"MATDANCE_AGENTS_DIR={agentsRoot}\"");
        lines.Add($"pushd {QuoteForCmd(workingDirectory)}");
        lines.Add(invocation);
        lines.Add("set \"MATDANCE_EXIT=%ERRORLEVEL%\"");
        lines.Add("popd");
        lines.Add("exit /b %MATDANCE_EXIT%");
        return string.Join("\r\n", lines);
    }

    private static string PowerShellScript(string workingDirectory, string? agentsRoot, string invocation)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(agentsRoot))
            lines.Add($"$env:MATDANCE_AGENTS_DIR = {QuoteForPowerShell(agentsRoot)}");
        lines.Add($"Push-Location {QuoteForPowerShell(workingDirectory)}");
        lines.Add("try {");
        lines.Add("    " + invocation);
        lines.Add("    $matdanceExit = if ($global:LASTEXITCODE -ne $null) { $global:LASTEXITCODE } else { 0 }");
        lines.Add("} finally {");
        lines.Add("    Pop-Location");
        lines.Add("}");
        lines.Add("$global:LASTEXITCODE = $matdanceExit");
        return string.Join("\r\n", lines);
    }

    private static string ShScript(string workingDirectory, string? agentsRoot, string invocation)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(agentsRoot))
            lines.Add($"export MATDANCE_AGENTS_DIR={QuoteForSh(agentsRoot)}");
        lines.Add($"cd {QuoteForSh(workingDirectory)}");
        lines.Add(invocation);
        return string.Join("\n", lines);
    }

    private static (string Cmd, string PowerShell, string Sh)? BuildSourceWrapperInvocation(string workingDirectory, string configuration, string argsToken, string? agentsRoot)
    {
        var psWrapper = Path.Combine(workingDirectory, "matdance.ps1");
        var shWrapper = Path.Combine(workingDirectory, "matdance");
        if (!File.Exists(psWrapper) && !File.Exists(shWrapper))
            return null;

        var sourceProject = Path.Combine(workingDirectory, "src", "Matdance.Cli", "Matdance.Cli.csproj");
        var cmd = WindowsCmdSourceWrapperScript(workingDirectory, agentsRoot, configuration, sourceProject, argsToken);

        var ps = File.Exists(psWrapper)
            ? PowerShellScript(
                workingDirectory,
                agentsRoot,
                $"$env:MATDANCE_SOURCE_CONFIGURATION = {QuoteForPowerShell(configuration)}; & {QuoteForPowerShell(psWrapper)} @args")
            : PowerShellScript(
                workingDirectory,
                agentsRoot,
                $"& dotnet run -c {configuration} --project {QuoteForPowerShell(sourceProject)} -- @args");

        var sh = File.Exists(shWrapper)
            ? ShScript(
                workingDirectory,
                agentsRoot,
                $"export MATDANCE_SOURCE_CONFIGURATION={QuoteForSh(configuration)}\nexec sh {QuoteForSh(shWrapper)} {argsToken}")
            : ShScript(
                workingDirectory,
                agentsRoot,
                $"exec dotnet run -c {QuoteForSh(configuration)} --project {QuoteForSh(sourceProject)} -- {argsToken}");

        return (cmd, ps, sh);
    }

    private static string WindowsCmdSourceWrapperScript(string workingDirectory, string? agentsRoot, string configuration, string sourceProject, string argsToken)
    {
        var dllPath = Path.Combine(Path.GetDirectoryName(sourceProject)!, "bin", configuration, "net9.0", "Matdance.Cli.dll");
        var lines = new List<string>
        {
            "setlocal EnableExtensions EnableDelayedExpansion"
        };
        if (!string.IsNullOrWhiteSpace(agentsRoot))
            lines.Add($"set \"MATDANCE_AGENTS_DIR={agentsRoot}\"");
        lines.Add($"set \"MATDANCE_SOURCE_CONFIGURATION={configuration}\"");
        lines.Add($"set \"MATDANCE_DLL={dllPath}\"");
        lines.Add($"set \"MATDANCE_PROJECT={sourceProject}\"");
        lines.Add("call :MATDANCE_PARSE " + argsToken);
        lines.Add("if exist \"%MATDANCE_DLL%\" (");
        lines.Add("  call :MATDANCE_DIRECT_POLICY");
        lines.Add("  if \"!MATDANCE_DIRECT!\"==\"1\" (");
        lines.Add("    dotnet \"%MATDANCE_DLL%\" " + argsToken);
        lines.Add("    exit /b !ERRORLEVEL!");
        lines.Add("  )");
        lines.Add(")");
        lines.Add("set \"MATDANCE_RESTORE_SUCCESS=1\"");
        lines.Add("call :MATDANCE_RESTORE_POLICY");
        lines.Add("set \"MATDANCE_WAS_RUNNING=0\"");
        lines.Add("if exist \"%MATDANCE_DLL%\" call :MATDANCE_SUSPEND");
        lines.Add($"pushd {QuoteForCmd(workingDirectory)}");
        lines.Add("dotnet run -c %MATDANCE_SOURCE_CONFIGURATION% --project \"%MATDANCE_PROJECT%\" -- " + argsToken);
        lines.Add("set \"MATDANCE_EXIT=%ERRORLEVEL%\"");
        lines.Add("popd");
        lines.Add("if \"%MATDANCE_EXIT%\"==\"0\" if \"!MATDANCE_RESTORE_SUCCESS!\"==\"0\" set \"MATDANCE_WAS_RUNNING=0\"");
        lines.Add("call :MATDANCE_RESTORE");
        lines.Add("exit /b %MATDANCE_EXIT%");
        lines.Add("");
        lines.Add(":MATDANCE_PARSE");
        lines.Add("set \"MATDANCE_CMD=\"");
        lines.Add("set \"MATDANCE_SUB=\"");
        lines.Add("set \"MATDANCE_THIRD=\"");
        lines.Add("set \"MATDANCE_SKIP=\"");
        lines.Add(":MATDANCE_PARSE_LOOP");
        lines.Add("if \"%~1\"==\"\" exit /b 0");
        lines.Add("set \"MATDANCE_ARG=%~1\"");
        lines.Add("if defined MATDANCE_SKIP (set \"MATDANCE_SKIP=\" & shift & goto MATDANCE_PARSE_LOOP)");
        lines.Add("if /i \"!MATDANCE_ARG!\"==\"--agents-dir\" (set \"MATDANCE_SKIP=1\" & shift & goto MATDANCE_PARSE_LOOP)");
        lines.Add("if /i \"!MATDANCE_ARG:~0,13!\"==\"--agents-dir=\" (shift & goto MATDANCE_PARSE_LOOP)");
        lines.Add("if \"!MATDANCE_ARG:~0,1!\"==\"-\" (shift & goto MATDANCE_PARSE_LOOP)");
        lines.Add("if not defined MATDANCE_CMD (set \"MATDANCE_CMD=!MATDANCE_ARG!\") else if not defined MATDANCE_SUB (set \"MATDANCE_SUB=!MATDANCE_ARG!\") else if not defined MATDANCE_THIRD (set \"MATDANCE_THIRD=!MATDANCE_ARG!\")");
        lines.Add("shift");
        lines.Add("goto MATDANCE_PARSE_LOOP");
        lines.Add("");
        lines.Add(":MATDANCE_DIRECT_POLICY");
        lines.Add("set \"MATDANCE_DIRECT=0\"");
        lines.Add("if /i \"!MATDANCE_CMD!\"==\"stop-all\" set \"MATDANCE_DIRECT=1\"");
        lines.Add("if /i \"!MATDANCE_CMD!\"==\"web-ui\" (");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"stop\" set \"MATDANCE_DIRECT=1\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"stop-all\" set \"MATDANCE_DIRECT=1\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"status\" set \"MATDANCE_DIRECT=1\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"supervise\" set \"MATDANCE_DIRECT=1\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"supervisor\" if /i \"!MATDANCE_THIRD!\"==\"status\" set \"MATDANCE_DIRECT=1\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"supervisor\" if /i \"!MATDANCE_THIRD!\"==\"disable\" set \"MATDANCE_DIRECT=1\"");
        lines.Add(")");
        lines.Add("exit /b 0");
        lines.Add("");
        lines.Add(":MATDANCE_RESTORE_POLICY");
        lines.Add("if /i \"!MATDANCE_CMD!\"==\"stop-all\" set \"MATDANCE_RESTORE_SUCCESS=0\"");
        lines.Add("if /i \"!MATDANCE_CMD!\"==\"web-ui\" (");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"start\" set \"MATDANCE_RESTORE_SUCCESS=0\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"restart\" set \"MATDANCE_RESTORE_SUCCESS=0\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"stop\" set \"MATDANCE_RESTORE_SUCCESS=0\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"stop-all\" set \"MATDANCE_RESTORE_SUCCESS=0\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"supervise\" set \"MATDANCE_RESTORE_SUCCESS=0\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"supervisor\" if /i \"!MATDANCE_THIRD!\"==\"enable\" set \"MATDANCE_RESTORE_SUCCESS=0\"");
        lines.Add("  if /i \"!MATDANCE_SUB!\"==\"supervisor\" if /i \"!MATDANCE_THIRD!\"==\"disable\" set \"MATDANCE_RESTORE_SUCCESS=0\"");
        lines.Add(")");
        lines.Add("exit /b 0");
        lines.Add("");
        lines.Add(":MATDANCE_SUSPEND");
        lines.Add("set \"MATDANCE_STATUS=\"");
        lines.Add("for /f \"usebackq tokens=*\" %%L in (`dotnet \"%MATDANCE_DLL%\" web-ui status 2^>nul`) do (");
        lines.Add("  echo %%L | findstr /c:\"[web-ui] running at http://\" >nul && set \"MATDANCE_STATUS=%%L\"");
        lines.Add(")");
        lines.Add("if not defined MATDANCE_STATUS exit /b 0");
        lines.Add("for /f \"tokens=4\" %%U in (\"!MATDANCE_STATUS!\") do set \"MATDANCE_URL=%%U\"");
        lines.Add("set \"MATDANCE_HOSTPORT=!MATDANCE_URL:http://=!\"");
        lines.Add("for /f \"tokens=1,2 delims=:\" %%H in (\"!MATDANCE_HOSTPORT!\") do (set \"MATDANCE_RESTORE_HOST=%%H\" & set \"MATDANCE_RESTORE_PORT=%%I\")");
        lines.Add("if defined MATDANCE_RESTORE_HOST if defined MATDANCE_RESTORE_PORT (");
        lines.Add("  dotnet \"%MATDANCE_DLL%\" web-ui stop >nul 2>nul");
        lines.Add("  set \"MATDANCE_WAS_RUNNING=1\"");
        lines.Add(")");
        lines.Add("exit /b 0");
        lines.Add("");
        lines.Add(":MATDANCE_RESTORE");
        lines.Add("if \"!MATDANCE_WAS_RUNNING!\"==\"1\" dotnet \"%MATDANCE_DLL%\" web-ui start --mode preserve --host \"!MATDANCE_RESTORE_HOST!\" --port \"!MATDANCE_RESTORE_PORT!\" >nul 2>nul");
        lines.Add("exit /b 0");
        return string.Join("\r\n", lines);
    }

    private static string PromoteInPath(string dir, EnvironmentVariableTarget target)
    {
        var current = Environment.GetEnvironmentVariable("Path", target) ?? "";
        var normalizedDir = Path.GetFullPath(dir);
        var parts = current.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        var wasPresent = false;
        var nextParts = new List<string> { dir };
        foreach (var part in parts)
        {
            if (PathEntryEquals(part, normalizedDir))
            {
                wasPresent = true;
                continue;
            }

            nextParts.Add(part);
        }

        var next = string.Join(Path.PathSeparator, nextParts);
        Environment.SetEnvironmentVariable("Path", next, target);
        return wasPresent
            ? $"Moved {dir} to the front of {PathTargetName(target)} so stale entries do not win"
            : $"Added {dir} to the front of {PathTargetName(target)} so stale entries do not win";
    }

    private static bool PathEntryEquals(string entry, string normalizedDir)
    {
        try
        {
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(Path.GetFullPath(Environment.ExpandEnvironmentVariables(entry.Trim('"'))), normalizedDir, comparison);
        }
        catch
        {
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool IsWindowsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static bool CanWriteDirectory(string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
                return false;

            var probe = Path.Combine(directory, ".matdance-write-test-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ReplaceSymlink(string source, string target)
    {
        try
        {
            if (File.Exists(target) || IsSymlink(target))
                File.Delete(target);

            File.CreateSymbolicLink(target, source);
        }
        catch
        {
            File.Copy(source, target, overwrite: true);
        }

        Chmod(target);
    }

    private static void Chmod(string path)
    {
        if (OperatingSystem.IsWindows())
            return;

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = "+x \"" + path.Replace("\"", "\\\"") + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc?.WaitForExit(5000);
        }
        catch { }
    }

    private static void WriteWindowsLaunchers(string cmdPath, string psPath, (string Cmd, string PowerShell, string Sh) invocation)
    {
        File.WriteAllText(cmdPath, "@echo off\r\n" + invocation.Cmd + "\r\n");
        File.WriteAllText(psPath, invocation.PowerShell + "\r\n");
    }

    private static (int Repaired, List<string> Blocked) RepairShadowedWindowsLaunchers((string Cmd, string PowerShell, string Sh) invocation, string currentBinRoot)
    {
        var repaired = 0;
        var blocked = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = Path.GetFullPath(currentBinRoot);

        foreach (var dir in EnumeratePathDirectories(EnvironmentVariableTarget.Process, EnvironmentVariableTarget.User, EnvironmentVariableTarget.Machine))
        {
            if (PathEntryEquals(dir, current))
                continue;

            foreach (var candidate in new[]
            {
                Path.Combine(dir, "matdance.cmd"),
                Path.Combine(dir, "matdance.bat"),
                Path.Combine(dir, "matdance.ps1")
            })
            {
                if (!seen.Add(candidate) || IsSourceWrapper(candidate) || !File.Exists(candidate) || !LooksLikeMatdanceLauncher(candidate))
                    continue;

                try
                {
                    if (candidate.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                        File.WriteAllText(candidate, invocation.PowerShell + "\r\n");
                    else
                        File.WriteAllText(candidate, "@echo off\r\n" + invocation.Cmd + "\r\n");
                    repaired++;
                }
                catch
                {
                    blocked.Add(candidate);
                }
            }
        }

        return (repaired, blocked);
    }

    private static (int Repaired, List<string> Blocked) RepairShadowedUnixLaunchers(string sourceScript, string currentTarget)
    {
        var repaired = 0;
        var blocked = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var normalizedTarget = Path.GetFullPath(currentTarget);

        foreach (var dir in EnumeratePathDirectories(EnvironmentVariableTarget.Process))
        {
            var candidate = Path.Combine(dir, "matdance");
            if (!seen.Add(candidate))
                continue;

            try
            {
                if (Path.GetFullPath(candidate).Equals(normalizedTarget, StringComparison.Ordinal))
                    continue;
            }
            catch
            {
                continue;
            }

            if (IsSourceWrapper(candidate) || !File.Exists(candidate) || !LooksLikeMatdanceLauncher(candidate))
                continue;

            try
            {
                ReplaceSymlink(sourceScript, candidate);
                repaired++;
            }
            catch
            {
                blocked.Add(candidate);
            }
        }

        return (repaired, blocked);
    }

    private static IEnumerable<string> EnumeratePathDirectories(params EnvironmentVariableTarget[] targets)
    {
        var seen = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var target in targets)
        {
            var value = Environment.GetEnvironmentVariable("Path", target);
            if (string.IsNullOrWhiteSpace(value))
                continue;

            foreach (var part in value.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var expanded = Environment.ExpandEnvironmentVariables(part.Trim().Trim('"'));
                if (string.IsNullOrWhiteSpace(expanded) || !Directory.Exists(expanded))
                    continue;

                string full;
                try { full = Path.GetFullPath(expanded); }
                catch { continue; }

                if (seen.Add(full))
                    yield return full;
            }
        }
    }

    private static bool LooksLikeMatdanceLauncher(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream);
            var buffer = new char[4096];
            var read = reader.ReadBlock(buffer, 0, buffer.Length);
            var text = new string(buffer, 0, read);
            return text.Contains("Matdance.Cli", StringComparison.OrdinalIgnoreCase)
                || text.Contains("src/Matdance.Cli", StringComparison.OrdinalIgnoreCase)
                || text.Contains("src\\Matdance.Cli", StringComparison.OrdinalIgnoreCase)
                || text.Contains(".matdance", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string? FindSourceProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var direct = Path.Combine(dir.FullName, "Matdance.Cli.csproj");
            if (File.Exists(direct))
                return direct;

            var nested = Path.Combine(dir.FullName, "src", "Matdance.Cli", "Matdance.Cli.csproj");
            if (File.Exists(nested))
                return nested;

            dir = dir.Parent;
        }

        return null;
    }

    private static string? FindSourceWorkingDirectory(string sourceProject)
    {
        var projectDir = Path.GetDirectoryName(sourceProject);
        var srcDir = projectDir == null ? null : Directory.GetParent(projectDir)?.FullName;
        var repoRoot = srcDir == null ? null : Directory.GetParent(srcDir)?.FullName;
        return repoRoot != null && Directory.Exists(repoRoot) ? repoRoot : projectDir;
    }

    private static (string Cmd, string PowerShell, string Sh, string Name) SourceRunConfigurationArgument()
    {
        try
        {
            var baseDir = new DirectoryInfo(AppContext.BaseDirectory);
            var configuration = baseDir.Parent?.Name;
            if (configuration is "Debug" or "Release")
                return ($" -c {configuration}", $" -c {configuration}", $" -c {configuration}", configuration);
        }
        catch
        {
            
        }

        return ("", "", "", "Debug");
    }

    private static string UnixRegistrationMessage(string target, int repaired, IReadOnlyCollection<string> blocked)
    {
        var message = $"Registered `matdance` at {target}.";
        if (repaired > 0)
            message += $" Repaired {repaired} older Matdance launcher(s) already present on PATH.";
        if (blocked.Count > 0)
            message += " Older Matdance launcher(s) still appear earlier on PATH but could not be updated: " + string.Join(", ", blocked) + ".";
        return message;
    }

    private static string WindowsRepairNote(int repaired, IReadOnlyCollection<string> blocked)
    {
        var message = "";
        if (repaired > 0)
            message += $" Repaired {repaired} older Matdance launcher(s) already present on PATH.";
        if (blocked.Count > 0)
            message += " Older Matdance launcher(s) still appear on PATH but could not be updated: " + string.Join(", ", blocked) + ".";
        return message;
    }

    private static string PathTargetName(EnvironmentVariableTarget target) =>
        target == EnvironmentVariableTarget.Machine ? "machine PATH" :
        target == EnvironmentVariableTarget.User ? "user PATH" :
        "process PATH";

    private static bool IsSymlink(string path)
    {
        try
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSourceWrapper(string path)
    {
        var sourceProject = FindSourceProject();
        if (string.IsNullOrWhiteSpace(sourceProject))
            return false;

        var projectDir = Path.GetDirectoryName(sourceProject);
        var srcDir = projectDir == null ? null : Directory.GetParent(projectDir)?.FullName;
        var repoRoot = srcDir == null ? null : Directory.GetParent(srcDir)?.FullName;
        if (repoRoot == null)
            return false;

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var full = Path.GetFullPath(path);
        return full.Equals(Path.Combine(repoRoot, "matdance"), comparison)
            || full.Equals(Path.Combine(repoRoot, "matdance.ps1"), comparison)
            || full.Equals(Path.Combine(repoRoot, "matdance.bat"), comparison);
    }

    private static string QuoteForCmd(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static string QuoteForPowerShell(string value) => "'" + value.Replace("'", "''") + "'";

    private static string QuoteForSh(string value) => "'" + value.Replace("'", "'\"'\"'") + "'";
}
