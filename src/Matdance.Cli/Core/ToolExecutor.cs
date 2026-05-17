using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;
using Matdance.Cli.Models;
using Matdance.Cli.Services;
using Matdance.Plugins.Browser;

namespace Matdance.Cli.Core;

public partial class ToolExecutor
{
    private readonly string _agentName;
    private readonly PathService _path;
    private readonly SessionState _state;
    private readonly bool _allowInteractiveConfirmation;
    private readonly string? _sessionId;
    private readonly BackgroundWorkCoordinator? _backgroundWork;
    private static readonly BrowserService _browser = BrowserService.Instance;
    private const int DefaultShellTimeoutSeconds = 30;
    private const int LongRunningShellTimeoutSeconds = 12;
    private const int MaxShellTimeoutSeconds = 120;
    private static readonly string[] PrivateShellVariableNames =
    {
        "userprofile", "home", "homedrive", "homepath", "homeshare",
        "appdata", "localappdata", "onedrive", "onedrivecommercial", "onedriveconsumer",
        "temp", "tmp", "xdg_config_home", "xdg_data_home", "xdg_cache_home", "xdg_state_home",
        "desktop", "documents", "downloads", "pictures", "music", "videos"
    };

    public ToolExecutor(string agentName, PathService path, SessionState state, bool allowInteractiveConfirmation = true, string? sessionId = null, BackgroundWorkCoordinator? backgroundWork = null)
    {
        _agentName = agentName;
        _path = path;
        _state = state;
        _allowInteractiveConfirmation = allowInteractiveConfirmation;
        _sessionId = sessionId;
        _backgroundWork = backgroundWork;
    }

    public async Task<string> ExecuteAsync(ToolCall toolCall)
    {
        var fn = toolCall.Function;
        Dictionary<string, JsonElement> args;
        try
        {
            args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(fn.Arguments) ?? new();
        }
        catch (JsonException ex)
        {
            var preview = fn.Arguments.Length > 240 ? fn.Arguments[..240] + "..." : fn.Arguments;
            return $"[error] Invalid JSON arguments for tool '{fn.Name}'. The model emitted incomplete or malformed tool parameters, so the tool was not executed. Details: {ex.Message} Args preview: {preview}";
        }

        try
        {
            return fn.Name switch
            {
                "bash" => await ExecuteBashAsync(args),
                "task_manager" => ExecuteTaskManager(args),
                "memory_store" => ExecuteMemoryStore(args),
                "memory_search" => ExecuteMemorySearch(args),
                "file_read" => ExecuteFileRead(args),
                "file_write" => ExecuteFileWrite(args),
                "scheduled_task_create" => ExecuteScheduledTaskCreate(args),
                "scheduled_task_edit" => ExecuteScheduledTaskEdit(args),
                "scheduled_task_list" => ExecuteScheduledTaskList(args),
                "scheduled_task_read" => ExecuteScheduledTaskRead(args),
                "scheduled_task_do" => await ExecuteScheduledTaskDoAsync(args),
                "scheduled_task_delete" => ExecuteScheduledTaskDelete(args),
                "skill_create" => await ExecuteSkillCreateAsync(args),
                "skill_read" => ExecuteSkillRead(args),
                "skill_editor" => await ExecuteSkillEditorAsync(args),
                "skill_delete" => await ExecuteSkillDeleteAsync(args),
                "image_generation_list_profiles" => ExecuteImageGenerationListProfiles(),
                "image_generation" => await ExecuteImageGenerationAsync(args),
                "text_to_speech_list_profiles" => ExecuteTextToSpeechListProfiles(),
                "text_to_speech" => await ExecuteTextToSpeechAsync(args),
                "web_search_list_profiles" => ExecuteWebSearchListProfiles(),
                "web_search" => await ExecuteWebSearchAsync(args),
                "browser_navigate" => await ExecuteBrowserNavigateAsync(args),
                "browser_click" => await ExecuteBrowserClickAsync(args),
                "browser_type" => await ExecuteBrowserTypeAsync(args),
                "browser_screenshot" => await ExecuteBrowserScreenshotAsync(args),
                "browser_get_content" => await ExecuteBrowserGetContentAsync(args),
                "browser_evaluate" => await ExecuteBrowserEvaluateAsync(args),
                "browser_wait_for" => await ExecuteBrowserWaitForAsync(args),
                "browser_query" => await ExecuteBrowserQueryAsync(args),
                "browser_scroll" => await ExecuteBrowserScrollAsync(args),
                "browser_inject_init_script" => await ExecuteBrowserInjectInitScriptAsync(args),
                "save_cookie" or "browser_save_cookie" => await ExecuteBrowserSaveCookieAsync(args),
                "list_cookie_by_site" or "browser_list_cookie_by_site" => await ExecuteBrowserListCookieBySiteAsync(args),
                "apply_cookie" or "browser_apply_cookie" => await ExecuteBrowserApplyCookieAsync(args),
                "browser_close" => await ExecuteBrowserCloseAsync(args),
                _ => $"[error] Unknown tool: {fn.Name}"
            };
        }
        catch (Exception ex)
        {
            return $"[error] Tool '{fn.Name}' failed: {ex.Message}";
        }
    }

    private async Task<string> ExecuteBashAsync(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("command", out var cmdEl) || cmdEl.ValueKind != JsonValueKind.String)
            return "[error] Missing required 'command' argument.";

        var command = cmdEl.GetString() ?? "";
        var hasExplicitTimeout = args.TryGetValue("timeout", out var t) && t.ValueKind == JsonValueKind.Number;
        var requestedTimeout = hasExplicitTimeout ? t.GetInt32() : DefaultShellTimeoutSeconds;
        var timeout = NormalizeShellTimeoutSeconds(command, requestedTimeout, hasExplicitTimeout);

        var securityBlock = GetShellSecurityBlockReason(command);
        if (securityBlock != null)
        {
            return "[blocked] " + securityBlock;
        }

