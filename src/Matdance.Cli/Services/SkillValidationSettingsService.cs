using System.Text.Json;

namespace Matdance.Cli.Services;

public sealed class SkillValidationSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly object Gate = new();

    public static string StatePath => Path.Combine(MatdanceRuntime.StateRoot, "skill-validation-settings.json");

    public SkillValidationSettings Load()
    {
        lock (Gate)
        {
            if (!File.Exists(StatePath))
                return Normalize(new SkillValidationSettings());

            try
            {
                return Normalize(JsonSerializer.Deserialize<SkillValidationSettings>(File.ReadAllText(StatePath), JsonOptions) ?? new SkillValidationSettings());
            }
            catch
            {
                return Normalize(new SkillValidationSettings());
            }
        }
    }

    public SkillValidationSettings Save(SkillValidationSettings settings)
    {
        lock (Gate)
        {
            var normalized = Normalize(settings);
            normalized.UpdatedAt = UserTimeZoneService.Now();
            Directory.CreateDirectory(MatdanceRuntime.StateRoot);
            AtomicFile.WriteAllText(StatePath, JsonSerializer.Serialize(normalized, JsonOptions));
            return normalized;
        }
    }

    public static SkillValidationSettings Normalize(SkillValidationSettings? settings)
    {
        settings ??= new SkillValidationSettings();
        return new SkillValidationSettings
        {
            AutoSkillValidationEnabled = settings.AutoSkillValidationEnabled,
            AutoSkillValidationIntervalHours = Math.Clamp(settings.AutoSkillValidationIntervalHours <= 0 ? 6 : settings.AutoSkillValidationIntervalHours, 1, 168),
            AutoSkillValidationBatchSize = Math.Clamp(settings.AutoSkillValidationBatchSize <= 0 ? 1 : settings.AutoSkillValidationBatchSize, 1, 3),
            UpdatedAt = settings.UpdatedAt
        };
    }
}

public sealed class SkillValidationSettings
{
    public bool AutoSkillValidationEnabled { get; set; } = true;
    public int AutoSkillValidationIntervalHours { get; set; } = 6;
    public int AutoSkillValidationBatchSize { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; }
}
