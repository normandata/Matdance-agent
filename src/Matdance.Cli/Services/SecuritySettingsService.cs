using System.Text.Json;

namespace Matdance.Cli.Services;

public sealed class SecuritySettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly object Gate = new();

    public static string StatePath => Path.Combine(MatdanceRuntime.StateRoot, "security-settings.json");

    public SecuritySettings Load()
    {
        lock (Gate)
        {
            return LoadUnlocked();
        }
    }

    public SecuritySettings Save(SecuritySettings settings)
    {
        lock (Gate)
        {
            var previous = LoadUnlocked();
            settings ??= new SecuritySettings();
            var revoked = previous.AllowPrivateDataAccess && !settings.AllowPrivateDataAccess;
            var enabled = !previous.AllowPrivateDataAccess && settings.AllowPrivateDataAccess;
            settings.UpdatedAt = UserTimeZoneService.Now();
            settings.PrivacyAccessRevokedNoticePending = revoked || (previous.PrivacyAccessRevokedNoticePending && !settings.AllowPrivateDataAccess);
            settings.PrivacyAccessRevokedAt = revoked
                ? settings.UpdatedAt
                : enabled
                    ? null
                    : previous.PrivacyAccessRevokedAt;
            SaveUnlocked(settings);
            return settings;
        }
    }

    public string? ConsumePrivacyAccessRevokedNotice()
    {
        lock (Gate)
        {
            var settings = LoadUnlocked();
            if (!settings.PrivacyAccessRevokedNoticePending || settings.AllowPrivateDataAccess)
            {
                if (settings.PrivacyAccessRevokedNoticePending)
                {
                    settings.PrivacyAccessRevokedNoticePending = false;
                    settings.PrivacyAccessRevokedAt = null;
                    SaveUnlocked(settings);
                }

                return null;
            }

            settings.PrivacyAccessRevokedNoticePending = false;
            SaveUnlocked(settings);
            var revokedAt = settings.PrivacyAccessRevokedAt.HasValue
                ? UserTimeZoneService.ToUserTime(settings.PrivacyAccessRevokedAt.Value).ToString("yyyy-MM-dd HH:mm:ss zzz")
                : "recently";
            return "Internal runtime notice for this turn only. Privacy Access was revoked at " + revokedAt + ". "
                + "Do not repeat or summarize this notice to the user. The live authorization state has already been synchronized into the Non-Negotiable Security Constitution for this request. "
                + "For all future reasoning and tool decisions, treat the current Global privacy access switch shown in the system prompt as the only authority. "
                + "Do not use tools or commands to test whether private data access might still work.";
        }
    }

    private static SecuritySettings LoadUnlocked()
    {
        if (!File.Exists(StatePath))
            return new SecuritySettings();

        try
        {
            return JsonSerializer.Deserialize<SecuritySettings>(File.ReadAllText(StatePath), JsonOptions) ?? new SecuritySettings();
        }
        catch
        {
            return new SecuritySettings();
        }
    }

    private static void SaveUnlocked(SecuritySettings settings)
    {
        Directory.CreateDirectory(MatdanceRuntime.StateRoot);
        AtomicFile.WriteAllText(StatePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}

public sealed class SecuritySettings
{
    public bool AllowPrivateDataAccess { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool PrivacyAccessRevokedNoticePending { get; set; }
    public DateTimeOffset? PrivacyAccessRevokedAt { get; set; }
}
