using System.Text.Json;
using System.Text.Json.Serialization;
using Matdance.Cli.Services;

namespace Matdance.Cli.Models;

public sealed class MultiModalConfigRoot
{
    [JsonPropertyName("global")]
    public MultiModalProfile Global { get; set; } = new();

    [JsonPropertyName("agents")]
    public Dictionary<string, MultiModalProfile> Agents { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MultiModalProfile
{
    [JsonPropertyName("image")]
    public ImageGenerationConfig Image { get; set; } = new();

    [JsonPropertyName("image_models")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ImageGenerationConfig>? ImageModels { get; set; }

    [JsonPropertyName("tts")]
    public TextToSpeechConfig Tts { get; set; } = new();

    [JsonPropertyName("tts_models")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TextToSpeechConfig>? TtsModels { get; set; }

    [JsonPropertyName("stt")]
    public SpeechToTextConfig Stt { get; set; } = new();

    [JsonPropertyName("search")]
    public WebSearchConfig Search { get; set; } = new();

    [JsonPropertyName("search_models")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WebSearchConfig>? SearchModels { get; set; }
}

public sealed class ImageGenerationConfig
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonPropertyName("base_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("endpoint_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndpointMode { get; set; }

    [JsonPropertyName("api_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ApiKey { get; set; }

    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Size { get; set; }

    [JsonPropertyName("quality")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Quality { get; set; }

    [JsonPropertyName("output_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OutputFormat { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class TextToSpeechConfig
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Mode { get; set; }

    [JsonPropertyName("auto_play")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AutoPlay { get; set; }

    [JsonPropertyName("base_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("endpoint_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndpointMode { get; set; }

    [JsonPropertyName("endpoint_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndpointPath { get; set; }

    [JsonPropertyName("api_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ApiKey { get; set; }

    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    [JsonPropertyName("voice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Voice { get; set; }

    [JsonPropertyName("language_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LanguageType { get; set; }

    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }

    [JsonPropertyName("optimize_instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OptimizeInstructions { get; set; }

    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class SpeechToTextConfig
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonPropertyName("send_after_transcription")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SendAfterTranscription { get; set; }

    [JsonPropertyName("base_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("endpoint_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndpointMode { get; set; }

    [JsonPropertyName("api_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ApiKey { get; set; }

    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }
}

public sealed class WebSearchConfig
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonPropertyName("provider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Provider { get; set; }

    [JsonPropertyName("base_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("endpoint_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndpointPath { get; set; }

    [JsonPropertyName("api_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ApiKey { get; set; }

    [JsonPropertyName("max_results")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxResults { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class EffectiveMultiModalConfig
{
    [JsonPropertyName("image")]
    public EffectiveImageGenerationConfig Image { get; set; } = new();

    [JsonPropertyName("image_models")]
    public List<EffectiveImageGenerationConfig> ImageModels { get; set; } = new();

    [JsonPropertyName("tts")]
    public EffectiveTextToSpeechConfig Tts { get; set; } = new();

    [JsonPropertyName("tts_models")]
    public List<EffectiveTextToSpeechConfig> TtsModels { get; set; } = new();

    [JsonPropertyName("stt")]
    public EffectiveSpeechToTextConfig Stt { get; set; } = new();

    [JsonPropertyName("search")]
    public EffectiveWebSearchConfig Search { get; set; } = new();

    [JsonPropertyName("search_models")]
    public List<EffectiveWebSearchConfig> SearchModels { get; set; } = new();
}

public sealed class EffectiveImageGenerationConfig
{
    public string Id { get; set; } = "default";
    public string Name { get; set; } = "Default image model";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "";
    public string EndpointMode { get; set; } = "native";
    [JsonIgnore]
    public string ApiKey { get; set; } = "";
    [JsonPropertyName("has_api_key")]
    public bool HasApiKey { get; set; }
    public string Model { get; set; } = "gpt-image-1";
    public string Size { get; set; } = "1024x1024";
    public string Quality { get; set; } = "auto";
    public string OutputFormat { get; set; } = "png";
}

public sealed class EffectiveTextToSpeechConfig
{
    public string Id { get; set; } = "default";
    public string Name { get; set; } = "Default TTS model";
    public string Mode { get; set; } = "off";
    public bool AutoPlay { get; set; }
    public string BaseUrl { get; set; } = "";
    public string EndpointMode { get; set; } = "native";
    public string EndpointPath { get; set; } = "audio/speech";
    [JsonIgnore]
    public string ApiKey { get; set; } = "";
    [JsonPropertyName("has_api_key")]
    public bool HasApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini-tts";
    public string Voice { get; set; } = "alloy";
    public string LanguageType { get; set; } = "Chinese";
    public string Instructions { get; set; } = "";
    public bool OptimizeInstructions { get; set; }
    public string Format { get; set; } = "mp3";
}

public sealed class EffectiveSpeechToTextConfig
{
    public bool Enabled { get; set; }
    public bool SendAfterTranscription { get; set; }
    public string BaseUrl { get; set; } = "";
    public string EndpointMode { get; set; } = "native";
    [JsonIgnore]
    public string ApiKey { get; set; } = "";
    [JsonPropertyName("has_api_key")]
    public bool HasApiKey { get; set; }
    public string Model { get; set; } = "whisper-1";
}

public sealed class EffectiveWebSearchConfig
{
    public string Id { get; set; } = "tavily";
    public string Name { get; set; } = "Tavily";
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "tavily";
    public string BaseUrl { get; set; } = "https://api.tavily.com";
    public string EndpointPath { get; set; } = "search";
    [JsonIgnore]
    public string ApiKey { get; set; } = "";
    [JsonPropertyName("has_api_key")]
    public bool HasApiKey { get; set; }
    public int MaxResults { get; set; } = 5;
}

public sealed class MultiModalSaveRequest
{
    public string? Agent { get; set; }
    public MultiModalProfile? Global { get; set; }
    public MultiModalProfile? AgentOverride { get; set; }
}

public sealed class ImageGenerationRequest
{
    public string Agent { get; set; } = string.Empty;
    public string? ImageProfile { get; set; }
    public bool AllowProfileFallback { get; set; } = true;
    public string? JobId { get; set; }
    public string? BatchId { get; set; }
    public string? Session { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Quality { get; set; }
    public string? OutputFormat { get; set; }
    public int Count { get; set; } = 1;
    public string? OutputPath { get; set; }
    public bool UseBrowserTemp { get; set; }
}

public sealed class TextToSpeechRequest
{
    public string Agent { get; set; } = string.Empty;
    public string? Session { get; set; }
    public int? MessageIndex { get; set; }
    public string? TtsProfile { get; set; }
    public bool AllowProfileFallback { get; set; } = true;
    public string Text { get; set; } = string.Empty;
    public string? Voice { get; set; }
    public string? Format { get; set; }
    public string? OutputPath { get; set; }
    public bool UseBrowserTemp { get; set; }
}

public sealed class WebSearchRequest
{
    public string Agent { get; set; } = string.Empty;
    public string? SearchProfile { get; set; }
    public bool AllowProfileFallback { get; set; } = true;
    public string Query { get; set; } = string.Empty;
    public int? MaxResults { get; set; }
}

public sealed class WebSearchResult
{
    public string Query { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? SearchProfileId { get; set; }
    public string? SearchProfileName { get; set; }
    public string? Answer { get; set; }
    public List<WebSearchResultItem> Items { get; set; } = new();
}

public sealed class WebSearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string? Source { get; set; }
    public double? Score { get; set; }
}

public sealed class GeneratedFileResult
{
    public string Path { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Format { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public string? BatchId { get; set; }
    public string? Prompt { get; set; }
    public string? RequestedImageProfile { get; set; }
    public bool? FallbackOccurred { get; set; }
    public string? ImageProfileId { get; set; }
    public string? ImageProfileName { get; set; }
    public string? TtsProfileId { get; set; }
    public string? TtsProfileName { get; set; }
    public string? Model { get; set; }
}

public sealed class ImageGenerationOutcome
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? ErrorType { get; set; }
    public string? ErrorCategory { get; set; }
    public bool FallbackOccurred { get; set; }
    public List<ImageGenerationAttempt> Attempts { get; set; } = new();
    public List<GeneratedFileResult> Results { get; set; } = new();
}

public sealed class ImageGenerationAttempt
{
    public int Order { get; set; }
    public string? ProfileId { get; set; }
    public string? ProfileName { get; set; }
    public string? Model { get; set; }
    public string Status { get; set; } = "pending";
    public string? Error { get; set; }
    public string? ErrorType { get; set; }
    public DateTimeOffset StartedAt { get; set; } = UserTimeZoneService.Now();
    public DateTimeOffset? FinishedAt { get; set; }
}

public sealed class ImageGenerationJob
{
    public string JobId { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;
    public string? Session { get; set; }
    public string Status { get; set; } = "queued";
    public string Prompt { get; set; } = string.Empty;
    public string? RequestedProfile { get; set; }
    public string? Size { get; set; }
    public string? Quality { get; set; }
    public string? OutputFormat { get; set; }
    public int Count { get; set; } = 1;
    public string? OutputPath { get; set; }
    public bool UseBrowserTemp { get; set; }
    public bool AllowProfileFallback { get; set; } = true;
    public bool FallbackOccurred { get; set; }
    public string? Error { get; set; }
    public string? ErrorType { get; set; }
    public string? ErrorCategory { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = UserTimeZoneService.Now();
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public List<ImageGenerationAttempt> Attempts { get; set; } = new();
    public List<GeneratedFileResult> Results { get; set; } = new();
}

public sealed class AudioAttachment
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("relative_path")]
    public string RelativePath { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = "mp3";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
