using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Matdance.Cli.Core;
using Matdance.Cli.Models;
using Matdance.Cli.Services;
using Matdance.Plugins.Browser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Matdance.Cli.Web;

public sealed class WebServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] IconExtensions = {".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".bmp", ".ico"};
    private static readonly string[] SoundCueExtensions = {".mp3", ".wav", ".ogg", ".oga", ".opus", ".m4a", ".aac", ".flac", ".webm"};
    private static readonly string[] ChatImageAttachmentExtensions = {".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp"};
    private static readonly string[] ChatDocumentAttachmentExtensions = {".txt", ".md", ".markdown", ".log", ".ini", ".html", ".htm", ".css", ".js", ".mjs", ".cjs", ".ts", ".tsx", ".jsx", ".py", ".ps1", ".sh", ".bat", ".cmd", ".sql", ".pdf", ".doc", ".docx", ".docm", ".odt", ".rtf", ".csv", ".tsv", ".json", ".xml", ".yaml", ".yml", ".toml", ".xls", ".xlsx", ".xlsm", ".xlsb", ".ods", ".ppt", ".pptx", ".pptm", ".odp", ".pages", ".numbers"};
    private static readonly string[] ChatArchiveAttachmentExtensions = {".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".bz2", ".xz"};
    private static readonly Regex SoundCueMarkerRegex = new(@"\{play[_-]?audio\s*[:：]\s*([^}]+)\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private const long MaxSoundCueUploadBytes = 8L * 1024 * 1024;
    private const long MaxSkillImportUploadBytes = 64L * 1024 * 1024;
    private const long MaxSkillImportExtractBytes = 64L * 1024 * 1024;
    private const int MaxSkillImportExtractFiles = 250;
    private const int MaxChatAttachments = 10;
    private const long MaxChatAttachmentBytes = 128L * 1024 * 1024;
    private const long MaxChatAttachmentTotalBytes = 512L * 1024 * 1024;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sessionLocks = new();
    private readonly AgentActivityService _activity = new();
    private readonly BackgroundTaskQueue _backgroundQueue = new();
    private readonly BackgroundWorkCoordinator _backgroundWork;
    private readonly PathService _path;
    private readonly string _host;
    private readonly int _port;
    private readonly BrowserService _browser = BrowserService.Instance;

    public WebServer(PathService path, string host, int port)
    {
        _path = path;
        _host = string.IsNullOrWhiteSpace(host) ? "localhost" : host;
        _port = port <= 0 ? 8765 : port;
        _backgroundWork = new BackgroundWorkCoordinator(_path, _backgroundQueue, _activity);
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        EnsureSafeWebBinding();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>(),
            ContentRootPath = Directory.GetCurrentDirectory()
        });

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = MaxChatAttachmentTotalBytes + 1024L * 1024;
            options.ValueLengthLimit = 4 * 1024 * 1024;
            options.MultipartHeadersLengthLimit = 64 * 1024;
        });
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = MaxChatAttachmentTotalBytes + 1024L * 1024;
        });
        builder.WebHost.UseUrls($"http://{_host}:{_port}");
        var app = builder.Build();
        var auth = WebAuthService.LoadOrCreate(_host);
        LogWebAuth(auth);

        app.UseWebSockets();
        app.Use(async (context, next) =>
        {
            if (!auth.Enabled || IsAuthExemptPath(context.Request.Path) || auth.IsAuthorized(context))
            {
                await next();
                return;
            }

            if (HttpMethods.IsGet(context.Request.Method) && context.Request.Path == "/")
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(LoginPageHtml, context.RequestAborted);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "authentication_required" }, JsonOptions, context.RequestAborted);
        });

        app.MapGet("/", () => Results.Content(WebPage.Html, "text/html; charset=utf-8"));
        app.MapGet("/assets/brand/{fileName}", GetBrandAssetHttp);
        app.MapGet("/favicon.png", () => GetBrandAssetHttp("matdance-icon.png"));
        app.MapGet("/favicon.ico", () => GetBrandAssetHttp("matdance-icon.png"));
        app.MapGet("/api/auth/status", () => Results.Json(new
        {
            enabled = auth.Enabled,
            remoteBinding = auth.RemoteBinding,
            source = auth.Source,
            singleToken = true
        }, JsonOptions));
        app.MapPost("/api/auth/login", async (HttpContext context) =>
        {
            if (!auth.Enabled)
                return Results.Json(new { ok = true, enabled = false }, JsonOptions);

            var request = await context.Request.ReadFromJsonAsync<WebAuthLoginRequest>(JsonOptions, context.RequestAborted);
            if (!auth.Validate(request?.Token))
                return Results.Unauthorized();

            WebAuthService.SetAuthCookie(context, request!.Token);
            return Results.Json(new { ok = true, enabled = true }, JsonOptions);
        });
        app.MapPost("/api/auth/logout", (HttpContext context) =>
        {
            WebAuthService.ClearAuthCookie(context);
            return Results.Json(new { ok = true }, JsonOptions);
        });
        app.MapGet("/api/agents", () => Results.Json(new { agents = ListAgents() }, JsonOptions));
        app.MapGet("/api/security-settings", () => Results.Json(new SecuritySettingsService().Load(), JsonOptions));
        app.MapPost("/api/security-settings", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<SecuritySettings>(JsonOptions, context.RequestAborted);
            return Results.Json(new SecuritySettingsService().Save(request ?? new SecuritySettings()), JsonOptions);
        });
        app.MapGet("/api/agent-config", (string agent) => Results.Json(LoadAgentConfigDto(agent), JsonOptions));
        app.MapGet("/api/multimodal-config", (string agent) => GetMultiModalConfigHttp(agent));
        app.MapPost("/api/multimodal-config", (Func<HttpContext, Task<IResult>>)SaveMultiModalConfigHttp);
        app.MapPost("/api/image-generation", (Func<HttpContext, Task<IResult>>)GenerateImageHttp);
        app.MapPost("/api/audio/speech", (Func<HttpContext, Task<IResult>>)TextToSpeechHttp);
        app.MapPost("/api/audio/transcriptions", (Func<HttpContext, Task<IResult>>)TranscribeAudioHttp);
        app.MapGet("/api/sound-cue-settings", (string? agent) =>
        {
            if (!string.IsNullOrWhiteSpace(agent))
                EnsureAgentExists(NormalizeAgentName(agent));
            return Results.Json(new SoundCueSettingsService().Load(), JsonOptions);
        });
        app.MapPost("/api/sound-cue-settings", (Func<HttpContext, Task<IResult>>)SaveSoundCueSettingsHttp);
        app.MapPost("/api/sound-cue", (Func<HttpContext, Task<IResult>>)UploadSoundCueAsync);
        app.MapGet("/api/runtime-status", () => Results.Json(new
        {
            backend = true,
            browser = _browser.IsRunning,
            browserDependencies = _browser.IsRunning || new DependencyInstallerService().HasPlaywrightChromium(),
            os = MatdanceRuntime.OsName,
            shell = MatdanceRuntime.ShellInvocation,
            timeZone = UserTimeZoneService.GetSnapshot(),
            uptime = UserTimeZoneService.Now()
        }, JsonOptions));
        app.MapGet("/api/runtime-supervisor", async () =>
            Results.Json(await new RuntimeSupervisorService(_path).GetSupervisorStatusAsync(), JsonOptions));
        app.MapPost("/api/runtime-supervisor", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<RuntimeSupervisorRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return Results.BadRequest("Missing supervisor request.");
            try
            {
                await new RuntimeSupervisorService(_path).ConfigureSystemTasksAsync(
                    request.Mode,
                    string.IsNullOrWhiteSpace(request.Host) ? _host : request.Host,
                    request.Port > 0 ? request.Port : _port,
                    context.RequestAborted);
                return Results.Json(await new RuntimeSupervisorService(_path).GetSupervisorStatusAsync(context.RequestAborted), JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapGet("/api/runtime-events", (string agent, int? take) =>
        {
            if (string.IsNullOrWhiteSpace(agent)) return Results.BadRequest("Missing agent.");
            return Results.Json(new BackgroundEventService(_path).GetDashboard(agent, take ?? 80), JsonOptions);
        });
        app.MapGet("/api/sound-cue/default/{fileName}", GetDefaultSoundCueHttp);

        // File serving endpoint for preview_file tool
        app.MapGet("/api/file", (string path, string? agent) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return Results.BadRequest("Missing path parameter.");

            var currentDir = Path.GetFullPath(Directory.GetCurrentDirectory());
            var browserTempDir = Path.GetFullPath(Path.Combine(currentDir, "browser_temp"));
            var foundPath = ResolvePreviewFilePath(path, agent, currentDir, browserTempDir);

            if (foundPath == null)
                return Results.NotFound($"File not found: {path}");

            var allowedRoots = GetPreviewAllowedRoots(agent, browserTempDir);
            bool isAllowed = IsPathUnderAnyRoot(foundPath, allowedRoots);

            if (!isAllowed || IsSensitivePreviewPath(foundPath))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var mimeType = GetMimeType(foundPath);
            return Results.File(foundPath, mimeType);
        });

        // Browser screencast WebSocket
        app.Map("/ws/browser", async (HttpContext context) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            Action<byte[]>? onFrame = null;
            onFrame = async (bytes) =>
            {
                try
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, context.RequestAborted);
                    }
                }
                catch { }
            };
            _browser.OnScreencastFrame += onFrame;
            try
            {
                try
                {
                    await _browser.StartScreencastAsync(quality: 75, maxWidth: 1280, maxHeight: 720);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ws/browser] Screencast start failed: {ex.Message}");
                    try { await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, "Browser unavailable", CancellationToken.None); } catch { }
                    return;
                }
                // Keep connection alive
                var buffer = new byte[1024];
                while (ws.State == WebSocketState.Open)
                {
                    // If browser was closed and reopened, restart screencast
                    if (!_browser.IsScreencastRunning && _browser.IsRunning)
                    {
                        try
                        {
                            await _browser.StartScreencastAsync(quality: 75, maxWidth: 1280, maxHeight: 720);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ws/browser] Screencast restart failed: {ex.Message}");
                        }
                    }
                    
                    var receiveTask = ws.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                    var timeoutTask = Task.Delay(500, context.RequestAborted);
                    var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
                    
                    if (completedTask == receiveTask)
                    {
                        var result = await receiveTask;
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                    }
                    // If timeout, loop continues and checks screencast status
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            finally
            {
                _browser.OnScreencastFrame -= onFrame;
                try { await _browser.StopScreencastAsync(); } catch { }
                if (ws.State == WebSocketState.Open)
                {
                    try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None); } catch { }
                }
            }
        });
        app.MapPost("/api/agents", (Func<HttpContext, Task<IResult>>)CreateAgentAsync);
        app.MapDelete("/api/agents", DeleteAgentHttp);
        app.MapGet("/api/agent-icon", GetAgentIconHttp);
        app.MapPost("/api/agent-icon", (Func<HttpContext, Task<IResult>>)UploadAgentIconAsync);
        app.MapGet("/api/sessions", (string agent) =>
        {
            try { return Results.Json(new { sessions = ListSessions(agent) }, JsonOptions); }
            catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }
        });
        app.MapGet("/api/session", (string agent, string session) =>
        {
            try { return Results.Json(LoadSessionDto(agent, session), JsonOptions); }
            catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }
        });
        var scheduledTasks = new ScheduledTaskService(_path);
        scheduledTasks.EnsureAllSystemTasks();
        app.MapPost("/api/user-time-zone", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<UserTimeZoneRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return (IResult)Results.BadRequest("Missing time zone request.");
            try
            {
                UserTimeZoneService.SetDefaultTimeZone(request.TimeZone);
                scheduledTasks.EnsureAllSystemTasks();
                return (IResult)Results.Json(UserTimeZoneService.GetSnapshot(), JsonOptions);
            }
            catch (InvalidOperationException ex)
            {
                return (IResult)Results.BadRequest(ex.Message);
            }
        });
        var bookmarks = new BookmarkService(_path);
        var memoryOrg = new MemoryOrganizationService(_path, scheduledTasks, bookmarks);
        var skillMaintenance = new SkillMaintenanceService(_path);
        var scheduledRunner = new ScheduledTaskRunner(_path, scheduledTasks, memoryOrg, skillMaintenance, _activity, AcquireSessionTurnAsync, _backgroundWork);
        var scheduledWorker = new ScheduledTaskWorker(scheduledTasks, scheduledRunner, _backgroundQueue, _activity, _path);
        var idleSkillValidationWorker = new SkillIdleValidationWorker(_path, skillMaintenance, scheduledTasks, _activity, _backgroundQueue);
        using var scheduledCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var scheduledWorkerTask = scheduledWorker.RunAsync(scheduledCts.Token);
        var idleSkillValidationTask = idleSkillValidationWorker.RunAsync(scheduledCts.Token);
        app.MapGet("/api/scheduled-tasks", (string agent, int? page, int? pageSize, string? status) => Results.Json(scheduledTasks.List(agent, page ?? 1, pageSize ?? 10, status), JsonOptions));
        app.MapGet("/api/scheduled-task", (string agent, string taskId, int? take) => Results.Json(new { task = scheduledTasks.Read(agent, taskId), runs = scheduledTasks.GetRuns(agent, taskId, take ?? 20) }, JsonOptions));
        app.MapPost("/api/scheduled-tasks", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<ScheduledTaskCreateRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return (IResult)Results.BadRequest("Missing scheduled task request.");
            try { return (IResult)Results.Json(scheduledTasks.Create(request), JsonOptions); }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapPut("/api/scheduled-tasks", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<ScheduledTaskEditRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return (IResult)Results.BadRequest("Missing scheduled task request.");
            try { return (IResult)Results.Json(scheduledTasks.Edit(request), JsonOptions); }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapPost("/api/scheduled-tasks/do", async (HttpContext context, bool? deliver) =>
        {
            var request = await context.Request.ReadFromJsonAsync<ScheduledTaskActionRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return (IResult)Results.BadRequest("Missing scheduled task action.");
            try
            {
                var agent = NormalizeAgentName(request.Agent);
                var taskInfo = scheduledTasks.Read(agent, request.TaskId);
                var resource = BackgroundWorkCoordinator.GetScheduledTaskResourceKey(taskInfo);
                using var foregroundLease = _backgroundWork.BeginForegroundWork(agent);
                IDisposable? resourceLease = null;
                if (resource != null)
                {
                    resourceLease = await _backgroundWork.TryAcquireResourceAsync(agent, resource, BackgroundWorkCoordinator.ResourceRetryTimeout, context.RequestAborted);
                    if (resourceLease == null)
                        return (IResult)Results.Json(new { status = "busy", message = $"Resource '{resource}' is locked. Please retry later." }, JsonOptions);
                }

                using (resourceLease)
                using (var linked = _backgroundWork.CreateAgentLinkedCancellation(agent, context.RequestAborted))
                {
                    var task = scheduledTasks.TryStartRun(agent, request.TaskId, manual: true);
                    if (task == null) return (IResult)Results.BadRequest("Scheduled task is not available or already running.");
                    var run = await scheduledRunner.ExecuteAsync(task, "manual", DateTimeOffset.UtcNow, deliver ?? false, linked.Token);
                    scheduledTasks.FinishRun(run);
                    return (IResult)Results.Json(run, JsonOptions);
                }
            }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapPost("/api/scheduled-tasks/retry", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<ScheduledTaskActionRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return (IResult)Results.BadRequest("Missing scheduled task action.");
            try { return (IResult)Results.Json(scheduledTasks.RetryNow(request.Agent, request.TaskId), JsonOptions); }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapPost("/api/scheduled-tasks/repair-retry", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<ScheduledTaskActionRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return (IResult)Results.BadRequest("Missing scheduled task action.");
            try { return (IResult)Results.Json(scheduledTasks.RepairAndRetry(request.Agent, request.TaskId, request.Reason), JsonOptions); }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapDelete("/api/scheduled-tasks", (string agent, string taskId) =>
        {
            try { return (IResult)Results.Json(scheduledTasks.Delete(agent, taskId), JsonOptions); }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });

        // ===== Skills API =====
        app.MapGet("/api/skills", (string agent) =>
        {
            try
            {
                var skillService = new SkillService(_path);
                return (IResult)Results.Json(skillService.List(agent), JsonOptions);
            }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapGet("/api/skill", (string agent, string skillId) =>
        {
            try
            {
                var skillService = new SkillService(_path);
                var skill = skillService.Read(agent, skillId);
                var skillDir = _path.GetSkillPath(agent, skillId);
                var reportPath = SkillValidationState.GetReportPath(skillDir);
                var importReportPath = Path.Combine(skillDir, "import-report.md");
                return (IResult)Results.Json(new
                {
                    skill.Id,
                    skill.Name,
                    skill.Description,
                    skill.Tags,
                    skill.Content,
                    skill.CreatedAt,
                    skill.UpdatedAt,
                    validationReport = File.Exists(reportPath) ? File.ReadAllText(reportPath) : null,
                    importReport = File.Exists(importReportPath) ? File.ReadAllText(importReportPath) : null
                }, JsonOptions);
            }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapGet("/api/skills/export", (string agent, string skillId) => ExportSkillHttp(agent, skillId));
        app.MapPost("/api/skills", async (HttpContext context) =>
        {
            try
            {
                var request = await context.Request.ReadFromJsonAsync<SkillCreateRequest>(JsonOptions, context.RequestAborted);
                if (request == null) return (IResult)Results.BadRequest("Missing skill request.");
                var skillService = new SkillService(_path);
                return (IResult)Results.Json(skillService.Create(context.Request.Query["agent"].ToString(), request), JsonOptions);
            }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapPut("/api/skills", async (HttpContext context) =>
        {
            try
            {
                var request = await context.Request.ReadFromJsonAsync<SkillEditRequest>(JsonOptions, context.RequestAborted);
                if (request == null) return (IResult)Results.BadRequest("Missing skill request.");
                var skillService = new SkillService(_path);
                return (IResult)Results.Json(skillService.Edit(context.Request.Query["agent"].ToString(), request), JsonOptions);
            }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapDelete("/api/skills", (string agent, string skillId) =>
        {
            try
            {
                var skillService = new SkillService(_path);
                skillService.Delete(agent, skillId);
                return (IResult)Results.Json(new { deleted = true }, JsonOptions);
            }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(ex.Message); }
        });
        app.MapPost("/api/skills/organize", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<SkillJobRequest>(JsonOptions, context.RequestAborted);
            if (request == null || string.IsNullOrWhiteSpace(request.Agent))
                return Results.BadRequest("Missing agent.");
            try
            {
                var job = await TryStartManualAgentJobAsync(
                    request.Agent,
                    BackgroundWorkCoordinator.SkillsResource,
                    (agent, token) => skillMaintenance.StartOrganization(agent, token),
                    jobId => skillMaintenance.GetJob(jobId)?.Status == "running",
                    context.RequestAborted,
                    scheduledCts.Token);
                if (!job.Started)
                    return Results.Json(new { status = "busy", message = job.Message }, JsonOptions);

                var jobId = job.JobId!;
                return Results.Json(new { status = "started", jobId }, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapPost("/api/skills/validate", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<SkillJobRequest>(JsonOptions, context.RequestAborted);
            if (request == null || string.IsNullOrWhiteSpace(request.Agent) || string.IsNullOrWhiteSpace(request.SkillId))
                return Results.BadRequest("Missing agent or skill id.");
            try
            {
                var skillId = request.SkillId;
                var job = await TryStartManualAgentJobAsync(
                    request.Agent,
                    BackgroundWorkCoordinator.SkillsResource,
                    (agent, token) => skillMaintenance.StartValidation(agent, skillId, token),
                    jobId => skillMaintenance.GetJob(jobId)?.Status == "running",
                    context.RequestAborted,
                    scheduledCts.Token);
                if (!job.Started)
                    return Results.Json(new { status = "busy", message = job.Message }, JsonOptions);

                var jobId = job.JobId!;
                return Results.Json(new { status = "started", jobId }, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapPost("/api/skills/learn-validate", async (HttpContext context) =>
        {
            SkillLearnRequest? request = null;
            try
            {
                request = await ReadSkillLearnRequestAsync(context);
                if (request == null || string.IsNullOrWhiteSpace(request.Agent))
                    return Results.BadRequest("Missing agent.");

                var job = await TryStartManualAgentJobAsync(
                    request.Agent,
                    BackgroundWorkCoordinator.SkillsResource,
                    (agent, token) =>
                    {
                        request.Agent = agent;
                        return skillMaintenance.StartLearningValidation(agent, request, token);
                    },
                    jobId => skillMaintenance.GetJob(jobId)?.Status == "running",
                    context.RequestAborted,
                    scheduledCts.Token);
                if (!job.Started)
                {
                    CleanupSkillLearnRequest(request);
                    return Results.Json(new { status = "busy", message = job.Message }, JsonOptions);
                }

                var jobId = job.JobId!;
                return Results.Json(new { status = "started", jobId }, JsonOptions);
            }
            catch (Exception ex)
            {
                if (request != null)
                    CleanupSkillLearnRequest(request);
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapGet("/api/skills/job/status", (string jobId) =>
        {
            var job = skillMaintenance.GetJob(jobId);
            if (job == null) return Results.NotFound("Job not found.");
            return Results.Json(new
            {
                job.JobId,
                job.Agent,
                job.Kind,
                job.SkillId,
                job.Status,
                job.Progress,
                job.Stage,
                job.Error,
                job.ResultSummary,
                job.Report,
                job.StartedAt,
                job.FinishedAt
            }, JsonOptions);
        });

        app.MapGet("/api/memory", (string agent) =>
        {
            try
            {
                var userPath = _path.GetUserPath(agent);
                var identityPath = _path.GetIdentityPath(agent);
                var hotMemoryPath = _path.GetHotMemoryPath(agent);
                var coreMemoryPath = _path.GetCoreMemoryPath(agent);
                var longTermDir = _path.GetLongTermMemoryPath(agent);
                var longTermItems = new List<object>();
                if (Directory.Exists(longTermDir))
                {
                    foreach (var file in Directory.GetFiles(longTermDir, "*.md").OrderByDescending(f => f).Take(20))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var content = File.ReadAllText(file);
                        longTermItems.Add(new
                        {
                            id = fileName,
                            timestamp = GetFileLastWriteUserTime(file),
                            content = content.Length > 200 ? content.Substring(0, 200) + "..." : content
                        });
                    }
                }
                return Results.Json(new
                {
                    userMd = File.Exists(userPath) ? File.ReadAllText(userPath) : string.Empty,
                    identityMd = File.Exists(identityPath) ? File.ReadAllText(identityPath) : string.Empty,
                    hotMemory = File.Exists(hotMemoryPath) ? File.ReadAllText(hotMemoryPath) : string.Empty,
                    coreMemory = File.Exists(coreMemoryPath) ? File.ReadAllText(coreMemoryPath) : string.Empty,
                    longTermItems
                }, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapGet("/api/memory/vector", (string agent, int? maxNodes) =>
        {
            try
            {
                var safeAgent = NormalizeAgentName(agent);
                EnsureAgentExists(safeAgent);
                var atlas = new VectorMemoryService(_path).GetAtlas(safeAgent, maxNodes ?? 240);
                return Results.Json(atlas, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapGet("/api/memory/vector/search", (string agent, string? query, int? take) =>
        {
            try
            {
                var safeAgent = NormalizeAgentName(agent);
                EnsureAgentExists(safeAgent);
                var result = new VectorMemoryService(_path).Search(safeAgent, query ?? string.Empty, Math.Clamp(take ?? 6, 1, 20));
                return Results.Json(new
                {
                    algorithm = result.Algorithm,
                    entryCount = result.EntryCount,
                    candidateCount = result.CandidateCount,
                    visitedNodes = result.VisitedNodes,
                    items = result.Items.Select(item => new
                    {
                        id = item.Entry.Id,
                        sourcePath = item.Entry.SourcePath,
                        kind = item.Entry.Kind,
                        title = item.Entry.Title,
                        chunkIndex = item.Entry.ChunkIndex,
                        startLine = item.Entry.StartLine,
                        endLine = item.Entry.EndLine,
                        textPreview = item.Entry.Text.Length > 360 ? item.Entry.Text[..360] + "..." : item.Entry.Text,
                        score = Math.Round(item.Score, 4),
                        cosine = Math.Round(item.Cosine, 4),
                        lexical = Math.Round(item.Lexical, 4),
                        hammingSimilarity = Math.Round(item.HammingSimilarity, 4),
                        hammingDistance = item.HammingDistance,
                        terms = item.Entry.Terms.Take(8).Select(term => term.Term).ToList()
                    }).ToList()
                }, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapPut("/api/memory", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<MemorySaveRequest>(JsonOptions, context.RequestAborted);
            if (request == null || string.IsNullOrWhiteSpace(request.Agent))
                return Results.BadRequest("Missing agent.");
            try
            {
                var agent = NormalizeAgentName(request.Agent);
                if (!string.IsNullOrWhiteSpace(request.UserMd))
                {
                    var userPath = _path.GetUserPath(agent);
                    Directory.CreateDirectory(Path.GetDirectoryName(userPath)!);
                    await AtomicFile.WriteAllTextAsync(userPath, request.UserMd, context.RequestAborted);
                }
                if (!string.IsNullOrWhiteSpace(request.IdentityMd))
                {
                    var identityPath = _path.GetIdentityPath(agent);
                    Directory.CreateDirectory(Path.GetDirectoryName(identityPath)!);
                    await AtomicFile.WriteAllTextAsync(identityPath, request.IdentityMd, context.RequestAborted);
                }
                if (!string.IsNullOrWhiteSpace(request.HotMemory))
                {
                    var hotMemoryPath = _path.GetHotMemoryPath(agent);
                    Directory.CreateDirectory(Path.GetDirectoryName(hotMemoryPath)!);
                    await AtomicFile.WriteAllTextAsync(hotMemoryPath, request.HotMemory, context.RequestAborted);
                }
                if (request.CoreMemory != null)
                {
                    var coreMemoryPath = _path.GetCoreMemoryPath(agent);
                    Directory.CreateDirectory(Path.GetDirectoryName(coreMemoryPath)!);
                    await AtomicFile.WriteAllTextAsync(coreMemoryPath, request.CoreMemory, context.RequestAborted);
                }
                new VectorMemoryService(_path).Refresh(agent);
                return Results.Ok(new { agent });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapGet("/api/memory/long-term", (string agent, int? page, int? pageSize, string? startDate, string? endDate) =>
        {
            try
            {
                var safeAgent = NormalizeAgentName(agent);
                var longTermDir = _path.GetLongTermMemoryPath(safeAgent);
                var allItems = new List<object>();
                DateTime? start = null, end = null;
                if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var sd)) start = sd.Date;
                if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var ed)) end = ed.Date.AddDays(1).AddTicks(-1);
                if (Directory.Exists(longTermDir))
                {
                    foreach (var file in Directory.GetFiles(longTermDir, "*.md").OrderByDescending(f => f))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (!DateTime.TryParse(fileName, out var fileDate)) continue;
                        if (start.HasValue && fileDate < start.Value) continue;
                        if (end.HasValue && fileDate > end.Value) continue;
                        var content = File.ReadAllText(file);
                        var timestamp = GetFileLastWriteUserTime(file);
                        allItems.Add(new
                        {
                            id = fileName,
                            timestamp,
                            content = content.Length > 300 ? content.Substring(0, 300) + "..." : content,
                            fullContent = content
                        });
                    }
                }
                var currentPage = Math.Max(1, page ?? 1);
                var size = Math.Clamp(pageSize ?? 10, 1, 100);
                var total = allItems.Count;
                var items = allItems.Skip((currentPage - 1) * size).Take(size).ToList();
                return Results.Json(new
                {
                    items,
                    page = currentPage,
                    pageSize = size,
                    total
                }, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapDelete("/api/memory/long-term", (string agent, string id) =>
        {
            try
            {
                var safeAgent = NormalizeAgentName(agent);
                var safeId = PathSafety.NormalizeFileNameSegment(id, "Memory id");
                var longTermDir = _path.GetLongTermMemoryPath(safeAgent);
                if (!Directory.Exists(longTermDir)) return Results.BadRequest("Long term memory directory not found.");
                var filePath = Path.Combine(longTermDir, safeId + ".md");
                if (!File.Exists(filePath)) return Results.BadRequest("Memory file not found.");
                File.Delete(filePath);
                new VectorMemoryService(_path).Refresh(safeAgent);
                return Results.Ok(new { deleted = true });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapPost("/api/memory/organize", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<OrganizeRequest>(JsonOptions, context.RequestAborted);
            if (request == null || string.IsNullOrWhiteSpace(request.Agent))
                return Results.BadRequest("Missing agent.");
            try
            {
                var safeAgent = NormalizeAgentName(request.Agent);
                var limits = new MemoryLimits
                {
                    HotMemoryLimit = request.HotMemoryLimit > 0 ? request.HotMemoryLimit : 10000,
                    CoreMemoryLimit = request.CoreMemoryLimit > 0 ? request.CoreMemoryLimit : 15000,
                    UserMdLimit = request.UserMdLimit > 0 ? request.UserMdLimit : 5000,
                    IdentityMdLimit = request.IdentityMdLimit > 0 ? request.IdentityMdLimit : 2000
                };
                var job = await TryStartManualAgentJobAsync(
                    safeAgent,
                    BackgroundWorkCoordinator.MemoryResource,
                    (agent, token) => memoryOrg.StartOrganization(agent, limits, request.ForceFullRebuild, token),
                    jobId => memoryOrg.GetJob(jobId)?.Status == "running",
                    context.RequestAborted,
                    scheduledCts.Token);
                if (!job.Started)
                    return Results.Json(new { status = "busy", message = job.Message }, JsonOptions);

                var jobId = job.JobId!;
                return Results.Json(new { status = "started", jobId }, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapGet("/api/memory/organize/status", (string jobId) =>
        {
            var job = memoryOrg.GetJob(jobId);
            if (job == null) return Results.NotFound("Job not found.");
            return Results.Json(new
            {
                job.JobId,
                job.Agent,
                job.Status,
                job.Progress,
                job.Stage,
                job.Error,
                job.ResultSummary,
                job.SnapshotPath,
                job.StartedAt,
                job.FinishedAt
            }, JsonOptions);
        });
        app.MapGet("/api/memory/snapshots", (string agent) =>
        {
            if (string.IsNullOrWhiteSpace(agent)) return Results.BadRequest("Missing agent.");
            return Results.Json(new { snapshots = memoryOrg.ListSnapshots(agent) }, JsonOptions);
        });
        app.MapPost("/api/memory/restore-snapshot", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<MemorySnapshotRestoreRequest>(JsonOptions, context.RequestAborted);
            if (request == null || string.IsNullOrWhiteSpace(request.Agent) || string.IsNullOrWhiteSpace(request.SnapshotId))
                return Results.BadRequest("Missing agent or snapshot id.");
            try
            {
                memoryOrg.RestoreSnapshot(request.Agent, request.SnapshotId);
                return Results.Json(new { restored = true }, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapPost("/api/agent-config", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<AgentConfigRequest>(JsonOptions, context.RequestAborted);
            if (request == null || string.IsNullOrWhiteSpace(request.Agent))
            {
                return Results.BadRequest("Missing agent.");
            }

            try
            {
                return Results.Json(SaveAgentConfig(request), JsonOptions);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapPost("/api/sessions", async (HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<CreateSessionRequest>(JsonOptions, context.RequestAborted);
            if (request == null || string.IsNullOrWhiteSpace(request.Agent))
            {
                return Results.BadRequest("Missing agent.");
            }

            try
            {
                var sessionId = CreateSession(request.Agent);
                return Results.Json(new { sessionId }, JsonOptions);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
        app.MapPost("/api/chat", HandleChatAsync);

        Console.WriteLine($"[web] Matdance Web UI: http://{_host}:{_port}");
        RefreshVectorMemoryIndexesOnStartup();

        await app.StartAsync(ct);
        Console.WriteLine("[web] Backend started.");

        // Pre-warm after the backend is listening without blocking Web UI startup.
        _ = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("[web] Pre-warming browser...");
                await _browser.EnsureBrowserAsync(headless: true);
                Console.WriteLine("[web] Browser pre-warmed and ready.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[web] Browser pre-warm failed: {ex.Message}");
            }
        }, CancellationToken.None);

        Console.WriteLine("[web] Press Ctrl+C to stop.");
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            scheduledCts.Cancel();
            try { await scheduledWorkerTask; } catch (OperationCanceledException) { }
            try { await idleSkillValidationTask; } catch (OperationCanceledException) { }
            await app.StopAsync(CancellationToken.None);
        }
    }

    private void EnsureSafeWebBinding()
    {
        if (IsLoopbackHost(_host))
            return;

        if (string.Equals(Environment.GetEnvironmentVariable("MATDANCE_ALLOW_REMOTE_WEB"), "1", StringComparison.Ordinal))
            return;

        throw new InvalidOperationException(
            $"Refusing to bind the Web UI to '{_host}'. Use localhost/127.0.0.1, or set MATDANCE_ALLOW_REMOTE_WEB=1 when you intentionally expose the single-token protected service.");
    }

    private static void LogWebAuth(WebAuthService auth)
    {
        if (!auth.Enabled)
        {
            Console.WriteLine("[web-auth] disabled for loopback-only Web UI.");
            return;
        }

        Console.WriteLine($"[web-auth] single-token authentication enabled source={auth.Source}.");
        if (!string.IsNullOrWhiteSpace(auth.GeneratedToken))
        {
            Console.WriteLine("[web-auth] Generated token for this Matdance runtime:");
            Console.WriteLine(auth.GeneratedToken);
            Console.WriteLine($"[web-auth] Token saved at {WebAuthService.StatePath}");
        }
    }

    private static bool IsAuthExemptPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.Equals("/api/auth/status", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/favicon.png", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/assets/brand/", StringComparison.OrdinalIgnoreCase);
    }

    private const string LoginPageHtml = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Matdance Login</title>
  <link rel="icon" type="image/png" href="/favicon.png">
  <link rel="apple-touch-icon" href="/assets/brand/matdance-icon.png">
  <style>
    :root { color-scheme: dark; font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; background: #08111f; color: #eef8ff; }
    body { min-height: 100vh; margin: 0; display: grid; place-items: center; background: radial-gradient(circle at top left, rgba(103,247,177,.14), transparent 32%), #08111f; }
    main { width: min(420px, calc(100vw - 32px)); display: grid; gap: 14px; }
    .login-logo { width: min(260px, 74vw); justify-self: center; filter: drop-shadow(0 20px 48px rgba(100,219,255,.18)); }
    h1 { margin: 0; font-size: 28px; line-height: 1.1; }
    p { margin: 0; color: #a9bac8; line-height: 1.55; }
    form { display: grid; gap: 12px; padding: 18px; border: 1px solid rgba(255,255,255,.12); border-radius: 8px; background: rgba(255,255,255,.055); box-shadow: 0 18px 60px rgba(0,0,0,.28); }
    label { display: grid; gap: 7px; color: #cfe0ea; font-size: 13px; }
    input { width: 100%; box-sizing: border-box; min-height: 44px; border-radius: 7px; border: 1px solid rgba(255,255,255,.16); background: rgba(2,6,14,.7); color: #fff; padding: 0 12px; font: inherit; }
    button { min-height: 42px; border: 0; border-radius: 7px; background: #67f7b1; color: #06130d; font-weight: 800; cursor: pointer; }
    .error { min-height: 18px; color: #ffb3b3; font-size: 13px; }
  </style>
</head>
<body>
  <main>
    <img class="login-logo" src="/assets/brand/matdance-logo.png" alt="Matdance">
    <form id="loginForm">
      <p id="intro">Enter the single access token for this Matdance service.</p>
      <label><span id="tokenLabel">Access token</span><input id="token" type="password" autocomplete="current-password" autofocus></label>
      <button id="submitButton" type="submit">Sign in</button>
      <div id="error" class="error"></div>
    </form>
  </main>
  <script>
    const zh = (navigator.language || '').toLowerCase().startsWith('zh');
    const text = zh ? {
      title: 'Matdance 登录',
      intro: '请输入此 Matdance 服务的单次访问 token。',
      tokenLabel: '访问 token',
      submit: '登录',
      invalid: 'Token 无效。'
    } : {
      title: 'Matdance Login',
      intro: 'Enter the single access token for this Matdance service.',
      tokenLabel: 'Access token',
      submit: 'Sign in',
      invalid: 'Invalid token.'
    };
    document.documentElement.lang = zh ? 'zh-CN' : 'en';
    document.title = text.title;
    document.getElementById('intro').textContent = text.intro;
    document.getElementById('tokenLabel').textContent = text.tokenLabel;
    document.getElementById('submitButton').textContent = text.submit;
    document.getElementById('loginForm').addEventListener('submit', async function(event) {
      event.preventDefault();
      const error = document.getElementById('error');
      error.textContent = '';
      const token = document.getElementById('token').value.trim();
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token })
      });
      if (!response.ok) {
        error.textContent = text.invalid;
        return;
      }
      location.replace('/');
    });
  </script>
</body>
</html>
""";

    private static IResult GetDefaultSoundCueHttp(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Results.BadRequest("Missing sound cue file name.");

        var safeFileName = Path.GetFileName(fileName);
        if (!string.Equals(safeFileName, fileName, StringComparison.Ordinal)
            || !IsAllowedSoundCueExtension(Path.GetExtension(safeFileName)))
        {
            return Results.BadRequest("Unsupported sound cue file name.");
        }

        var foundPath = ResolveDefaultSoundCuePath(safeFileName);
        if (foundPath == null)
            return Results.NotFound("Sound cue file not found.");

        return Results.File(foundPath, GetMimeType(foundPath), enableRangeProcessing: true);
    }

    private static IResult GetBrandAssetHttp(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Results.BadRequest("Missing brand asset file name.");

        var safeFileName = Path.GetFileName(fileName);
        if (!string.Equals(safeFileName, fileName, StringComparison.Ordinal)
            || !IconExtensions.Contains(Path.GetExtension(safeFileName), StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Unsupported brand asset file name.");
        }

        var foundPath = ResolveBrandAssetPath(safeFileName);
        if (foundPath == null)
            return Results.NotFound("Brand asset not found.");

        return Results.File(foundPath, GetMimeType(foundPath), enableRangeProcessing: true);
    }

    private static string? ResolveDefaultSoundCuePath(string fileName)
    {
        foreach (var root in GetDefaultSoundCueRoots())
        {
            var candidate = Path.Combine(root, fileName);
            try
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full) && PathSafety.IsUnderRoot(full, root))
                    return full;
            }
            catch
            {
            }
        }

        return null;
    }

    private static string? ResolveBrandAssetPath(string fileName)
    {
        foreach (var root in GetBrandAssetRoots())
        {
            var candidate = Path.Combine(root, fileName);
            try
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full) && PathSafety.IsUnderRoot(full, root))
                    return full;
            }
            catch
            {
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetDefaultSoundCueRoots()
    {
        return new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Web", "Assets", "Sounds"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Matdance.Cli", "Web", "Assets", "Sounds")
        }
        .Select(path =>
        {
            try { return Path.GetFullPath(path); }
            catch { return null; }
        })
        .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        .Cast<string>()
        .Distinct(StringComparer.FromComparison(PathSafety.PathComparison))
        .ToList();
    }

    private static IReadOnlyList<string> GetBrandAssetRoots()
    {
        return new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Web", "Assets", "Brand"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Matdance.Cli", "Web", "Assets", "Brand")
        }
        .Select(path =>
        {
            try { return Path.GetFullPath(path); }
            catch { return null; }
        })
        .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        .Cast<string>()
        .Distinct(StringComparer.FromComparison(PathSafety.PathComparison))
        .ToList();
    }

    private static bool IsLoopbackHost(string host)
    {
        var value = (host ?? string.Empty).Trim().Trim('[', ']');
        if (value.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        return IPAddress.TryParse(value, out var address) && IPAddress.IsLoopback(address);
    }

    private IReadOnlyList<string> GetPreviewAllowedRoots(string? agent, string browserTempDir)
    {
        var roots = new List<string> { browserTempDir };
        roots.AddRange(GetDefaultSoundCueRoots());

        if (!string.IsNullOrWhiteSpace(agent))
        {
            try
            {
                roots.Add(_path.GetWorkspacePath(NormalizeAgentName(agent)));
            }
            catch (InvalidOperationException)
            {
            }
        }
        else if (Directory.Exists(_path.AgentsRoot))
        {
            foreach (var agentDir in Directory.GetDirectories(_path.AgentsRoot))
            {
                var agentName = Path.GetFileName(agentDir);
                if (string.IsNullOrWhiteSpace(agentName)) continue;
                try
                {
                    roots.Add(_path.GetWorkspacePath(agentName));
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        return roots
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.FromComparison(PathSafety.PathComparison))
            .ToList();
    }

    private static bool IsSensitivePreviewPath(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.Equals("agent_config.json", StringComparison.OrdinalIgnoreCase))
            return true;
        if (fileName.Equals("multimodal_config.json", StringComparison.OrdinalIgnoreCase))
            return true;
        if (fileName.Equals(".env", StringComparison.OrdinalIgnoreCase) || fileName.StartsWith(".env.", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static async Task<SkillLearnRequest?> ReadSkillLearnRequestAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            var jsonRequest = await context.Request.ReadFromJsonAsync<SkillLearnRequest>(JsonOptions, context.RequestAborted);
            if (jsonRequest != null)
                PrepareSkillLearnSourcePaths(jsonRequest);
            return jsonRequest;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var formRequest = new SkillLearnRequest
        {
            Agent = FormValue(form, "agent") ?? string.Empty,
            SourcePath = FormValue(form, "sourcePath"),
            SourceText = FormValue(form, "sourceText"),
            NameHint = FormValue(form, "nameHint")
        };

        var sourcePaths = new List<string>();
        string? tempRoot = null;
        AddPreparedSkillLearnPath(sourcePaths, formRequest.SourcePath, ref tempRoot);

        if (form.Files.Count > 0)
        {
            var uploadRoot = Path.Combine(EnsureSkillLearnTempRoot(ref tempRoot), "uploads");
            Directory.CreateDirectory(uploadRoot);
            foreach (var file in form.Files)
                await SaveSkillLearnUploadAsync(file, uploadRoot, context.RequestAborted);
            sourcePaths.Add(uploadRoot);
        }

        if (sourcePaths.Count > 0)
        {
            formRequest.SourcePath = null;
            formRequest.SourcePaths = sourcePaths;
        }

        formRequest.CleanupPath = tempRoot;
        return formRequest;
    }

    private static void PrepareSkillLearnSourcePaths(SkillLearnRequest request)
    {
        var rawPaths = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.SourcePath))
            rawPaths.Add(request.SourcePath);
        if (request.SourcePaths != null)
            rawPaths.AddRange(request.SourcePaths.Where(path => !string.IsNullOrWhiteSpace(path)));

        if (rawPaths.Count == 0)
            return;

        var sourcePaths = new List<string>();
        string? tempRoot = null;
        foreach (var rawPath in rawPaths)
            AddPreparedSkillLearnPath(sourcePaths, rawPath, ref tempRoot);

        request.SourcePath = null;
        request.SourcePaths = sourcePaths;
        request.CleanupPath = tempRoot;
    }

    private static void AddPreparedSkillLearnPath(List<string> sourcePaths, string? rawPath, ref string? tempRoot)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return;

        var path = rawPath.Trim();
        if (File.Exists(path) && Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var archiveRoot = Path.Combine(
                EnsureSkillLearnTempRoot(ref tempRoot),
                "archives",
                MakeSafePathSegment(Path.GetFileNameWithoutExtension(path), "archive"));
            using var archive = ZipFile.OpenRead(path);
            ExtractZipArchiveSafely(archive, archiveRoot);
            sourcePaths.Add(archiveRoot);
            return;
        }

        sourcePaths.Add(path);
    }

    private static async Task SaveSkillLearnUploadAsync(IFormFile file, string uploadRoot, CancellationToken ct)
    {
        if (file.Length <= 0)
            return;
        if (file.Length > MaxSkillImportUploadBytes)
            throw new InvalidOperationException("Uploaded skill source is too large.");

        var relativePath = NormalizeUploadRelativePath(file.FileName, "source");
        if (Path.GetExtension(relativePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var archiveRoot = Path.Combine(
                uploadRoot,
                "archives",
                MakeSafePathSegment(Path.GetFileNameWithoutExtension(relativePath), "archive"));
            await using var stream = file.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            ExtractZipArchiveSafely(archive, archiveRoot);
            return;
        }

        var fileRoot = Path.Combine(uploadRoot, "files");
        var target = Path.GetFullPath(Path.Combine(fileRoot, relativePath));
        if (!IsPathUnderRoot(target, fileRoot))
            throw new InvalidOperationException("Uploaded file path is outside the import directory.");

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await using var output = File.Create(target);
        await file.CopyToAsync(output, ct);
    }

    private static void ExtractZipArchiveSafely(ZipArchive archive, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);
        long extractedBytes = 0;
        var extractedFiles = 0;
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
                continue;
            extractedFiles++;
            extractedBytes += entry.Length;
            if (extractedFiles > MaxSkillImportExtractFiles || extractedBytes > MaxSkillImportExtractBytes)
                throw new InvalidOperationException("Zip archive is too large to import safely.");

            var relativePath = NormalizeUploadRelativePath(entry.FullName, entry.Name);
            var target = Path.GetFullPath(Path.Combine(destinationRoot, relativePath));
            if (!IsPathUnderRoot(target, destinationRoot))
                throw new InvalidOperationException("Zip archive contains an unsafe path.");

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    private static string EnsureSkillLearnTempRoot(ref string? tempRoot)
    {
        if (string.IsNullOrWhiteSpace(tempRoot))
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "matdance-skill-import-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        return tempRoot;
    }

    private static string NormalizeUploadRelativePath(string value, string fallback)
    {
        var normalized = (value ?? "").Replace('\\', '/').Trim('/');
        var parts = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part != "." && part != "..")
            .Select(part => MakeSafePathSegment(part, fallback))
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        return parts.Count == 0 ? MakeSafePathSegment(fallback, "source") : Path.Combine(parts.ToArray());
    }

    private static string MakeSafePathSegment(string? value, string fallback)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var chars = text.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var safe = new string(chars).Trim('.', ' ');
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }

    private static string? FormValue(IFormCollection form, string key)
    {
        return form.TryGetValue(key, out var value) ? value.ToString() : null;
    }

    private static void CleanupSkillLearnRequest(SkillLearnRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CleanupPath))
            return;

        try
        {
            if (Directory.Exists(request.CleanupPath))
                Directory.Delete(request.CleanupPath, recursive: true);
        }
        catch
        {
        }
        finally
        {
            request.CleanupPath = null;
        }
    }

    private void RefreshVectorMemoryIndexesOnStartup()
    {
        if (!Directory.Exists(_path.AgentsRoot))
        {
            return;
        }

        var vectorMemory = new VectorMemoryService(_path);
        var refreshed = 0;
        var failed = 0;

        foreach (var dir in Directory.GetDirectories(_path.AgentsRoot).OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
        {
            var agent = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(agent))
            {
                continue;
            }

            try
            {
                vectorMemory.Refresh(agent);
                refreshed++;
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"[web] Vector memory refresh failed for '{agent}': {ex.Message}");
            }
        }

        Console.WriteLine($"[web] Vector memory refreshed for {refreshed} agent(s)" + (failed > 0 ? $", failed: {failed}" : "."));
    }

    private IReadOnlyList<object> ListAgents()
    {
        if (!Directory.Exists(_path.AgentsRoot))
        {
            return Array.Empty<object>();
        }

        return Directory.GetDirectories(_path.AgentsRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .Select(name =>
            {
                var configPath = _path.GetAgentConfigJsonPath(name);
                var config = AgentConfig.Load(configPath);
                var displayName = AgentDisplayName(name, config);
                return (object)new
                {
                    name,
                    displayName,
                    initial = AgentInitial(displayName),
                    iconUrl = GetAgentIconUrl(name),
                    modelId = config.ModelId,
                    apiType = config.ApiType,
                    contextWindow = config.ContextWindow,
                    maxConcurrency = config.MaxConcurrency,
                    hasApiKey = !string.IsNullOrWhiteSpace(config.ApiKey) && !config.ApiKey.StartsWith("sk-xxxxx", StringComparison.OrdinalIgnoreCase)
                };
            })
            .ToList();
    }

    private object LoadAgentConfigDto(string agent)
    {
        EnsureAgentExists(agent);
        var configPath = _path.GetAgentConfigJsonPath(agent);
        var config = AgentConfig.Load(configPath);
        var displayName = AgentDisplayName(agent, config);
        var sessionsDir = _path.GetSessionsPath(agent);
        var sessionCount = Directory.Exists(sessionsDir)
            ? Directory.GetFiles(sessionsDir, "*.json").Count(file => !Path.GetFileName(file).EndsWith(".state.json", StringComparison.OrdinalIgnoreCase))
            : 0;

        return new
        {
            agent,
            config = new
            {
                name = displayName,
                displayName,
                initial = AgentInitial(displayName),
                iconUrl = GetAgentIconUrl(agent),
                baseUrl = config.BaseUrl,
                modelId = config.ModelId,
                apiType = config.ApiType,
                contextWindow = config.ContextWindow,
                maxOutputToken = config.MaxOutputToken,
                maxConcurrency = config.MaxConcurrency,
                temperature = config.Temperature,
                hasApiKey = HasRealApiKey(config.ApiKey),
                configPath,
                workspacePath = _path.GetWorkspacePath(agent),
                memoryPath = _path.GetMemoryPath(agent),
                hotMemoryPath = _path.GetHotMemoryPath(agent),
                coreMemoryPath = _path.GetCoreMemoryPath(agent),
                iconsPath = _path.GetIconsPath(agent),
                hotMemoryExists = File.Exists(_path.GetHotMemoryPath(agent)),
                coreMemoryExists = File.Exists(_path.GetCoreMemoryPath(agent)),
                sessionCount
            },
            apiTypes = ModelProviderCatalog.ApiTypes(),
            providers = ModelProviderCatalog.All
        };
    }

    private object SaveAgentConfig(AgentConfigRequest request)
    {
        var agent = NormalizeAgentName(request.Agent);
        EnsureAgentExists(agent);
        var configPath = _path.GetAgentConfigJsonPath(agent);
        var config = AgentConfig.Load(configPath);

        config.Name = agent;
        config.ApiType = RequireValue(request.ApiType, nameof(request.ApiType));
        config.ModelId = RequireValue(request.ModelId, nameof(request.ModelId));
        var provider = ModelProviderCatalog.FindProvider(config.ApiType);
        config.BaseUrl = provider?.LocksBaseUrl == true
            ? provider.BaseUrl
            : RequireValue(request.BaseUrl, nameof(request.BaseUrl));
        if (provider?.LocksTokenLimits == true)
        {
            var preset = ModelProviderCatalog.FindModel(config.ApiType, config.ModelId) ?? provider.Models.FirstOrDefault();
            if (preset != null)
            {
                config.ContextWindow = preset.ContextWindow;
                config.MaxOutputToken = preset.MaxOutputToken;
            }
        }
        else
        {
            config.ContextWindow = RequirePositive(request.ContextWindow, nameof(request.ContextWindow));
            config.MaxOutputToken = RequirePositive(request.MaxOutputToken, nameof(request.MaxOutputToken));
        }
        config.Temperature = Math.Clamp(request.Temperature ?? config.Temperature, 0f, 2f);
        config.MaxConcurrency = Math.Clamp(request.MaxConcurrency ?? config.MaxConcurrency, 1, 16);
        ModelProviderCatalog.ApplyDefaults(
            config,
            preserveCustomModelId: true,
            preserveCustomTokenLimits: true);
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            config.ApiKey = request.ApiKey.Trim();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        config.Save(configPath);
        return LoadAgentConfigDto(agent);
    }

    private IResult GetMultiModalConfigHttp(string agent)
    {
        try
        {
            var safeAgent = NormalizeAgentName(agent);
            EnsureAgentExists(safeAgent);
            return Results.Json(new MultiModalConfigService(_path).GetDisplayConfig(safeAgent), JsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private async Task<IResult> SaveMultiModalConfigHttp(HttpContext context)
    {
        try
        {
            var request = await context.Request.ReadFromJsonAsync<MultiModalSaveRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return Results.BadRequest("Missing multimodal config request.");
            var agent = NormalizeAgentName(request.Agent);
            EnsureAgentExists(agent);
            request.Agent = agent;
            return Results.Json(new MultiModalConfigService(_path).Save(request), JsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private async Task<IResult> GenerateImageHttp(HttpContext context)
    {
        try
        {
            var request = await context.Request.ReadFromJsonAsync<ImageGenerationRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return Results.BadRequest("Missing image generation request.");
            var agent = NormalizeAgentName(request.Agent);
            EnsureAgentExists(agent);
            request.Agent = agent;
            var results = await new MultiModalClient(_path).GenerateImageAsync(agent, request, context.RequestAborted);
            return Results.Json(new { results }, JsonOptions);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or JsonException)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private async Task<IResult> TextToSpeechHttp(HttpContext context)
    {
        try
        {
            var request = await context.Request.ReadFromJsonAsync<TextToSpeechRequest>(JsonOptions, context.RequestAborted);
            if (request == null) return Results.BadRequest("Missing text_to_speech request.");
            var agent = NormalizeAgentName(request.Agent);
            EnsureAgentExists(agent);
            request.Agent = agent;
            var result = await new MultiModalClient(_path).TextToSpeechAsync(agent, request, context.RequestAborted);
            if (!string.IsNullOrWhiteSpace(request.Session) && request.MessageIndex.HasValue)
            {
                AttachAudioToMessage(agent, request.Session, request.MessageIndex.Value, result);
            }

            return Results.Json(new { audio = result }, JsonOptions);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or JsonException)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private async Task<IResult> TranscribeAudioHttp(HttpContext context)
    {
        try
        {
            var agent = NormalizeAgentName(context.Request.Query["agent"].ToString());
            EnsureAgentExists(agent);
            if (!context.Request.HasFormContentType) return Results.BadRequest("Expected multipart form data.");
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            var file = form.Files["file"];
            if (file == null || file.Length == 0) return Results.BadRequest("Missing audio file.");

            await using var stream = file.OpenReadStream();
            var text = await new MultiModalClient(_path).TranscribeAsync(agent, stream, file.FileName, file.ContentType, context.RequestAborted);
            return Results.Json(new { text }, JsonOptions);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or JsonException)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private async Task<IResult> CreateAgentAsync(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<CreateAgentRequest>(JsonOptions, context.RequestAborted);
        if (request == null) return Results.BadRequest("Missing agent name.");
        var agent = NormalizeAgentName(request.Name);
        if (Directory.Exists(_path.GetAgentPath(agent))) return Results.BadRequest("Agent already exists.");
        new AgentService(_path).Create(agent);
        Directory.CreateDirectory(_path.GetIconsPath(agent));
        return Results.Json(new { agents = ListAgents(), agent }, JsonOptions);
    }

    private IResult DeleteAgentHttp(string agent)
    {
        try
        {
            var safeAgent = NormalizeAgentName(agent);
            EnsureAgentExists(safeAgent);
            _backgroundWork.ForgetAgent(safeAgent);
            MemoryOrganizationService.CancelAgentJobs(safeAgent, "Agent was deleted.");
            SkillMaintenanceService.CancelAgentJobs(safeAgent, "Agent was deleted.");
            RemoveSessionLocks(safeAgent);
            new AgentService(_path).Delete(safeAgent);
            return Results.Json(new { agents = ListAgents() }, JsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private IResult ExportSkillHttp(string agent, string skillId)
    {
        try
        {
            var safeAgent = NormalizeAgentName(agent);
            EnsureAgentExists(safeAgent);
            var safeSkillId = _path.NormalizePathSegment(skillId, "Skill id");
            var skillService = new SkillService(_path);
            var skill = skillService.Read(safeAgent, safeSkillId);
            var skillDir = Path.GetFullPath(_path.GetSkillPath(safeAgent, safeSkillId));
            if (!Directory.Exists(skillDir))
                throw new InvalidOperationException($"Skill '{safeSkillId}' not found.");

            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var file in Directory.EnumerateFiles(skillDir, "*", SearchOption.AllDirectories).OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
                {
                    var fullPath = Path.GetFullPath(file);
                    if (!PathSafety.IsUnderRoot(fullPath, skillDir))
                        continue;

                    var entryName = Path.GetRelativePath(skillDir, fullPath).Replace('\\', '/');
                    if (string.IsNullOrWhiteSpace(entryName) || entryName.StartsWith("../", StringComparison.Ordinal) || entryName.Contains("/../", StringComparison.Ordinal))
                        continue;

                    archive.CreateEntryFromFile(fullPath, entryName, CompressionLevel.Fastest);
                }
            }

            var fileName = MakeSafePathSegment(skill.Id, "skill") + ".zip";
            return Results.File(stream.ToArray(), "application/zip", fileName);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (IOException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private void RemoveSessionLocks(string agent)
    {
        var prefix = agent + "\u001f";
        foreach (var key in _sessionLocks.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                _sessionLocks.TryRemove(key, out _);
        }
    }

    private static string NormalizeAgentName(string? agent)
    {
        return PathSafety.NormalizeFileNameSegment(agent, "Agent name");
    }

    private static string NormalizeSessionId(string? session)
    {
        return PathSafety.NormalizeFileNameSegment(session, "Session id");
    }

    private static string AgentDisplayName(string agent, AgentConfig config)
    {
        var name = config.Name;
        if (string.IsNullOrWhiteSpace(name)) return agent;
        if (name.Equals("your agent name", StringComparison.OrdinalIgnoreCase)) return agent;
        return name.Trim();
    }

    private static string AgentInitial(string displayName)
    {
        var value = string.IsNullOrWhiteSpace(displayName) ? "A" : displayName.Trim();
        return value[..1].ToUpperInvariant();
    }

    private IResult GetAgentIconHttp(string agent)
    {
        try
        {
            var safeAgent = NormalizeAgentName(agent);
            EnsureAgentExists(safeAgent);
            var iconPath = FindAgentIconPath(safeAgent);
            if (iconPath == null) return Results.NotFound();
            return Results.File(iconPath, GetIconContentType(Path.GetExtension(iconPath)));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private async Task<IResult> UploadAgentIconAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType) return Results.BadRequest("Expected multipart form data.");
        try
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            var agent = NormalizeAgentName(form["agent"].ToString());
            EnsureAgentExists(agent);
            var file = form.Files["icon"];
            if (file == null) return Results.BadRequest("Missing icon file.");
            if (file.Length == 0) return Results.BadRequest("Missing icon file.");
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!IsAllowedIconExtension(extension)) return Results.BadRequest("Unsupported icon format.");
            var iconsDir = _path.GetIconsPath(agent);
            Directory.CreateDirectory(iconsDir);
            foreach (var oldFile in Directory.GetFiles(iconsDir, "avatar.*"))
            {
                File.Delete(oldFile);
            }
            var iconPath = Path.Combine(iconsDir, "avatar" + extension);
            await using var stream = File.Create(iconPath);
            await file.CopyToAsync(stream, context.RequestAborted);
            return Results.Json(LoadAgentConfigDto(agent), JsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (IOException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private async Task<IResult> UploadSoundCueAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType) return Results.BadRequest("Expected multipart form data.");
        try
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            var agent = NormalizeAgentName(form["agent"].ToString());
            EnsureAgentExists(agent);

            var type = NormalizeSoundCueType(form["type"].ToString());
            if (type == null) return Results.BadRequest("Unsupported sound cue type.");

            var file = form.Files["audio"] ?? form.Files.FirstOrDefault();
            if (file == null || file.Length == 0) return Results.BadRequest("Missing audio file.");
            if (file.Length > MaxSoundCueUploadBytes) return Results.BadRequest("Audio file is too large.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!IsAllowedSoundCueExtension(extension)) return Results.BadRequest("Unsupported audio format.");

            var cuesDir = Path.Combine(_path.GetWorkspacePath(agent), "generated", "audio", "cues", type);
            Directory.CreateDirectory(cuesDir);

            var fileName = $"{UserTimeZoneService.Now():yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}";
            fileName = fileName[..Math.Min(fileName.Length, 45 - extension.Length)] + extension;
            var fullPath = Path.Combine(cuesDir, fileName);
            await using (var stream = File.Create(fullPath))
            {
                await file.CopyToAsync(stream, context.RequestAborted);
            }

            var relativePath = Path.GetRelativePath(_path.GetWorkspacePath(agent), fullPath).Replace('\\', '/');
            var url = "/api/file?agent=" + Uri.EscapeDataString(agent) + "&path=" + Uri.EscapeDataString(relativePath);
            return Results.Json(new
            {
                type,
                name = string.IsNullOrWhiteSpace(file.FileName) ? fileName : Path.GetFileName(file.FileName),
                fileName,
                relativePath,
                url,
                size = file.Length
            }, JsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (IOException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private async Task<IResult> SaveSoundCueSettingsHttp(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<SoundCueSettingsSaveRequest>(JsonOptions, context.RequestAborted);
        if (request == null) return Results.BadRequest("Missing sound cue settings request.");

        try
        {
            if (!string.IsNullOrWhiteSpace(request.Agent))
                EnsureAgentExists(NormalizeAgentName(request.Agent));

            var saved = new SoundCueSettingsService().Save(request.Settings ?? new SoundCueSettings());
            return Results.Json(saved, JsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (IOException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static string? NormalizeSoundCueType(string? type)
    {
        var value = Regex.Replace((type ?? string.Empty).Trim().ToLowerInvariant(), @"[\s\-]+", "_");
        return value switch
        {
            "reply" or "reply_done" or "done" or "complete" => "reply_done",
            "thinking" or "think" => "thinking",
            "confused" or "confusion" => "confused",
            "help" or "seeking_help" or "need_help" or "help_me" => "help",
            "confident" or "confidence" => "confident",
            "low_confidence" or "discouraged" or "unsure" or "hit" => "low_confidence",
            "idea" or "sudden_idea" or "eureka" => "idea",
            "happy" or "joy" or "joyful" or "glad" => "happy",
            "sad" or "sadness" or "down" => "sad",
            "perfunctory" or "casual" or "dismissive" => "perfunctory",
            "considering" or "light_thinking" or "slight_thinking" => "considering",
            "working_hard" or "workinghard" or "effort" or "trying" => "working_hard",
            "tired" or "exhausted" or "fatigue" => "tired",
            "energized" or "energetic" or "energy" => "energized",
            "angry" or "anger" or "mad" or "annoyed" => "angry",
            "relieved" or "relief" => "relieved",
            "awkward" or "embarrassed" => "awkward",
            "surprised" or "surprise" or "shocked" => "surprised",
            "apologetic" or "apology" or "sorry" => "apologetic",
            "skeptical" or "doubt" or "doubtful" or "suspicious" => "skeptical",
            "alert" or "warning" or "cautious" => "alert",
            "celebrate" or "celebration" or "victory" or "win" => "celebrate",
            "gentle" or "soft" or "tender" => "gentle",
            "playful" or "naughty" or "witty" => "playful",
            _ when Regex.IsMatch(value, @"^custom_[a-z0-9_]{1,56}$", RegexOptions.IgnoreCase) => value,
            _ => null
        };
    }

    private static bool IsAllowedSoundCueExtension(string extension)
    {
        return SoundCueExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private string? GetAgentIconUrl(string agent)
    {
        var iconPath = FindAgentIconPath(agent);
        if (iconPath == null) return null;
        var version = File.GetLastWriteTimeUtc(iconPath).Ticks;
        return $"/api/agent-icon?agent={Uri.EscapeDataString(agent)}&v={version}";
    }

    private string? FindAgentIconPath(string agent)
    {
        var iconsDir = _path.GetIconsPath(agent);
        if (!Directory.Exists(iconsDir)) return null;
        foreach (var extension in IconExtensions)
        {
            var preferred = Path.Combine(iconsDir, "avatar" + extension);
            if (File.Exists(preferred)) return preferred;
        }
        string? best = null;
        var bestTime = DateTime.MinValue;
        foreach (var file in Directory.GetFiles(iconsDir))
        {
            var extension = Path.GetExtension(file);
            if (!IsAllowedIconExtension(extension)) continue;
            var time = File.GetLastWriteTimeUtc(file);
            if (best == null)
            {
                best = file;
                bestTime = time;
                continue;
            }
            if (DateTime.Compare(time, bestTime) == 1)
            {
                best = file;
                bestTime = time;
            }
        }
        return best;
    }
    private static bool IsAllowedIconExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return false;
        return IconExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetIconContentType(string extension)
    {
        var value = extension.ToLowerInvariant();
        if (value == ".svg") return "image/svg+xml";
        if (value == ".jpg") return "image/jpeg";
        if (value == ".jpeg") return "image/jpeg";
        if (value == ".gif") return "image/gif";
        if (value == ".webp") return "image/webp";
        if (value == ".bmp") return "image/bmp";
        if (value == ".ico") return "image/x-icon";
        return "image/png";
    }

    private void EnsureAgentExists(string agent)
    {
        var safeAgent = NormalizeAgentName(agent);
        if (!Directory.Exists(_path.GetAgentPath(safeAgent)))
        {
            throw new InvalidOperationException($"Agent '{agent}' does not exist.");
        }
    }

    private static bool HasRealApiKey(string apiKey) =>
        !string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith("sk-xxxxx", StringComparison.OrdinalIgnoreCase);

    private static string RequireValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} is required.");
        }

        return value.Trim();
    }

    private static int RequirePositive(int? value, string name)
    {
        if (value is null or <= 0)
        {
            throw new InvalidOperationException($"{name} must be greater than 0.");
        }

        return value.Value;
    }

    private IReadOnlyList<object> ListSessions(string agent)
    {
        var safeAgent = NormalizeAgentName(agent);
        var sessionsDir = _path.GetSessionsPath(safeAgent);
        if (!Directory.Exists(sessionsDir))
        {
            return Array.Empty<object>();
        }

        return Directory.GetFiles(sessionsDir, "*.json")
            .Where(file => !Path.GetFileName(file).EndsWith(".state.json", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(file =>
            {
                var session = SessionData.Load(file);
                return (object)new
                {
                    id = string.IsNullOrWhiteSpace(session.SessionId) ? Path.GetFileNameWithoutExtension(file) : session.SessionId,
                    sessionId = string.IsNullOrWhiteSpace(session.SessionId) ? Path.GetFileNameWithoutExtension(file) : session.SessionId,
                    createdAt = session.CreateAt,
                    contextUsage = session.ContextUsage,
                    totalMessages = session.TotalMessages,
                    toolMessagesCount = session.ToolMessagesCount,
                    tokens = session.Tokens
                };
            })
            .ToList();
    }

    private string CreateSession(string agent)
    {
        var safeAgent = NormalizeAgentName(agent);
        var sessionsDir = _path.GetSessionsPath(safeAgent);
        if (!Directory.Exists(sessionsDir))
        {
            throw new InvalidOperationException($"Agent '{agent}' does not exist.");
        }

        string sessionId;
        string filePath;
        do
        {
            sessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            filePath = _path.GetSessionJsonPath(safeAgent, sessionId);
        }
        while (File.Exists(filePath));

        new SessionData
        {
            SessionId = sessionId,
            CreateAt = UserTimeZoneService.Now(),
            LastActivity = UserTimeZoneService.Now()
        }.Save(filePath);

        new SessionState().Save(filePath);
        return sessionId;
    }

    private object LoadSessionDto(string agent, string session)
    {
        var safeAgent = NormalizeAgentName(agent);
        var safeSession = NormalizeSessionId(session);
        var sessionFile = GetSessionFile(safeAgent, safeSession);
        var data = SessionData.Load(sessionFile);
        if (string.IsNullOrWhiteSpace(data.SessionId))
        {
            data.SessionId = safeSession;
        }

        var state = SessionState.Load(sessionFile);
        var config = AgentConfig.Load(_path.GetAgentConfigJsonPath(safeAgent));
        return new
        {
            agent = new
            {
                name = safeAgent,
                modelId = config.ModelId,
                apiType = config.ApiType,
                contextWindow = config.ContextWindow,
                maxOutputToken = config.MaxOutputToken,
                temperature = config.Temperature
            },
            session = SessionDto(data),
            messages = state.Messages.Select(MessageDto).ToList(),
            activeTask = state.ActiveTask,
            tracedFiles = state.TracedFiles.Select(file => new { file.Id, file.Kind, file.Mode, file.Path, length = file.Content.Length, file.LastRead, file.Status }).ToList()
        };
    }

    private async Task HandleChatAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/x-ndjson; charset=utf-8";
        context.Response.Headers.CacheControl = "no-cache";

        ChatRequest? request;
        try
        {
            request = await ReadChatRequestAsync(context);
        }
        catch (InvalidOperationException ex)
        {
            await WriteEventAsync(context, "error", new { message = ex.Message });
            return;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Agent) || string.IsNullOrWhiteSpace(request.Session) || (!request.ContinueFromHostNotice && string.IsNullOrWhiteSpace(request.Message) && request.UploadFiles.Count == 0))
        {
            await WriteEventAsync(context, "error", new { message = "Missing agent, session or message." });
            return;
        }

        try
        {
            request.Agent = NormalizeAgentName(request.Agent);
            request.Session = NormalizeSessionId(request.Session);
            EnsureAgentExists(request.Agent);
            if (request.UploadFiles.Count > 0)
            {
                request.Attachments = await SaveChatAttachmentsAsync(request.Agent, request.Session, request.UploadFiles, context.RequestAborted);
            }
        }
        catch (InvalidOperationException ex)
        {
            await WriteEventAsync(context, "error", new { message = ex.Message });
            return;
        }

        using var userTurn = _activity.BeginUserTurn(request.Agent);
        var key = $"{request.Agent}\u001f{request.Session}";
        var gate = _sessionLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(context.RequestAborted);
        IDisposable? hostNoticeLease = null;
        try
        {
            hostNoticeLease = SessionHostNoticeHub.BeginActive(request.Agent, request.Session);
            await ProcessChatAsync(context, request);
        }
        finally
        {
            hostNoticeLease?.Dispose();
            PersistQueuedHostImageNotices(request.Agent, request.Session);
            gate.Release();
        }
    }

    private async Task<ChatRequest?> ReadChatRequestAsync(HttpContext context)
    {
        if (context.Request.HasFormContentType)
        {
            IFormCollection form;
            try
            {
                form = await context.Request.ReadFormAsync(context.RequestAborted);
            }
            catch (BadHttpRequestException ex)
            {
                throw new InvalidOperationException($"Attachment upload is too large or malformed. Up to {MaxChatAttachments} files, {FormatBytes(MaxChatAttachmentBytes)} each, {FormatBytes(MaxChatAttachmentTotalBytes)} total are allowed. {ex.Message}");
            }
            catch (System.IO.InvalidDataException ex)
            {
                throw new InvalidOperationException($"Attachment upload is too large or malformed. Up to {MaxChatAttachments} files, {FormatBytes(MaxChatAttachmentBytes)} each, {FormatBytes(MaxChatAttachmentTotalBytes)} total are allowed. {ex.Message}");
            }

            var request = new ChatRequest
            {
                Agent = FormValue(form, "agent") ?? string.Empty,
                Session = FormValue(form, "session") ?? string.Empty,
                Message = FormValue(form, "message") ?? string.Empty,
                UploadFiles = form.Files.ToList()
            };

            ValidateChatAttachmentLimits(request.UploadFiles);

            return request;
        }

        return await context.Request.ReadFromJsonAsync<ChatRequest>(JsonOptions, context.RequestAborted);
    }

    private async Task<List<ChatAttachment>> SaveChatAttachmentsAsync(string agent, string session, IReadOnlyList<IFormFile> files, CancellationToken ct)
    {
        if (files.Count == 0)
            return new List<ChatAttachment>();
        ValidateChatAttachmentLimits(files);

        var safeSession = NormalizeSessionId(session);
        var root = Path.GetFullPath(Path.Combine(_path.GetWorkspacePath(agent), "attachments", safeSession));
        Directory.CreateDirectory(root);

        var result = new List<ChatAttachment>();
        foreach (var file in files)
        {
            if (file.Length <= 0)
                throw new InvalidOperationException("Attachment file is empty.");
            if (file.Length > MaxChatAttachmentBytes)
                throw new InvalidOperationException($"Attachment '{file.FileName}' is too large. Limit is {FormatBytes(MaxChatAttachmentBytes)}.");

            var originalName = Path.GetFileName(string.IsNullOrWhiteSpace(file.FileName) ? "attachment" : file.FileName);
            var extension = Path.GetExtension(originalName).ToLowerInvariant();
            var kind = GetChatAttachmentKind(extension, file.ContentType);
            if (kind == null)
                throw new InvalidOperationException($"Unsupported attachment type: {extension}. Allowed: common images, documents, spreadsheets, presentations, and archives.");

            var safeBase = MakeSafePathSegment(Path.GetFileNameWithoutExtension(originalName), "attachment");
            var id = UserTimeZoneService.Now().ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N")[..8];
            var fileName = $"{id}-{safeBase}{extension}";
            var fullPath = Path.GetFullPath(Path.Combine(root, fileName));
            if (!IsPathUnderRoot(fullPath, root))
                throw new InvalidOperationException("Attachment path resolves outside the session attachment directory.");

            await using (var output = File.Create(fullPath))
            {
                await file.CopyToAsync(output, ct);
            }

            var relativePath = NormalizeSlashes(Path.GetRelativePath(_path.GetWorkspacePath(agent), fullPath));
            var mime = GetChatAttachmentMimeType(extension, file.ContentType);
            result.Add(new ChatAttachment
            {
                Id = id,
                Name = originalName,
                Kind = kind,
                MimeType = mime,
                Extension = extension,
                Size = file.Length,
                Path = fullPath,
                RelativePath = relativePath,
                Url = "/api/file?agent=" + Uri.EscapeDataString(agent) + "&path=" + Uri.EscapeDataString(relativePath),
                Summary = BuildChatAttachmentSummary(kind, originalName, extension, file.Length)
            });
        }

        return result;
    }

    private static void ValidateChatAttachmentLimits(IReadOnlyList<IFormFile> files)
    {
        if (files.Count > MaxChatAttachments)
            throw new InvalidOperationException($"At most {MaxChatAttachments} attachments are allowed.");

        var total = files.Sum(file => file.Length);
        if (total > MaxChatAttachmentTotalBytes)
            throw new InvalidOperationException($"Attachments are too large. Total limit is {FormatBytes(MaxChatAttachmentTotalBytes)}.");
    }

    private static string? GetChatAttachmentKind(string extension, string? reportedMime = null)
    {
        if (ChatImageAttachmentExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "image";
        if (ChatArchiveAttachmentExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "archive";
        if (ChatDocumentAttachmentExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "document";

        var mime = (reportedMime ?? string.Empty).Trim().ToLowerInvariant();
        if (mime.StartsWith("image/", StringComparison.Ordinal))
            return "image";
        if (mime.Contains("zip", StringComparison.Ordinal)
            || mime.Contains("rar", StringComparison.Ordinal)
            || mime.Contains("7z", StringComparison.Ordinal)
            || mime.Contains("tar", StringComparison.Ordinal)
            || mime.Contains("gzip", StringComparison.Ordinal)
            || mime.Contains("bzip2", StringComparison.Ordinal)
            || mime.Contains("xz", StringComparison.Ordinal)
            || mime.Contains("compressed", StringComparison.Ordinal)
            || mime.Contains("archive", StringComparison.Ordinal))
        {
            return "archive";
        }
        if (mime.StartsWith("text/", StringComparison.Ordinal)
            || mime is "application/pdf" or "application/json" or "application/xml" or "application/rtf" or "application/msword"
            || mime.StartsWith("application/vnd.ms-", StringComparison.Ordinal)
            || mime.StartsWith("application/vnd.openxmlformats-", StringComparison.Ordinal)
            || mime.StartsWith("application/vnd.oasis.opendocument", StringComparison.Ordinal)
            || mime is "application/vnd.apple.pages" or "application/vnd.apple.numbers")
        {
            return "document";
        }
        return null;
    }

    private static string GetChatAttachmentMimeType(string extension, string? reported)
    {
        if (!string.IsNullOrWhiteSpace(reported) && !reported.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            return reported;

        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".docm" => "application/vnd.ms-word.document.macroEnabled.12",
            ".odt" => "application/vnd.oasis.opendocument.text",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xlsm" => "application/vnd.ms-excel.sheet.macroEnabled.12",
            ".xlsb" => "application/vnd.ms-excel.sheet.binary.macroEnabled.12",
            ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".pptm" => "application/vnd.ms-powerpoint.presentation.macroEnabled.12",
            ".odp" => "application/vnd.oasis.opendocument.presentation",
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            ".tar" => "application/x-tar",
            ".gz" or ".tgz" => "application/gzip",
            ".bz2" => "application/x-bzip2",
            ".xz" => "application/x-xz",
            ".csv" => "text/csv",
            ".tsv" => "text/tab-separated-values",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".md" or ".markdown" => "text/markdown",
            ".txt" => "text/plain",
            ".log" or ".ini" or ".sql" => "text/plain",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" or ".mjs" or ".cjs" => "application/javascript",
            ".ts" or ".tsx" or ".jsx" or ".py" or ".ps1" or ".sh" or ".bat" or ".cmd" => "text/plain",
            ".yaml" or ".yml" => "application/yaml",
            ".toml" => "application/toml",
            ".rtf" => "application/rtf",
            ".pages" => "application/vnd.apple.pages",
            ".numbers" => "application/vnd.apple.numbers",
            _ => "application/octet-stream"
        };
    }

    private static string BuildChatAttachmentSummary(string kind, string name, string extension, long size)
        => kind switch
        {
            "image" => $"Image attachment '{name}' ({extension}, {FormatBytes(size)}). If the model supports multimodal input, Matdance will include the image pixels in the current request.",
            "archive" => $"Archive attachment '{name}' ({extension}, {FormatBytes(size)}). Inspect or extract contents only when the task requires it.",
            _ => $"Document attachment '{name}' ({extension}, {FormatBytes(size)}). Use file tools for text-compatible files or safe local conversion/extraction tools when needed."
        };

    private static string NormalizeSlashes(string value) => value.Replace('\\', '/');

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return bytes + " B";
        if (bytes < 1024 * 1024)
            return (bytes / 1024d).ToString("0.#") + " KB";
        return (bytes / 1024d / 1024d).ToString("0.#") + " MB";
    }

    private async Task<IDisposable?> AcquireSessionTurnAsync(string agent, string session, CancellationToken ct)
    {
        agent = NormalizeAgentName(agent);
        session = NormalizeSessionId(session);
        var key = $"{agent}\u001f{session}";
        var gate = _sessionLocks.GetOrAdd(key, _=> new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        return new SemaphoreLease(gate);
    }
    private sealed class SemaphoreLease : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private bool _disposed;
        public SemaphoreLease(SemaphoreSlim gate) { _gate = gate; }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gate.Release();
        }
    }

    private async Task ProcessChatAsync(HttpContext context, ChatRequest request)
    {
        var sessionFile = GetSessionFile(request.Agent, request.Session);
        var data = SessionData.Load(sessionFile);
        if (string.IsNullOrWhiteSpace(data.SessionId))
        {
            data.SessionId = request.Session;
        }

        var state = SessionState.Load(sessionFile);
        state.ClearTraceLocks();
        var turnStartMessageIndex = state.Messages.Count;
        var config = AgentConfig.Load(_path.GetAgentConfigJsonPath(request.Agent));
        var llm = new LlmClient(config);
        var executor = new ToolExecutor(request.Agent, _path, state, allowInteractiveConfirmation: false, sessionId: request.Session, backgroundWork: _backgroundWork);
        var tools = ToolRegistry.GetAll();
        // Compress context if needed before building request
        var mainContextMessages = state.Messages.Where(PromptBuilder.ShouldIncludeInMainContext).ToList();
        var compressor = new ContextCompressor(config);
        List<ChatMessage> compressedHistory;
        if (compressor.ShouldCompress(mainContextMessages))
        {
            compressedHistory = await compressor.CompressAsync(mainContextMessages, context.RequestAborted);
        }
        else
        {
            compressedHistory = mainContextMessages;
        }

        var requestMessages = PromptBuilder.BuildRequestMessages(request.Agent, _path, config, state, compressedHistory);
        var privacyRevocationNotice = new SecuritySettingsService().ConsumePrivacyAccessRevokedNotice();
        if (!string.IsNullOrWhiteSpace(privacyRevocationNotice))
        {
            requestMessages.Insert(Math.Min(1, requestMessages.Count), ChatMessage.System(privacyRevocationNotice));
        }
        if (string.IsNullOrWhiteSpace(request.Message) && request.Attachments.Count > 0)
        {
            request.Message = "Please inspect the attached file(s).";
        }
        ChatMessage? userMsg = null;
        if (request.ContinueFromHostNotice)
        {
            requestMessages.Add(ChatMessage.System("Host continuation trigger: authoritative image-generation notice(s) were added to this session. Continue from the latest host notice, inspect successful outputs, report failures or partial completion, and decide the next step without asking the user to restate the task."));
        }
        else
        {
            userMsg = ChatMessage.User(request.Message, request.Attachments.Count > 0 ? request.Attachments : null);
            requestMessages.Add(userMsg);
            data.TotalMessages++;
        }

        var userMsgSaved = request.ContinueFromHostNotice;
        var hasToolCalls = true;
        var loop = 0;
        var sawToolCalls = false;
        var emptyContinuations = 0;
        var transientResponseRetries = 0;
        var executedToolCalls = new HashSet<string>(StringComparer.Ordinal);
        var thinkingToolNoticeSent = false;
        const int maxLoops = 10000;//以往轮次太少，导致对某些需要探索的复杂任务过早的进行拦截，我强烈建议不要改动此值，起码要大于项目早期设置的20轮。
        const int maxEmptyContinuations = 3;
        const int maxTransientResponseRetries = 1;
        var events = Channel.CreateUnbounded<StreamEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        var writerTask = WriteEventsAsync(context, events.Reader);

        try
        {
            while (hasToolCalls && loop < maxLoops && !context.RequestAborted.IsCancellationRequested)
            {
                loop++;
                if (DrainHostImageNotices(request.Agent, request.Session, sessionFile, data, state, requestMessages) > 0)
                {
                    requestMessages.Add(ChatMessage.System("Host image-generation notice inserted during this turn. Treat it as authoritative host state and handle it before finalizing the user-facing response."));
                    QueueEvent(events.Writer, "stats", StatsDto(data, state));
                }
                QueueEvent(events.Writer, "phase", new { phase = loop == 1 ? "thinking" : "integrating" });
                PromptBuilder.UpsertLiveFileLocksSnapshot(requestMessages, state);

                var streamed = new StringBuilder();
                var streamFilter = new LlmResponseGuard.StreamingFilter();
                var contentCueBuffer = string.Empty;
                void EmitChunk(string text)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        return;
                    }

                    streamed.Append(text);
                    contentCueBuffer = QueueSoundCueEvents(events.Writer, contentCueBuffer + text);
                    QueueEvent(events.Writer, "chunk", new { text });
                }

                var assistantMsg = await llm.SendAsync(
                    requestMessages,
                    tools,
                    chunk => streamFilter.OnChunk(chunk, EmitChunk),
                    context.RequestAborted,
                    onReasoningChunk: chunk =>
                    {
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            QueueEvent(events.Writer, "thinking", new { text = chunk });
                        }
                    },
                    enableThinking: false);

                var isTransientFailure = LlmResponseGuard.IsTransientAssistantFailure(assistantMsg);
                if (!isTransientFailure)
                {
                    streamFilter.FlushIfAllowed(EmitChunk);
                }

                if (string.IsNullOrEmpty(assistantMsg.Content) && streamed.Length > 0)
                {
                    assistantMsg.Content = streamed.ToString();
                }

                var hasCurrentToolCalls = assistantMsg.ToolCalls != null && assistantMsg.ToolCalls.Count > 0;
                if (!hasCurrentToolCalls && !thinkingToolNoticeSent && LlmResponseGuard.HasTextualToolRequestInThinking(assistantMsg))
                {
                    thinkingToolNoticeSent = true;
                    requestMessages.Add(ChatMessage.User(LlmResponseGuard.ThinkingTextToolRequestNotice));
                    QueueEvent(events.Writer, "phase", new { phase = "continuing" });
                    continue;
                }
                if (isTransientFailure && !hasCurrentToolCalls && (!LlmResponseGuard.IsNoResponse(assistantMsg) || !sawToolCalls))
                {
                    if (transientResponseRetries < maxTransientResponseRetries)
                    {
                        transientResponseRetries++;
                        QueueEvent(events.Writer, "phase", new { phase = "retrying" });
                        continue;
                    }

                    if (LlmResponseGuard.IsUpstreamRejection(assistantMsg))
                    {
                        LlmResponseGuard.MarkAsExcluded(assistantMsg);
                        assistantMsg.Content = "Upstream model gateway rejected this turn before Matdance received a usable answer. Please retry.";
                    }
                    else
                    {
                        LlmResponseGuard.MarkAsNoResponse(assistantMsg);
                        assistantMsg.Content = "The model returned an empty response. Please retry.";
                    }
                    EmitChunk(assistantMsg.Content);
                }

                if (string.IsNullOrWhiteSpace(assistantMsg.Content) && !hasCurrentToolCalls && sawToolCalls && emptyContinuations < maxEmptyContinuations)
                {
                    emptyContinuations++;
                    requestMessages.Add(ChatMessage.User("Continue from the latest tool results. If the task is unfinished, proceed with the next concrete step; if it is finished, give the user a concise final update. Do not stop silently."));
                    QueueEvent(events.Writer, "phase", new { phase = "continuing" });
                    continue;
                }

                if (string.IsNullOrEmpty(assistantMsg.Content) && !hasCurrentToolCalls)
                {
                    LlmResponseGuard.MarkAsNoResponse(assistantMsg);
                    assistantMsg.Content = "The model returned an empty response. Please retry.";
                    QueueEvent(events.Writer, "chunk", new { text = assistantMsg.Content });
                }

                data.Tokens = TokenCounter.EstimateMessages(requestMessages)
                    + TokenCounter.Estimate(assistantMsg.Content)
                    + TokenCounter.Estimate(assistantMsg.ReasoningContent ?? string.Empty);
                UpdateContextUsage(data, config);

                if (!userMsgSaved)
                {
                    if (userMsg != null)
                        state.Messages.Add(userMsg);
                    userMsgSaved = true;
                }

                state.Messages.Add(assistantMsg);
                requestMessages.Add(assistantMsg);
                SaveSession(sessionFile, data, state);
                QueueEvent(events.Writer, "stats", StatsDto(data, state));

                if (assistantMsg.ToolCalls == null || assistantMsg.ToolCalls.Count == 0)
                {
                    if (DrainHostImageNotices(request.Agent, request.Session, sessionFile, data, state, requestMessages) > 0)
                    {
                        requestMessages.Add(ChatMessage.System("Host image-generation notice arrived after the latest assistant response. Continue now and handle the completed image job before ending the turn."));
                        QueueEvent(events.Writer, "stats", StatsDto(data, state));
                        hasToolCalls = true;
                        continue;
                    }
                    hasToolCalls = false;
                    continue;
                }

                sawToolCalls = true;
                emptyContinuations = 0;
                QueueEvent(events.Writer, "phase", new { phase = "tooling" });
                foreach (var toolCall in assistantMsg.ToolCalls)
                {
                    var detail = TryGetToolArgsPreview(toolCall.Function.Arguments);
                    var duplicateToolCall = !executedToolCalls.Add(ToolFingerprint(toolCall));
                    QueueEvent(events.Writer, "tool_start", new
                    {
                        id = toolCall.Id,
                        name = toolCall.Function.Name,
                        detail
                    });

                    var result = duplicateToolCall
                        ? "[skipped] Duplicate tool call suppressed for this turn."
                        : await executor.ExecuteAsync(toolCall, context.RequestAborted);
                    var toolMsg = ChatMessage.Tool(toolCall.Id, result);
                    state.Messages.Add(toolMsg);
                    requestMessages.Add(toolMsg);
                    if (!duplicateToolCall)
                    {
                        data.ToolMessagesCount++;
                    }
                    data.Tokens = TokenCounter.EstimateMessages(requestMessages);
                    UpdateContextUsage(data, config);
                    SaveSession(sessionFile, data, state);

                    QueueEvent(events.Writer, "tool_result", new
                    {
                        id = toolCall.Id,
                        name = toolCall.Function.Name,
                        detail,
                        status = duplicateToolCall ? "skipped" : IsToolError(result) ? "error" : "done",
                        result = TrimForTransport(result, 12000)
                    });
                    QueueEvent(events.Writer, "stats", StatsDto(data, state));
                }

                if (ShouldStopAfterToolBatch(assistantMsg))
                {
                    hasToolCalls = false;
                }
            }

            if (hasToolCalls)
            {
                QueueEvent(events.Writer, "error", new { message = $"Stopped after {maxLoops} tool rounds to keep the session safe." });
            }

            var lastAssistantIndex = FindLastSpeakableAssistantIndex(state, turnStartMessageIndex);
            GeneratedFileResult? audioResult = null;
            string? audioError = null;
            if (lastAssistantIndex >= 0)
            {
                try
                {
                    audioResult = await TryGenerateAlwaysSpeechAsync(request.Agent, state.Messages[lastAssistantIndex].Content, context.RequestAborted);
                    if (audioResult != null)
                    {
                        state.Messages[lastAssistantIndex].Audio = ToAudioAttachment(audioResult);
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or JsonException)
                {
                    audioError = ex.Message;
                    QueueEvent(events.Writer, "audio_error", new { message = ex.Message });
                }
            }

            UpdateContextUsage(data, config);
            state.ClearTraceLocks();
            SaveSession(sessionFile, data, state);
            QueueEvent(events.Writer, "done", new
            {
                session = SessionDto(data),
                activeTask = state.ActiveTask,
                lastAssistantIndex,
                speechText = lastAssistantIndex >= 0 ? SpeechText(state.Messages[lastAssistantIndex].Content) : "",
                audio = audioResult,
                audioError
            });
        }
        catch (OperationCanceledException)
        {
            state.ClearTraceLocks();
            SaveSession(sessionFile, data, state);
        }
        catch (Exception ex)
        {
            QueueEvent(events.Writer, "error", new { message = ex.Message });
            state.ClearTraceLocks();
            SaveSession(sessionFile, data, state);
        }
        finally
        {
            var cleared = state.ClearTraceLocks();
            if (cleared > 0)
            {
                try { SaveSession(sessionFile, data, state); } catch { }
            }
            events.Writer.TryComplete();
            try
            {
                await writerTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private string GetSessionFile(string agent, string session)
    {
        var safeAgent = NormalizeAgentName(agent);
        var safeSession = NormalizeSessionId(session);
        var sessionsDir = _path.GetSessionsPath(safeAgent);
        if (!Directory.Exists(sessionsDir))
        {
            throw new InvalidOperationException($"Agent '{safeAgent}' does not exist.");
        }

        var sessionFile = _path.GetSessionJsonPath(safeAgent, safeSession);
        if (!File.Exists(sessionFile))
        {
            throw new InvalidOperationException($"Session '{safeSession}' does not exist.");
        }

        return sessionFile;
    }

    private static void SaveSession(string sessionFile, SessionData data, SessionState state)
    {
        data.LastActivity = UserTimeZoneService.Now();
        data.Save(sessionFile);
        state.Save(sessionFile);
    }

    private static int DrainHostImageNotices(
        string agent,
        string session,
        string sessionFile,
        SessionData data,
        SessionState state,
        List<ChatMessage> requestMessages)
    {
        var notices = SessionHostNoticeHub.Drain(agent, session)
            .Where(message => string.Equals(message.MessageType, "image_generation_notice", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (notices.Count == 0)
            return 0;

        foreach (var notice in notices)
        {
            state.Messages.Add(notice);
            requestMessages.Add(notice);
            data.TotalMessages++;
        }

        SaveSession(sessionFile, data, state);
        return notices.Count;
    }

    private void PersistQueuedHostImageNotices(string agent, string session)
    {
        var notices = SessionHostNoticeHub.Drain(agent, session)
            .Where(message => string.Equals(message.MessageType, "image_generation_notice", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (notices.Count == 0)
            return;

        try
        {
            var sessionFile = GetSessionFile(agent, session);
            var data = SessionData.Load(sessionFile);
            var state = SessionState.Load(sessionFile);
            foreach (var notice in notices)
            {
                state.Messages.Add(notice);
                data.TotalMessages++;
            }
            SaveSession(sessionFile, data, state);
        }
        catch
        {
        }
    }

    private void AttachAudioToMessage(string agent, string session, int messageIndex, GeneratedFileResult audio)
    {
        var sessionFile = GetSessionFile(agent, session);
        var data = SessionData.Load(sessionFile);
        var state = SessionState.Load(sessionFile);
        if (messageIndex < 0 || messageIndex >= state.Messages.Count)
        {
            throw new InvalidOperationException("message_index is out of range.");
        }

        var message = state.Messages[messageIndex];
        if (!message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Audio can only be attached to assistant messages.");
        }

        message.Audio = ToAudioAttachment(audio);
        SaveSession(sessionFile, data, state);
    }

    private static AudioAttachment ToAudioAttachment(GeneratedFileResult audio) => new()
    {
        Path = audio.Path,
        RelativePath = audio.RelativePath,
        Url = audio.Url,
        Format = audio.Format,
        Size = audio.Size
    };

    private static object SessionDto(SessionData data) => new
    {
        sessionId = data.SessionId,
        contextUsage = data.ContextUsage,
        totalMessages = data.TotalMessages,
        toolMessagesCount = data.ToolMessagesCount,
        tokens = data.Tokens,
        createAt = data.CreateAt,
        lastActivity = data.LastActivity
    };

    private static object StatsDto(SessionData data, SessionState state) => new
    {
        session = SessionDto(data),
        activeTask = state.ActiveTask
    };

    private async Task<GeneratedFileResult?> TryGenerateAlwaysSpeechAsync(string agent, string text, CancellationToken ct)
    {
        var effective = new MultiModalConfigService(_path).GetEffective(agent);
        if (!effective.Tts.Mode.Equals("always", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var spoken = SpeechText(text);
        if (string.IsNullOrWhiteSpace(spoken) || !effective.Tts.HasApiKey)
        {
            return null;
        }

        return await new MultiModalClient(_path).TextToSpeechAsync(agent, new TextToSpeechRequest
        {
            Agent = agent,
            Text = spoken
        }, ct);
    }

    private static string SpeechText(string text)
    {
        var withoutShowFile = Regex.Replace(text ?? string.Empty, @"\{show_file:[^}]+\}", string.Empty, RegexOptions.IgnoreCase);
        var withoutPlayAudio = Regex.Replace(withoutShowFile, @"`{1,3}\s*\{play[_-]?audio\s*[:：][^}]+\}\s*`{1,3}", string.Empty, RegexOptions.IgnoreCase);
        withoutPlayAudio = Regex.Replace(withoutPlayAudio, @"\{play[_-]?audio\s*[:：][^}]+\}", string.Empty, RegexOptions.IgnoreCase);
        return Regex.Replace(withoutPlayAudio, @"\[preview:[^\]]+\]", string.Empty, RegexOptions.IgnoreCase).Trim();
    }

    private static int FindLastSpeakableAssistantIndex(SessionState state, int minIndex = 0)
    {
        var floor = Math.Max(0, minIndex);
        for (var i = state.Messages.Count - 1; i >= floor; i--)
        {
            var message = state.Messages[i];
            if (!message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(message.MessageType)) continue;
            if (string.IsNullOrWhiteSpace(message.Content)) continue;
            if (message.ToolCalls is { Count: > 0 }) continue;
            return i;
        }

        return -1;
    }

    private static object MessageDto(ChatMessage msg) => new
    {
        role = msg.Role,
        content = msg.Content,
        reasoningContent = msg.ReasoningContent,
        toolCallId = msg.ToolCallId,
        messageType = msg.MessageType,
        importance = msg.Importance,
        timestamp = msg.Timestamp,
        audio = msg.Audio,
        attachments = msg.Attachments,
        toolCalls = msg.ToolCalls?.Select(tool => new
        {
            id = tool.Id,
            type = tool.Type,
            name = tool.Function.Name,
            arguments = TrimForTransport(tool.Function.Arguments, 1000)
        }).ToList()
    };

    private static void UpdateContextUsage(SessionData data, AgentConfig config)
    {
        var contextWindow = Math.Max(1, config.ContextWindow);
        data.ContextUsage = Math.Min(100, (int)Math.Round((double)data.Tokens / contextWindow * 100));
    }

    private static bool ShouldStopAfterToolBatch(ChatMessage assistantMsg)
    {
        if (string.IsNullOrWhiteSpace(assistantMsg.Content) || assistantMsg.ToolCalls == null || assistantMsg.ToolCalls.Count == 0)
        {
            return false;
        }

        return assistantMsg.ToolCalls.All(IsFinalBookkeepingToolCall);
    }

    private static bool IsFinalBookkeepingToolCall(ToolCall toolCall)
    {
        var name = toolCall.Function.Name;
        if (string.Equals(name, "memory_store", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(name, "task_manager", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(TryGetToolAction(toolCall), "done", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string? TryGetToolAction(ToolCall toolCall)
    {
        try
        {
            using var doc = JsonDocument.Parse(toolCall.Function.Arguments);
            return doc.RootElement.TryGetProperty("action", out var action) && action.ValueKind == JsonValueKind.String
                ? action.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ToolFingerprint(ToolCall toolCall)
    {
        return string.Concat(toolCall.Function.Name.Trim().ToLowerInvariant(), "\n", (toolCall.Function.Arguments ?? string.Empty).Trim());
    }

    private static bool IsToolError(string result)
    {
        var trimmed = result.TrimStart();
        return trimmed.StartsWith("[error]", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("[blocked]", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("[timeout]", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimForTransport(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "\n...[truncated]";
    }

    private static string? TryGetToolArgsPreview(string arguments)
    {
        try
        {
            using var doc = JsonDocument.Parse(arguments);
            var parts = new List<string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var value = prop.Value.ToString();
                if (value.Length > 42)
                {
                    value = value[..42] + "...";
                }
                parts.Add($"{prop.Name}={value}");
            }

            return string.Join(", ", parts);
        }
        catch
        {
            return null;
        }
    }

    private string? ResolvePreviewFilePath(string requestedPath, string? agent, string currentDir, string browserTempDir)
    {
        var candidates = new List<string>();
        var normalizedPath = PathSafety.NormalizeSeparators(requestedPath);

        if (Path.IsPathRooted(normalizedPath))
        {
            candidates.Add(normalizedPath);
        }
        else
        {
            AddAgentWorkspaceCandidates(candidates, agent, normalizedPath);
            candidates.Add(Path.Combine(currentDir, normalizedPath));
            candidates.Add(Path.Combine(currentDir, "workspace", normalizedPath));
            candidates.Add(Path.Combine(browserTempDir, normalizedPath));

            if (string.IsNullOrWhiteSpace(agent) && Directory.Exists(_path.AgentsRoot))
            {
                foreach (var agentDir in Directory.GetDirectories(_path.AgentsRoot))
                {
                    candidates.Add(Path.Combine(agentDir, "workspace", normalizedPath));
                }
            }
        }

        foreach (var candidate in candidates.Distinct(StringComparer.FromComparison(PathSafety.PathComparison)))
        {
            try
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
                // Ignore malformed candidate paths and keep trying the rest.
            }
        }

        return null;
    }

    private void AddAgentWorkspaceCandidates(List<string> candidates, string? agent, string requestedPath)
    {
        if (string.IsNullOrWhiteSpace(agent))
        {
            return;
        }

        string workspace;
        try
        {
            workspace = _path.GetWorkspacePath(NormalizeAgentName(agent));
        }
        catch (InvalidOperationException)
        {
            return;
        }
        candidates.Add(Path.Combine(workspace, requestedPath));

        const string workspacePrefix = "workspace";
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        foreach (var separator in separators)
        {
            var prefix = workspacePrefix + separator;
            if (requestedPath.StartsWith(prefix, PathSafety.PathComparison))
            {
                candidates.Add(Path.Combine(workspace, requestedPath[prefix.Length..]));
                break;
            }
        }
    }

    private static bool IsPathUnderAnyRoot(string path, IEnumerable<string> roots)
    {
        return PathSafety.IsUnderAnyRoot(path, roots);
    }

    private static bool IsPathUnderRoot(string path, string root)
    {
        return PathSafety.IsUnderRoot(path, root);
    }

    private static DateTimeOffset GetFileLastWriteUserTime(string path)
    {
        return UserTimeZoneService.ToUserTime(new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero));
    }

    private static async Task WriteEventAsync(HttpContext context, string type, object payload)
    {
        var json = JsonSerializer.Serialize(MergeEvent(type, payload), JsonOptions);
        await context.Response.WriteAsync(json + "\n", Encoding.UTF8, context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }

    private static async Task WriteEventsAsync(HttpContext context, ChannelReader<StreamEvent> events)
    {
        await foreach (var streamEvent in events.ReadAllAsync(context.RequestAborted))
        {
            await WriteEventAsync(context, streamEvent.Type, streamEvent.Payload);
        }
    }

    private static void QueueEvent(ChannelWriter<StreamEvent> events, string type, object payload)
    {
        events.TryWrite(new StreamEvent(type, payload));
    }

    private static string QueueSoundCueEvents(ChannelWriter<StreamEvent> events, string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var lastEnd = 0;
        foreach (Match match in SoundCueMarkerRegex.Matches(text))
        {
            var cue = match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(cue))
            {
                QueueEvent(events, "audio_cue", new { cue });
            }
            lastEnd = match.Index + match.Length;
        }

        var tail = lastEnd > 0 ? text[lastEnd..] : text;
        var markerStart = tail.LastIndexOf("{play", StringComparison.OrdinalIgnoreCase);
        if (markerStart >= 0)
        {
            return tail[markerStart..];
        }

        const int maxTail = 64;
        return tail.Length <= maxTail ? tail : tail[^maxTail..];
    }

    private static Dictionary<string, object?> MergeEvent(string type, object payload)
    {
        var result = new Dictionary<string, object?> { ["type"] = type };
        var node = JsonSerializer.SerializeToElement(payload, JsonOptions);
        foreach (var prop in node.EnumerateObject())
        {
            result[prop.Name] = prop.Value.Clone();
        }
        return result;
    }

    private sealed record StreamEvent(string Type, object Payload);

    private sealed class CreateAgentRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class CreateSessionRequest
    {
        public string Agent { get; set; } = string.Empty;
    }

    private sealed class AgentConfigRequest
    {
        public string Agent { get; set; } = string.Empty;
        public string? BaseUrl { get; set; }
        public string? ModelId { get; set; }
        public string? ApiType { get; set; }
        public string? ApiKey { get; set; }
        public int? ContextWindow { get; set; }
        public int? MaxOutputToken { get; set; }
        public int? MaxConcurrency { get; set; }
        public float? Temperature { get; set; }
    }

    private sealed class ChatRequest
    {
        public string Agent { get; set; } = string.Empty;
        public string Session { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool ContinueFromHostNotice { get; set; }
        public List<IFormFile> UploadFiles { get; set; } = new();
        public List<ChatAttachment> Attachments { get; set; } = new();
    }

    private sealed class MemorySaveRequest
    {
        public string Agent { get; set; } = string.Empty;
        public string? UserMd { get; set; }
        public string? IdentityMd { get; set; }
        public string? HotMemory { get; set; }
        public string? CoreMemory { get; set; }
    }

    private sealed class OrganizeRequest
    {
        public string Agent { get; set; } = string.Empty;
        public int HotMemoryLimit { get; set; }
        public int CoreMemoryLimit { get; set; }
        public int UserMdLimit { get; set; }
        public int IdentityMdLimit { get; set; }
        public bool ForceFullRebuild { get; set; }
    }

    private sealed class MemorySnapshotRestoreRequest
    {
        public string Agent { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
    }

    private sealed class RuntimeSupervisorRequest
    {
        public string Mode { get; set; } = RuntimeSupervisorService.ModeFragile;
        public string? Host { get; set; }
        public int Port { get; set; }
    }

    private sealed class WebAuthLoginRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    private sealed class UserTimeZoneRequest
    {
        public string TimeZone { get; set; } = string.Empty;
    }

    private sealed class SkillJobRequest
    {
        public string Agent { get; set; } = string.Empty;
        public string? SkillId { get; set; }
    }

    private async Task<BackgroundJobStartResult> TryStartManualAgentJobAsync(
        string agent,
        string resource,
        Func<string, CancellationToken, string> startJob,
        Func<string, bool> isRunning,
        CancellationToken requestCt,
        CancellationToken hostCt)
    {
        var safeAgent = NormalizeAgentName(agent);
        IDisposable? foregroundLease = null;
        IDisposable? resourceLease = null;
        CancellationTokenSource? linked = null;

        try
        {
            foregroundLease = _backgroundWork.BeginForegroundWork(safeAgent);
            resourceLease = await _backgroundWork.TryAcquireResourceAsync(safeAgent, resource, BackgroundWorkCoordinator.ResourceRetryTimeout, requestCt);
            if (resourceLease == null)
            {
                foregroundLease.Dispose();
                return BackgroundJobStartResult.Busy($"Resource '{resource}' is locked. Please retry later.");
            }

            linked = _backgroundWork.CreateAgentLinkedCancellation(safeAgent, hostCt);
            var jobId = startJob(safeAgent, linked.Token);

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!hostCt.IsCancellationRequested && isRunning(jobId))
                        await Task.Delay(TimeSpan.FromSeconds(2), hostCt);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    linked.Dispose();
                    resourceLease.Dispose();
                    foregroundLease.Dispose();
                }
            }, CancellationToken.None);

            return BackgroundJobStartResult.Success(jobId);
        }
        catch
        {
            linked?.Dispose();
            resourceLease?.Dispose();
            foregroundLease?.Dispose();
            throw;
        }
    }

    private sealed class BackgroundJobStartResult
    {
        public bool Started { get; private init; }
        public string? JobId { get; private init; }
        public string Message { get; private init; } = string.Empty;

        public static BackgroundJobStartResult Success(string jobId) => new() { Started = true, JobId = jobId };
        public static BackgroundJobStartResult Busy(string message) => new() { Started = false, Message = message };
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".bmp" => "image/bmp",
            ".ico" => "image/x-icon",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".md" or ".markdown" => "text/markdown",
            ".xml" => "application/xml",
            ".csv" => "text/csv",
            ".tsv" => "text/tab-separated-values",
            ".yaml" or ".yml" => "application/yaml",
            ".toml" => "application/toml",
            ".rtf" => "application/rtf",
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            ".tar" => "application/x-tar",
            ".gz" or ".tgz" => "application/gzip",
            ".bz2" => "application/x-bzip2",
            ".xz" => "application/x-xz",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".docm" => "application/vnd.ms-word.document.macroEnabled.12",
            ".odt" => "application/vnd.oasis.opendocument.text",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xlsm" => "application/vnd.ms-excel.sheet.macroEnabled.12",
            ".xlsb" => "application/vnd.ms-excel.sheet.binary.macroEnabled.12",
            ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".pptm" => "application/vnd.ms-powerpoint.presentation.macroEnabled.12",
            ".odp" => "application/vnd.oasis.opendocument.presentation",
            ".pages" => "application/vnd.apple.pages",
            ".numbers" => "application/vnd.apple.numbers",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".oga" => "audio/ogg",
            ".opus" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            ".webm" => "audio/webm",
            _ => "application/octet-stream"
        };
    }
}
