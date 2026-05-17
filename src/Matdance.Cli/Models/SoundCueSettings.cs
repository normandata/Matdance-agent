using System.Text.Json.Serialization;

namespace Matdance.Cli.Models;

public sealed class SoundCueSettings
{
    public bool Enabled { get; set; } = true;
    public double Volume { get; set; } = 0.65;
    public int DelayMs { get; set; } = 5000;
    public Dictionary<string, SoundCueTypeConfig> Types { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<SoundCueCustomType> CustomTypes { get; set; } = new();
}

public sealed class SoundCueTypeConfig
{
    public bool Enabled { get; set; } = true;
    public List<string> DisabledItemIds { get; set; } = new();
    public List<SoundCueAsset> Custom { get; set; } = new();
}

public sealed class SoundCueAsset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? RelativePath { get; set; }
    public string? Source { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }
}

public sealed class SoundCueCustomType
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public bool Custom { get; set; } = true;
}

public sealed class SoundCueSettingsSaveRequest
{
    public string Agent { get; set; } = string.Empty;
    public SoundCueSettings? Settings { get; set; }
}

public sealed class PromptSoundCueType
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Desc { get; init; } = string.Empty;
    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
}
