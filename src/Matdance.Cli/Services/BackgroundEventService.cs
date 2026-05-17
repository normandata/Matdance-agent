using System.Text.Json;
using System.Text.Json.Serialization;

namespace Matdance.Cli.Services;

public sealed class BackgroundEventService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private static readonly object Gate = new();
    private readonly PathService _path;

    public BackgroundEventService(PathService path)
    {
        _path = path;
    }

    public void Record(string agent, string category, string jobId, string kind, string status, string message, string? adviceKey = null)
    {
        if (string.IsNullOrWhiteSpace(agent)) return;
        if (!Directory.Exists(_path.GetAgentPath(agent))) return;

        var item = new BackgroundEventItem
        {
            Agent = agent,
            Category = category,
            JobId = jobId,
            Kind = kind,
            Status = status,
            Message = message,
            AdviceKey = adviceKey,
            Timestamp = UserTimeZoneService.Now()
        };

        var path = GetEventPath(agent, item.Timestamp);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = JsonSerializer.Serialize(item, JsonOptions);
        lock (Gate)
        {
            File.AppendAllText(path, line + Environment.NewLine);
            WriteJobState(agent, item);
        }
    }

    public IReadOnlyList<BackgroundEventItem> List(string agent, int take = 50)
    {
        take = Math.Clamp(take, 1, 500);
        var root = GetEventsRoot(agent);
        if (!Directory.Exists(root)) return Array.Empty<BackgroundEventItem>();

        var result = new List<BackgroundEventItem>();
        foreach (var file in Directory.GetFiles(root, "*.jsonl").OrderByDescending(Path.GetFileName))
        {
            foreach (var line in File.ReadLines(file).Reverse())
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var item = JsonSerializer.Deserialize<BackgroundEventItem>(line, JsonOptions);
                    if (item != null) result.Add(item);
                    if (result.Count >= take) return result;
                }
                catch
                {
                }
            }
        }
        return result;
    }

    public BackgroundEventDashboard GetDashboard(string agent, int take = 80)
    {
        var events = List(agent, take);
        var latest = LoadLatestJobStates(agent, events);
        var summary = new BackgroundEventSummary();

        foreach (var item in latest)
        {
            var bucket = ClassifyStatus(item.Status);
            if (bucket == BackgroundEventBucket.Completed) summary.Completed++;
            else if (bucket == BackgroundEventBucket.Skipped) summary.Skipped++;
            else if (bucket == BackgroundEventBucket.Failed) summary.Failed++;
            else summary.Unfinished++;
        }

        var remaining = latest
            .Where(item => ClassifyStatus(item.Status) == BackgroundEventBucket.Unfinished)
            .OrderByDescending(item => item.Timestamp)
            .ToList();

        summary.Total = events.Count;
        summary.TrackedJobs = latest.Count;
        summary.Remaining = remaining.Count;

        return new BackgroundEventDashboard
        {
            Events = events,
            Summary = summary,
            Remaining = remaining
        };
    }

    public int RecoverStaleJobs(string agent, DateTimeOffset cutoff, string reason)
    {
        if (string.IsNullOrWhiteSpace(agent) || !Directory.Exists(_path.GetAgentPath(agent)))
            return 0;

        var jobsRoot = Path.Combine(_path.GetAgentPath(agent), "runtime", "jobs");
        if (!Directory.Exists(jobsRoot))
            return 0;

        var recovered = 0;
        foreach (var file in Directory.GetFiles(jobsRoot, "*.json"))
        {
            BackgroundEventItem? item = null;
            try { item = JsonSerializer.Deserialize<BackgroundEventItem>(File.ReadAllText(file), JsonOptions); }
            catch { }
            if (item == null || string.IsNullOrWhiteSpace(item.JobId))
                continue;
            if (ClassifyStatus(item.Status) != BackgroundEventBucket.Unfinished)
                continue;
            if (item.Timestamp > cutoff)
                continue;

            var message = reason + " Last known state: [" + item.Status + "] " + item.Message;
            Record(agent, item.Category, item.JobId, item.Kind, "interrupted", message, "retry_manual");
            recovered++;
        }
        return recovered;
    }

    public int RecoverStaleJobsForAllAgents(DateTimeOffset cutoff, string reason)
    {
        if (!Directory.Exists(_path.AgentsRoot))
            return 0;

        var recovered = 0;
        foreach (var dir in Directory.GetDirectories(_path.AgentsRoot))
        {
            var agent = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(agent))
                continue;
            recovered += RecoverStaleJobs(agent, cutoff, reason);
        }
        return recovered;
    }

    private string GetEventPath(string agent, DateTimeOffset timestamp)
        => Path.Combine(GetEventsRoot(agent), timestamp.ToString("yyyyMMdd") + ".jsonl");

    private string GetEventsRoot(string agent)
        => Path.Combine(_path.GetAgentPath(agent), "runtime", "events");

    private void WriteJobState(string agent, BackgroundEventItem item)
    {
        if (string.IsNullOrWhiteSpace(item.JobId)) return;
        var jobsRoot = Path.Combine(_path.GetAgentPath(agent), "runtime", "jobs");
        Directory.CreateDirectory(jobsRoot);
        var safeId = string.Concat(item.JobId.Select(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' ? ch : '_'));
        var path = Path.Combine(jobsRoot, safeId + ".json");
        AtomicFile.WriteAllText(path, JsonSerializer.Serialize(item, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
    }

    private IReadOnlyList<BackgroundEventItem> LoadLatestJobStates(string agent, IReadOnlyList<BackgroundEventItem> fallbackEvents)
    {
        var latest = new Dictionary<string, BackgroundEventItem>(StringComparer.OrdinalIgnoreCase);
        var jobsRoot = Path.Combine(_path.GetAgentPath(agent), "runtime", "jobs");
        if (Directory.Exists(jobsRoot))
        {
            foreach (var file in Directory.GetFiles(jobsRoot, "*.json"))
            {
                try
                {
                    var item = JsonSerializer.Deserialize<BackgroundEventItem>(File.ReadAllText(file), JsonOptions);
                    if (item != null && !string.IsNullOrWhiteSpace(item.JobId))
                        latest[JobKey(item)] = item;
                }
                catch
                {
                }
            }
        }

        foreach (var item in fallbackEvents)
        {
            if (string.IsNullOrWhiteSpace(item.JobId)) continue;
            var key = JobKey(item);
            if (!latest.TryGetValue(key, out var existing) || item.Timestamp > existing.Timestamp)
                latest[key] = item;
        }

        return latest.Values.ToList();
    }

    private static string JobKey(BackgroundEventItem item)
        => string.Join("|", item.Agent, item.Category, item.JobId, item.Kind);

    public static BackgroundEventBucket ClassifyStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant().Replace("_", "-");
        return normalized switch
        {
            "completed" or "complete" or "succeeded" or "success" or "done" or "delivered" => BackgroundEventBucket.Completed,
            "skipped" or "skip" or "no-op" or "noop" => BackgroundEventBucket.Skipped,
            "failed" or "failure" or "error" or "errored" or "canceled" or "cancelled" or "rejected" or "interrupted" or "stalled" => BackgroundEventBucket.Failed,
            _ => BackgroundEventBucket.Unfinished
        };
    }
}

public enum BackgroundEventBucket
{
    Completed,
    Unfinished,
    Skipped,
    Failed
}

public sealed class BackgroundEventDashboard
{
    [JsonPropertyName("events")]
    public IReadOnlyList<BackgroundEventItem> Events { get; set; } = Array.Empty<BackgroundEventItem>();

    [JsonPropertyName("summary")]
    public BackgroundEventSummary Summary { get; set; } = new();

    [JsonPropertyName("remaining")]
    public IReadOnlyList<BackgroundEventItem> Remaining { get; set; } = Array.Empty<BackgroundEventItem>();
}

public sealed class BackgroundEventSummary
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("tracked_jobs")]
    public int TrackedJobs { get; set; }

    [JsonPropertyName("completed")]
    public int Completed { get; set; }

    [JsonPropertyName("unfinished")]
    public int Unfinished { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("remaining")]
    public int Remaining { get; set; }
}

public sealed class BackgroundEventItem
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = UserTimeZoneService.Now();

    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("job_id")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("advice_key")]
    public string? AdviceKey { get; set; }
}
