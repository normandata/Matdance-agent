using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public static class ModelProviderCatalog
{
    public const string OpenAiCompatible = "openai_chat";
    public const string DeepSeek = "deepseek";
    public const string ZaiGlm = "zai_glm";
    public const string ZaiGlmCodingPlan = "zai_glm_coding_plan";
    public const string BaiduQianfanCodingPlan = "baidu_qianfan_coding_plan";
    public const string XiaomiMimo = "xiaomi_mimo";
    public const string XiaomiMimoTokenPlan = "xiaomi_mimo_token_plan";
    public const string Anthropic = "anthropic";

    private const string DeepSeekBaseUrl = "https://api.deepseek.com";
    private const int DeepSeekContextWindow = 1_000_000;
    private const int DeepSeekMaxOutputToken = 384_000;
    private const string ZaiGlmBaseUrl = "https://api.z.ai/api/paas/v4";
    private const string ZaiGlmCodingPlanBaseUrl = "https://open.bigmodel.cn/api/coding/paas/v4";
    private const string ZaiApiKeyUrl = "https://z.ai/manage-apikey/apikey-list";
    private const string BaiduQianfanCodingPlanBaseUrl = "https://qianfan.baidubce.com/v2/coding";
    private const string BaiduQianfanApiKeyUrl = "https://console.bce.baidu.com/qianfan/ais/console/applicationConsole/application";
    private const string XiaomiMimoBaseUrl = "https://api.xiaomimimo.com/v1";
    private const string XiaomiMimoApiKeyUrl = "https://platform.xiaomimimo.com/#/console/api-keys";
    private const string XiaomiMimoTokenPlanBaseUrl = "https://token-plan-cn.xiaomimimo.com/v1";
    private const string XiaomiMimoTokenPlanApiKeyUrl = "https://platform.xiaomimimo.com/console/plan-manage";

    private static readonly List<ModelProviderDefinition> Definitions = new()
    {
        new ModelProviderDefinition
        {
            Id = OpenAiCompatible,
            Label = "OpenAI-compatible",
            BaseUrl = AgentConfig.DefaultBaseUrl,
            ManagedDefaults = false,
            Models = new List<ModelPreset>
            {
                new ModelPreset { Id = "gpt-5.5", ContextWindow = 256_000, MaxOutputToken = 32_000 },
                new ModelPreset { Id = "gpt-5.4", ContextWindow = 256_000, MaxOutputToken = 32_000 }
            }
        },
        new ModelProviderDefinition
        {
            Id = DeepSeek,
            Label = "DeepSeek",
            BaseUrl = DeepSeekBaseUrl,
            ApiKeyUrl = "https://platform.deepseek.com/api_keys",
            ManagedDefaults = true,
            LocksBaseUrl = true,
            LocksModelId = true,
            LocksTokenLimits = true,
            Models = new List<ModelPreset>
            {
                new ModelPreset { Id = "deepseek-v4-flash", ContextWindow = DeepSeekContextWindow, MaxOutputToken = DeepSeekMaxOutputToken, SupportsThinking = true },
                new ModelPreset { Id = "deepseek-v4-pro", ContextWindow = DeepSeekContextWindow, MaxOutputToken = DeepSeekMaxOutputToken, SupportsThinking = true },
                new ModelPreset { Id = "deepseek-chat", ContextWindow = DeepSeekContextWindow, MaxOutputToken = DeepSeekMaxOutputToken, Notes = "Legacy non-thinking alias" },
                new ModelPreset { Id = "deepseek-reasoner", ContextWindow = DeepSeekContextWindow, MaxOutputToken = DeepSeekMaxOutputToken, SupportsThinking = true, Notes = "Legacy thinking alias" }
            }
        },
        new ModelProviderDefinition
        {
            Id = ZaiGlm,
            Label = "Z.AI GLM",
            BaseUrl = ZaiGlmBaseUrl,
            ApiKeyUrl = ZaiApiKeyUrl,
            ManagedDefaults = true,
            LocksBaseUrl = true,
            LocksModelId = true,
            LocksTokenLimits = true,
            Models = new List<ModelPreset>
            {
                new ModelPreset { Id = "glm-5.1", ContextWindow = 200_000, MaxOutputToken = 128_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-5-turbo", ContextWindow = 200_000, MaxOutputToken = 128_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-4.7", ContextWindow = 200_000, MaxOutputToken = 64_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-4.5", ContextWindow = 128_000, MaxOutputToken = 32_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-4.5-air", ContextWindow = 128_000, MaxOutputToken = 32_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-4.5-x", ContextWindow = 128_000, MaxOutputToken = 32_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-4.5-airx", ContextWindow = 128_000, MaxOutputToken = 32_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-4.5-flash", ContextWindow = 128_000, MaxOutputToken = 32_000, SupportsThinking = true }
            }
        },
        new ModelProviderDefinition
        {
            Id = ZaiGlmCodingPlan,
            Label = "Z.AI GLM Coding Plan",
            BaseUrl = ZaiGlmCodingPlanBaseUrl,
            ApiKeyUrl = ZaiApiKeyUrl,
            ManagedDefaults = true,
            LocksBaseUrl = true,
            LocksModelId = true,
            LocksTokenLimits = true,
            Models = new List<ModelPreset>
            {
                new ModelPreset { Id = "glm-5.1", ContextWindow = 200_000, MaxOutputToken = 128_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-5-turbo", ContextWindow = 200_000, MaxOutputToken = 128_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-4.7", ContextWindow = 200_000, MaxOutputToken = 64_000, SupportsThinking = true },
                new ModelPreset { Id = "glm-4.5-air", ContextWindow = 128_000, MaxOutputToken = 32_000, SupportsThinking = true }
            }
        },
        new ModelProviderDefinition
        {
            Id = BaiduQianfanCodingPlan,
            Label = "Baidu Qianfan Coding Plan",
            BaseUrl = BaiduQianfanCodingPlanBaseUrl,
            ApiKeyUrl = BaiduQianfanApiKeyUrl,
            ManagedDefaults = true,
            LocksBaseUrl = true,
            LocksModelId = true,
            LocksTokenLimits = true,
            Models = new List<ModelPreset>
            {
                new ModelPreset
                {
                    Id = "qianfan-code-latest",
                    ContextWindow = 128_000,
                    MaxInputToken = 96_000,
                    MaxOutputToken = 12_288,
                    Notes = "Console-controlled Coding Plan model name; defaults use the conservative floor across listed Qianfan coding models. Choose an explicit model ID for exact limits."
                },
                new ModelPreset { Id = "deepseek-v4-flash", ContextWindow = 1_000_000, MaxInputToken = 1_000_000, MaxOutputToken = 131_072 },
                new ModelPreset { Id = "glm-5.1", ContextWindow = 198_000, MaxInputToken = 198_000, MaxOutputToken = 131_072 },
                new ModelPreset { Id = "glm-5", ContextWindow = 198_000, MaxInputToken = 198_000, MaxOutputToken = 131_072 },
                new ModelPreset { Id = "minimax-m2.5", ContextWindow = 192_000, MaxInputToken = 192_000, MaxOutputToken = 131_072 },
                new ModelPreset { Id = "kimi-k2.5", ContextWindow = 256_000, MaxInputToken = 224_000, MaxOutputToken = 65_536, SupportsThinking = true },
                new ModelPreset { Id = "deepseek-v3.2", ContextWindow = 128_000, MaxInputToken = 96_000, MaxOutputToken = 32_768 },
                new ModelPreset { Id = "ernie-4.5-turbo-20260402", ContextWindow = 128_000, MaxInputToken = 123_000, MaxOutputToken = 12_288 }
            }
        },
        new ModelProviderDefinition
        {
            Id = XiaomiMimo,
            Label = "Xiaomi MiMo",
            BaseUrl = XiaomiMimoBaseUrl,
            ApiKeyUrl = XiaomiMimoApiKeyUrl,
            ManagedDefaults = true,
            LocksBaseUrl = true,
            LocksModelId = true,
            LocksTokenLimits = true,
            Models = new List<ModelPreset>
            {
                new ModelPreset { Id = "mimo-v2.5-pro", ContextWindow = 1_000_000, MaxOutputToken = 131_072, SupportsThinking = true },
                new ModelPreset { Id = "mimo-v2.5", ContextWindow = 1_000_000, MaxOutputToken = 32_768, SupportsThinking = true },
                new ModelPreset { Id = "mimo-v2-pro", ContextWindow = 1_000_000, MaxOutputToken = 131_072, SupportsThinking = true },
                new ModelPreset { Id = "mimo-v2-omni", ContextWindow = 256_000, MaxOutputToken = 32_768, SupportsThinking = true },
                new ModelPreset { Id = "mimo-v2-flash", ContextWindow = 256_000, MaxOutputToken = 65_536, SupportsThinking = true }
            }
        },
        new ModelProviderDefinition
        {
            Id = XiaomiMimoTokenPlan,
            Label = "Xiaomi MiMo Token Plan",
            BaseUrl = XiaomiMimoTokenPlanBaseUrl,
            ApiKeyUrl = XiaomiMimoTokenPlanApiKeyUrl,
            ManagedDefaults = true,
            LocksBaseUrl = true,
            LocksModelId = true,
            LocksTokenLimits = true,
            Models = new List<ModelPreset>
            {
                new ModelPreset { Id = "mimo-v2.5-pro", ContextWindow = 1_000_000, MaxOutputToken = 131_072, SupportsThinking = true },
                new ModelPreset { Id = "mimo-v2.5", ContextWindow = 1_000_000, MaxOutputToken = 32_768, SupportsThinking = true },
                new ModelPreset { Id = "mimo-v2-pro", ContextWindow = 1_000_000, MaxOutputToken = 131_072, SupportsThinking = true },
                new ModelPreset { Id = "mimo-v2-omni", ContextWindow = 256_000, MaxOutputToken = 32_768, SupportsThinking = true },
                new ModelPreset { Id = "mimo-v2-flash", ContextWindow = 256_000, MaxOutputToken = 65_536, SupportsThinking = true }
            }
        },
        new ModelProviderDefinition
        {
            Id = Anthropic,
            Label = "Anthropic-compatible",
            BaseUrl = "https://api.anthropic.com/v1",
            ManagedDefaults = true,
            LocksBaseUrl = false,
            LocksModelId = false,
            LocksTokenLimits = false,
            Models = new List<ModelPreset>
            {
                new ModelPreset { Id = "claude-sonnet-4-6", ContextWindow = 1_000_000, MaxInputToken = 1_000_000, MaxOutputToken = 64_000 },
                new ModelPreset { Id = "claude-opus-4-7", ContextWindow = 1_000_000, MaxInputToken = 1_000_000, MaxOutputToken = 128_000 },
                new ModelPreset { Id = "claude-haiku-4-5-20251001", ContextWindow = 200_000, MaxInputToken = 200_000, MaxOutputToken = 64_000 }
            }
        }
    };

    public static IReadOnlyList<ModelProviderDefinition> All => Definitions;

    public static ModelProviderDefinition? FindProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return null;
        }

        return Definitions.FirstOrDefault(item => item.Id.Equals(provider.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static ModelPreset? FindModel(string? provider, string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        return FindProvider(provider)?.Models.FirstOrDefault(item => item.Id.Equals(model.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static string[] ApiTypes() => Definitions.Select(item => item.Id).ToArray();

    public static void ApplyDefaults(
        AgentConfig config,
        bool preserveCustomModelId = false,
        bool preserveCustomTokenLimits = false)
    {
        var provider = FindProvider(config.ApiType);
        if (provider == null)
        {
            return;
        }

        if (provider.ManagedDefaults)
        {
            if (provider.LocksBaseUrl
                || string.IsNullOrWhiteSpace(config.BaseUrl)
                || config.BaseUrl.Equals(AgentConfig.DefaultBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                config.BaseUrl = provider.BaseUrl;
            }

            var hasModelPreset = FindModel(config.ApiType, config.ModelId) != null;
            if (string.IsNullOrWhiteSpace(config.ModelId)
                || (!hasModelPreset && (provider.LocksModelId || !preserveCustomModelId)))
            {
                config.ModelId = provider.Models.First().Id;
            }
        }

        var preset = FindModel(config.ApiType, config.ModelId);
        if (preset != null
            && provider.ManagedDefaults
            && (provider.LocksTokenLimits || !preserveCustomTokenLimits))
        {
            config.ContextWindow = preset.ContextWindow;
            config.MaxOutputToken = preset.MaxOutputToken;
        }
    }
}

public sealed class ModelProviderDefinition
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string? ApiKeyUrl { get; set; }
    public bool ManagedDefaults { get; set; }
    public bool LocksBaseUrl { get; set; }
    public bool LocksModelId { get; set; }
    public bool LocksTokenLimits { get; set; }
    public List<ModelPreset> Models { get; set; } = new();
}

public sealed class ModelPreset
{
    public string Id { get; set; } = "";
    public int ContextWindow { get; set; }
    public int MaxInputToken { get; set; }
    public int MaxOutputToken { get; set; }
    public float Temperature { get; set; } = 1.0f;
    public bool SupportsThinking { get; set; }
    public string? Notes { get; set; }
}
