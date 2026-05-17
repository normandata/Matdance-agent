using System.Text.Json;
using System.Text.Json.Serialization;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public static class ModelCapabilityCacheService
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private const int AmbiguousImageFailureThreshold = 3;

    public static string StatePath => Path.Combine(MatdanceRuntime.StateRoot, "model-capabilities.json");

    public static bool? GetVisionSupport(AgentConfig config)
    {
        lock (Gate)
        {
            var state = ReadState();
            return state.Models.TryGetValue(Key(config), out var record) ? record.VisionSupported : null;
        }
    }

    public static string? GetAnthropicMessagesUrl(AgentConfig config)
    {
        lock (Gate)
        {
            var state = ReadState();
            return state.Models.TryGetValue(Key(config), out var record)
                && !string.IsNullOrWhiteSpace(record.AnthropicMessagesUrl)
                ? record.AnthropicMessagesUrl
                : null;
        }
    }

    public static void RecordVisionSupported(AgentConfig config)
    {
        lock (Gate)
        {
            var state = ReadState();
            var record = GetOrCreate(state, config);
            record.VisionSupported = true;
            record.AmbiguousImageFailures = 0;
            record.LastReason = "Image payload accepted by the upstream model/API.";
            record.UpdatedAt = UserTimeZoneService.Now();
            WriteState(state);
        }
    }

    public static void RecordVisionUnsupported(AgentConfig config, string reason)
    {
        lock (Gate)
        {
            var state = ReadState();
            var record = GetOrCreate(state, config);
            record.VisionSupported = false;
            record.AmbiguousImageFailures = Math.Max(record.AmbiguousImageFailures, AmbiguousImageFailureThreshold);
            record.LastReason = TrimReason(reason);
            record.UpdatedAt = UserTimeZoneService.Now();
            WriteState(state);
        }
    }

    public static void RecordAmbiguousImageFailureWithTextOnlySuccess(AgentConfig config, string reason)
    {
        lock (Gate)
        {
            var state = ReadState();
            var record = GetOrCreate(state, config);
            record.AmbiguousImageFailures++;
            record.LastReason = TrimReason(reason);
            if (record.AmbiguousImageFailures >= AmbiguousImageFailureThreshold)
            {
                record.VisionSupported = false;
                record.LastReason = "Image payload repeatedly failed while text-only retry succeeded. " + record.LastReason;
            }
            record.UpdatedAt = UserTimeZoneService.Now();
            WriteState(state);
        }
    }

    public static void RecordAnthropicMessagesEndpointSuccess(AgentConfig config, string endpoint)
    {
        lock (Gate)
        {
            var state = ReadState();
            var record = GetOrCreate(state, config);
            record.AnthropicMessagesUrl = endpoint.Trim().TrimEnd('/');
            record.AnthropicTextSupported = true;
            record.AnthropicTextLastReason = "Anthropic-compatible text request succeeded.";
            record.UpdatedAt = UserTimeZoneService.Now();
            WriteState(state);
        }
    }

    public static void RecordAnthropicMessagesEndpointNotFound(AgentConfig config, string reason)
    {
        lock (Gate)
        {
            var state = ReadState();
            var record = GetOrCreate(state, config);
            record.AnthropicMessagesUrl = null;
            record.AnthropicTextSupported = false;
            record.AnthropicTextLastReason = TrimReason(reason);
            record.UpdatedAt = UserTimeZoneService.Now();
            WriteState(state);
        }
    }

    private static CapabilityRecord GetOrCreate(CapabilityState state, AgentConfig config)
    {
        var key = Key(config);
        if (!state.Models.TryGetValue(key, out var record))
        {
            record = new CapabilityRecord
            {
                ApiType = config.ApiType,
                BaseUrl = NormalizeBaseUrl(config.BaseUrl),
                ModelId = config.ModelId
            };
            state.Models[key] = record;
        }

        record.ApiType = config.ApiType;
        record.BaseUrl = NormalizeBaseUrl(config.BaseUrl);
        record.ModelId = config.ModelId;
        return record;
    }

    private static CapabilityState ReadState()
    {
        try
        {
            if (!File.Exists(StatePath))
                return new CapabilityState();

            return JsonSerializer.Deserialize<CapabilityState>(File.ReadAllText(StatePath), JsonOptions)
                ?? new CapabilityState();
        }
        catch
        {
            return new CapabilityState();
        }
    }

    private static void WriteState(CapabilityState state)
    {
        state.UpdatedAt = UserTimeZoneService.Now();
        AtomicFile.WriteAllText(StatePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    private static string Key(AgentConfig config)
        => string.Join("|",
            Normalize(config.ApiType),
            Normalize(NormalizeBaseUrl(config.BaseUrl)),
            Normalize(config.ModelId));

    private static string NormalizeBaseUrl(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? AgentConfig.DefaultBaseUrl.TrimEnd('/')
            : value.Trim().TrimEnd('/');

    private static string Normalize(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static string TrimReason(string? reason)
    {
        var value = string.IsNullOrWhiteSpace(reason) ? "No reason recorded." : reason.Trim();
        return value.Length <= 1600 ? value : value[..1600] + "...";
    }

    private sealed class CapabilityState
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = UserTimeZoneService.Now();

        [JsonPropertyName("models")]
        public Dictionary<string, CapabilityRecord> Models { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class CapabilityRecord
    {
        [JsonPropertyName("api_type")]
        public string ApiType { get; set; } = string.Empty;

        [JsonPropertyName("base_url")]
        public string BaseUrl { get; set; } = string.Empty;

        [JsonPropertyName("model_id")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("vision_supported")]
        public bool? VisionSupported { get; set; }

        [JsonPropertyName("ambiguous_image_failures")]
        public int AmbiguousImageFailures { get; set; }

        [JsonPropertyName("anthropic_messages_url")]
        public string? AnthropicMessagesUrl { get; set; }

        [JsonPropertyName("anthropic_text_supported")]
        public bool? AnthropicTextSupported { get; set; }

        [JsonPropertyName("anthropic_text_last_reason")]
        public string AnthropicTextLastReason { get; set; } = string.Empty;

        [JsonPropertyName("last_reason")]
        public string LastReason { get; set; } = string.Empty;

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = UserTimeZoneService.Now();
    }
}
