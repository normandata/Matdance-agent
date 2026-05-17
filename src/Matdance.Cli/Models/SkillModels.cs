using System.Text.Json;
using System.Text.Json.Serialization;

using Matdance.Cli.Services;

namespace Matdance.Cli.Models;

public class SkillItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = UserTimeZoneService.Now();

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = UserTimeZoneService.Now();
}

public class SkillCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class SkillEditRequest
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public string? Content { get; set; }
}

public class SkillLearnRequest
{
    public string Agent { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
    public List<string>? SourcePaths { get; set; }
    public string? SourceText { get; set; }
    public string? NameHint { get; set; }

    [JsonIgnore]
    public string? CleanupPath { get; set; }
}

public class SkillSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

public class SkillListResult
{
    [JsonPropertyName("skills")]
    public List<SkillSummary> Skills { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
