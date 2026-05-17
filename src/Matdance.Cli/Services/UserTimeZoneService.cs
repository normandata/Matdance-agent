using System.Text.Json;

namespace Matdance.Cli.Services;

public static class UserTimeZoneService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly object Gate = new();

    public static string StatePath => Path.Combine(MatdanceRuntime.StateRoot, "user-time-zone.json");

    public static string GetDefaultTimeZoneId()
    {
        var env = Environment.GetEnvironmentVariable("MATDANCE_TIME_ZONE");
        if (TryFindZone(env, out _))
            return env!.Trim();

        lock (Gate)
        {
            var state = ReadState();
            if (TryFindZone(state?.TimeZone, out _))
                return state!.TimeZone.Trim();
        }

        return TimeZoneInfo.Local.Id;
    }

    public static UserTimeZoneSnapshot GetSnapshot()
    {
        var id = GetDefaultTimeZoneId();
        var zone = FindZone(id);
        return new UserTimeZoneSnapshot
        {
            TimeZone = id,
            ResolvedTimeZone = zone.Id,
            Offset = FormatOffset(zone.GetUtcOffset(DateTimeOffset.UtcNow)),
            Now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone)
        };
    }

    public static string SetDefaultTimeZone(string? timeZone)
    {
        var forced = Environment.GetEnvironmentVariable("MATDANCE_TIME_ZONE");
        if (TryFindZone(forced, out _))
            return forced!.Trim();

        var id = NormalizeTimeZoneId(timeZone);
        lock (Gate)
        {
            Directory.CreateDirectory(MatdanceRuntime.StateRoot);
            AtomicFile.WriteAllText(StatePath, JsonSerializer.Serialize(new UserTimeZoneState
            {
                TimeZone = id,
                UpdatedAt = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, FindZone(id))
            }, JsonOptions));
        }
        return id;
    }

    public static DateTimeOffset Now()
    {
        var zone = FindZone(GetDefaultTimeZoneId());
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
    }

    public static DateTimeOffset ToUserTime(DateTimeOffset instant)
    {
        var zone = FindZone(GetDefaultTimeZoneId());
        return TimeZoneInfo.ConvertTime(instant, zone);
    }

    public static string NormalizeTimeZoneId(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return GetDefaultTimeZoneId();

        var id = timeZone.Trim();
        return TryFindZone(id, out _) ? id : GetDefaultTimeZoneId();
    }

    public static TimeZoneInfo FindZone(string? timeZone)
    {
        if (TryFindZone(timeZone, out var zone))
            return zone;

        var fallback = GetDefaultTimeZoneId();
        if (!string.Equals(fallback, timeZone, StringComparison.OrdinalIgnoreCase)
            && TryFindZone(fallback, out zone))
            return zone;

        return TimeZoneInfo.Local;
    }

    private static bool TryFindZone(string? timeZone, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Local;
        if (string.IsNullOrWhiteSpace(timeZone))
            return false;

        var id = timeZone.Trim();
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch
        {
        }

        if (string.Equals(id, "Asia/Shanghai", StringComparison.OrdinalIgnoreCase))
            return TryFindSystemZone("China Standard Time", out zone);
        if (string.Equals(id, "China Standard Time", StringComparison.OrdinalIgnoreCase))
            return TryFindSystemZone("Asia/Shanghai", out zone);
        if (string.Equals(id, "Etc/UTC", StringComparison.OrdinalIgnoreCase))
            return TryFindSystemZone("UTC", out zone);

        try
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId)
                && TryFindSystemZone(windowsId, out zone))
                return true;
        }
        catch
        {
        }

        try
        {
            if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId)
                && TryFindSystemZone(ianaId, out zone))
                return true;
        }
        catch
        {
        }

        return false;
    }

    private static bool TryFindSystemZone(string id, out TimeZoneInfo zone)
    {
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch
        {
            zone = TimeZoneInfo.Local;
            return false;
        }
    }

    private static UserTimeZoneState? ReadState()
    {
        try
        {
            if (!File.Exists(StatePath))
                return null;

            return JsonSerializer.Deserialize<UserTimeZoneState>(File.ReadAllText(StatePath), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string FormatOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        offset = offset.Duration();
        return sign + offset.ToString(@"hh\:mm");
    }

    private sealed class UserTimeZoneState
    {
        public string TimeZone { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; }
    }
}

public sealed class UserTimeZoneSnapshot
{
    public string TimeZone { get; set; } = string.Empty;
    public string ResolvedTimeZone { get; set; } = string.Empty;
    public string Offset { get; set; } = string.Empty;
    public DateTimeOffset Now { get; set; }
}
