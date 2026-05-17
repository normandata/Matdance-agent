using System.Text.Json;
using System.Text.Json.Serialization;
using Matdance.Cli.Services;

namespace Matdance.Cli.Models;

public class AgentConfig
{
    public const string DefaultBaseUrl = "https://api.openai.com/v1";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "your agent name";

    [JsonPropertyName("base_url")]
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    [JsonPropertyName("model_id")]
    public string ModelId { get; set; } = "gpt-5.5";

    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = "sk-xxxxx";

    [JsonPropertyName("api_type")]
    public string ApiType { get; set; } = "openai_chat";

    [JsonPropertyName("context_window")]
    public int ContextWindow { get; set; } = 256000;

    [JsonPropertyName("max_output_token")]
    public int MaxOutputToken { get; set; } = 32000;

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 1.0f;

    [JsonPropertyName("max_concurrency")]
    public int MaxConcurrency { get; set; } = 1;

    [JsonPropertyName("compression_threshold")]
    public float CompressionThreshold { get; set; } = 0.7f;

    [JsonPropertyName("hot_memory_limit")]
    public int HotMemoryLimit { get; set; } = 10000;

    [JsonPropertyName("core_memory_limit")]
    public int CoreMemoryLimit { get; set; } = 15000;

    [JsonPropertyName("user_md_limit")]
    public int UserMdLimit { get; set; } = 5000;

    [JsonPropertyName("identity_md_limit")]
    public int IdentityMdLimit { get; set; } = 2000;

    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        AtomicFile.WriteAllText(path, json);
    }

    public static AgentConfig Load(string path)
    {
        if (!File.Exists(path))
            return new AgentConfig();
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AgentConfig>(json) ?? new AgentConfig();
        config.MaxConcurrency = Math.Clamp(config.MaxConcurrency <= 0 ? 1 : config.MaxConcurrency, 1, 16);
        return config;
    }
}
