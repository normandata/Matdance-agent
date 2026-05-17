using System.Text.Json;
using System.Text.RegularExpressions;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public sealed class SoundCueSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly Regex CustomTypeIdRegex = new("^custom_[a-z0-9_]{1,56}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "reply_done", "thinking", "confused", "help", "confident", "low_confidence", "idea",
        "happy", "sad", "perfunctory", "considering", "working_hard", "tired", "energized",
        "angry", "relieved", "awkward", "surprised", "apologetic", "skeptical", "alert",
        "celebrate", "gentle", "playful"
    };

    public string SettingsPath => Path.Combine(MatdanceRuntime.StateRoot, "sound-cue-settings.json");

    public SoundCueSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return Normalize(new SoundCueSettings());

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return Normalize(JsonSerializer.Deserialize<SoundCueSettings>(json, JsonOptions) ?? new SoundCueSettings());
        }
        catch (JsonException)
        {
            return Normalize(new SoundCueSettings());
        }
        catch (IOException)
        {
            return Normalize(new SoundCueSettings());
        }
    }

    public SoundCueSettings Save(SoundCueSettings settings)
    {
        var normalized = Normalize(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        AtomicFile.WriteAllText(SettingsPath, JsonSerializer.Serialize(normalized, JsonOptions));
        return normalized;
    }

    public IReadOnlyList<PromptSoundCueType> GetPromptCustomTypes()
    {
        var settings = Load();
        return settings.CustomTypes
            .Where(type => HasPlayableCustomCue(settings, type.Id))
            .Select(type => new PromptSoundCueType
            {
                Id = type.Id,
                Name = type.Name,
                Desc = type.Desc,
                Aliases = type.Aliases
                    .Where(alias => !alias.Equals(type.Name, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(6)
                    .ToArray()
            })
            .ToArray();
    }

    private static SoundCueSettings Normalize(SoundCueSettings? source)
    {
        source ??= new SoundCueSettings();
        var result = new SoundCueSettings
        {
            Enabled = source.Enabled,
            Volume = Math.Clamp(source.Volume, 0.0, 1.0),
            DelayMs = Math.Clamp(source.DelayMs, 0, 30000)
        };

        var customIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceType in source.CustomTypes ?? new List<SoundCueCustomType>())
        {
            var id = NormalizeCustomTypeId(sourceType.Id);
            var name = TrimText(sourceType.Name, 80);
            if (id == null || string.IsNullOrWhiteSpace(name) || customIds.Contains(id))
                continue;

            var aliases = new List<string> { name };
            aliases.AddRange(sourceType.Aliases ?? new List<string>());

            result.CustomTypes.Add(new SoundCueCustomType
            {
                Id = id,
                Name = name,
                Desc = TrimText(sourceType.Desc, 240),
                Aliases = aliases
                    .Select(alias => TrimText(alias, 80))
                    .Where(alias => !string.IsNullOrWhiteSpace(alias))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(12)
                    .ToList(),
                Custom = true
            });
            customIds.Add(id);
        }

        foreach (var pair in source.Types ?? new Dictionary<string, SoundCueTypeConfig>(StringComparer.OrdinalIgnoreCase))
        {
            var typeId = NormalizeTypeId(pair.Key, customIds);
            if (typeId == null)
                continue;

            var config = pair.Value ?? new SoundCueTypeConfig();
            var disabled = (config.DisabledItemIds ?? new List<string>())
                .Select(id => TrimText(id, 160))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(256)
                .ToList();

            var assets = (config.Custom ?? new List<SoundCueAsset>())
                .Select(NormalizeAsset)
                .Where(asset => asset != null)
                .Cast<SoundCueAsset>()
                .Take(128)
                .ToList();

            result.Types[typeId] = new SoundCueTypeConfig
            {
                Enabled = config.Enabled,
                DisabledItemIds = disabled,
                Custom = assets
            };
        }

        foreach (var customId in customIds)
        {
            if (!result.Types.ContainsKey(customId))
                result.Types[customId] = new SoundCueTypeConfig();
        }

        return result;
    }

    private static SoundCueAsset? NormalizeAsset(SoundCueAsset? source)
    {
        if (source == null)
            return null;

        var url = TrimText(source.Url ?? string.Empty, 512);
        var relativePath = TrimText(source.RelativePath ?? string.Empty, 300);
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(relativePath))
            return null;

        return new SoundCueAsset
        {
            Id = TrimText(source.Id, 160),
            Name = TrimText(source.Name, 120),
            Url = string.IsNullOrWhiteSpace(url) ? null : url,
            RelativePath = string.IsNullOrWhiteSpace(relativePath) ? null : relativePath,
            Source = TrimText(source.Source ?? "custom", 40),
            Enabled = source.Enabled
        };
    }

    private static bool HasPlayableCustomCue(SoundCueSettings settings, string id)
    {
        if (!settings.Types.TryGetValue(id, out var config) || config.Enabled == false)
            return false;

        var disabled = new HashSet<string>(config.DisabledItemIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        return (config.Custom ?? new List<SoundCueAsset>()).Any(asset =>
            asset.Enabled != false
            && !string.IsNullOrWhiteSpace(asset.Id)
            && !disabled.Contains(asset.Id)
            && (!string.IsNullOrWhiteSpace(asset.Url) || !string.IsNullOrWhiteSpace(asset.RelativePath)));
    }

    private static string? NormalizeTypeId(string? value, HashSet<string> customIds)
    {
        var id = (value ?? string.Empty).Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        if (BuiltInTypes.Contains(id))
            return id;
        return customIds.Contains(id) ? id : null;
    }

    private static string? NormalizeCustomTypeId(string? value)
    {
        var id = (value ?? string.Empty).Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        return CustomTypeIdRegex.IsMatch(id) ? id : null;
    }

    private static string TrimText(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
