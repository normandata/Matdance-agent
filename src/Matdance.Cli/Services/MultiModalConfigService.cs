using System.Text.Json;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public sealed class MultiModalConfigService
{
    private const string DefaultOpenAiBaseUrl = "https://api.openai.com/v1";
    private const string DefaultAliyunDashScopeBaseUrl = "https://dashscope.aliyuncs.com/api/v1";
    private const string DefaultTavilyBaseUrl = "https://api.tavily.com";
    private const string DefaultBraveSearchBaseUrl = "https://api.search.brave.com";
    private const string DefaultFirecrawlBaseUrl = "https://api.firecrawl.dev";
    private static readonly JsonSerializerOptions FileJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly PathService _path;

    public MultiModalConfigService(PathService path)
    {
        _path = path;
    }

    public string ConfigPath => Path.Combine(_path.AgentsRoot, "multimodal_config.json");

    public MultiModalConfigRoot Load()
    {
        if (!File.Exists(ConfigPath))
        {
            return CreateDefaultRoot();
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var root = JsonSerializer.Deserialize<MultiModalConfigRoot>(json, FileJsonOptions) ?? CreateDefaultRoot();
            root.Global ??= CreateDefaultProfile();
            root.Global.Image ??= new ImageGenerationConfig();
            root.Global.ImageModels = NormalizeImageModels(root.Global.ImageModels, root.Global.Image);
            root.Global.Tts ??= new TextToSpeechConfig();
            root.Global.TtsModels = NormalizeTtsModels(root.Global.TtsModels, root.Global.Tts);
            root.Global.Stt ??= new SpeechToTextConfig();
            root.Global.Search ??= new WebSearchConfig();
            root.Global.SearchModels = NormalizeSearchModels(root.Global.SearchModels, root.Global.Search, useProviderPresetsWhenEmpty: !HasSearchOverride(root.Global.Search));
            root.Agents = root.Agents == null
                ? new Dictionary<string, MultiModalProfile>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, MultiModalProfile>(root.Agents, StringComparer.OrdinalIgnoreCase);
            foreach (var profile in root.Agents.Values)
            {
                profile.Image ??= new ImageGenerationConfig();
                profile.ImageModels = NormalizeImageModels(profile.ImageModels, profile.Image);
                profile.Tts ??= new TextToSpeechConfig();
                if (profile.TtsModels is { Count: > 0 } || HasTtsOverride(profile.Tts))
                {
                    profile.TtsModels = NormalizeTtsModels(profile.TtsModels, profile.Tts);
                }
                profile.Stt ??= new SpeechToTextConfig();
                profile.Search ??= new WebSearchConfig();
                if (profile.SearchModels is { Count: > 0 } || HasSearchOverride(profile.Search))
                {
                    profile.SearchModels = NormalizeSearchModels(profile.SearchModels, profile.Search, useProviderPresetsWhenEmpty: false);
                }
            }
            return root;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid multimodal config: {ConfigPath}. {ex.Message}");
        }
    }

    public object GetDisplayConfig(string agent)
    {
        var root = Load();
        root.Agents.TryGetValue(agent, out var agentProfile);
        var effective = GetEffective(agent, root);
        return new
        {
            agent,
            configPath = ConfigPath,
            global = ProfileDto(root.Global),
            agentOverride = ProfileDto(agentProfile ?? new MultiModalProfile()),
            effective
        };
    }

    public object Save(MultiModalSaveRequest request)
    {
        var agent = (request.Agent ?? string.Empty).Trim();
        var root = Load();
        if (request.Global != null)
        {
            MergeProfile(root.Global, request.Global);
        }

        if (!string.IsNullOrWhiteSpace(agent) && request.AgentOverride != null)
        {
            if (!root.Agents.TryGetValue(agent, out var existing))
            {
                existing = new MultiModalProfile();
                root.Agents[agent] = existing;
            }

            MergeProfile(existing, request.AgentOverride);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        AtomicFile.WriteAllText(ConfigPath, JsonSerializer.Serialize(root, FileJsonOptions));
        return GetDisplayConfig(agent);
    }

    public EffectiveMultiModalConfig GetEffective(string agent)
    {
        return GetEffective(agent, Load());
    }

    private EffectiveMultiModalConfig GetEffective(string agent, MultiModalConfigRoot root)
    {
        root.Agents.TryGetValue(agent, out var agentProfile);
        var defaults = CreateDefaultProfile();
        var imageModels = EffectiveImageModels(agentProfile, root.Global, defaults.Image);
        var ttsModels = EffectiveTtsModels(agentProfile, root.Global, defaults.Tts);
        var searchModels = EffectiveSearchModels(agentProfile, root.Global);
        return new EffectiveMultiModalConfig
        {
            Image = imageModels.FirstOrDefault(model => model.Enabled) ?? imageModels.First(),
            ImageModels = imageModels,
            Tts = ttsModels.FirstOrDefault(model => !model.Mode.Equals("off", StringComparison.OrdinalIgnoreCase)) ?? ttsModels.First(),
            TtsModels = ttsModels,
            Stt = new EffectiveSpeechToTextConfig
            {
                Enabled = BoolValue(agentProfile?.Stt.Enabled, root.Global.Stt.Enabled, defaults.Stt.Enabled) ?? false,
                SendAfterTranscription = BoolValue(agentProfile?.Stt.SendAfterTranscription, root.Global.Stt.SendAfterTranscription, defaults.Stt.SendAfterTranscription) ?? false,
                BaseUrl = StringValue(agentProfile?.Stt.BaseUrl, root.Global.Stt.BaseUrl, defaults.Stt.BaseUrl) ?? "",
                EndpointMode = NormalizeEndpointMode(StringValue(agentProfile?.Stt.EndpointMode, root.Global.Stt.EndpointMode, defaults.Stt.EndpointMode)),
                ApiKey = StringValue(agentProfile?.Stt.ApiKey, root.Global.Stt.ApiKey, defaults.Stt.ApiKey) ?? "",
                Model = StringValue(agentProfile?.Stt.Model, root.Global.Stt.Model, defaults.Stt.Model) ?? "whisper-1"
            },
            Search = searchModels.FirstOrDefault(model => model.Enabled) ?? searchModels.First(),
            SearchModels = searchModels
        }.WithKeyFlags();
    }

    private static MultiModalConfigRoot CreateDefaultRoot() => new()
    {
        Global = CreateDefaultProfile()
    };

    private static MultiModalProfile CreateDefaultProfile() => new()
    {
        Image = new ImageGenerationConfig
        {
            Id = "default",
            Name = "Default image model",
            Enabled = false,
            BaseUrl = DefaultOpenAiBaseUrl,
            EndpointMode = "native",
            Model = "gpt-image-1",
            Size = "1024x1024",
            Quality = "auto",
            OutputFormat = "png"
        },
        Tts = new TextToSpeechConfig
        {
            Id = "default",
            Name = "Default TTS model",
            Mode = "off",
            AutoPlay = false,
            BaseUrl = DefaultOpenAiBaseUrl,
            EndpointMode = "native",
            EndpointPath = "audio/speech",
            Model = "gpt-4o-mini-tts",
            Voice = "alloy",
            LanguageType = "Chinese",
            Instructions = null,
            OptimizeInstructions = false,
            Format = "mp3"
        },
        Stt = new SpeechToTextConfig
        {
            Enabled = false,
            SendAfterTranscription = false,
            BaseUrl = DefaultOpenAiBaseUrl,
            EndpointMode = "native",
            Model = "whisper-1"
        },
        Search = CreateDefaultSearchModels()[0],
        SearchModels = CreateDefaultSearchModels()
    };

    private static object ProfileDto(MultiModalProfile profile) => new
    {
        image = new
        {
            enabled = profile.Image.Enabled,
            id = profile.Image.Id,
            name = profile.Image.Name,
            baseUrl = profile.Image.BaseUrl,
            endpointMode = profile.Image.EndpointMode,
            model = profile.Image.Model,
            size = profile.Image.Size,
            quality = profile.Image.Quality,
            outputFormat = profile.Image.OutputFormat,
            hasApiKey = !string.IsNullOrWhiteSpace(profile.Image.ApiKey)
        },
        imageModels = (profile.ImageModels ?? NormalizeImageModels(null, profile.Image)).Select(image => new
        {
            enabled = image.Enabled,
            id = image.Id,
            name = image.Name,
            baseUrl = image.BaseUrl,
            endpointMode = image.EndpointMode,
            model = image.Model,
            size = image.Size,
            quality = image.Quality,
            outputFormat = image.OutputFormat,
            hasApiKey = !string.IsNullOrWhiteSpace(image.ApiKey)
        }).ToList(),
        tts = new
        {
            id = profile.Tts.Id,
            name = profile.Tts.Name,
            mode = profile.Tts.Mode,
            autoPlay = profile.Tts.AutoPlay,
            baseUrl = profile.Tts.BaseUrl,
            endpointMode = profile.Tts.EndpointMode,
            endpointPath = profile.Tts.EndpointPath,
            model = profile.Tts.Model,
            voice = profile.Tts.Voice,
            languageType = profile.Tts.LanguageType,
            instructions = profile.Tts.Instructions,
            optimizeInstructions = profile.Tts.OptimizeInstructions,
            format = profile.Tts.Format,
            hasApiKey = !string.IsNullOrWhiteSpace(profile.Tts.ApiKey)
        },
        ttsModels = (profile.TtsModels ?? NormalizeTtsModels(null, profile.Tts)).Select(tts => new
        {
            id = tts.Id,
            name = tts.Name,
            mode = tts.Mode,
            autoPlay = tts.AutoPlay,
            baseUrl = tts.BaseUrl,
            endpointMode = tts.EndpointMode,
            endpointPath = tts.EndpointPath,
            model = tts.Model,
            voice = tts.Voice,
            languageType = tts.LanguageType,
            instructions = tts.Instructions,
            optimizeInstructions = tts.OptimizeInstructions,
            format = tts.Format,
            hasApiKey = !string.IsNullOrWhiteSpace(tts.ApiKey)
        }).ToList(),
        stt = new
        {
            enabled = profile.Stt.Enabled,
            sendAfterTranscription = profile.Stt.SendAfterTranscription,
            baseUrl = profile.Stt.BaseUrl,
            endpointMode = profile.Stt.EndpointMode,
            model = profile.Stt.Model,
            hasApiKey = !string.IsNullOrWhiteSpace(profile.Stt.ApiKey)
        },
        search = new
        {
            enabled = profile.Search.Enabled,
            id = profile.Search.Id,
            name = profile.Search.Name,
            provider = profile.Search.Provider,
            baseUrl = profile.Search.BaseUrl,
            endpointPath = profile.Search.EndpointPath,
            maxResults = profile.Search.MaxResults,
            hasApiKey = !string.IsNullOrWhiteSpace(profile.Search.ApiKey)
        },
        searchModels = (profile.SearchModels ?? NormalizeSearchModels(null, profile.Search, useProviderPresetsWhenEmpty: !HasSearchOverride(profile.Search))).Select(search => new
        {
            enabled = search.Enabled,
            id = search.Id,
            name = search.Name,
            provider = search.Provider,
            baseUrl = search.BaseUrl,
            endpointPath = search.EndpointPath,
            maxResults = search.MaxResults,
            hasApiKey = !string.IsNullOrWhiteSpace(search.ApiKey)
        }).ToList()
    };

    private static void MergeProfile(MultiModalProfile target, MultiModalProfile incoming)
    {
        target.Image ??= new ImageGenerationConfig();
        target.Tts ??= new TextToSpeechConfig();
        target.Stt ??= new SpeechToTextConfig();
        target.Search ??= new WebSearchConfig();
        incoming.Image ??= new ImageGenerationConfig();
        incoming.Tts ??= new TextToSpeechConfig();
        incoming.Stt ??= new SpeechToTextConfig();
        incoming.Search ??= new WebSearchConfig();
        if (incoming.ImageModels is { Count: > 0 })
        {
            target.ImageModels = MergeImageModelList(target.ImageModels, incoming.ImageModels, target.Image);
            target.Image = CloneImage(target.ImageModels[0]);
        }
        else
        {
            MergeImage(target.Image, incoming.Image);
            target.ImageModels = NormalizeImageModels(target.ImageModels, target.Image);
            if (target.ImageModels.Count == 1)
            {
                target.ImageModels[0] = CloneImage(target.Image);
            }
        }

        if (incoming.TtsModels is { Count: > 0 })
        {
            target.TtsModels = MergeTtsModelList(target.TtsModels, incoming.TtsModels, target.Tts);
            target.Tts = CloneTts(target.TtsModels[0]);
        }
        else
        {
            MergeTts(target.Tts, incoming.Tts);
            target.TtsModels = NormalizeTtsModels(target.TtsModels, target.Tts);
            if (target.TtsModels.Count == 1)
            {
                target.TtsModels[0] = CloneTts(target.Tts);
            }
        }

        MergeStt(target.Stt, incoming.Stt);
        if (incoming.SearchModels is { Count: > 0 })
        {
            target.SearchModels = MergeSearchModelList(target.SearchModels, incoming.SearchModels, target.Search);
            target.Search = CloneSearch(target.SearchModels[0]);
        }
        else
        {
            MergeSearch(target.Search, incoming.Search);
            target.SearchModels = NormalizeSearchModels(target.SearchModels, target.Search, useProviderPresetsWhenEmpty: !HasSearchOverride(target.Search));
            if (target.SearchModels.Count == 1)
            {
                target.SearchModels[0] = CloneSearch(target.Search);
            }
        }
    }

    private static void MergeImage(ImageGenerationConfig target, ImageGenerationConfig incoming)
    {
        target.Id = CleanId(incoming.Id) ?? target.Id ?? "default";
        target.Name = CleanOptional(incoming.Name) ?? target.Name;
        target.Enabled = incoming.Enabled;
        target.BaseUrl = CleanOptional(incoming.BaseUrl);
        target.EndpointMode = NormalizeImageEndpointMode(incoming.EndpointMode);
        PreserveSecret(target, incoming.ApiKey, (cfg, value) => cfg.ApiKey = value);
        target.Model = CleanOptional(incoming.Model);
        target.Size = CleanOptional(incoming.Size);
        target.Quality = CleanOptional(incoming.Quality);
        target.OutputFormat = NormalizeFormat(CleanOptional(incoming.OutputFormat), null);
        target.Extra = MergeExtra(target.Extra, incoming.Extra);
    }

    private static void MergeTts(TextToSpeechConfig target, TextToSpeechConfig incoming)
    {
        target.Id = CleanId(incoming.Id) ?? target.Id ?? "default";
        target.Name = CleanOptional(incoming.Name) ?? target.Name;
        target.Mode = NormalizeTtsMode(incoming.Mode);
        target.AutoPlay = incoming.AutoPlay;
        target.BaseUrl = CleanOptional(incoming.BaseUrl);
        target.EndpointMode = NormalizeEndpointMode(incoming.EndpointMode);
        target.EndpointPath = CleanOptional(incoming.EndpointPath) ?? EndpointPathForMode(target.EndpointMode);
        PreserveSecret(target, incoming.ApiKey, (cfg, value) => cfg.ApiKey = value);
        target.Model = CleanOptional(incoming.Model);
        target.Voice = CleanOptional(incoming.Voice);
        target.LanguageType = CleanOptional(incoming.LanguageType);
        target.Instructions = CleanOptional(incoming.Instructions);
        target.OptimizeInstructions = incoming.OptimizeInstructions;
        target.Format = NormalizeFormat(CleanOptional(incoming.Format), null);
        target.Extra = MergeExtra(target.Extra, incoming.Extra);
    }

    private static void MergeStt(SpeechToTextConfig target, SpeechToTextConfig incoming)
    {
        target.Enabled = incoming.Enabled;
        target.SendAfterTranscription = incoming.SendAfterTranscription;
        target.BaseUrl = CleanOptional(incoming.BaseUrl);
        target.EndpointMode = NormalizeEndpointMode(incoming.EndpointMode);
        PreserveSecret(target, incoming.ApiKey, (cfg, value) => cfg.ApiKey = value);
        target.Model = CleanOptional(incoming.Model);
    }

    private static void MergeSearch(WebSearchConfig target, WebSearchConfig incoming)
    {
        target.Id = CleanId(incoming.Id) ?? target.Id ?? "search";
        target.Name = CleanOptional(incoming.Name) ?? target.Name;
        target.Enabled = incoming.Enabled;
        target.Provider = NormalizeSearchProvider(incoming.Provider);
        target.BaseUrl = CleanOptional(incoming.BaseUrl);
        target.EndpointPath = CleanOptional(incoming.EndpointPath) ?? SearchEndpointPath(target.Provider);
        PreserveSecret(target, incoming.ApiKey, (cfg, value) => cfg.ApiKey = value);
        target.MaxResults = incoming.MaxResults;
        target.Extra = MergeExtra(target.Extra, incoming.Extra);
    }

    private static void PreserveSecret<T>(T target, string? incoming, Action<T, string?> setter)
    {
        if (!string.IsNullOrWhiteSpace(incoming))
        {
            setter(target, incoming.Trim());
        }
    }

    private static bool? BoolValue(params bool?[] values)
    {
        foreach (var value in values)
        {
            if (value.HasValue) return value.Value;
        }

        return null;
    }

    private static string? StringValue(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }

        return null;
    }

    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeTtsMode(string? value)
    {
        var mode = string.IsNullOrWhiteSpace(value) ? "off" : value.Trim().ToLowerInvariant();
        return mode is "off" or "chat_visible_only" or "always" ? mode : "off";
    }

    private static string NormalizeEndpointMode(string? value)
    {
        var mode = string.IsNullOrWhiteSpace(value) ? "native" : value.Trim().ToLowerInvariant();
        return mode;
    }

    private static string NormalizeImageEndpointMode(string? value)
    {
        var mode = string.IsNullOrWhiteSpace(value) ? "native" : value.Trim().ToLowerInvariant();
        return mode is "native" ? mode : "native";
    }

    private static string NormalizeSearchProvider(string? value)
    {
        var provider = string.IsNullOrWhiteSpace(value) ? "tavily" : value.Trim().ToLowerInvariant();
        return provider is "tavily" or "brave" or "firecrawl" or "custom" ? provider : "custom";
    }

    private static string SearchEndpointPath(string? provider) =>
        NormalizeSearchProvider(provider) switch
        {
            "brave" => "res/v1/web/search",
            "firecrawl" => "v1/search",
            _ => "search"
        };

    private static string SearchBaseUrl(string? provider) =>
        NormalizeSearchProvider(provider) switch
        {
            "brave" => DefaultBraveSearchBaseUrl,
            "firecrawl" => DefaultFirecrawlBaseUrl,
            _ => DefaultTavilyBaseUrl
        };

    private static bool IsAliyunQwenTtsEndpointMode(string endpointMode) =>
        endpointMode.Equals("aliyun_qwen_tts", StringComparison.OrdinalIgnoreCase);

    private static string EffectiveTtsBaseUrl(string endpointMode, string? configuredBaseUrl, string? defaultBaseUrl)
    {
        var configured = CleanOptional(configuredBaseUrl);
        if (IsAliyunQwenTtsEndpointMode(endpointMode))
        {
            return string.IsNullOrWhiteSpace(configured) || configured.Equals(DefaultOpenAiBaseUrl, StringComparison.OrdinalIgnoreCase)
                ? DefaultAliyunDashScopeBaseUrl
                : configured;
        }

        return configured ?? (CleanOptional(defaultBaseUrl) ?? DefaultOpenAiBaseUrl);
    }

    private static string EndpointPathForMode(string endpointMode) =>
        endpointMode.Equals("tts", StringComparison.OrdinalIgnoreCase) ? "tts" :
        endpointMode.Equals("aliyun_qwen_tts", StringComparison.OrdinalIgnoreCase) ? "services/aigc/multimodal-generation/generation" :
        "audio/speech";

    private static List<ImageGenerationConfig> NormalizeImageModels(List<ImageGenerationConfig>? models, ImageGenerationConfig fallback)
    {
        var source = models is { Count: > 0 } ? models : new List<ImageGenerationConfig> { fallback };
        var normalized = new List<ImageGenerationConfig>();
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var model in source)
        {
            index++;
            var clone = CloneImage(model);
            clone.Id = UniqueImageId(CleanId(clone.Id) ?? CleanId(clone.Name) ?? $"image-{index}", usedIds);
            clone.Name = CleanOptional(clone.Name) ?? clone.Id;
            clone.EndpointMode = NormalizeImageEndpointMode(clone.EndpointMode);
            clone.OutputFormat = NormalizeFormat(clone.OutputFormat, null);
            usedIds.Add(clone.Id);
            normalized.Add(clone);
        }

        if (normalized.Count == 0)
        {
            normalized.Add(CloneImage(fallback));
        }

        return normalized;
    }

    private static List<ImageGenerationConfig> MergeImageModelList(List<ImageGenerationConfig>? existing, List<ImageGenerationConfig> incoming, ImageGenerationConfig fallback)
    {
        var current = NormalizeImageModels(existing, fallback);
        var merged = new List<ImageGenerationConfig>();
        for (var i = 0; i < incoming.Count; i++)
        {
            var next = CloneImage(incoming[i]);
            var nextId = CleanId(next.Id) ?? CleanId(next.Name);
            var previous = !string.IsNullOrWhiteSpace(nextId)
                ? current.FirstOrDefault(item => CleanId(item.Id)?.Equals(nextId, StringComparison.OrdinalIgnoreCase) == true)
                : null;
            previous ??= i < current.Count ? current[i] : null;
            if (string.IsNullOrWhiteSpace(next.ApiKey) && !string.IsNullOrWhiteSpace(previous?.ApiKey))
            {
                next.ApiKey = previous.ApiKey;
            }
            next.Extra = MergeExtra(previous?.Extra, next.Extra);

            merged.Add(next);
        }

        return NormalizeImageModels(merged, fallback);
    }

    private static List<TextToSpeechConfig> NormalizeTtsModels(List<TextToSpeechConfig>? models, TextToSpeechConfig fallback)
    {
        var source = models is { Count: > 0 } ? models : new List<TextToSpeechConfig> { fallback };
        var normalized = new List<TextToSpeechConfig>();
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var model in source)
        {
            index++;
            var clone = CloneTts(model);
            clone.Id = UniqueTtsId(CleanId(clone.Id) ?? CleanId(clone.Name) ?? $"tts-{index}", usedIds);
            clone.Name = CleanOptional(clone.Name) ?? clone.Id;
            clone.Mode = NormalizeTtsMode(clone.Mode);
            clone.EndpointMode = NormalizeEndpointMode(clone.EndpointMode);
            clone.EndpointPath = CleanOptional(clone.EndpointPath) ?? EndpointPathForMode(clone.EndpointMode);
            clone.Format = NormalizeFormat(clone.Format, null);
            usedIds.Add(clone.Id);
            normalized.Add(clone);
        }

        if (normalized.Count == 0)
        {
            normalized.Add(CloneTts(fallback));
        }

        return normalized;
    }

    private static List<TextToSpeechConfig> MergeTtsModelList(List<TextToSpeechConfig>? existing, List<TextToSpeechConfig> incoming, TextToSpeechConfig fallback)
    {
        var current = NormalizeTtsModels(existing, fallback);
        var merged = new List<TextToSpeechConfig>();
        for (var i = 0; i < incoming.Count; i++)
        {
            var next = CloneTts(incoming[i]);
            var nextId = CleanId(next.Id) ?? CleanId(next.Name);
            var previous = !string.IsNullOrWhiteSpace(nextId)
                ? current.FirstOrDefault(item => CleanId(item.Id)?.Equals(nextId, StringComparison.OrdinalIgnoreCase) == true)
                : null;
            previous ??= i < current.Count ? current[i] : null;
            if (string.IsNullOrWhiteSpace(next.ApiKey) && !string.IsNullOrWhiteSpace(previous?.ApiKey))
            {
                next.ApiKey = previous.ApiKey;
            }
            next.Extra = MergeExtra(previous?.Extra, next.Extra);

            merged.Add(next);
        }

        return NormalizeTtsModels(merged, fallback);
    }

    private static List<WebSearchConfig> CreateDefaultSearchModels() => new()
    {
        new WebSearchConfig
        {
            Id = "tavily",
            Name = "Tavily",
            Enabled = false,
            Provider = "tavily",
            BaseUrl = DefaultTavilyBaseUrl,
            EndpointPath = "search",
            MaxResults = 5
        },
        new WebSearchConfig
        {
            Id = "brave",
            Name = "Brave Search",
            Enabled = false,
            Provider = "brave",
            BaseUrl = DefaultBraveSearchBaseUrl,
            EndpointPath = "res/v1/web/search",
            MaxResults = 5
        },
        new WebSearchConfig
        {
            Id = "firecrawl",
            Name = "Firecrawl",
            Enabled = false,
            Provider = "firecrawl",
            BaseUrl = DefaultFirecrawlBaseUrl,
            EndpointPath = "v1/search",
            MaxResults = 5
        }
    };

    private static List<WebSearchConfig> NormalizeSearchModels(List<WebSearchConfig>? models, WebSearchConfig fallback, bool useProviderPresetsWhenEmpty)
    {
        var source = models is { Count: > 0 }
            ? models
            : useProviderPresetsWhenEmpty
                ? CreateDefaultSearchModels()
                : new List<WebSearchConfig> { fallback };
        var normalized = new List<WebSearchConfig>();
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var model in source)
        {
            index++;
            var clone = CloneSearch(model);
            clone.Provider = NormalizeSearchProvider(clone.Provider);
            clone.Id = UniqueSearchId(CleanId(clone.Id) ?? CleanId(clone.Name) ?? clone.Provider ?? $"search-{index}", usedIds);
            clone.Name = CleanOptional(clone.Name) ?? clone.Id;
            clone.BaseUrl = CleanOptional(clone.BaseUrl) ?? SearchBaseUrl(clone.Provider);
            clone.EndpointPath = CleanOptional(clone.EndpointPath) ?? SearchEndpointPath(clone.Provider);
            clone.MaxResults = Math.Clamp(clone.MaxResults ?? 5, 1, 20);
            usedIds.Add(clone.Id);
            normalized.Add(clone);
        }

        if (normalized.Count == 0)
        {
            normalized.AddRange(CreateDefaultSearchModels());
        }

        return normalized;
    }

    private static List<WebSearchConfig> MergeSearchModelList(List<WebSearchConfig>? existing, List<WebSearchConfig> incoming, WebSearchConfig fallback)
    {
        var current = NormalizeSearchModels(existing, fallback, useProviderPresetsWhenEmpty: !HasSearchOverride(fallback));
        var merged = new List<WebSearchConfig>();
        for (var i = 0; i < incoming.Count; i++)
        {
            var next = CloneSearch(incoming[i]);
            var nextId = CleanId(next.Id) ?? CleanId(next.Name);
            var previous = !string.IsNullOrWhiteSpace(nextId)
                ? current.FirstOrDefault(item => CleanId(item.Id)?.Equals(nextId, StringComparison.OrdinalIgnoreCase) == true)
                : null;
            previous ??= i < current.Count ? current[i] : null;
            if (string.IsNullOrWhiteSpace(next.ApiKey) && !string.IsNullOrWhiteSpace(previous?.ApiKey))
            {
                next.ApiKey = previous.ApiKey;
            }
            next.Extra = MergeExtra(previous?.Extra, next.Extra);
            merged.Add(next);
        }

        return NormalizeSearchModels(merged, fallback, useProviderPresetsWhenEmpty: false);
    }

    private static List<EffectiveImageGenerationConfig> EffectiveImageModels(MultiModalProfile? agentProfile, MultiModalProfile globalProfile, ImageGenerationConfig defaults)
    {
        var source = agentProfile?.ImageModels is { Count: > 0 }
            ? agentProfile.ImageModels
            : globalProfile.ImageModels is { Count: > 0 }
                ? globalProfile.ImageModels
                : new List<ImageGenerationConfig> { globalProfile.Image };

        return NormalizeImageModels(source, globalProfile.Image)
            .Select((image, index) => new EffectiveImageGenerationConfig
            {
                Id = CleanId(image.Id) ?? $"image-{index + 1}",
                Name = CleanOptional(image.Name) ?? CleanId(image.Id) ?? $"Image model {index + 1}",
                Enabled = BoolValue(image.Enabled, defaults.Enabled) ?? false,
                BaseUrl = StringValue(image.BaseUrl, defaults.BaseUrl) ?? "",
                EndpointMode = NormalizeImageEndpointMode(StringValue(image.EndpointMode, defaults.EndpointMode)),
                ApiKey = StringValue(image.ApiKey, defaults.ApiKey) ?? "",
                Model = StringValue(image.Model, defaults.Model) ?? "gpt-image-1",
                Size = StringValue(image.Size, defaults.Size) ?? "1024x1024",
                Quality = StringValue(image.Quality, defaults.Quality) ?? "auto",
                OutputFormat = NormalizeFormat(StringValue(image.OutputFormat, defaults.OutputFormat), "png") ?? "png"
            })
            .ToList()
            .DefaultIfEmpty(new EffectiveImageGenerationConfig())
            .ToList();
    }

    private static List<EffectiveTextToSpeechConfig> EffectiveTtsModels(MultiModalProfile? agentProfile, MultiModalProfile globalProfile, TextToSpeechConfig defaults)
    {
        if (agentProfile?.TtsModels is { Count: > 0 })
        {
            return NormalizeTtsModels(agentProfile.TtsModels, agentProfile.Tts)
                .Select((tts, index) => EffectiveTtsModel(tts, defaults, index))
                .ToList()
                .DefaultIfEmpty(new EffectiveTextToSpeechConfig())
                .ToList();
        }

        if (HasTtsOverride(agentProfile?.Tts))
        {
            return new List<EffectiveTextToSpeechConfig>
            {
                EffectiveTtsModel(MergedLegacyTts(agentProfile!.Tts, globalProfile.Tts, defaults), defaults, 0)
            };
        }

        var source = globalProfile.TtsModels is { Count: > 0 }
            ? globalProfile.TtsModels
            : new List<TextToSpeechConfig> { globalProfile.Tts };

        return NormalizeTtsModels(source, globalProfile.Tts)
            .Select((tts, index) => EffectiveTtsModel(tts, defaults, index))
            .ToList()
            .DefaultIfEmpty(new EffectiveTextToSpeechConfig())
            .ToList();
    }

    private static EffectiveTextToSpeechConfig EffectiveTtsModel(TextToSpeechConfig tts, TextToSpeechConfig defaults, int index)
    {
        var endpointMode = NormalizeEndpointMode(StringValue(tts.EndpointMode, defaults.EndpointMode));
        return new EffectiveTextToSpeechConfig
        {
            Id = CleanId(tts.Id) ?? $"tts-{index + 1}",
            Name = CleanOptional(tts.Name) ?? CleanId(tts.Id) ?? $"TTS model {index + 1}",
            Mode = NormalizeTtsMode(StringValue(tts.Mode, defaults.Mode)),
            AutoPlay = BoolValue(tts.AutoPlay, defaults.AutoPlay) ?? false,
            BaseUrl = EffectiveTtsBaseUrl(endpointMode, StringValue(tts.BaseUrl, defaults.BaseUrl), defaults.BaseUrl),
            EndpointMode = endpointMode,
            EndpointPath = StringValue(tts.EndpointPath) ?? EndpointPathForMode(endpointMode),
            ApiKey = StringValue(tts.ApiKey, defaults.ApiKey) ?? "",
            Model = StringValue(tts.Model, defaults.Model) ?? "gpt-4o-mini-tts",
            Voice = StringValue(tts.Voice, defaults.Voice) ?? "alloy",
            LanguageType = StringValue(tts.LanguageType, defaults.LanguageType) ?? "Chinese",
            Instructions = StringValue(tts.Instructions, defaults.Instructions) ?? "",
            OptimizeInstructions = BoolValue(tts.OptimizeInstructions, defaults.OptimizeInstructions) ?? false,
            Format = NormalizeFormat(StringValue(tts.Format, defaults.Format), "mp3") ?? "mp3"
        };
    }

    private static List<EffectiveWebSearchConfig> EffectiveSearchModels(MultiModalProfile? agentProfile, MultiModalProfile globalProfile)
    {
        if (agentProfile?.SearchModels is { Count: > 0 })
        {
            return NormalizeSearchModels(agentProfile.SearchModels, agentProfile.Search, useProviderPresetsWhenEmpty: false)
                .Select(EffectiveSearchModel)
                .ToList()
                .DefaultIfEmpty(new EffectiveWebSearchConfig())
                .ToList();
        }

        if (HasSearchOverride(agentProfile?.Search))
        {
            return new List<EffectiveWebSearchConfig>
            {
                EffectiveSearchModel(MergedLegacySearch(agentProfile!.Search, globalProfile.Search))
            };
        }

        var source = globalProfile.SearchModels is { Count: > 0 }
            ? globalProfile.SearchModels
            : CreateDefaultSearchModels();

        return NormalizeSearchModels(source, globalProfile.Search, useProviderPresetsWhenEmpty: true)
            .Select(EffectiveSearchModel)
            .ToList()
            .DefaultIfEmpty(new EffectiveWebSearchConfig())
            .ToList();
    }

    private static EffectiveWebSearchConfig EffectiveSearchModel(WebSearchConfig search)
    {
        var provider = NormalizeSearchProvider(search.Provider);
        return new EffectiveWebSearchConfig
        {
            Id = CleanId(search.Id) ?? provider,
            Name = CleanOptional(search.Name) ?? CleanId(search.Id) ?? provider,
            Enabled = BoolValue(search.Enabled) ?? false,
            Provider = provider,
            BaseUrl = StringValue(search.BaseUrl, SearchBaseUrl(provider)) ?? SearchBaseUrl(provider),
            EndpointPath = StringValue(search.EndpointPath, SearchEndpointPath(provider)) ?? SearchEndpointPath(provider),
            ApiKey = StringValue(search.ApiKey) ?? "",
            MaxResults = Math.Clamp(search.MaxResults ?? 5, 1, 20)
        };
    }

    private static TextToSpeechConfig MergedLegacyTts(TextToSpeechConfig agentTts, TextToSpeechConfig globalTts, TextToSpeechConfig defaults) => new()
    {
        Id = StringValue(agentTts.Id, globalTts.Id, defaults.Id),
        Name = StringValue(agentTts.Name, globalTts.Name, defaults.Name),
        Mode = StringValue(agentTts.Mode, globalTts.Mode, defaults.Mode),
        AutoPlay = BoolValue(agentTts.AutoPlay, globalTts.AutoPlay, defaults.AutoPlay),
        BaseUrl = StringValue(agentTts.BaseUrl, globalTts.BaseUrl, defaults.BaseUrl),
        EndpointMode = StringValue(agentTts.EndpointMode, globalTts.EndpointMode, defaults.EndpointMode),
        EndpointPath = StringValue(agentTts.EndpointPath, globalTts.EndpointPath, defaults.EndpointPath),
        ApiKey = StringValue(agentTts.ApiKey, globalTts.ApiKey, defaults.ApiKey),
        Model = StringValue(agentTts.Model, globalTts.Model, defaults.Model),
        Voice = StringValue(agentTts.Voice, globalTts.Voice, defaults.Voice),
        LanguageType = StringValue(agentTts.LanguageType, globalTts.LanguageType, defaults.LanguageType),
        Instructions = StringValue(agentTts.Instructions, globalTts.Instructions, defaults.Instructions),
        OptimizeInstructions = BoolValue(agentTts.OptimizeInstructions, globalTts.OptimizeInstructions, defaults.OptimizeInstructions),
        Format = StringValue(agentTts.Format, globalTts.Format, defaults.Format),
        Extra = MergeExtra(globalTts.Extra, agentTts.Extra)
    };

    private static WebSearchConfig MergedLegacySearch(WebSearchConfig agentSearch, WebSearchConfig globalSearch) => new()
    {
        Id = StringValue(agentSearch.Id, globalSearch.Id),
        Name = StringValue(agentSearch.Name, globalSearch.Name),
        Enabled = BoolValue(agentSearch.Enabled, globalSearch.Enabled),
        Provider = StringValue(agentSearch.Provider, globalSearch.Provider),
        BaseUrl = StringValue(agentSearch.BaseUrl, globalSearch.BaseUrl),
        EndpointPath = StringValue(agentSearch.EndpointPath, globalSearch.EndpointPath),
        ApiKey = StringValue(agentSearch.ApiKey, globalSearch.ApiKey),
        MaxResults = agentSearch.MaxResults ?? globalSearch.MaxResults,
        Extra = MergeExtra(globalSearch.Extra, agentSearch.Extra)
    };

    private static bool HasTtsOverride(TextToSpeechConfig? tts)
    {
        if (tts == null) return false;
        return !string.IsNullOrWhiteSpace(tts.Id)
            || !string.IsNullOrWhiteSpace(tts.Name)
            || !string.IsNullOrWhiteSpace(tts.Mode)
            || tts.AutoPlay.HasValue
            || !string.IsNullOrWhiteSpace(tts.BaseUrl)
            || !string.IsNullOrWhiteSpace(tts.EndpointMode)
            || !string.IsNullOrWhiteSpace(tts.EndpointPath)
            || !string.IsNullOrWhiteSpace(tts.ApiKey)
            || !string.IsNullOrWhiteSpace(tts.Model)
            || !string.IsNullOrWhiteSpace(tts.Voice)
            || !string.IsNullOrWhiteSpace(tts.LanguageType)
            || !string.IsNullOrWhiteSpace(tts.Instructions)
            || tts.OptimizeInstructions.HasValue
            || !string.IsNullOrWhiteSpace(tts.Format)
            || tts.Extra is { Count: > 0 };
    }

    private static bool HasSearchOverride(WebSearchConfig? search)
    {
        if (search == null) return false;
        return !string.IsNullOrWhiteSpace(search.Id)
            || !string.IsNullOrWhiteSpace(search.Name)
            || search.Enabled.HasValue
            || !string.IsNullOrWhiteSpace(search.Provider)
            || !string.IsNullOrWhiteSpace(search.BaseUrl)
            || !string.IsNullOrWhiteSpace(search.EndpointPath)
            || !string.IsNullOrWhiteSpace(search.ApiKey)
            || search.MaxResults.HasValue
            || search.Extra is { Count: > 0 };
    }

    private static ImageGenerationConfig CloneImage(ImageGenerationConfig source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Enabled = source.Enabled,
        BaseUrl = source.BaseUrl,
        EndpointMode = source.EndpointMode,
        ApiKey = source.ApiKey,
        Model = source.Model,
        Size = source.Size,
        Quality = source.Quality,
        OutputFormat = source.OutputFormat,
        Extra = CloneExtra(source.Extra)
    };

    private static TextToSpeechConfig CloneTts(TextToSpeechConfig source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Mode = source.Mode,
        AutoPlay = source.AutoPlay,
        BaseUrl = source.BaseUrl,
        EndpointMode = source.EndpointMode,
        EndpointPath = source.EndpointPath,
        ApiKey = source.ApiKey,
        Model = source.Model,
        Voice = source.Voice,
        LanguageType = source.LanguageType,
        Instructions = source.Instructions,
        OptimizeInstructions = source.OptimizeInstructions,
        Format = source.Format,
        Extra = CloneExtra(source.Extra)
    };

    private static WebSearchConfig CloneSearch(WebSearchConfig source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Enabled = source.Enabled,
        Provider = source.Provider,
        BaseUrl = source.BaseUrl,
        EndpointPath = source.EndpointPath,
        ApiKey = source.ApiKey,
        MaxResults = source.MaxResults,
        Extra = CloneExtra(source.Extra)
    };

    private static Dictionary<string, JsonElement>? MergeExtra(Dictionary<string, JsonElement>? existing, Dictionary<string, JsonElement>? incoming)
    {
        if ((existing == null || existing.Count == 0) && (incoming == null || incoming.Count == 0))
        {
            return null;
        }

        var merged = existing == null
            ? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, JsonElement>(existing, StringComparer.OrdinalIgnoreCase);
        if (incoming != null)
        {
            foreach (var pair in incoming)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
    }

    private static Dictionary<string, JsonElement>? CloneExtra(Dictionary<string, JsonElement>? source) =>
        source == null || source.Count == 0
            ? null
            : new Dictionary<string, JsonElement>(source, StringComparer.OrdinalIgnoreCase);

    private static string UniqueImageId(string seed, HashSet<string> usedIds)
    {
        var candidate = seed;
        var suffix = 2;
        while (usedIds.Contains(candidate))
        {
            candidate = seed + "-" + suffix++;
        }

        return candidate;
    }

    private static string UniqueTtsId(string seed, HashSet<string> usedIds)
    {
        var candidate = seed;
        var suffix = 2;
        while (usedIds.Contains(candidate))
        {
            candidate = seed + "-" + suffix++;
        }

        return candidate;
    }

    private static string UniqueSearchId(string seed, HashSet<string> usedIds)
    {
        var candidate = seed;
        var suffix = 2;
        while (usedIds.Contains(candidate))
        {
            candidate = seed + "-" + suffix++;
        }

        return candidate;
    }

    private static string? CleanId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = new string(value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        while (cleaned.Contains("--", StringComparison.Ordinal)) cleaned = cleaned.Replace("--", "-");
        return cleaned.Trim('-');
    }

    private static string? NormalizeFormat(string? value, string? fallback)
    {
        var format = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().TrimStart('.').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(format) ? null : format;
    }
}

internal static class EffectiveMultiModalConfigExtensions
{
    public static EffectiveMultiModalConfig WithKeyFlags(this EffectiveMultiModalConfig config)
    {
        config.Image.HasApiKey = !string.IsNullOrWhiteSpace(config.Image.ApiKey);
        foreach (var image in config.ImageModels)
        {
            image.HasApiKey = !string.IsNullOrWhiteSpace(image.ApiKey);
        }

        config.Tts.HasApiKey = !string.IsNullOrWhiteSpace(config.Tts.ApiKey);
        foreach (var tts in config.TtsModels)
        {
            tts.HasApiKey = !string.IsNullOrWhiteSpace(tts.ApiKey);
        }

        config.Stt.HasApiKey = !string.IsNullOrWhiteSpace(config.Stt.ApiKey);
        config.Search.HasApiKey = !string.IsNullOrWhiteSpace(config.Search.ApiKey);
        foreach (var search in config.SearchModels)
        {
            search.HasApiKey = !string.IsNullOrWhiteSpace(search.ApiKey);
        }
        return config;
    }
}
