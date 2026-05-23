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
    private readonly bool _synchronousImageGeneration;
    private static readonly BrowserService _browser = BrowserService.Instance;
    private const int DefaultShellTimeoutSeconds = 30;
    private const int LongRunningShellTimeoutSeconds = 12;
    private const int MaxShellTimeoutSeconds = 120;
    private const int MaxFileSearchFiles = 250;
    private const int MaxFileSearchBytes = 2_000_000;
    private const int MaxAgentFileReadBytes = 5_000_000;
    private const int MaxAgentFileWriteBytes = 5_000_000;
    private const int FileTraceToolBudgetSeconds = 12;
    private static readonly string[] VisualImageFileExtensions =
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".ico", ".tif", ".tiff", ".avif", ".heic", ".heif"
    };
    private static readonly TimeSpan DefaultToolExecutionTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan FileToolExecutionTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BrowserToolExecutionTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan LongBackgroundToolExecutionTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan FileSearchTimeBudget = TimeSpan.FromSeconds(8);
    private static readonly string[] PrivateShellVariableNames =
    {
        "userprofile", "home", "homedrive", "homepath", "homeshare",
        "appdata", "localappdata", "onedrive", "onedrivecommercial", "onedriveconsumer",
        "temp", "tmp", "xdg_config_home", "xdg_data_home", "xdg_cache_home", "xdg_state_home",
        "desktop", "documents", "downloads", "pictures", "music", "videos"
    };

    public ToolExecutor(string agentName, PathService path, SessionState state, bool allowInteractiveConfirmation = true, string? sessionId = null, BackgroundWorkCoordinator? backgroundWork = null, bool synchronousImageGeneration = false)
    {
        _agentName = agentName;
        _path = path;
        _state = state;
        _allowInteractiveConfirmation = allowInteractiveConfirmation;
        _sessionId = sessionId;
        _backgroundWork = backgroundWork;
        _synchronousImageGeneration = synchronousImageGeneration;
    }

    public async Task<string> ExecuteAsync(ToolCall toolCall, CancellationToken ct = default)
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

        var toolTimeout = GetToolExecutionTimeout(fn.Name);
        using var toolCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var toolTask = Task.Run(() => ExecuteToolCoreAsync(fn.Name, args, toolCts.Token), toolCts.Token);
        try
        {
            return await toolTask.WaitAsync(toolTimeout, ct);
        }
        catch (TimeoutException)
        {
            toolCts.Cancel();
            _ = toolTask.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            return $"[error] Tool '{fn.Name}' timed out after {FormatDuration(toolTimeout)}. The call was stopped so the agent turn cannot hang forever. Retry with a narrower scope, smaller file/page range, shorter timeout, or ask the user to intervene.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return $"[error] Tool '{fn.Name}' was cancelled.";
        }
        catch (Exception ex)
        {
            return $"[error] Tool '{fn.Name}' failed: {ex.Message}";
        }
    }

    private async Task<string> ExecuteToolCoreAsync(string name, Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        return name switch
        {
            "bash" => await ExecuteBashAsync(args, ct),
            "task_manager" => ExecuteTaskManager(args),
            "memory_store" => ExecuteMemoryStore(args),
            "memory_search" => ExecuteMemorySearch(args),
            "file_search" => ExecuteFileSearch(args),
            "file_trace_open" => ExecuteFileTraceOpen(args),
            "file_trace_show" => ExecuteFileTraceShow(args),
            "file_trace_close" => ExecuteFileTraceClose(args),
            "file_read" => ExecuteFileRead(args),
            "file_write" => ExecuteFileWrite(args),
            "file_write_locks" => ExecuteFileWriteLocks(args),
            "file_write_lock_close" => ExecuteFileWriteLockClose(args),
            "session_list" => ExecuteSessionList(args),
            "scheduled_task_create" => ExecuteScheduledTaskCreate(args),
            "scheduled_task_edit" => ExecuteScheduledTaskEdit(args),
            "scheduled_task_list" => ExecuteScheduledTaskList(args),
            "scheduled_task_read" => ExecuteScheduledTaskRead(args),
            "scheduled_task_do_a_test" or "scheduled_task_do" => await ExecuteScheduledTaskDoAsync(args, ct),
            "scheduled_task_delete" => ExecuteScheduledTaskDelete(args),
            "skill_create" => await ExecuteSkillCreateAsync(args),
            "skill_read" => ExecuteSkillRead(args),
            "skill_editor" => await ExecuteSkillEditorAsync(args),
            "skill_delete" => await ExecuteSkillDeleteAsync(args),
            "image_generation_list_profiles" => ExecuteImageGenerationListProfiles(),
            "image_generation" => await ExecuteImageGenerationAsync(args, ct),
            "image_edit" => await ExecuteImageEditAsync(args, ct),
            "image_generation_show_process" => ExecuteImageGenerationShowProcess(args),
            "image_generation_cancel" => ExecuteImageGenerationCancel(args),
            "image_generation_retry" => ExecuteImageGenerationRetry(args),
            "text_to_speech_list_profiles" => ExecuteTextToSpeechListProfiles(),
            "text_to_speech" => await ExecuteTextToSpeechAsync(args, ct),
            "web_search_list_profiles" => ExecuteWebSearchListProfiles(),
            "web_search" => await ExecuteWebSearchAsync(args, ct),
            "browser_navigate" => await ExecuteBrowserNavigateAsync(args, ct),
            "browser_click" => await ExecuteBrowserClickAsync(args, ct),
            "browser_type" => await ExecuteBrowserTypeAsync(args, ct),
            "browser_screenshot" => await ExecuteBrowserScreenshotAsync(args, ct),
            "browser_get_content" => await ExecuteBrowserGetContentAsync(args, ct),
            "browser_evaluate" => await ExecuteBrowserEvaluateAsync(args, ct),
            "browser_wait_for" => await ExecuteBrowserWaitForAsync(args, ct),
            "browser_query" => await ExecuteBrowserQueryAsync(args, ct),
            "browser_source_analyze" => await ExecuteBrowserSourceAnalyzeAsync(args, ct),
            "browser_scroll" => await ExecuteBrowserScrollAsync(args, ct),
            "browser_verify" => await ExecuteBrowserVerifyAsync(args, ct),
            "browser_crawl" => await ExecuteBrowserCrawlAsync(args, ct),
            "browser_trace" => await ExecuteBrowserTraceAsync(args, ct),
            "browser_inject_init_script" => await ExecuteBrowserInjectInitScriptAsync(args, ct),
            "save_cookie" or "browser_save_cookie" => await ExecuteBrowserSaveCookieAsync(args, ct),
            "list_cookie_by_site" or "browser_list_cookie_by_site" => await ExecuteBrowserListCookieBySiteAsync(args, ct),
            "apply_cookie" or "browser_apply_cookie" => await ExecuteBrowserApplyCookieAsync(args, ct),
            "browser_close" => await ExecuteBrowserCloseAsync(args, ct),
            _ => $"[error] Unknown tool: {name}"
        };
    }

    private TimeSpan GetToolExecutionTimeout(string toolName)
    {
        if (toolName.Equals("bash", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromSeconds(MaxShellTimeoutSeconds + 15);
        if (toolName.StartsWith("file_", StringComparison.OrdinalIgnoreCase))
            return FileToolExecutionTimeout;
        if (toolName.StartsWith("browser_", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("save_cookie", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("list_cookie_by_site", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("apply_cookie", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("browser_save_cookie", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("browser_list_cookie_by_site", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("browser_apply_cookie", StringComparison.OrdinalIgnoreCase))
            return BrowserToolExecutionTimeout;
        if (toolName.Equals("image_generation", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("image_edit", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("text_to_speech", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("scheduled_task_do", StringComparison.OrdinalIgnoreCase)
            || toolName.Equals("scheduled_task_do_a_test", StringComparison.OrdinalIgnoreCase))
            return LongBackgroundToolExecutionTimeout;
        if (toolName.Equals("web_search", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromMinutes(3);
        return DefaultToolExecutionTimeout;
    }

    private static string FormatDuration(TimeSpan value)
    {
        return value.TotalMinutes >= 1
            ? $"{value.TotalMinutes:0.#} minute(s)"
            : $"{value.TotalSeconds:0.#} second(s)";
    }

    private async Task<string> ExecuteBashAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
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

        var waitTask = proc.WaitForExitAsync(ct);
        var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct)) == waitTask;
        if (ct.IsCancellationRequested)
        {
            try { proc.Kill(entireProcessTree: true); } catch { try { proc.Kill(); } catch { } }
            ct.ThrowIfCancellationRequested();
        }
        if (!completed)
        {
            try { proc.Kill(entireProcessTree: true); } catch { try { proc.Kill(); } catch { } }
            return $"[timeout] Command timed out after {timeout}s and the process tree was terminated. Do not keep foreground servers or watchers running inside bash; use a short bounded check or an external managed service.\nstdout:\n{stdout}\nstderr:\n{stderr}";
        }
        await waitTask;

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

    private string ExecuteFileSearch(Dictionary<string, JsonElement> args)
    {
        var queries = GetStringList(args, "queries");
        if (args.TryGetValue("query", out var queryEl) && queryEl.ValueKind == JsonValueKind.String)
        {
            var query = queryEl.GetString();
            if (!string.IsNullOrWhiteSpace(query))
                queries.Add(query!);
        }
        queries = queries.Where(q => !string.IsNullOrWhiteSpace(q)).Distinct(StringComparer.Ordinal).ToList();
        if (queries.Count == 0)
            return "[error] Missing 'query' or 'queries'.";

        var requestedPaths = GetStringList(args, "paths");
        if (requestedPaths.Count == 0)
        {
            var workspace = _path.GetWorkspacePath(_agentName);
            var projects = Path.Combine(workspace, "projects");
            requestedPaths.Add(Directory.Exists(projects) ? projects : workspace);
        }

        var regex = args.TryGetValue("regex", out var regexEl) && regexEl.ValueKind == JsonValueKind.True;
        var caseSensitive = args.TryGetValue("case_sensitive", out var caseEl) && caseEl.ValueKind == JsonValueKind.True;
        var maxMatches = Math.Clamp(GetInt(args, "max_matches", 80), 1, 300);
        var before = Math.Clamp(GetInt(args, "before", 1), 0, 20);
        var after = Math.Clamp(GetInt(args, "after", 1), 0, 20);
        var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        var regexQueries = new List<Regex>();
        if (regex)
        {
            foreach (var query in queries)
            {
                try
                {
                    regexQueries.Add(new Regex(query, regexOptions, TimeSpan.FromMilliseconds(250)));
                }
                catch (ArgumentException ex)
                {
                    return $"[error] Invalid regular expression '{query}': {ex.Message}";
                }
            }
        }

        var deadline = DateTimeOffset.UtcNow + FileSearchTimeBudget;
        var files = ResolveSearchFiles(requestedPaths, MaxFileSearchFiles, deadline);
        var matches = new List<string>();
        var searched = 0;
        foreach (var file in files)
        {
            if (DateTimeOffset.UtcNow > deadline)
                break;
            if (matches.Count >= maxMatches)
                break;
            if (!LooksTextSearchable(file))
                continue;

            searched++;
            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch
            {
                continue;
            }

            var lines = FileTraceLockService.SplitLines(text);
            for (var i = 0; i < lines.Length && matches.Count < maxMatches; i++)
            {
                for (var queryIndex = 0; queryIndex < queries.Count; queryIndex++)
                {
                    var query = queries[queryIndex];
                    bool hit;
                    try
                    {
                        hit = regex
                            ? regexQueries[queryIndex].IsMatch(lines[i])
                            : lines[i].IndexOf(query, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        return $"[error] Regex search for '{query}' timed out on a line. Use a simpler regex or a literal query.";
                    }
                    if (!hit)
                        continue;

                    var start = Math.Max(1, i + 1 - before);
                    var end = Math.Min(lines.Length, i + 1 + after);
                    var snippet = RenderPlainLines(lines, start, end);
                    matches.Add($"File: {file}\nQuery: {query}\nMatch line: L{i + 1}\n```\n{snippet}\n```");
                    break;
                }
            }
        }

        var timedOut = DateTimeOffset.UtcNow > deadline;
        if (matches.Count == 0)
            return $"[file_search] No matches. searched_files={searched}, scanned_candidates={files.Count}, time_budget={FileSearchTimeBudget.TotalSeconds:F0}s{(timedOut ? ", stopped_by_time_budget=true" : "")}. Open a narrower trace/search path or adjust the query if needed.";

        var sb = new StringBuilder();
        sb.AppendLine($"[file_search] Found {matches.Count} match(es) across {searched} searched file(s). scanned_candidates={files.Count}, time_budget={FileSearchTimeBudget.TotalSeconds:F0}s{(timedOut ? ", stopped_by_time_budget=true" : "")}. Search results are navigation hints only; open a read lock before relying on nearby content for edits.");
        sb.AppendLine();
        sb.AppendLine(string.Join("\n\n---\n\n", matches));
        if (matches.Count >= maxMatches)
            sb.AppendLine($"\n[truncated] max_matches={maxMatches}");
        return sb.ToString().TrimEnd();
    }

    private string ExecuteFileTraceOpen(Dictionary<string, JsonElement> args)
    {
        var requests = ReadTraceOpenRequests(args);
        if (requests.Count == 0)
            return "[error] Missing file path. Provide path or locks[].";

        var readLockCount = _state.TracedFiles.Count(IsReadLock);
        if (readLockCount + requests.Count > FileTraceLockService.MaxReadLocks)
            return $"[error] read_lock_limit_exceeded: {readLockCount} read lock(s) are already open and only {FileTraceLockService.MaxReadLocks} are allowed. Close unused read locks with file_trace_close before opening more.";

        var resolvedRequests = new List<(TraceOpenRequest Request, string FullPath)>();
        foreach (var request in requests)
        {
            var fullPath = ResolveWorkspacePath(request.Path, forWrite: false);
            if (File.Exists(fullPath) && IsVisualImageFile(fullPath))
                return VisualImageReadMessage("file_trace_open", request.Path, fullPath);
            resolvedRequests.Add((request, fullPath));
        }

        var created = new List<TracedFileInfo>();
        foreach (var (request, fullPath) in resolvedRequests)
        {
            var trace = FileTraceLockService.CreateReadLock(fullPath, request.StartLine, request.EndLine, request.Anchor, request.Mode, request.MaxLines);
            if (string.Equals(trace.Status, "missing", StringComparison.OrdinalIgnoreCase))
                return $"[error] File not found: {request.Path}";
            _state.TracedFiles.Add(trace);
            created.Add(trace);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[file_trace_open] Opened {created.Count} read lock(s). Read locks are live; use file_trace_show to refresh and file_trace_close when they are no longer useful.");
        foreach (var trace in created)
        {
            sb.AppendLine();
            sb.AppendLine(FileTraceLockService.DescribeLock(trace));
        }
        return sb.ToString().TrimEnd();
    }

    private string ExecuteFileTraceShow(Dictionary<string, JsonElement> args)
    {
        var ids = GetStringList(args, "ids");
        var kind = GetString(args, "kind", "all").ToLowerInvariant();
        var locks = _state.TracedFiles
            .Where(t => ids.Count == 0 || ids.Contains(t.Id, StringComparer.OrdinalIgnoreCase))
            .Where(t => kind == "all" || string.Equals(t.Kind, kind, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (locks.Count == 0)
            return "[file_trace_show] No matching locks.";

        var sb = new StringBuilder();
        sb.AppendLine($"[file_trace_show] Refreshed {locks.Count} live lock(s). These current lock views are more authoritative than older tool results or memory.");
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(FileTraceToolBudgetSeconds);
        foreach (var trace in locks)
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                sb.AppendLine();
                sb.AppendLine($"[truncated] file_trace_show stopped after {FileTraceToolBudgetSeconds}s. Close unneeded locks or request ids for fewer locks.");
                break;
            }
            sb.AppendLine();
            sb.AppendLine(FileTraceLockService.DescribeLock(trace));
        }
        return sb.ToString().TrimEnd();
    }

    private string ExecuteFileTraceClose(Dictionary<string, JsonElement> args)
    {
        var ids = GetStringList(args, "ids");
        var kind = GetString(args, "kind", "read").ToLowerInvariant();
        string? fullPath = null;
        if (args.TryGetValue("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
            fullPath = ResolveWorkspacePath(pathEl.GetString() ?? "", forWrite: false);

        if (ids.Count == 0 && string.IsNullOrWhiteSpace(fullPath))
            return "[error] Provide ids or path so the tool does not close useful locks accidentally.";

        var toClose = _state.TracedFiles
            .Where(t => (kind == "all" || string.Equals(t.Kind, kind, StringComparison.OrdinalIgnoreCase))
                && (ids.Count == 0 || ids.Contains(t.Id, StringComparer.OrdinalIgnoreCase))
                && (fullPath == null || string.Equals(t.Path, fullPath, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var trace in toClose)
            _state.TracedFiles.Remove(trace);

        return toClose.Count == 0
            ? "[file_trace_close] No matching locks were open."
            : $"[file_trace_close] Closed {toClose.Count} lock(s): {string.Join(", ", toClose.Select(t => t.Id))}.";
    }

    private string ExecuteFileWriteLocks(Dictionary<string, JsonElement> args)
    {
        var ids = GetStringList(args, "ids");
        var locks = _state.TracedFiles
            .Where(IsWriteLock)
            .Where(t => ids.Count == 0 || ids.Contains(t.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (locks.Count == 0)
            return "[file_write_locks] No matching write locks.";

        var sb = new StringBuilder();
        sb.AppendLine($"[file_write_locks] Refreshed {locks.Count} write lock(s). Verify changed regions here before continuing or closing locks.");
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(FileTraceToolBudgetSeconds);
        foreach (var trace in locks)
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                sb.AppendLine();
                sb.AppendLine($"[truncated] file_write_locks stopped after {FileTraceToolBudgetSeconds}s. Request ids for fewer locks or close verified locks.");
                break;
            }
            sb.AppendLine();
            sb.AppendLine(FileTraceLockService.DescribeLock(trace));
        }
        return sb.ToString().TrimEnd();
    }

    private string ExecuteFileWriteLockClose(Dictionary<string, JsonElement> args)
    {
        var ids = GetStringList(args, "ids");
        string? fullPath = null;
        if (args.TryGetValue("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
            fullPath = ResolveWorkspacePath(pathEl.GetString() ?? "", forWrite: false);

        if (ids.Count == 0 && string.IsNullOrWhiteSpace(fullPath))
            return "[error] Provide ids or path so the tool does not close useful write locks accidentally.";

        var toClose = _state.TracedFiles
            .Where(IsWriteLock)
            .Where(t => ids.Count == 0 || ids.Contains(t.Id, StringComparer.OrdinalIgnoreCase))
            .Where(t => fullPath == null || string.Equals(t.Path, fullPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var trace in toClose)
            _state.TracedFiles.Remove(trace);

        return toClose.Count == 0
            ? "[file_write_lock_close] No matching write locks were open."
            : $"[file_write_lock_close] Closed {toClose.Count} write lock(s): {string.Join(", ", toClose.Select(t => t.Id))}.";
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
            var existing = _state.TracedFiles.FirstOrDefault(t => IsReadLock(t) && t.Path == fullPath);
            if (existing != null)
            {
                _state.TracedFiles.Remove(existing);
                return $"[file_read] Stopped tracing {filePath}.";
            }
            return $"[file_read] File was not traced: {filePath}.";
        }

        if (!File.Exists(fullPath))
            return $"[error] File not found: {filePath}";
        if (IsVisualImageFile(fullPath))
            return VisualImageReadMessage("file_read", filePath, fullPath);
        if (new FileInfo(fullPath).Length > MaxAgentFileReadBytes)
            return $"[error] File is larger than {MaxAgentFileReadBytes} bytes. Use file_search plus file_trace_open on a narrow range instead of legacy full file_read.";

        var content = File.ReadAllText(fullPath);
        var traced = _state.TracedFiles.FirstOrDefault(t => IsReadLock(t) && t.Path == fullPath);
        if (traced != null)
        {
            traced.Mode = "full";
            traced.StartLine = 1;
            traced.MaxLines = FileTraceLockService.MaxReadLockLines;
            traced.EndLine = Math.Min(FileTraceLockService.SplitLines(content).Length, traced.MaxLines);
            FileTraceLockService.Refresh(traced);
        }
        else
        {
            var readLocks = _state.TracedFiles.Where(IsReadLock).ToList();
            if (readLocks.Count >= FileTraceLockService.MaxReadLocks)
                _state.TracedFiles.Remove(readLocks[0]); // compatibility behavior for legacy file_read
            _state.TracedFiles.Add(FileTraceLockService.CreateReadLock(fullPath, 1, FileTraceLockService.MaxReadLockLines, null, "full", FileTraceLockService.MaxReadLockLines));
        }

        var limit = args.TryGetValue("limit", out var limitEl) && limitEl.ValueKind == JsonValueKind.Number ? Math.Clamp(limitEl.GetInt32(), 500, 50000) : 50000;
        var preview = content.Length > limit ? content[..limit] + "\n...[truncated]" : content;
        return $"[file_read] {filePath} (resolved to {fullPath}, {content.Length} chars). Compatibility read trace is active; prefer file_trace_open/show for live window work.\n```\n{preview}\n```";
    }

    private static bool IsVisualImageFile(string path)
        => VisualImageFileExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static string VisualImageReadMessage(string toolName, string requestedPath, string fullPath)
        => $"[{toolName}] {requestedPath} resolves to image file {fullPath}. Image reading is a visual-inspection task, not a binary/text dump. Do not inspect this file as code or raw bytes; use image pixels from the user's upload when available, show/request a visual preview, or ask the user to upload the image in a multimodal-capable turn.";

    private string ExecuteFileWrite(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return "[error] Missing required 'path' argument.";

        var filePath = pathEl.GetString() ?? "";
        var hasContent = args.TryGetValue("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String;
        var content = hasContent ? contentEl.GetString() ?? "" : string.Empty;
        var append = args.TryGetValue("append", out var a) && a.ValueKind == JsonValueKind.True;
        var hasExpected = args.TryGetValue("expected", out var expectedEl) && expectedEl.ValueKind == JsonValueKind.String;
        var expected = hasExpected ? expectedEl.GetString() ?? "" : string.Empty;
        var hasReplaceWith = args.TryGetValue("replace_with", out var replaceEl) && replaceEl.ValueKind == JsonValueKind.String;
        var replaceWith = hasReplaceWith ? replaceEl.GetString() ?? "" : string.Empty;
        var replaceAll = args.TryGetValue("replace_all", out var allEl) && allEl.ValueKind == JsonValueKind.True;

        if (!hasContent && !(hasExpected && hasReplaceWith))
            return "[error] Provide content for overwrite/append mode, or expected and replace_with for targeted replace mode.";

        var fullPath = ResolveWorkspacePath(filePath, forWrite: true);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        if (File.Exists(fullPath) && new FileInfo(fullPath).Length > MaxAgentFileWriteBytes)
            return $"[error] Existing file is larger than {MaxAgentFileWriteBytes} bytes. Refuse full-file edit through file_write; split it or use a narrower purpose-built tool.";

        var before = File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
        string after;
        string operation;
        int centerLine;
        if (hasExpected && hasReplaceWith)
        {
            if (string.IsNullOrEmpty(expected))
                return "[error] expected cannot be empty in targeted replace mode.";

            var index = before.IndexOf(expected, StringComparison.Ordinal);
            if (index < 0)
                return "[error] expected text was not found. Refresh the relevant read/write lock or search again before editing.";

            operation = replaceAll ? "replace_all" : "replace";
            centerLine = LineNumberAtIndex(before, index);
            after = replaceAll
                ? before.Replace(expected, replaceWith, StringComparison.Ordinal)
                : before.Remove(index, expected.Length).Insert(index, replaceWith);
        }
        else if (append)
        {
            operation = "append";
            centerLine = Math.Max(1, FileTraceLockService.SplitLines(before).Length);
            after = before + content;
        }
        else
        {
            operation = "overwrite";
            after = content;
            centerLine = FindFirstChangedLine(before, after);
        }

        var afterLineCount = Math.Max(1, FileTraceLockService.SplitLines(after).Length);
        if (Encoding.UTF8.GetByteCount(after) > MaxAgentFileWriteBytes)
            return $"[error] Resulting file would exceed {MaxAgentFileWriteBytes} bytes. Refuse write to prevent tool/output hangs.";
        if (!CanCreateOrReuseWriteLock(fullPath, centerLine, afterLineCount, out var lockError))
            return lockError;

        AtomicFile.WriteAllText(fullPath, after);

        foreach (var trace in _state.TracedFiles.Where(t => string.Equals(t.Path, fullPath, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            FileTraceLockService.Refresh(trace);
        }

        var writeLock = OpenOrRefreshWriteLock(fullPath, centerLine, afterLineCount);
        AddFileEditAudit(operation, fullPath, before, after, writeLock.Id);

        var sb = new StringBuilder();
        sb.AppendLine($"[file_write] Succeeded: {operation} {filePath} (resolved to {fullPath}). Full diff was saved to session-state audit; verify using the live write lock below.");
        sb.AppendLine();
        sb.AppendLine(FileTraceLockService.DescribeLock(writeLock));
        return sb.ToString().TrimEnd();
    }

    private List<string> ResolveSearchFiles(List<string> requestedPaths, int maxFiles, DateTimeOffset deadline)
    {
        var files = new List<string>();
        foreach (var requestedPath in requestedPaths)
        {
            if (files.Count >= maxFiles || DateTimeOffset.UtcNow > deadline)
                break;

            var fullPath = ResolveWorkspacePath(requestedPath, forWrite: false);
            if (File.Exists(fullPath))
            {
                files.Add(fullPath);
                continue;
            }

            if (!Directory.Exists(fullPath))
                continue;

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.Hidden
            };

            foreach (var file in Directory.EnumerateFiles(fullPath, "*", options))
            {
                if (files.Count >= maxFiles || DateTimeOffset.UtcNow > deadline)
                    break;
                try
                {
                    if (ShouldSkipSearchPath(file))
                        continue;
                    files.Add(ResolveWorkspacePath(file, forWrite: false));
                }
                catch
                {
                    // Skip files blocked by file-tool policy.
                }
            }
        }
        return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool LooksTextSearchable(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > MaxFileSearchBytes)
                return false;
        }
        catch
        {
            return false;
        }

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext))
            return true;
        var binary = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".bmp", ".mp3", ".wav", ".ogg", ".mp4",
            ".mov", ".avi", ".zip", ".7z", ".rar", ".pdf", ".dll", ".exe", ".pdb", ".db", ".sqlite"
        };
        return !binary.Contains(ext);
    }

    private static bool ShouldSkipSearchPath(string path)
    {
        var segments = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || segment.Equals(".build_verify", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("generated", StringComparison.OrdinalIgnoreCase));
    }

    private static string RenderPlainLines(string[] lines, int start, int end)
    {
        var width = Math.Max(1, end.ToString().Length);
        var sb = new StringBuilder();
        for (var lineNo = start; lineNo <= end && lineNo <= lines.Length; lineNo++)
        {
            sb.Append(lineNo.ToString().PadLeft(width));
            sb.Append(" | ");
            sb.AppendLine(lines[lineNo - 1]);
        }
        return sb.ToString().TrimEnd();
    }

    private List<TraceOpenRequest> ReadTraceOpenRequests(Dictionary<string, JsonElement> args)
    {
        var requests = new List<TraceOpenRequest>();
        if (args.TryGetValue("locks", out var locksEl) && locksEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in locksEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                var path = GetString(item, "path");
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                requests.Add(new TraceOpenRequest(
                    path!,
                    GetNullableInt(item, "start_line"),
                    GetNullableInt(item, "end_line"),
                    GetString(item, "anchor"),
                    GetString(item, "mode"),
                    GetNullableInt(item, "max_lines")));
            }
            return requests;
        }

        var singlePath = GetString(args, "path");
        if (!string.IsNullOrWhiteSpace(singlePath))
        {
            requests.Add(new TraceOpenRequest(
                singlePath!,
                GetNullableInt(args, "start_line"),
                GetNullableInt(args, "end_line"),
                GetString(args, "anchor"),
                GetString(args, "mode"),
                GetNullableInt(args, "max_lines")));
        }
        return requests;
    }

    private bool CanCreateOrReuseWriteLock(string fullPath, int centerLine, int totalLines, out string error)
    {
        error = string.Empty;
        if (FindReusableWriteLock(fullPath, centerLine, totalLines) != null)
            return true;

        var writeLockCount = _state.TracedFiles.Count(IsWriteLock);
        if (writeLockCount < FileTraceLockService.MaxWriteLocks)
            return true;

        error = "[error] write_lock_limit_exceeded: 3 write locks are already open and this write would start a distant verification region. Close a verified write lock with file_write_lock_close before writing here.";
        return false;
    }

    private TracedFileInfo OpenOrRefreshWriteLock(string fullPath, int centerLine, int totalLines)
    {
        var existing = FindReusableWriteLock(fullPath, centerLine, totalLines);
        var (start, end) = FileTraceLockService.WriteWindow(totalLines, centerLine);
        if (existing != null)
        {
            existing.StartLine = start;
            existing.EndLine = end;
            existing.CenterLine = Math.Clamp(centerLine, 1, Math.Max(1, totalLines));
            existing.MaxLines = Math.Max(1, end - start + 1);
            FileTraceLockService.Refresh(existing);
            return existing;
        }

        var created = FileTraceLockService.CreateWriteLock(fullPath, centerLine, totalLines);
        _state.TracedFiles.Add(created);
        return created;
    }

    private TracedFileInfo? FindReusableWriteLock(string fullPath, int centerLine, int totalLines)
    {
        var (newStart, newEnd) = FileTraceLockService.WriteWindow(totalLines, centerLine);
        return _state.TracedFiles
            .Where(IsWriteLock)
            .Where(t => string.Equals(t.Path, fullPath, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(t => Math.Abs(t.CenterLine - centerLine) <= 50 || RangesOverlap(t.StartLine, t.EndLine, newStart, newEnd));
    }

    private static bool RangesOverlap(int aStart, int aEnd, int bStart, int bEnd)
    {
        return aStart <= bEnd && bStart <= aEnd;
    }

    private void AddFileEditAudit(string operation, string fullPath, string before, string after, string writeLockId)
    {
        _state.FileEditAudits.Add(new FileEditAuditInfo
        {
            Operation = operation,
            Path = fullPath,
            BeforeHash = FileTraceLockService.Sha256(before),
            AfterHash = FileTraceLockService.Sha256(after),
            Diff = BuildSimpleDiff(before, after),
            WriteLockId = writeLockId,
            Timestamp = UserTimeZoneService.Now()
        });
        if (_state.FileEditAudits.Count > 100)
            _state.FileEditAudits.RemoveRange(0, _state.FileEditAudits.Count - 100);
    }

    private static string BuildSimpleDiff(string before, string after)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
            return "(no textual changes)";

        var oldLines = FileTraceLockService.SplitLines(before);
        var newLines = FileTraceLockService.SplitLines(after);
        var prefix = 0;
        while (prefix < oldLines.Length && prefix < newLines.Length && string.Equals(oldLines[prefix], newLines[prefix], StringComparison.Ordinal))
            prefix++;

        var suffix = 0;
        while (suffix < oldLines.Length - prefix
            && suffix < newLines.Length - prefix
            && string.Equals(oldLines[oldLines.Length - 1 - suffix], newLines[newLines.Length - 1 - suffix], StringComparison.Ordinal))
        {
            suffix++;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"@@ changed old L{prefix + 1}-L{oldLines.Length - suffix} new L{prefix + 1}-L{newLines.Length - suffix} @@");
        for (var i = prefix; i < oldLines.Length - suffix; i++)
            sb.AppendLine("-" + oldLines[i]);
        for (var i = prefix; i < newLines.Length - suffix; i++)
            sb.AppendLine("+" + newLines[i]);
        var diff = sb.ToString().TrimEnd();
        return diff.Length > 40_000 ? diff[..40_000] + "\n...[diff truncated]" : diff;
    }

    private static int LineNumberAtIndex(string text, int index)
    {
        if (index <= 0)
            return 1;
        var line = 1;
        for (var i = 0; i < Math.Min(index, text.Length); i++)
        {
            if (text[i] == '\n')
                line++;
        }
        return line;
    }

    private static int FindFirstChangedLine(string before, string after)
    {
        var oldLines = FileTraceLockService.SplitLines(before);
        var newLines = FileTraceLockService.SplitLines(after);
        var max = Math.Min(oldLines.Length, newLines.Length);
        for (var i = 0; i < max; i++)
        {
            if (!string.Equals(oldLines[i], newLines[i], StringComparison.Ordinal))
                return i + 1;
        }
        return Math.Max(1, max + 1);
    }

    private static bool IsReadLock(TracedFileInfo trace)
    {
        return !string.Equals(trace.Kind, "write", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWriteLock(TracedFileInfo trace)
    {
        return string.Equals(trace.Kind, "write", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> GetStringList(Dictionary<string, JsonElement> args, string name)
    {
        if (!args.TryGetValue(name, out var el) || el.ValueKind != JsonValueKind.Array)
            return new List<string>();
        return el.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static string GetString(Dictionary<string, JsonElement> args, string name, string fallback = "")
    {
        return args.TryGetValue(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? fallback
            : fallback;
    }

    private static int GetInt(Dictionary<string, JsonElement> args, string name, int fallback)
    {
        return args.TryGetValue(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var value)
            ? value
            : fallback;
    }

    private static string? GetString(JsonElement obj, string name)
    {
        return obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }

    private static int? GetNullableInt(Dictionary<string, JsonElement> args, string name)
    {
        return args.TryGetValue(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var value) ? value : null;
    }

    private static int? GetNullableInt(JsonElement obj, string name)
    {
        return obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var value) ? value : null;
    }

    private sealed record TraceOpenRequest(string Path, int? StartLine, int? EndLine, string? Anchor, string? Mode, int? MaxLines);

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
                Content = RequiredString(args, "content"),
                ResourceFiles = OptionalSkillResourceFiles(args)
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
        var resources = BuildSkillResourceReadContext(skillDir);
        return $"## Skill: {skill.Name}\n\n{tags}{skill.Content}\n\n---\n## Skill Resources\n\n{resources}\n\n---\n## Skill Reports\n\n{reports}\n\n---\n[skill_read] Loaded skill '{skill.Name}' (ID: {skill.Id}) with current validation/import notes and skill-local resource inventory.";
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
                Content = OptionalString(args, "content"),
                ResourceFiles = OptionalSkillResourceFiles(args)
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

    private static List<SkillResourceFile>? OptionalSkillResourceFiles(Dictionary<string, JsonElement> args)
    {
        if (!TryArg(args, "resource_files", out var element) || element.ValueKind != JsonValueKind.Array)
            return null;

        var resources = new List<SkillResourceFile>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            var path = OptionalObjectString(item, "path");
            if (string.IsNullOrWhiteSpace(path))
                continue;
            var content = OptionalObjectString(item, "content");
            resources.Add(new SkillResourceFile { Path = path, Content = content });
        }

        return resources.Count == 0 ? null : resources;
    }

    private static string BuildSkillResourceReadContext(string skillDir)
    {
        if (!Directory.Exists(skillDir))
            return "No skill directory found.";

        var allowedRoots = new[] { "scripts", "templates", "resources", "assets", "examples", "config", "configs" };
        var files = allowedRoots
            .Select(root => Path.Combine(skillDir, root))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(80)
            .ToList();

        if (files.Count == 0)
            return "No skill-local resources found.";

        var sb = new StringBuilder();
        var normalizedSkillDir = EnsureTrailingSeparator(Path.GetFullPath(skillDir));
        foreach (var file in files)
        {
            try
            {
                var info = new FileInfo(file);
                var relative = "./" + Path.GetRelativePath(normalizedSkillDir, file).Replace(Path.DirectorySeparatorChar, '/');
                sb.AppendLine($"- `{relative}` ({info.Length} bytes)");
                if (info.Length <= 20000 && LooksLikeTextResource(file))
                {
                    var content = File.ReadAllText(file);
                    var preview = content.Length > 4000 ? content[..4000] + "\n...[truncated]" : content;
                    sb.AppendLine("```");
                    sb.AppendLine(preview);
                    sb.AppendLine("```");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"- `{file}` (unreadable: {ex.Message})");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static bool LooksLikeTextResource(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".txt" or ".md" or ".markdown" or ".json" or ".yaml" or ".yml" or ".toml" or ".xml" or ".html" or ".css" or ".js" or ".mjs" or ".cjs" or ".ts" or ".tsx" or ".jsx" or ".py" or ".ps1" or ".sh" or ".bat" or ".cmd" or ".csv" or ".ini" or ".sql" or "";
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
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

    private async Task<string> ExecuteImageGenerationAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var request = new ImageGenerationRequest
        {
            Agent = _agentName,
            ImageProfile = OptionalString(args, "profile"),
            BatchId = OptionalString(args, "batch_id"),
            Session = _sessionId,
            Prompt = RequiredString(args, "prompt"),
            Size = OptionalString(args, "size"),
            Quality = OptionalString(args, "quality"),
            OutputFormat = OptionalString(args, "output_format"),
            Count = IntArg(args, "count", 1),
            OutputPath = OptionalString(args, "output_path")
        };

        return await ExecuteImageOperationAsync("image_generation", request, ct);
    }

    private async Task<string> ExecuteImageEditAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var request = new ImageGenerationRequest
        {
            Agent = _agentName,
            ImageProfile = OptionalString(args, "profile"),
            BatchId = OptionalString(args, "batch_id"),
            Session = _sessionId,
            SourceImagePath = RequiredString(args, "source_image_path"),
            Prompt = RequiredString(args, "prompt"),
            Size = OptionalString(args, "size"),
            Quality = OptionalString(args, "quality"),
            OutputFormat = OptionalString(args, "output_format"),
            Count = IntArg(args, "count", 1),
            OutputPath = OptionalString(args, "output_path")
        };

        return await ExecuteImageOperationAsync("image_edit", request, ct);
    }

    private async Task<string> ExecuteImageOperationAsync(string operation, ImageGenerationRequest request, CancellationToken ct)
    {
        if (_synchronousImageGeneration)
        {
            request.JobId = NewImageGenerationToolId(operation == "image_edit" ? "imge" : "img");
            request.BatchId = string.IsNullOrWhiteSpace(request.BatchId) ? NewImageGenerationToolId("batch") : request.BatchId;
            var outcome = await new MultiModalClient(_path).GenerateImageDetailedAsync(_agentName, request, ct);
            var outcomeJson = JsonSerializer.Serialize(outcome, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
            if (!outcome.Success)
            {
                return $"[{operation}] Synchronous {OperationLabel(operation)} failed. Error category: {outcome.ErrorCategory ?? "unknown"}. No automatic retry was performed; decide the next step from the authoritative tool result.\n{outcomeJson}";
            }

            var previews = string.Join(", ", outcome.Results.Select(result => result.RelativePath));
            var usedProfiles = string.Join(", ", outcome.Results
                .Select(result => result.ImageProfileName ?? result.ImageProfileId ?? result.Model)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase));
            var fallback = outcome.FallbackOccurred ? " Provider fallback occurred." : "";
            var profileNote = string.IsNullOrWhiteSpace(usedProfiles) ? "" : $" using {usedProfiles}";
            var verb = operation == "image_edit" ? "edited" : "generated";
            return $"[{operation}] Synchronously {verb} {outcome.Results.Count} image(s){profileNote}.{fallback} Preview them for the user with {{show_file:{previews}}}.\n{outcomeJson}";
        }

        var job = new ImageGenerationJobService(_path).Start(_agentName, request, _sessionId);
        var json = JsonSerializer.Serialize(job, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        return $"[{operation}] Started asynchronous {OperationLabel(operation)} job {job.JobId} in batch {job.BatchId}. Do other work while the host runs it; use image_generation_show_process(job_id='{job.JobId}') for authoritative status, final files, fallback details, and errors. Completion or failure will also be recorded by the host.\n{json}";
    }

    private static string OperationLabel(string operation)
        => operation == "image_edit" ? "image edit" : "image generation";

    private static string NewImageGenerationToolId(string prefix)
        => prefix + "_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "_" + Guid.NewGuid().ToString("N")[..8];

    private string ExecuteImageGenerationShowProcess(Dictionary<string, JsonElement> args)
    {
        var service = new ImageGenerationJobService(_path);
        var jobId = OptionalString(args, "job_id");
        var batchId = OptionalString(args, "batch_id");
        var take = IntArg(args, "take", 20);
        var jobs = service.Query(_agentName, jobId, batchId, take);
        var json = JsonSerializer.Serialize(new
        {
            authoritative = true,
            count = jobs.Count,
            jobs
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });

        var previews = jobs
            .SelectMany(job => job.Results)
            .Select(result => result.RelativePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var previewNote = previews.Count > 0
            ? $"\nPreview successful files for the user with {{show_file:{string.Join(", ", previews)}}}."
            : string.Empty;
        return $"[image_generation_show_process] Authoritative host image-generation state.{previewNote}\n{json}";
    }

    private string ExecuteImageGenerationCancel(Dictionary<string, JsonElement> args)
    {
        var jobId = OptionalString(args, "job_id");
        var batchId = OptionalString(args, "batch_id");
        if (string.IsNullOrWhiteSpace(jobId) && string.IsNullOrWhiteSpace(batchId))
            return "[error] Provide job_id or batch_id to cancel image generation jobs.";

        var jobs = new ImageGenerationJobService(_path).Cancel(_agentName, jobId, batchId);
        var json = JsonSerializer.Serialize(jobs, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        return $"[image_generation_cancel] Cancellation requested for {jobs.Count} queued/running image generation job(s). Already generated files are preserved.\n{json}";
    }

    private string ExecuteImageGenerationRetry(Dictionary<string, JsonElement> args)
    {
        var service = new ImageGenerationJobService(_path);
        var oldJob = service.Get(_agentName, RequiredString(args, "job_id"));
        if (oldJob == null)
            return "[error] Image generation job not found.";

        var request = new ImageGenerationRequest
        {
            Agent = _agentName,
            ImageProfile = OptionalString(args, "profile") ?? oldJob.RequestedProfile,
            BatchId = OptionalString(args, "batch_id") ?? oldJob.BatchId,
            Session = _sessionId ?? oldJob.Session,
            Prompt = OptionalString(args, "prompt") ?? oldJob.Prompt,
            Size = OptionalString(args, "size") ?? oldJob.Size,
            Quality = OptionalString(args, "quality") ?? oldJob.Quality,
            OutputFormat = OptionalString(args, "output_format") ?? oldJob.OutputFormat,
            Count = IntArg(args, "count", oldJob.Count),
            OutputPath = OptionalString(args, "output_path") ?? oldJob.OutputPath,
            SourceImagePath = oldJob.SourceImagePath,
            UseBrowserTemp = oldJob.UseBrowserTemp,
            AllowProfileFallback = oldJob.AllowProfileFallback
        };

        var newJob = service.Start(_agentName, request, request.Session);
        var json = JsonSerializer.Serialize(new { previousJob = oldJob.JobId, newJob }, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        return $"[image_generation_retry] Started retry/replacement image generation job {newJob.JobId} in batch {newJob.BatchId}. Use image_generation_show_process(job_id='{newJob.JobId}') for authoritative status.\n{json}";
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

    private async Task<string> ExecuteTextToSpeechAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
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
            ct.ThrowIfCancellationRequested();
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
            results.Add(await client.TextToSpeechAsync(_agentName, request, ct));
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

    private async Task<string> ExecuteWebSearchAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
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

        var result = await client.SearchAsync(_agentName, request, ct);
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

    private async Task<string> ExecuteBrowserNavigateAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var url = RequiredString(args, "url");
        var headless = BoolArg(args, "headless", true);
        var waitNetworkIdle = IntArg(args, "wait_network_idle", 0);
        var ensureResult = await _browser.EnsureBrowserAsync(headless, ct);
        var navigateResult = await _browser.NavigateAsync(url, waitNetworkIdle, ct);
        return ensureResult.Contains("ignored", StringComparison.OrdinalIgnoreCase)
            ? ensureResult + "\n" + navigateResult
            : navigateResult;
    }

    private async Task<string> ExecuteBrowserClickAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var selector = RequiredString(args, "selector");
        var timeout = IntArg(args, "timeout", 5000);
        return await _browser.ClickAsync(selector, timeout, ct);
    }

    private async Task<string> ExecuteBrowserTypeAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var selector = RequiredString(args, "selector");
        var text = RequiredString(args, "text");
        var submit = BoolArg(args, "submit", false);
        var timeout = IntArg(args, "timeout", 5000);
        return await _browser.TypeAsync(selector, text, submit, timeout, ct);
    }

    private async Task<string> ExecuteBrowserScreenshotAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var outputPath = OptionalString(args, "output_path");
        var fullPage = BoolArg(args, "full_page", false);
        return await _browser.ScreenshotAsync(outputPath, fullPage, ct);
    }

    private async Task<string> ExecuteBrowserGetContentAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var html = BoolArg(args, "html", false);
        var maxLength = IntArg(args, "max_length", 12000);
        return await _browser.GetContentAsync(html, maxLength, ct);
    }

    private async Task<string> ExecuteBrowserEvaluateAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var script = RequiredString(args, "script");
        var timeout = IntArg(args, "timeout", 8000);
        return await _browser.EvaluateAsync(script, timeout, ct);
    }

    private async Task<string> ExecuteBrowserWaitForAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var kind = RequiredString(args, "kind");
        var selector = OptionalString(args, "selector");
        var text = OptionalString(args, "text");
        var state = OptionalString(args, "state");
        var regex = BoolArg(args, "regex", false);
        var timeout = IntArg(args, "timeout", 10000);
        return await _browser.WaitForAsync(kind, selector, text, state, regex, timeout, ct);
    }

    private async Task<string> ExecuteBrowserQueryAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var selector = OptionalString(args, "selector");
        var text = OptionalString(args, "text");
        var limit = IntArg(args, "limit", 30);
        return await _browser.QueryAsync(selector, text, limit, ct);
    }

    private async Task<string> ExecuteBrowserScrollAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var selector = OptionalString(args, "selector");
        var direction = OptionalString(args, "direction");
        var pixels = IntArg(args, "pixels", 900);
        var steps = IntArg(args, "steps", 1);
        var untilSelector = OptionalString(args, "until_selector");
        var untilText = OptionalString(args, "until_text");
        var delay = IntArg(args, "delay", 300);
        return await _browser.ScrollAsync(selector, direction, pixels, steps, untilSelector, untilText, delay, ct);
    }

    private async Task<string> ExecuteBrowserSourceAnalyzeAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var includeInline = BoolArg(args, "include_inline", false);
        var limit = IntArg(args, "limit", 80);
        return await _browser.SourceAnalyzeAsync(includeInline, limit, ct);
    }

    private async Task<string> ExecuteBrowserVerifyAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var kind = RequiredString(args, "kind");
        var selector = OptionalString(args, "selector");
        var text = OptionalString(args, "text");
        var state = OptionalString(args, "state");
        var regex = BoolArg(args, "regex", false);
        var negate = BoolArg(args, "negate", false);
        var timeout = IntArg(args, "timeout", 10000);
        return await _browser.VerifyAsync(kind, selector, text, state, regex, negate, timeout, ct);
    }

    private async Task<string> ExecuteBrowserCrawlAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var startUrl = OptionalString(args, "start_url");
        var maxPages = IntArg(args, "max_pages", 5);
        var maxDepth = IntArg(args, "max_depth", 1);
        var sameOrigin = BoolArg(args, "same_origin", true);
        var maxChars = IntArg(args, "max_chars", 2000);
        var restore = BoolArg(args, "restore", true);
        return await _browser.CrawlAsync(startUrl, maxPages, maxDepth, sameOrigin, maxChars, restore, ct);
    }

    private async Task<string> ExecuteBrowserTraceAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var action = OptionalString(args, "action") ?? "read";
        var network = BoolArg(args, "network", true);
        var console = BoolArg(args, "console", true);
        var maxEvents = IntArg(args, "max_events", 200);
        var take = IntArg(args, "take", 80);
        return await _browser.TraceAsync(action, network, console, maxEvents, take, ct);
    }

    private async Task<string> ExecuteBrowserInjectInitScriptAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var script = RequiredString(args, "script");
        var purpose = RequiredString(args, "purpose");
        return await _browser.InjectInitScriptAsync(script, purpose, ct);
    }

    private async Task<string> ExecuteBrowserSaveCookieAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var site = OptionalString(args, "site");
        return await _browser.SaveCookiesAsync(_path.GetBrowserCookiesJsonPath(_agentName), site, ct);
    }

    private async Task<string> ExecuteBrowserListCookieBySiteAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var site = OptionalString(args, "site");
        return await _browser.ListCookiesBySiteAsync(_path.GetBrowserCookiesJsonPath(_agentName), site, ct);
    }

    private async Task<string> ExecuteBrowserApplyCookieAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        var site = OptionalString(args, "site");
        return await _browser.ApplyCookiesAsync(_path.GetBrowserCookiesJsonPath(_agentName), site, ct);
    }

    private async Task<string> ExecuteBrowserCloseAsync(Dictionary<string, JsonElement> args, CancellationToken ct)
    {
        return await _browser.CloseAsync(ct: ct);
    }

    #endregion
}
