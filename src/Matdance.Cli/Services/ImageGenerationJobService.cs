using System.Collections.Concurrent;
using System.Text.Json;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public sealed class ImageGenerationJobService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> CancellationTokens = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();
    private readonly PathService _path;
    private readonly BackgroundEventService _events;

    public ImageGenerationJobService(PathService path)
    {
        _path = path;
        _events = new BackgroundEventService(path);
    }

    public ImageGenerationJob Start(string agent, ImageGenerationRequest request, string? session = null)
    {
        if (!Directory.Exists(_path.GetAgentPath(agent)))
            throw new InvalidOperationException($"Agent '{agent}' does not exist.");
        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new InvalidOperationException("prompt is required.");

        var jobId = NewId("img");
        var batchId = NormalizeOptionalId(request.BatchId, "batch") ?? NewId("batch");
        var job = new ImageGenerationJob
        {
            JobId = jobId,
            BatchId = batchId,
            Agent = agent,
            Session = string.IsNullOrWhiteSpace(session) ? request.Session : session,
            Status = "queued",
            Prompt = request.Prompt.Trim(),
            RequestedProfile = request.ImageProfile,
            Size = request.Size,
            Quality = request.Quality,
            OutputFormat = request.OutputFormat,
            Count = Math.Clamp(request.Count <= 0 ? 1 : request.Count, 1, 4),
            OutputPath = request.OutputPath,
            UseBrowserTemp = request.UseBrowserTemp,
            AllowProfileFallback = request.AllowProfileFallback,
            CreatedAt = UserTimeZoneService.Now()
        };

        Save(agent, job);
        _events.Record(agent, "image_generation", job.JobId, "image_generation", "queued", $"Queued image generation job {job.JobId} in batch {job.BatchId}.", "wait_for_completion");

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(25));
        CancellationTokens[job.JobId] = cts;
        _ = Task.Run(() => RunAsync(job.JobId, cts.Token));
        return job;
    }

    public IReadOnlyList<ImageGenerationJob> Query(string agent, string? jobId = null, string? batchId = null, int take = 20)
    {
        take = Math.Clamp(take, 1, 200);
        var jobs = new List<ImageGenerationJob>();
        if (!Directory.Exists(GetJobsRoot(agent)))
            return jobs;

        var normalizedJob = NormalizeOptionalId(jobId, "img");
        var normalizedBatch = NormalizeOptionalId(batchId, "batch");
        foreach (var file in Directory.GetFiles(GetJobsRoot(agent), "*.json"))
        {
            try
            {
                var job = JsonSerializer.Deserialize<ImageGenerationJob>(File.ReadAllText(file), JsonOptions);
                if (job == null) continue;
                if (normalizedJob != null && !string.Equals(job.JobId, normalizedJob, StringComparison.OrdinalIgnoreCase)) continue;
                if (normalizedBatch != null && !string.Equals(job.BatchId, normalizedBatch, StringComparison.OrdinalIgnoreCase)) continue;
                jobs.Add(job);
            }
            catch
            {
            }
        }

        return jobs
            .OrderByDescending(job => job.CreatedAt)
            .Take(take)
            .ToList();
    }

    public ImageGenerationJob? Get(string agent, string jobId)
        => Query(agent, jobId: jobId, take: 1).FirstOrDefault();

    public IReadOnlyList<ImageGenerationJob> Cancel(string agent, string? jobId = null, string? batchId = null)
    {
        var targets = Query(agent, jobId, batchId, take: 200)
            .Where(job => job.Status is "queued" or "running")
            .ToList();

        foreach (var job in targets)
        {
            if (CancellationTokens.TryGetValue(job.JobId, out var cts))
            {
                try { cts.Cancel(); } catch { }
            }
            else
            {
                job.Status = "canceled";
                job.Error = "Canceled before the runtime could find an active worker.";
                job.ErrorType = "OperationCanceledException";
                job.ErrorCategory = "canceled";
                job.FinishedAt = UserTimeZoneService.Now();
                Save(agent, job);
                NotifySession(job);
            }
        }

        return targets;
    }

    private async Task RunAsync(string jobId, CancellationToken ct)
    {
        ImageGenerationJob? job = null;
        try
        {
            job = FindJobById(jobId);
            if (job == null) return;
            if (job.Status == "canceled") return;

            job.Status = "running";
            job.StartedAt = UserTimeZoneService.Now();
            Save(job.Agent, job);
            _events.Record(job.Agent, "image_generation", job.JobId, "image_generation", "running", $"Running image generation job {job.JobId}.", "wait_for_completion");

            var request = new ImageGenerationRequest
            {
                Agent = job.Agent,
                JobId = job.JobId,
                BatchId = job.BatchId,
                Session = job.Session,
                ImageProfile = job.RequestedProfile,
                AllowProfileFallback = job.AllowProfileFallback,
                Prompt = job.Prompt,
                Size = job.Size,
                Quality = job.Quality,
                OutputFormat = job.OutputFormat,
                Count = job.Count,
                OutputPath = job.OutputPath,
                UseBrowserTemp = job.UseBrowserTemp
            };

            var outcome = await new MultiModalClient(_path).GenerateImageDetailedAsync(job.Agent, request, ct);
            job.Attempts = outcome.Attempts;
            job.Results = outcome.Results;
            job.FallbackOccurred = outcome.FallbackOccurred;
            if (outcome.Success)
            {
                job.Status = "succeeded";
            }
            else
            {
                job.Status = "failed";
                job.Error = outcome.Error;
                job.ErrorType = outcome.ErrorType;
                job.ErrorCategory = outcome.ErrorCategory;
            }
        }
        catch (OperationCanceledException ex)
        {
            if (job != null)
            {
                job.Status = "canceled";
                job.Error = ex.Message;
                job.ErrorType = ex.GetType().Name;
                job.ErrorCategory = "canceled";
            }
        }
        catch (Exception ex)
        {
            if (job != null)
            {
                job.Status = "failed";
                job.Error = ex.Message;
                job.ErrorType = ex.GetType().Name;
                job.ErrorCategory = MultiModalClient.ClassifyImageGenerationError(ex);
            }
        }
        finally
        {
            CancellationTokens.TryRemove(jobId, out var cts);
            cts?.Dispose();
            if (job != null)
            {
                job.FinishedAt = UserTimeZoneService.Now();
                Save(job.Agent, job);
                _events.Record(job.Agent, "image_generation", job.JobId, "image_generation", job.Status, BuildEventMessage(job), job.Status == "succeeded" ? "review_result" : "review_error");
                NotifySession(job);
            }
        }
    }

    private ImageGenerationJob? FindJobById(string jobId)
    {
        if (!Directory.Exists(_path.AgentsRoot))
            return null;

        foreach (var agentDir in Directory.GetDirectories(_path.AgentsRoot))
        {
            var agent = Path.GetFileName(agentDir);
            if (string.IsNullOrWhiteSpace(agent)) continue;
            var path = GetJobPath(agent, jobId);
            if (!File.Exists(path)) continue;
            try { return JsonSerializer.Deserialize<ImageGenerationJob>(File.ReadAllText(path), JsonOptions); }
            catch { return null; }
        }

        return null;
    }

    private void NotifySession(ImageGenerationJob job)
    {
        if (string.IsNullOrWhiteSpace(job.Session))
            return;

        try
        {
            var sessionFile = _path.GetSessionJsonPath(job.Agent, job.Session);
            if (!File.Exists(sessionFile))
                return;

            var batchJobs = Query(job.Agent, batchId: job.BatchId, take: 200);
            var notice = new ChatMessage
            {
                Role = "assistant",
                Content = BuildSessionNotice(job, batchJobs),
                MessageType = "image_generation_notice",
                IncludeInMainContext = true,
                Importance = "notification",
                Timestamp = UserTimeZoneService.Now()
            };

            if (SessionHostNoticeHub.PublishIfActive(job.Agent, job.Session, notice))
                return;

            var data = SessionData.Load(sessionFile);
            var state = SessionState.Load(sessionFile);
            state.Messages.Add(notice);
            data.TotalMessages++;
            data.LastActivity = UserTimeZoneService.Now();
            lock (Gate)
            {
                data.Save(sessionFile);
                state.Save(sessionFile);
            }
        }
        catch
        {
        }
    }

    private static string BuildEventMessage(ImageGenerationJob job)
    {
        var count = job.Results.Count;
        var fallback = job.FallbackOccurred ? " Fallback occurred." : string.Empty;
        return job.Status == "succeeded"
            ? $"Image generation job {job.JobId} succeeded with {count} file(s).{fallback}"
            : $"Image generation job {job.JobId} ended as {job.Status}: {job.Error ?? job.ErrorCategory ?? "unknown"}.";
    }

    private static string BuildSessionNotice(ImageGenerationJob job, IReadOnlyList<ImageGenerationJob> batchJobs)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Image Generation Host Notice");
        sb.AppendLine();
        sb.AppendLine($"Job ID: {job.JobId}");
        sb.AppendLine($"Batch ID: {job.BatchId}");
        sb.AppendLine($"Status: {job.Status}");
        if (batchJobs.Count > 0)
        {
            var active = batchJobs.Count(item => item.Status is "queued" or "running");
            var succeeded = batchJobs.Count(item => item.Status == "succeeded");
            var failed = batchJobs.Count(item => item.Status == "failed");
            var canceled = batchJobs.Count(item => item.Status == "canceled");
            sb.AppendLine(active == 0
                ? $"Batch status: complete ({succeeded} succeeded, {failed} failed, {canceled} canceled)"
                : $"Batch status: active ({active} queued/running, {succeeded} succeeded, {failed} failed, {canceled} canceled)");
        }
        sb.AppendLine($"Prompt: {job.Prompt}");
        if (!string.IsNullOrWhiteSpace(job.RequestedProfile)) sb.AppendLine($"Requested profile: {job.RequestedProfile}");
        if (job.FallbackOccurred) sb.AppendLine("Provider fallback: yes");
        var finalProfiles = job.Results
            .Select(result => result.ImageProfileName ?? result.ImageProfileId ?? result.Model)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (finalProfiles.Count > 0) sb.AppendLine("Final provider/model: " + string.Join(", ", finalProfiles));
        if (!string.IsNullOrWhiteSpace(job.Error)) sb.AppendLine($"Error: {job.Error}");
        if (!string.IsNullOrWhiteSpace(job.ErrorCategory)) sb.AppendLine($"Error category: {job.ErrorCategory}");
        if (job.Results.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Generated files:");
            foreach (var result in job.Results)
                sb.AppendLine($"- {result.RelativePath} (prompt: {result.Prompt ?? job.Prompt})");
            sb.AppendLine();
            sb.AppendLine("{show_file:" + string.Join(", ", job.Results.Select(result => result.RelativePath)) + "}");
        }
        var activeBatchJobs = batchJobs.Count(item => item.Status is "queued" or "running");
        var batchResults = batchJobs
            .SelectMany(item => item.Results.Select(result => new { Job = item, Result = result }))
            .Where(item => !string.IsNullOrWhiteSpace(item.Result.RelativePath))
            .ToList();
        if (activeBatchJobs == 0 && batchResults.Count > job.Results.Count)
        {
            sb.AppendLine();
            sb.AppendLine("Batch generated files:");
            foreach (var item in batchResults)
                sb.AppendLine($"- {item.Result.RelativePath} (job: {item.Job.JobId}, prompt: {item.Result.Prompt ?? item.Job.Prompt})");
            sb.AppendLine();
            sb.AppendLine("{show_file:" + string.Join(", ", batchResults.Select(item => item.Result.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase)) + "}");
        }
        sb.AppendLine();
        sb.AppendLine("This notice is authoritative host state for this image job.");
        return sb.ToString();
    }

    private void Save(string agent, ImageGenerationJob job)
    {
        Directory.CreateDirectory(GetJobsRoot(agent));
        lock (Gate)
        {
            AtomicFile.WriteAllText(GetJobPath(agent, job.JobId), JsonSerializer.Serialize(job, JsonOptions));
        }
    }

    private string GetJobsRoot(string agent)
        => Path.Combine(_path.GetAgentPath(agent), "runtime", "image_generation", "jobs");

    private string GetJobPath(string agent, string jobId)
        => Path.Combine(GetJobsRoot(agent), NormalizeOptionalId(jobId, "img") ?? jobId) + ".json";

    private static string NewId(string prefix)
        => prefix + "_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "_" + Guid.NewGuid().ToString("N")[..8];

    private static string? NormalizeOptionalId(string? value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var safe = string.Concat(value.Trim().Select(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' ? ch : '_')).Trim('_', '-');
        return string.IsNullOrWhiteSpace(safe) ? null : safe;
    }
}