        // Simple safety check
        bool needsConfirm = IsDangerousCommand(command);
        if (needsConfirm)
        {
            if (!_allowInteractiveConfirmation)
            {
                return "[blocked] Dangerous command requires interactive confirmation and is disabled in Web mode.";
            }

            var confirm = AnsiConsole.Confirm($"[bash] Execute: {command.EscapeMarkup()}?", false);
            if (!confirm)
            {
                return "[cancelled] User rejected command execution.";
            }
        }

        var workspace = _path.GetWorkspacePath(_agentName);
        Directory.CreateDirectory(workspace);
        
        var psi = new ProcessStartInfo
        {
            FileName = MatdanceRuntime.ShellExecutable,
            Arguments = OperatingSystem.IsWindows()
                ? $"/d /s /c {command}"
                : $"-c \"{command.Replace("\"", "\\\"")}\"",
            WorkingDirectory = workspace,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var proc = Process.Start(psi);
        if (proc == null) return "[error] Failed to start process.";

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        var completed = await Task.Run(() => proc.WaitForExit(timeout * 1000));
        if (!completed)
        {
            try { proc.Kill(entireProcessTree: true); } catch { try { proc.Kill(); } catch { } }
            return $"[timeout] Command timed out after {timeout}s and the process tree was terminated. Do not keep foreground servers or watchers running inside bash; use a short bounded check or an external managed service.\nstdout:\n{stdout}\nstderr:\n{stderr}";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"exit_code: {proc.ExitCode}");
        if (stdout.Length > 0) { sb.AppendLine("stdout:"); sb.AppendLine(stdout.ToString()); }
        if (stderr.Length > 0) { sb.AppendLine("stderr:"); sb.AppendLine(stderr.ToString()); }
        return sb.ToString().Trim();
    }

    private static int NormalizeShellTimeoutSeconds(string command, int requestedTimeout, bool hasExplicitTimeout)
    {
        var timeout = Math.Clamp(requestedTimeout <= 0 ? DefaultShellTimeoutSeconds : requestedTimeout, 1, MaxShellTimeoutSeconds);
        if (!hasExplicitTimeout && LooksLikeLongRunningShellCommand(command))
        {
            timeout = Math.Min(timeout, LongRunningShellTimeoutSeconds);
        }

        return timeout;
    }

    private static bool LooksLikeLongRunningShellCommand(string command)
    {
        var normalized = Regex.Replace(command.ToLowerInvariant(), @"\s+", " ").Trim();
        return Regex.IsMatch(normalized, @"\b(npm|pnpm|yarn|bun)\s+(run\s+)?(dev|start|serve|watch)\b", RegexOptions.IgnoreCase)
            || Regex.IsMatch(normalized, @"\b(dotnet\s+watch|dotnet\s+run|python\s+-m\s+http\.server|http-server|vite|next\s+dev|nuxt\s+dev|webpack-dev-server)\b", RegexOptions.IgnoreCase)
            || Regex.IsMatch(normalized, @"\b(flask\s+run|uvicorn|gunicorn|rails\s+server|php\s+-S|go\s+run)\b", RegexOptions.IgnoreCase);
    }

    private static bool IsDangerousCommand(string command)
    {
        var lower = command.ToLowerInvariant();
        var compact = Regex.Replace(lower, @"\s+", " ").Trim();
        var dangerous = new[]
        {
            "rm ", "rm -", "del ", "erase ", "rd /s", "rmdir /s", "remove-item", " ri ",
            "format", "mkfs", "dd if=", "> /dev", ":(){ :|:& };:",
            "shutdown", "restart-computer", "stop-computer", "taskkill", "stop-process",
            "powershell ", "powershell.exe", "pwsh ", "pwsh.exe", "cmd /c", "cmd.exe /c",
            "bash -c", "sh -c"
        };

        if (dangerous.Any(d => compact.Contains(d)))
            return true;

        return Regex.IsMatch(compact, @"(^|[;&|]\s*)(rm|del|erase|rd|rmdir|remove-item|ri)\b", RegexOptions.IgnoreCase)
            || Regex.IsMatch(compact, @"\b(rm|remove-item)\b.*\s(-r|-rf|-fr|/s)\b", RegexOptions.IgnoreCase);
    }

    private string ExecuteTaskManager(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("action", out var actionEl) || actionEl.ValueKind != JsonValueKind.String)
            return "[error] Missing required 'action' argument.";

        var action = actionEl.GetString() ?? "";

        switch (action)
        {
            case "create":
                if (_state.ActiveTask != null && _state.ActiveTask.Status == "in_process")
                    return "[error] A task is already in_process. Finish it first.";

                if (!args.TryGetValue("title", out var titleEl) || titleEl.ValueKind != JsonValueKind.String)
                    return "[error] Missing 'title' for create action. Please provide a task title.";

                var title = titleEl.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(title))
                    return "[error] Task title cannot be empty.";

                var stepsArr = args.TryGetValue("steps", out var s) && s.ValueKind == JsonValueKind.Array
                    ? s.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                    : new List<string>();
                stepsArr = NormalizeTaskSteps(stepsArr);

                var task = new ActiveTaskInfo
                {
                    TaskId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    Title = title,
                    Status = "in_process",
                    Steps = stepsArr.Select((desc, i) => new TaskStep
                    {
                        Index = i + 1,
                        ForWhat = desc,
                        Status = i == 0 ? "in_process" : "pending"
                    }).ToList()
                };
                _state.ActiveTask = task;
                return $"[task_manager] Created task '{title}' with {task.Steps.Count} steps.";

            case "update":
                if (_state.ActiveTask == null) return "[error] No active task.";
                var stepIdx = args.TryGetValue("step_index", out var si) ? si.GetInt32() : -1;
                var newStatus = args.TryGetValue("status", out var st) ? st.GetString() : null;
                var note = args.TryGetValue("note", out var n) ? n.GetString() : null;

                if (stepIdx > 0 && stepIdx <= _state.ActiveTask.Steps.Count)
                {
                    var step = _state.ActiveTask.Steps[stepIdx - 1];
                    if (newStatus != null) step.Status = newStatus;
                }
                if (newStatus != null && _state.ActiveTask.Steps.All(s => s.Status != "in_process" && s.Status != "pending"))
                {
                    _state.ActiveTask.Status = newStatus;
                }
                return $"[task_manager] Updated. {note ?? ""}";

            case "done":
                if (_state.ActiveTask == null) return "[error] No active task.";
                _state.ActiveTask.Status = "done";
                foreach (var step in _state.ActiveTask.Steps)
                    if (step.Status == "in_process" || step.Status == "pending")
                        step.Status = "done";
                return $"[task_manager] Task '{_state.ActiveTask.Title}' marked as done.";

            case "status":
                if (_state.ActiveTask == null) return "[task_manager] No active task.";
                var sb = new StringBuilder();
                sb.AppendLine($"Task: {_state.ActiveTask.Title} ({_state.ActiveTask.Status})");
                foreach (var step in _state.ActiveTask.Steps)
                    sb.AppendLine($"  Step {step.Index}: [{step.Status}] {step.ForWhat}");
                return sb.ToString().Trim();

            default:
                return $"[error] Unknown action: {action}";
        }
    }

    private string ExecuteMemoryStore(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("target", out var targetEl) || targetEl.ValueKind != JsonValueKind.String)
            return "[error] Missing required 'target' argument.";
        if (!args.TryGetValue("content", out var contentEl) || contentEl.ValueKind != JsonValueKind.String)
            return "[error] Missing required 'content' argument.";

        var target = targetEl.GetString() ?? "";
        var content = contentEl.GetString() ?? "";

        if (target == "hot")
        {
            var path = _path.GetHotMemoryPath(_agentName);
            if (!AppendNarrativeMemory(path, content, "Hot Memory"))
                return "[memory_store] Duplicate hot memory skipped.";

            new VectorMemoryService(_path).Refresh(_agentName);
            return "[memory_store] Hot memory appended and vector index refreshed.";
        }
        if (target == "core")
        {
            var path = _path.GetCoreMemoryPath(_agentName);
            if (!AppendNarrativeMemory(path, content, "Core Memory"))
                return "[memory_store] Duplicate core memory skipped.";

            new VectorMemoryService(_path).Refresh(_agentName);
            return "[memory_store] Core memory appended and vector index refreshed.";
        }
        return "[error] target must be 'hot' or 'core'.";
    }

    private static bool AppendNarrativeMemory(string path, string content, string title)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var trimmed = content.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        var existing = File.Exists(path) ? File.ReadAllText(path).TrimEnd() : $"# {title}";
        if (ContainsRecentDuplicateMemory(existing, trimmed))
        {
            return false;
        }

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(existing))
        {
            sb.AppendLine(existing);
            sb.AppendLine();
        }
        sb.AppendLine($"## {UserTimeZoneService.Now():yyyy-MM-dd HH:mm} Narrative Update");
        sb.AppendLine(trimmed);
        AtomicFile.WriteAllText(path, sb.ToString().TrimEnd() + Environment.NewLine);
        return true;
    }

    private static List<string> NormalizeTaskSteps(List<string> steps)
    {
        var cleaned = steps
            .Select(step => (step ?? string.Empty).Trim())
            .Where(step => !string.IsNullOrWhiteSpace(step))
            .Take(3)
            .ToList();

        if (cleaned.Count == 0)
            cleaned.Add("Complete the requested task.");

        return cleaned;
    }

    private static bool ContainsRecentDuplicateMemory(string existing, string content)
    {
        var normalizedContent = NormalizeMemoryContent(content);
        if (normalizedContent.Length == 0) return true;
        var recent = existing.Length > 20000 ? existing[^20000..] : existing;
        return NormalizeMemoryContent(recent).Contains(normalizedContent, StringComparison.Ordinal);
    }

    private static string NormalizeMemoryContent(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private string ExecuteMemorySearch(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("query", out var queryEl) || queryEl.ValueKind != JsonValueKind.String)
            return "[error] Missing required 'query' argument.";

        var query = queryEl.GetString() ?? "";
        var date = args.TryGetValue("date", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;

        if (!string.IsNullOrEmpty(date))
        {
            var path = Path.Combine(_path.GetLongTermMemoryPath(_agentName), $"{date}.md");
            if (!File.Exists(path)) return $"[memory_search] No memory found for {date}.";
            return $"[memory_search] {date}:\n{File.ReadAllText(path)}";
        }

        var result = new VectorMemoryService(_path).Search(_agentName, query, take: 5);
        if (result.Items.Count == 0)
            return $"[memory_search] No relevant memories found. vector_entries={result.EntryCount}";

        var sb = new StringBuilder();
        sb.AppendLine($"[memory_search] Vector results ({result.Algorithm}, entries={result.EntryCount}, candidates={result.CandidateCount}, visited={result.VisitedNodes}):");
        foreach (var item in result.Items)
        {
            var entry = item.Entry;
            var preview = entry.Text.Length > 900 ? entry.Text[..900] + "\n...[truncated]" : entry.Text;
            sb.AppendLine($"--- {entry.Kind}: {entry.Title} #{entry.ChunkIndex} score={item.Score:0.000} ---");
            sb.AppendLine($"path: {entry.SourcePath}:{entry.StartLine}");
            sb.AppendLine($"rerank: cosine={item.Cosine:0.000}, lexical={item.Lexical:0.000}, hamming={item.HammingSimilarity:0.000}");
            sb.AppendLine(preview);
        }
        return sb.ToString().Trim();
    }

    private string ExecuteFileRead(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return "[error] Missing required 'path' argument.";

        var filePath = pathEl.GetString() ?? "";
        var untrace = args.TryGetValue("untrace", out var u) && u.ValueKind == JsonValueKind.True;

        var fullPath = ResolveWorkspacePath(filePath, forWrite: false);

        if (untrace)
        {
            var existing = _state.TracedFiles.FirstOrDefault(t => t.Path == fullPath);
            if (existing != null)
            {
                _state.TracedFiles.Remove(existing);
                return $"[file_read] Stopped tracing {filePath}.";
            }
            return $"[file_read] File was not traced: {filePath}.";
        }

        if (!File.Exists(fullPath))
            return $"[error] File not found: {filePath}";

        var content = File.ReadAllText(fullPath);
        var traced = _state.TracedFiles.FirstOrDefault(t => t.Path == fullPath);
        if (traced != null)
        {
            traced.Content = content;
            traced.LastRead = UserTimeZoneService.Now();
        }
        else
        {
            if (_state.TracedFiles.Count >= 3)
                _state.TracedFiles.RemoveAt(0); // Remove oldest
            _state.TracedFiles.Add(new TracedFileInfo
            {
                Path = fullPath,
                Content = content,
                LastRead = UserTimeZoneService.Now()
            });
        }

        var limit = args.TryGetValue("limit", out var limitEl) && limitEl.ValueKind == JsonValueKind.Number ? limitEl.GetInt32() : 50000;
        var preview = content.Length > limit ? content[..limit] + "\n...[truncated]" : content;
        return $"[file_read] {filePath} (resolved to {fullPath}, {content.Length} chars):\n```\n{preview}\n```";
    }

    private string ExecuteFileWrite(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return "[error] Missing required 'path' argument.";
        if (!args.TryGetValue("content", out var contentEl) || contentEl.ValueKind != JsonValueKind.String)
            return "[error] Missing required 'content' argument.";

        var filePath = pathEl.GetString() ?? "";
        var content = contentEl.GetString() ?? "";
        var append = args.TryGetValue("append", out var a) && a.ValueKind == JsonValueKind.True;

        var fullPath = ResolveWorkspacePath(filePath, forWrite: true);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        if (append && File.Exists(fullPath))
            File.AppendAllText(fullPath, content);
        else
            AtomicFile.WriteAllText(fullPath, content);

        // Update traced file if exists
        var traced = _state.TracedFiles.FirstOrDefault(t => t.Path == fullPath);
        if (traced != null)
        {
            traced.Content = File.ReadAllText(fullPath);
            traced.LastRead = UserTimeZoneService.Now();
        }

        return $"[file_write] {(append ? "Appended to" : "Wrote")} {filePath} (resolved to {fullPath}, {content.Length} chars).";
    }

    private string ResolveWorkspacePath(string filePath, bool forWrite)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new InvalidOperationException("File path cannot be empty.");

        var currentDir = Directory.GetCurrentDirectory();
        var workspace = _path.GetWorkspacePath(_agentName);
        var normalized = PathSafety.NormalizeSeparators(filePath);

        // If absolute path, validate it's within allowed directories
        if (Path.IsPathRooted(normalized))
        {
            var fullPath = Path.GetFullPath(normalized);
            var browserTempDir = Path.GetFullPath(Path.Combine(currentDir, "browser_temp"));
            EnsurePathAllowedForAgentFileTool(fullPath, forWrite, workspace, browserTempDir, currentDir);

            return fullPath;
        }

        // Reject path traversal attempts
        if (PathSafety.ContainsParentTraversal(normalized))
            throw new InvalidOperationException("Path traversal (../ or ..\\) is not allowed.");
        
        // Strip leading separators that could cause issues
        normalized = normalized.TrimStart(Path.DirectorySeparatorChar);
        
        // If path has no subdirectory, default to projects/ subfolder
        if (!normalized.Contains(Path.DirectorySeparatorChar))
        {
            return Path.GetFullPath(Path.Combine(workspace, "projects", normalized));
        }

        var resolved = Path.GetFullPath(Path.Combine(workspace, normalized));
        if (!PathSafety.IsUnderRoot(resolved, workspace))
            throw new InvalidOperationException("Path resolves outside workspace.");
        EnsurePathAllowedForAgentFileTool(resolved, forWrite, workspace, Path.GetFullPath(Path.Combine(currentDir, "browser_temp")), currentDir);

        return resolved;
    }

    private void EnsurePathAllowedForAgentFileTool(string fullPath, bool forWrite, string workspace, string browserTempDir, string currentDir)
    {
        fullPath = Path.GetFullPath(fullPath);
        workspace = Path.GetFullPath(workspace);
        browserTempDir = Path.GetFullPath(browserTempDir);
        currentDir = Path.GetFullPath(currentDir);

        if (IsBlockedMatdanceInternalPath(fullPath, workspace, browserTempDir, currentDir))
        {
            throw new InvalidOperationException("Matdance source, runtime state, credentials, task run records, browser cookie stores, and internal queues are not accessible through agent file tools.");
        }

        if (LooksLikeSecretFile(fullPath))
        {
            throw new InvalidOperationException("Secret-bearing files such as env/key/token/cookie/password files are not accessible through agent file tools.");
        }

        if (forWrite)
        {
            if (!PathSafety.IsUnderRoot(fullPath, workspace))
                throw new InvalidOperationException("file_write is limited to the agent workspace and cannot modify Matdance source, runtime state, agent config, or private user locations.");
            return;
        }

        if (PathSafety.IsUnderAnyRoot(fullPath, new[] { workspace, browserTempDir }))
            return;

        if (!new SecuritySettingsService().Load().AllowPrivateDataAccess)
        {
            throw new InvalidOperationException("Private data access is disabled in Settings. Ask the user to either paste a filtered excerpt or temporarily enable the global privacy access switch for this task.");
        }
    }

    private bool IsBlockedMatdanceInternalPath(string fullPath, string workspace, string browserTempDir, string currentDir)
    {
        if (PathSafety.IsUnderAnyRoot(fullPath, new[] { workspace, browserTempDir }))
            return false;

        var agentsRoot = Path.GetFullPath(_path.AgentsRoot);
        var runtimeRoot = Path.GetFullPath(MatdanceRuntime.RuntimeRoot);
        var stateRoot = Path.GetFullPath(MatdanceRuntime.StateRoot);
        var srcRoot = Path.GetFullPath(Path.Combine(currentDir, "src"));
        var pluginsRoot = Path.GetFullPath(Path.Combine(currentDir, "plugins"));
        var gitRoot = Path.GetFullPath(Path.Combine(currentDir, ".git"));

        if (PathSafety.IsUnderAnyRoot(fullPath, new[] { runtimeRoot, stateRoot, srcRoot, pluginsRoot, gitRoot }))
            return true;

        if (!PathSafety.IsUnderRoot(fullPath, agentsRoot))
            return false;

        var relative = Path.GetRelativePath(agentsRoot, fullPath);
        var segments = PathSafety.NormalizeSeparators(relative)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            return true;

        if (segments.Length >= 3 && segments[1].Equals("workspace", StringComparison.OrdinalIgnoreCase))
            return false;

        if (segments.Length >= 3 && segments[1].Equals("icons", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static bool LooksLikeSecretFile(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is ".env" or ".key" or ".pem" or ".pfx" or ".p12" or ".sqlite" or ".db")
            return true;

        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        var markers = new[]
        {
            "password", "passwd", "secret", "token", "api_key", "apikey", "credential",
            "credentials", "cookie", "cookies", "browser_cookies", "web-auth"
        };
        return markers.Any(marker => name.Contains(marker, StringComparison.Ordinal) || normalized.Contains("/" + marker, StringComparison.Ordinal));
    }

    private static string? GetShellSecurityBlockReason(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return "Empty shell command.";

        var normalized = command.Replace('\\', '/').ToLowerInvariant();
        if (Regex.IsMatch(normalized, @"(^|[\s""'])(\.\.)(/|\\|$)"))
            return "Shell commands may not use parent-directory traversal. Work inside the agent workspace.";

        var blockedMarkers = new[]
        {
            ".matdance", "/src/", "src/", "/plugins/", "plugins/", "/.git/", ".git/",
            "agent_config.json", "multimodal_config.json", "browser_cookies", "cookies.json",
            "web-auth.json", "security-settings.json", "scheduled_tasks/runs", "/runtime/",
            "api_key", "apikey", "password", "passwd", "secret", "token", ".env", ".pem", ".pfx", ".p12"
        };
        if (blockedMarkers.Any(marker => normalized.Contains(marker, StringComparison.Ordinal)))
            return "Shell commands may not access Matdance source/internal state or credential-like files.";

        if (!IsPrivateDataAccessAllowed())
        {
            var privateAccessReason = GetPrivateShellAccessBlockReason(command, normalized);
            if (privateAccessReason != null)
                return privateAccessReason;
        }

        return null;
    }

    private static bool IsPrivateDataAccessAllowed() => new SecuritySettingsService().Load().AllowPrivateDataAccess;

    private static string? GetPrivateShellAccessBlockReason(string command, string normalizedCommand)
    {
        if (LooksLikeAbsoluteUserPath(normalizedCommand))
            return "Private data access is disabled in Settings. The shell tool cannot inspect user-private absolute paths in this mode.";

        if (LooksLikePrivateShellVariableAccess(normalizedCommand))
            return "Private data access is disabled in Settings. The shell tool cannot use user-profile, home, app-data, cloud-drive, temp, or shell-known-folder environment variables in this mode.";

        if (LooksLikeKnownFolderAccess(normalizedCommand))
            return "Private data access is disabled in Settings. The shell tool cannot resolve user known folders, shell folders, or user-profile registry paths in this mode.";

        var unescaped = Uri.UnescapeDataString(command).Replace('\\', '/').ToLowerInvariant();
        if (!ReferenceEquals(unescaped, normalizedCommand) && LooksLikeAbsoluteUserPath(unescaped))
            return "Private data access is disabled in Settings. The shell tool cannot inspect encoded user-private paths in this mode.";

        return null;
    }

    private static bool LooksLikeAbsoluteUserPath(string normalizedCommand)
    {
        if (Regex.IsMatch(normalizedCommand, @"[a-z]:/users/[^ \t\r\n""']+", RegexOptions.IgnoreCase))
            return true;
        if (Regex.IsMatch(normalizedCommand, @"/users/[^ \t\r\n""']+", RegexOptions.IgnoreCase))
            return true;
        if (Regex.IsMatch(normalizedCommand, @"/home/[^ \t\r\n""']+", RegexOptions.IgnoreCase))
            return true;
        if (Regex.IsMatch(normalizedCommand, @"/documents and settings/[^ \t\r\n""']+", RegexOptions.IgnoreCase))
            return true;
        if (normalizedCommand.Contains("~/", StringComparison.Ordinal))
            return true;
        return Regex.IsMatch(normalizedCommand, @"(^|[\s""'=:/;&|])~($|[/\s""';&|])", RegexOptions.IgnoreCase);
    }

    private static bool LooksLikePrivateShellVariableAccess(string normalizedCommand)
    {
        foreach (var name in PrivateShellVariableNames)
        {
            var escaped = Regex.Escape(name);
            if (Regex.IsMatch(normalizedCommand, $@"%{escaped}(:[^%]*)?%", RegexOptions.IgnoreCase))
                return true;
            if (Regex.IsMatch(normalizedCommand, $@"!{escaped}(:[^!]*)?!", RegexOptions.IgnoreCase))
                return true;
            if (Regex.IsMatch(normalizedCommand, $@"\$env:{escaped}\b", RegexOptions.IgnoreCase))
                return true;
            if (Regex.IsMatch(normalizedCommand, $@"\$\{{env:{escaped}\}}", RegexOptions.IgnoreCase))
                return true;
            if (Regex.IsMatch(normalizedCommand, $@"\$\{{{escaped}\}}", RegexOptions.IgnoreCase))
                return true;
        }

        return Regex.IsMatch(normalizedCommand, @"(^|[^\w])\$home\b", RegexOptions.IgnoreCase);
    }

    private static bool LooksLikeKnownFolderAccess(string normalizedCommand)
    {
        var markers = new[]
        {
            "[environment]::getfolderpath",
            "system.environment]::getfolderpath",
            ".getfolderpath(",
            "shgetknownfolderpath",
            "knownfolder",
            "user shell folders",
            "explorer/shell folders",
            "explorer/user shell folders",
            "hkcu/software/microsoft/windows/currentversion/explorer",
            "hkey_current_user/software/microsoft/windows/currentversion/explorer"
        };
        if (markers.Any(marker => normalizedCommand.Contains(marker, StringComparison.Ordinal)))
            return true;

        return Regex.IsMatch(
            normalizedCommand,
            @"shell:\s*(desktop|personal|documents|downloads|my pictures|pictures|my music|music|my video|videos|appdata|local appdata|startup|sendto|recent|favorites|cookies|cache|history)\b",
            RegexOptions.IgnoreCase);
    }

    private async Task<string> ExecuteSkillCreateAsync(Dictionary<string, JsonElement> args)
    {
        return await WithSkillsResourceLockAsync(() =>
        {
            var service = new SkillService(_path);
            var request = new SkillCreateRequest
            {
                Name = RequiredString(args, "name"),
                Description = RequiredString(args, "description"),
                Tags = OptionalStringArray(args, "tags"),
                Content = RequiredString(args, "content")
            };
            var skill = service.Create(_agentName, request);
            return $"[skill_create] Created skill '{skill.Name}' (ID: {skill.Id}). Use skill_read(skill_id='{skill.Id}') to load it.";
        });
    }

    private string ExecuteSkillRead(Dictionary<string, JsonElement> args)
    {
        var service = new SkillService(_path);
        var skillId = RequiredString(args, "skill_id");
        var skill = service.Read(_agentName, skillId);
        var skillDir = _path.GetSkillPath(_agentName, skillId);
        var tags = skill.Tags.Count > 0 ? $"Tags: {string.Join(", ", skill.Tags)}\n" : "";
        var reports = SkillValidationState.BuildSkillReportContext(skillDir, detailed: true);
        return $"## Skill: {skill.Name}\n\n{tags}{skill.Content}\n\n---\n## Skill Reports\n\n{reports}\n\n---\n[skill_read] Loaded skill '{skill.Name}' (ID: {skill.Id}) with current validation/import notes.";
    }

    private async Task<string> ExecuteSkillEditorAsync(Dictionary<string, JsonElement> args)
    {
        return await WithSkillsResourceLockAsync(() =>
        {
            var service = new SkillService(_path);
            var request = new SkillEditRequest
            {
                Id = RequiredString(args, "skill_id"),
                Name = OptionalString(args, "name"),
                Description = OptionalString(args, "description"),
                Tags = OptionalStringArray(args, "tags"),
                Content = OptionalString(args, "content")
            };
            var skill = service.Edit(_agentName, request);
            return $"[skill_editor] Updated skill '{skill.Name}' (ID: {skill.Id}). Updated at {UserTimeZoneService.ToUserTime(skill.UpdatedAt):yyyy-MM-dd HH:mm:ss zzz}.";
        });
    }

    private async Task<string> ExecuteSkillDeleteAsync(Dictionary<string, JsonElement> args)
    {
        return await WithSkillsResourceLockAsync(() =>
        {
            var service = new SkillService(_path);
            var skillId = RequiredString(args, "skill_id");
            service.Delete(_agentName, skillId);
            return $"[skill_delete] Deleted skill '{skillId}'.";
        });
    }

    private async Task<string> WithSkillsResourceLockAsync(Func<string> action)
    {
        if (_backgroundWork == null)
            return action();

        using var lease = await _backgroundWork.TryAcquireResourceAsync(
            _agentName,
            BackgroundWorkCoordinator.SkillsResource,
            BackgroundWorkCoordinator.ResourceRetryTimeout,
            CancellationToken.None);

        if (lease == null)
            return "[blocked] The skills resource is busy for this agent. Retry after the current skill organization, validation, or edit finishes.";

        return action();
    }

    private string ExecuteImageGenerationListProfiles()
    {
        var effective = new MultiModalConfigService(_path).GetEffective(_agentName);
        var profiles = effective.ImageModels.Count > 0
            ? effective.ImageModels
            : new List<EffectiveImageGenerationConfig> { effective.Image };
        var defaultProfile = profiles.FirstOrDefault(profile => profile.Enabled) ?? profiles.First();
        var payload = new
        {
            agent = _agentName,
            defaultProfile = new
            {
                id = defaultProfile.Id,
                name = defaultProfile.Name
            },
            autoSelection = "Calling image_generation without profile uses enabled profiles in this configured order and falls back to the next enabled profile if a candidate fails.",
            profiles = profiles.Select((profile, index) => new
            {
                order = index + 1,
                id = profile.Id,
                name = profile.Name,
                enabled = profile.Enabled,
                isDefault = ReferenceEquals(profile, defaultProfile),
                usable = profile.Enabled && profile.HasApiKey && !string.IsNullOrWhiteSpace(profile.BaseUrl),
                model = profile.Model,
                baseUrl = profile.BaseUrl,
                endpointMode = profile.EndpointMode,
                size = profile.Size,
                quality = profile.Quality,
                outputFormat = profile.OutputFormat,
                hasApiKey = profile.HasApiKey
            }).ToList()
        };

        var enabledCount = profiles.Count(profile => profile.Enabled);
        var usableCount = profiles.Count(profile => profile.Enabled && profile.HasApiKey && !string.IsNullOrWhiteSpace(profile.BaseUrl));
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        return $"[image_generation_list_profiles] {profiles.Count} profile(s), {enabledCount} enabled, {usableCount} usable. Omit profile in image_generation to use the default/auto order.\n{json}";
    }

    private async Task<string> ExecuteImageGenerationAsync(Dictionary<string, JsonElement> args)
    {
        var client = new MultiModalClient(_path);
        var request = new ImageGenerationRequest
        {
            Agent = _agentName,
            ImageProfile = OptionalString(args, "profile"),
            Prompt = RequiredString(args, "prompt"),
            Size = OptionalString(args, "size"),
            Quality = OptionalString(args, "quality"),
            OutputFormat = OptionalString(args, "output_format"),
            Count = IntArg(args, "count", 1),
            OutputPath = OptionalString(args, "output_path")
        };

        var results = await client.GenerateImageAsync(_agentName, request);
        var previews = string.Join(", ", results.Select(result => result.RelativePath));
        var usedProfiles = string.Join(", ", results
            .Select(result => result.ImageProfileName ?? result.ImageProfileId ?? result.Model)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        var profileNote = string.IsNullOrWhiteSpace(usedProfiles) ? "" : $" using {usedProfiles}";
        return $"[image_generation] Generated {results.Count} image(s){profileNote}. Preview them for the user with {{show_file:{previews}}}.\n{json}";
    }

    private string ExecuteTextToSpeechListProfiles()
    {
        var effective = new MultiModalConfigService(_path).GetEffective(_agentName);
        var profiles = effective.TtsModels.Count > 0
            ? effective.TtsModels
            : new List<EffectiveTextToSpeechConfig> { effective.Tts };
        var defaultProfile = profiles.FirstOrDefault(profile => !profile.Mode.Equals("off", StringComparison.OrdinalIgnoreCase)) ?? profiles.First();
        var payload = new
        {
            agent = _agentName,
            defaultProfile = new
            {
                id = defaultProfile.Id,
                name = defaultProfile.Name
            },
            autoSelection = "Calling text_to_speech without profile uses enabled TTS profiles in this configured order and falls back to the next enabled profile if a candidate fails.",
            usagePolicy = "Usually avoid proactive TTS in ordinary chat. Use it for requested lines/scripts/narration or when audio assets are a reasonable part of the creative task. For long scripts or narration, prefer sentence-bounded batches; the host can retry retryable long-input failures by splitting into up to 10 chunks and merging one final wav.",
            profiles = profiles.Select((profile, index) => new
            {
                order = index + 1,
                id = profile.Id,
                name = profile.Name,
                enabled = !profile.Mode.Equals("off", StringComparison.OrdinalIgnoreCase),
                isDefault = ReferenceEquals(profile, defaultProfile),
                usable = !profile.Mode.Equals("off", StringComparison.OrdinalIgnoreCase) && profile.HasApiKey && !string.IsNullOrWhiteSpace(profile.BaseUrl),
                mode = profile.Mode,
                autoPlay = profile.AutoPlay,
                model = profile.Model,
                voice = profile.Voice,
                languageType = profile.LanguageType,
                endpointMode = profile.EndpointMode,
                baseUrl = profile.BaseUrl,
                format = profile.Format,
                hasApiKey = profile.HasApiKey
            }).ToList()
        };

        var enabledCount = profiles.Count(profile => !profile.Mode.Equals("off", StringComparison.OrdinalIgnoreCase));
        var usableCount = profiles.Count(profile => !profile.Mode.Equals("off", StringComparison.OrdinalIgnoreCase) && profile.HasApiKey && !string.IsNullOrWhiteSpace(profile.BaseUrl));
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        return $"[text_to_speech_list_profiles] {profiles.Count} profile(s), {enabledCount} enabled, {usableCount} usable. Omit profile in text_to_speech to use the default/auto order.\n{json}";
    }

    private async Task<string> ExecuteTextToSpeechAsync(Dictionary<string, JsonElement> args)
    {
        var texts = new List<string>();
        var singleText = OptionalString(args, "text");
        if (!string.IsNullOrWhiteSpace(singleText))
        {
            texts.Add(singleText.Trim());
        }

        var textList = OptionalStringArray(args, "texts");
        if (textList != null)
        {
            texts.AddRange(textList.Select(text => text.Trim()).Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        if (texts.Count == 0)
        {
            return "[error] text_to_speech requires 'text' or a non-empty 'texts' array.";
        }

        var client = new MultiModalClient(_path);
        var profile = OptionalString(args, "profile");
        var outputPath = OptionalString(args, "output_path");
        var results = new List<GeneratedFileResult>();
        for (var i = 0; i < texts.Count; i++)
        {
            var request = new TextToSpeechRequest
            {
                Agent = _agentName,
                TtsProfile = profile,
                AllowProfileFallback = BoolArg(args, "allow_profile_fallback", string.IsNullOrWhiteSpace(profile)),
                Text = texts[i],
                Voice = OptionalString(args, "voice"),
                Format = OptionalString(args, "format"),
                OutputPath = IndexedOutputPath(outputPath, i, texts.Count)
            };
            results.Add(await client.TextToSpeechAsync(_agentName, request));
        }

        var previews = string.Join(", ", results.Select(result => result.RelativePath));
        var usedProfiles = string.Join(", ", results
            .Select(result => result.TtsProfileName ?? result.TtsProfileId ?? result.Model)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        var profileNote = string.IsNullOrWhiteSpace(usedProfiles) ? "" : $" using {usedProfiles}";
        return $"[text_to_speech] Generated {results.Count} audio file(s){profileNote}. Preview them for the user with {{show_file:{previews}}}.\n{json}";
    }

    private string ExecuteWebSearchListProfiles()
    {
        var effective = new MultiModalConfigService(_path).GetEffective(_agentName);
        var profiles = effective.SearchModels.Count > 0
            ? effective.SearchModels
            : new List<EffectiveWebSearchConfig> { effective.Search };
        var defaultProfile = profiles.FirstOrDefault(profile => profile.Enabled) ?? profiles.First();
        var payload = new
        {
            agent = _agentName,
            defaultProfile = new
            {
                id = defaultProfile.Id,
                name = defaultProfile.Name
            },
            autoSelection = "Calling web_search without profile uses enabled search profiles in configured order and falls back to the next enabled profile if a candidate fails.",
            usagePolicy = "Use web_search for current information and source discovery. Do not use it to bypass paywalls, authentication, CAPTCHA, robots restrictions, or provider terms.",
            profiles = profiles.Select((profile, index) => new
            {
                order = index + 1,
                id = profile.Id,
                name = profile.Name,
                enabled = profile.Enabled,
                isDefault = ReferenceEquals(profile, defaultProfile),
                usable = profile.Enabled && profile.HasApiKey && !string.IsNullOrWhiteSpace(profile.BaseUrl),
                provider = profile.Provider,
                baseUrl = profile.BaseUrl,
                endpointPath = profile.EndpointPath,
                maxResults = profile.MaxResults,
                hasApiKey = profile.HasApiKey
            }).ToList()
        };

        var enabledCount = profiles.Count(profile => profile.Enabled);
        var usableCount = profiles.Count(profile => profile.Enabled && profile.HasApiKey && !string.IsNullOrWhiteSpace(profile.BaseUrl));
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        return $"[web_search_list_profiles] {profiles.Count} profile(s), {enabledCount} enabled, {usableCount} usable. Omit profile in web_search to use the default/auto order.\n{json}";
    }

    private async Task<string> ExecuteWebSearchAsync(Dictionary<string, JsonElement> args)
    {
        var client = new MultiModalClient(_path);
        var profile = OptionalString(args, "profile");
        var request = new WebSearchRequest
        {
            Agent = _agentName,
            SearchProfile = profile,
            AllowProfileFallback = BoolArg(args, "allow_profile_fallback", string.IsNullOrWhiteSpace(profile)),
            Query = RequiredString(args, "query"),
            MaxResults = args.TryGetValue("max_results", out var maxResults) && maxResults.ValueKind == JsonValueKind.Number ? maxResults.GetInt32() : null
        };

        var result = await client.SearchAsync(_agentName, request);
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        return $"[web_search] {result.Items.Count} result(s) from {result.SearchProfileName ?? result.Provider} for query: {result.Query}\n{json}";
    }

    private static string? IndexedOutputPath(string? outputPath, int index, int count)
    {
        if (string.IsNullOrWhiteSpace(outputPath) || count <= 1)
        {
            return outputPath;
        }

        var extension = Path.GetExtension(outputPath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return outputPath;
        }

        var directory = Path.GetDirectoryName(outputPath);
        var fileName = Path.GetFileNameWithoutExtension(outputPath) + $"_{index + 1}" + extension;
        return string.IsNullOrWhiteSpace(directory) ? fileName : Path.Combine(directory, fileName);
    }

    #region Browser Tools

    private async Task<string> ExecuteBrowserNavigateAsync(Dictionary<string, JsonElement> args)
    {
        var url = RequiredString(args, "url");
        var headless = BoolArg(args, "headless", true);
        var waitNetworkIdle = IntArg(args, "wait_network_idle", 0);
        var ensureResult = await _browser.EnsureBrowserAsync(headless);
        var navigateResult = await _browser.NavigateAsync(url, waitNetworkIdle);
        return ensureResult.Contains("ignored", StringComparison.OrdinalIgnoreCase)
            ? ensureResult + "\n" + navigateResult
            : navigateResult;
    }

    private async Task<string> ExecuteBrowserClickAsync(Dictionary<string, JsonElement> args)
    {
        var selector = RequiredString(args, "selector");
        var timeout = IntArg(args, "timeout", 5000);
        return await _browser.ClickAsync(selector, timeout);
    }

    private async Task<string> ExecuteBrowserTypeAsync(Dictionary<string, JsonElement> args)
    {
        var selector = RequiredString(args, "selector");
        var text = RequiredString(args, "text");
        var submit = BoolArg(args, "submit", false);
        var timeout = IntArg(args, "timeout", 5000);
        return await _browser.TypeAsync(selector, text, submit, timeout);
    }

    private async Task<string> ExecuteBrowserScreenshotAsync(Dictionary<string, JsonElement> args)
    {
        var outputPath = OptionalString(args, "output_path");
        var fullPage = BoolArg(args, "full_page", false);
        return await _browser.ScreenshotAsync(outputPath, fullPage);
    }

    private async Task<string> ExecuteBrowserGetContentAsync(Dictionary<string, JsonElement> args)
    {
        var html = BoolArg(args, "html", false);
        var maxLength = IntArg(args, "max_length", 12000);
        return await _browser.GetContentAsync(html, maxLength);
    }

    private async Task<string> ExecuteBrowserEvaluateAsync(Dictionary<string, JsonElement> args)
    {
        var script = RequiredString(args, "script");
        var timeout = IntArg(args, "timeout", 8000);
        return await _browser.EvaluateAsync(script, timeout);
    }

    private async Task<string> ExecuteBrowserWaitForAsync(Dictionary<string, JsonElement> args)
    {
        var kind = RequiredString(args, "kind");
        var selector = OptionalString(args, "selector");
        var text = OptionalString(args, "text");
        var state = OptionalString(args, "state");
        var regex = BoolArg(args, "regex", false);
        var timeout = IntArg(args, "timeout", 10000);
        return await _browser.WaitForAsync(kind, selector, text, state, regex, timeout);
    }

    private async Task<string> ExecuteBrowserQueryAsync(Dictionary<string, JsonElement> args)
    {
        var selector = OptionalString(args, "selector");
        var text = OptionalString(args, "text");
        var limit = IntArg(args, "limit", 30);
        return await _browser.QueryAsync(selector, text, limit);
    }

    private async Task<string> ExecuteBrowserScrollAsync(Dictionary<string, JsonElement> args)
    {
        var selector = OptionalString(args, "selector");
        var direction = OptionalString(args, "direction");
        var pixels = IntArg(args, "pixels", 900);
        var steps = IntArg(args, "steps", 1);
        var untilSelector = OptionalString(args, "until_selector");
        var untilText = OptionalString(args, "until_text");
        var delay = IntArg(args, "delay", 300);
        return await _browser.ScrollAsync(selector, direction, pixels, steps, untilSelector, untilText, delay);
    }

    private async Task<string> ExecuteBrowserInjectInitScriptAsync(Dictionary<string, JsonElement> args)
    {
        var script = RequiredString(args, "script");
        var purpose = RequiredString(args, "purpose");
        return await _browser.InjectInitScriptAsync(script, purpose);
    }

    private async Task<string> ExecuteBrowserSaveCookieAsync(Dictionary<string, JsonElement> args)
    {
        var site = OptionalString(args, "site");
        return await _browser.SaveCookiesAsync(_path.GetBrowserCookiesJsonPath(_agentName), site);
    }

    private async Task<string> ExecuteBrowserListCookieBySiteAsync(Dictionary<string, JsonElement> args)
    {
        var site = OptionalString(args, "site");
        return await _browser.ListCookiesBySiteAsync(_path.GetBrowserCookiesJsonPath(_agentName), site);
    }

    private async Task<string> ExecuteBrowserApplyCookieAsync(Dictionary<string, JsonElement> args)
    {
        var site = OptionalString(args, "site");
        return await _browser.ApplyCookiesAsync(_path.GetBrowserCookiesJsonPath(_agentName), site);
    }

    private async Task<string> ExecuteBrowserCloseAsync(Dictionary<string, JsonElement> args)
    {
        return await _browser.CloseAsync();
    }

    #endregion
}
