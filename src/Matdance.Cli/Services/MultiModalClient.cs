using System.Net.Http.Headers;
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public sealed class MultiModalClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex UrlRegex = new(@"https?://[^\s)'""<>]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly PathService _path;
    private readonly MultiModalConfigService _config;

    public MultiModalClient(PathService path)
    {
        _path = path;
        _config = new MultiModalConfigService(path);
    }

    public async Task<IReadOnlyList<GeneratedFileResult>> GenerateImageAsync(string agent, ImageGenerationRequest request, CancellationToken ct = default)
    {
        var outcome = await GenerateImageDetailedAsync(agent, request, ct);
        if (!outcome.Success)
        {
            throw new InvalidOperationException(outcome.Error ?? "image_generation failed.");
        }

        return outcome.Results;
    }

    public async Task<ImageGenerationOutcome> GenerateImageDetailedAsync(string agent, ImageGenerationRequest request, CancellationToken ct = default)
    {
        var effective = _config.GetEffective(agent);
        if (string.IsNullOrWhiteSpace(request.Prompt)) throw new InvalidOperationException("prompt is required.");

        var candidates = ImageCandidates(effective, request.ImageProfile, request.AllowProfileFallback).ToList();
        if (candidates.Count == 0)
        {
            throw string.IsNullOrWhiteSpace(request.ImageProfile)
                ? new InvalidOperationException("image_generation is disabled in multimodal settings.")
                : new InvalidOperationException($"image_generation profile '{request.ImageProfile}' is disabled or not configured.");
        }

        Exception? lastError = null;
        var attempts = new List<ImageGenerationAttempt>();
        for (var index = 0; index < candidates.Count; index++)
        {
            var image = candidates[index];
            var attempt = new ImageGenerationAttempt
            {
                Order = index + 1,
                ProfileId = image.Id,
                ProfileName = image.Name,
                Model = image.Model,
                Status = "running",
                StartedAt = UserTimeZoneService.Now()
            };
            attempts.Add(attempt);
            try
            {
                RequireEndpoint(image.BaseUrl, image.ApiKey, $"image_generation profile '{image.Name}'");
                var results = (await GenerateImageWithProfileAsync(agent, image, request, ct)).ToList();
                var fallbackOccurred = attempts.Any(item => string.Equals(item.Status, "failed", StringComparison.OrdinalIgnoreCase));
                foreach (var result in results)
                {
                    result.FallbackOccurred = fallbackOccurred;
                }

                attempt.Status = "succeeded";
                attempt.FinishedAt = UserTimeZoneService.Now();
                return new ImageGenerationOutcome
                {
                    Success = true,
                    FallbackOccurred = fallbackOccurred,
                    Attempts = attempts,
                    Results = results
                };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                attempt.Status = "canceled";
                attempt.Error = "image_generation was canceled.";
                attempt.ErrorType = nameof(OperationCanceledException);
                attempt.FinishedAt = UserTimeZoneService.Now();
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex;
                attempt.Status = "failed";
                attempt.Error = ex.Message;
                attempt.ErrorType = ex.GetType().Name;
                attempt.FinishedAt = UserTimeZoneService.Now();
            }
        }

        return new ImageGenerationOutcome
        {
            Success = false,
            Error = lastError?.Message ?? "image_generation failed before selecting a profile.",
            ErrorType = lastError?.GetType().Name,
            ErrorCategory = ClassifyImageGenerationError(lastError),
            FallbackOccurred = attempts.Count > 1 && attempts.Any(item => string.Equals(item.Status, "failed", StringComparison.OrdinalIgnoreCase)),
            Attempts = attempts
        };
    }

    private async Task<IReadOnlyList<GeneratedFileResult>> GenerateImageWithProfileAsync(string agent, EffectiveImageGenerationConfig image, ImageGenerationRequest request, CancellationToken ct)
    {
        var count = Math.Clamp(request.Count <= 0 ? 1 : request.Count, 1, 4);
        var outputFormat = NormalizeFormat(request.OutputFormat, image.OutputFormat, "png");

        var payload = new Dictionary<string, object?>
        {
            ["model"] = image.Model,
            ["prompt"] = request.Prompt,
            ["n"] = count
        };

        var size = FirstValue(request.Size, image.Size);
        if (!string.IsNullOrWhiteSpace(size)) payload["size"] = size;

        var quality = FirstValue(request.Quality, image.Quality);
        if (!string.IsNullOrWhiteSpace(quality) && !quality.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            payload["quality"] = quality;
        }

        if (!outputFormat.Equals("png", StringComparison.OrdinalIgnoreCase))
        {
            payload["output_format"] = outputFormat;
        }

        var endpoint = Endpoint(image.BaseUrl, "images/generations");
        var modelScopeAsync = IsLikelyModelScope(image.BaseUrl);
        var generationResponse = await SendImageGenerationRequestAsync(endpoint, image.ApiKey, payload, modelScopeAsync, ct);
        if (!generationResponse.Success && !modelScopeAsync && ShouldRetryModelScopeAsync(generationResponse.Body))
        {
            modelScopeAsync = true;
            generationResponse = await SendImageGenerationRequestAsync(endpoint, image.ApiKey, payload, useModelScopeAsync: true, ct);
        }

        if (!generationResponse.Success)
        {
            throw new HttpRequestException($"image_generation failed: {generationResponse.StatusCode} - {Trim(generationResponse.Body, 2000)}");
        }

        using var document = JsonDocument.Parse(generationResponse.Body);
        if (document.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            return await SaveOpenAiImageDataAsync(agent, request, data, outputFormat, image, ct);
        }

        if (TryGetString(document.RootElement, "task_id", out var taskId) || TryGetNestedString(document.RootElement, "data", "task_id", out taskId))
        {
            var urls = await PollModelScopeImageTaskAsync(image.BaseUrl, image.ApiKey, taskId, ct);
            return await SaveImageUrlsAsync(agent, request, urls, outputFormat, image, ct);
        }

        throw new InvalidOperationException("image_generation response did not contain a data array or async task_id.");
    }

    private async Task<IReadOnlyList<GeneratedFileResult>> SaveOpenAiImageDataAsync(
        string agent,
        ImageGenerationRequest request,
        JsonElement data,
        string outputFormat,
        EffectiveImageGenerationConfig image,
        CancellationToken ct)
    {
        var results = new List<GeneratedFileResult>();
        var count = data.GetArrayLength();
        var index = 0;
        foreach (var item in data.EnumerateArray())
        {
            index++;
            byte[] bytes;
            if (item.TryGetProperty("b64_json", out var b64) && b64.ValueKind == JsonValueKind.String)
            {
                bytes = Convert.FromBase64String(b64.GetString() ?? "");
            }
            else if (item.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
            {
                bytes = await GetByteArrayWithReconnectAsync(url.GetString() ?? "", ct);
            }
            else
            {
                continue;
            }

            var filePath = ResolveGeneratedPath(agent, request.OutputPath, request.UseBrowserTemp, "images", "image", outputFormat, index, count);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllBytesAsync(filePath, bytes, ct);
            results.Add(Result(agent, filePath, "image", outputFormat, image: image, imageRequest: request));
        }

        if (results.Count == 0)
        {
            throw new InvalidOperationException("image_generation completed but returned no usable image payload.");
        }

        return results;
    }

    private async Task<IReadOnlyList<GeneratedFileResult>> SaveImageUrlsAsync(
        string agent,
        ImageGenerationRequest request,
        IReadOnlyList<string> urls,
        string outputFormat,
        EffectiveImageGenerationConfig image,
        CancellationToken ct)
    {
        return await SaveImagePayloadsAsync(agent, request, urls.Select(url => ImagePayload.FromUrl(url)).ToList(), outputFormat, image, ct);
    }

    public async Task<GeneratedFileResult> TextToSpeechAsync(string agent, TextToSpeechRequest request, CancellationToken ct = default)
    {
        var effective = _config.GetEffective(agent);
        if (string.IsNullOrWhiteSpace(request.Text)) throw new InvalidOperationException("text is required.");

        var candidates = TtsCandidates(effective, request.TtsProfile, request.AllowProfileFallback).ToList();
        if (candidates.Count == 0)
        {
            throw string.IsNullOrWhiteSpace(request.TtsProfile)
                ? new InvalidOperationException("text_to_speech is disabled in multimodal settings.")
                : new InvalidOperationException($"text_to_speech profile '{request.TtsProfile}' is disabled or not configured.");
        }

        Exception? lastError = null;
        foreach (var tts in candidates)
        {
            try
            {
                RequireEndpoint(tts.BaseUrl, tts.ApiKey, $"text_to_speech profile '{tts.Name}'");
                return await TextToSpeechWithProfileAsync(agent, tts, request, ct);
            }
            catch (Exception ex)
            {
                if (ShouldAttemptTtsChunkFallback(ex))
                {
                    try
                    {
                        return await TextToSpeechWithChunkFallbackAsync(agent, tts, request, ex, ct);
                    }
                    catch (Exception fallbackEx)
                    {
                        if (candidates.Count > 1)
                        {
                            lastError = fallbackEx;
                            continue;
                        }

                        throw;
                    }
                }

                if (candidates.Count > 1)
                {
                    lastError = ex;
                    continue;
                }

                throw;
            }
        }

        throw lastError ?? new InvalidOperationException("text_to_speech failed before selecting a profile.");
    }

    public async Task<WebSearchResult> SearchAsync(string agent, WebSearchRequest request, CancellationToken ct = default)
    {
        var effective = _config.GetEffective(agent);
        if (string.IsNullOrWhiteSpace(request.Query)) throw new InvalidOperationException("query is required.");

        var candidates = SearchCandidates(effective, request.SearchProfile, request.AllowProfileFallback).ToList();
        if (candidates.Count == 0)
        {
            throw string.IsNullOrWhiteSpace(request.SearchProfile)
                ? new InvalidOperationException("web_search is disabled in multimodal settings.")
                : new InvalidOperationException($"web_search profile '{request.SearchProfile}' is disabled or not configured.");
        }

        Exception? lastError = null;
        foreach (var search in candidates)
        {
            try
            {
                RequireEndpoint(search.BaseUrl, search.ApiKey, $"web_search profile '{search.Name}'");
                return await SearchWithProfileAsync(search, request, ct);
            }
            catch (Exception ex) when (candidates.Count > 1)
            {
                lastError = ex;
            }
        }

        throw lastError ?? new InvalidOperationException("web_search failed before selecting a profile.");
    }

    private async Task<WebSearchResult> SearchWithProfileAsync(EffectiveWebSearchConfig search, WebSearchRequest request, CancellationToken ct)
    {
        var maxResults = Math.Clamp(request.MaxResults ?? search.MaxResults, 1, 20);
        return search.Provider.ToLowerInvariant() switch
        {
            "brave" => await SearchBraveAsync(search, request.Query, maxResults, ct),
            "firecrawl" => await SearchFirecrawlAsync(search, request.Query, maxResults, ct),
            _ => await SearchTavilyAsync(search, request.Query, maxResults, ct)
        };
    }

    private async Task<WebSearchResult> SearchTavilyAsync(EffectiveWebSearchConfig search, string query, int maxResults, CancellationToken ct)
    {
        var payload = new Dictionary<string, object?>
        {
            ["query"] = query,
            ["max_results"] = maxResults,
            ["search_depth"] = "basic",
            ["include_answer"] = false,
            ["include_raw_content"] = false,
            ["api_key"] = search.ApiKey
        };

        var response = await SendJsonRequestAsync(Endpoint(search.BaseUrl, SearchEndpoint(search)), search.ApiKey, payload, ct);
        if (!response.Success)
        {
            throw new HttpRequestException($"web_search tavily failed: {response.StatusCode} - {Trim(response.Body, 2000)}");
        }

        using var document = JsonDocument.Parse(response.Body);
        var result = SearchResult(search, query);
        if (TryGetString(document.RootElement, "answer", out var answer))
        {
            result.Answer = answer;
        }
        if (document.RootElement.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in results.EnumerateArray().Take(maxResults))
            {
                result.Items.Add(new WebSearchResultItem
                {
                    Title = FirstJsonString(item, "title"),
                    Url = FirstJsonString(item, "url"),
                    Snippet = FirstJsonString(item, "content", "snippet", "description"),
                    Source = "tavily",
                    Score = TryGetDouble(item, "score")
                });
            }
        }

        return EnsureSearchItems(result);
    }

    private async Task<WebSearchResult> SearchBraveAsync(EffectiveWebSearchConfig search, string query, int maxResults, CancellationToken ct)
    {
        var endpoint = Endpoint(search.BaseUrl, SearchEndpoint(search));
        var separator = endpoint.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var braveResponse = await SendStringRequestWithReconnectAsync(() =>
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}{separator}q={Uri.EscapeDataString(query)}&count={maxResults}");
            httpRequest.Headers.TryAddWithoutValidation("X-Subscription-Token", search.ApiKey);
            httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");
            return httpRequest;
        }, ct);
        var body = braveResponse.Body;
        if (!braveResponse.Success)
        {
            throw new HttpRequestException($"web_search brave failed: {braveResponse.StatusCode} - {Trim(body, 2000)}");
        }

        using var document = JsonDocument.Parse(body);
        var result = SearchResult(search, query);
        if (document.RootElement.TryGetProperty("web", out var web)
            && web.ValueKind == JsonValueKind.Object
            && web.TryGetProperty("results", out var results)
            && results.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in results.EnumerateArray().Take(maxResults))
            {
                result.Items.Add(new WebSearchResultItem
                {
                    Title = FirstJsonString(item, "title"),
                    Url = FirstJsonString(item, "url"),
                    Snippet = FirstJsonString(item, "description", "snippet"),
                    Source = "brave"
                });
            }
        }

        return EnsureSearchItems(result);
    }

    private async Task<WebSearchResult> SearchFirecrawlAsync(EffectiveWebSearchConfig search, string query, int maxResults, CancellationToken ct)
    {
        var payload = new Dictionary<string, object?>
        {
            ["query"] = query,
            ["limit"] = maxResults
        };

        var response = await SendJsonRequestAsync(Endpoint(search.BaseUrl, SearchEndpoint(search)), search.ApiKey, payload, ct);
        if (!response.Success)
        {
            throw new HttpRequestException($"web_search firecrawl failed: {response.StatusCode} - {Trim(response.Body, 2000)}");
        }

        using var document = JsonDocument.Parse(response.Body);
        var result = SearchResult(search, query);
        var results = default(JsonElement);
        if (document.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            results = data;
        }
        else if (document.RootElement.TryGetProperty("results", out var rootResults) && rootResults.ValueKind == JsonValueKind.Array)
        {
            results = rootResults;
        }

        if (results.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in results.EnumerateArray().Take(maxResults))
            {
                result.Items.Add(new WebSearchResultItem
                {
                    Title = FirstJsonString(item, "title"),
                    Url = FirstJsonString(item, "url"),
                    Snippet = FirstJsonString(item, "description", "snippet", "markdown", "content"),
                    Source = "firecrawl"
                });
            }
        }

        return EnsureSearchItems(result);
    }

    private async Task<GeneratedFileResult> TextToSpeechWithProfileAsync(string agent, EffectiveTextToSpeechConfig tts, TextToSpeechRequest request, CancellationToken ct)
    {
        var format = NormalizeFormat(request.Format, tts.Format, "mp3");
        if (IsAliyunQwenTtsMode(tts.EndpointMode))
        {
            return await TextToSpeechViaAliyunQwenAsync(agent, tts, request, ct);
        }

        if (IsChatCompletionsMode(tts.EndpointMode))
        {
            return await TextToSpeechViaChatCompletionsAsync(agent, tts, request, format, ct);
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = tts.Model,
            ["input"] = request.Text,
            ["voice"] = FirstValue(request.Voice, tts.Voice),
            ["response_format"] = format
        };

        var ttsResponse = await SendBytesRequestWithReconnectAsync(() =>
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint(tts.BaseUrl, TtsEndpoint(tts)));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tts.ApiKey);
            httpRequest.Content = JsonContent(payload);
            return httpRequest;
        }, ct);
        var bytes = ttsResponse.Body;
        if (!ttsResponse.Success)
        {
            throw new HttpRequestException($"text_to_speech failed: {ttsResponse.StatusCode} - {Trim(Encoding.UTF8.GetString(bytes), 2000)}");
        }

        var filePath = ResolveGeneratedPath(agent, request.OutputPath, request.UseBrowserTemp, "audio", "speech", format, 1, 1);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, bytes, ct);
        return Result(agent, filePath, "audio", format, tts: tts);
    }

    private async Task<GeneratedFileResult> TextToSpeechWithChunkFallbackAsync(
        string agent,
        EffectiveTextToSpeechConfig tts,
        TextToSpeechRequest request,
        Exception originalError,
        CancellationToken ct)
    {
        var chunks = SplitTextIntoTtsChunks(request.Text, maxChunks: 10);
        if (chunks.Count <= 1)
        {
            throw new InvalidOperationException($"text_to_speech failed and fallback could not split the text into multiple sentence-ended chunks: {originalError.Message}", originalError);
        }

        var stamp = UserTimeZoneService.Now().ToString("yyyyMMdd_HHmmssfff") + "_" + Guid.NewGuid().ToString("N")[..8];
        var chunkRoot = Path.Combine("generated", "audio", ".tts_chunks", stamp);
        var tasks = chunks.Select((chunk, index) =>
        {
            var chunkRequest = CloneTtsRequestForChunk(request, chunk, Path.Combine(chunkRoot, $"part_{index + 1:00}.wav"));
            return TextToSpeechWithProfileAsync(agent, tts, chunkRequest, ct);
        }).ToArray();

        GeneratedFileResult[] chunkResults;
        try
        {
            chunkResults = await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"text_to_speech fallback split the input into {chunks.Count} chunk(s), but at least one chunk still failed. Original error: {originalError.Message}. Chunk retry error: {ex.Message}", ex);
        }

        var nonWav = chunkResults
            .Select((result, index) => new { result, index })
            .FirstOrDefault(item => !item.result.Format.Equals("wav", StringComparison.OrdinalIgnoreCase));
        if (nonWav != null)
        {
            throw new InvalidOperationException($"text_to_speech fallback requires mergeable wav chunk output, but chunk {nonWav.index + 1} returned format '{nonWav.result.Format}'. Original error: {originalError.Message}");
        }

        var finalOutputPath = WithExtension(request.OutputPath, "wav");
        var finalPath = ResolveGeneratedPath(agent, finalOutputPath, request.UseBrowserTemp, "audio", "speech", "wav", 1, 1);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        MergeWaveFiles(chunkResults.Select(result => result.Path).ToList(), finalPath);

        foreach (var result in chunkResults)
        {
            TryDeleteFile(result.Path);
        }

        return Result(agent, finalPath, "audio", "wav", tts: tts);
    }

    private static TextToSpeechRequest CloneTtsRequestForChunk(TextToSpeechRequest source, string text, string outputPath) => new()
    {
        Agent = source.Agent,
        Session = null,
        MessageIndex = null,
        TtsProfile = source.TtsProfile,
        AllowProfileFallback = false,
        Text = text,
        Voice = source.Voice,
        Format = "wav",
        OutputPath = outputPath,
        UseBrowserTemp = source.UseBrowserTemp
    };

    public async Task<string> TranscribeAsync(string agent, Stream audio, string fileName, string contentType, CancellationToken ct = default)
    {
        var effective = _config.GetEffective(agent);
        var stt = effective.Stt;
        if (!stt.Enabled) throw new InvalidOperationException("speech_to_text is disabled in multimodal settings.");
        RequireEndpoint(stt.BaseUrl, stt.ApiKey, "speech_to_text");

        if (IsChatCompletionsMode(stt.EndpointMode))
        {
            return await TranscribeViaChatCompletionsAsync(stt, audio, fileName, contentType, ct);
        }

        using var audioBuffer = new MemoryStream();
        await audio.CopyToAsync(audioBuffer, ct);
        var audioBytes = audioBuffer.ToArray();
        var response = await SendStringRequestWithReconnectAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint(stt.BaseUrl, "audio/transcriptions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", stt.ApiKey);
            var form = new MultipartFormDataContent();
            form.Add(new StringContent(stt.Model), "model");
            var fileContent = new ByteArrayContent(audioBytes);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            }

            form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "audio.webm" : fileName);
            request.Content = form;
            return request;
        }, ct);
        var body = response.Body;
        if (!response.Success)
        {
            throw new HttpRequestException($"speech_to_text failed: {response.StatusCode} - {Trim(body, 2000)}");
        }

        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("text", out var text))
        {
            return text.GetString() ?? "";
        }

        return body;
    }

    private async Task<GeneratedFileResult> TextToSpeechViaChatCompletionsAsync(
        string agent,
        EffectiveTextToSpeechConfig tts,
        TextToSpeechRequest request,
        string format,
        CancellationToken ct)
    {
        var voice = FirstValue(request.Voice, tts.Voice);
        var endpoint = Endpoint(tts.BaseUrl, TtsEndpoint(tts));
        var payload = new
        {
            model = tts.Model,
            stream = false,
            modalities = new[] { "text", "audio" },
            audio = new
            {
                voice,
                format
            },
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = request.Text
                }
            }
        };

        var ttsResponse = await SendJsonRequestAsync(endpoint, tts.ApiKey, payload, ct);
        var body = ttsResponse.Body;
        if (!ttsResponse.Success && ShouldRetryPlainChatTts(ttsResponse.Body))
        {
            var plainPayload = new
            {
                model = tts.Model,
                stream = false,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Synthesize the following text as speech. Voice: {voice}. Format: {format}. Return an audio URL or base64 audio payload only.\n\n{request.Text}"
                    }
                }
            };

            var fallbackResponse = await SendJsonRequestAsync(endpoint, tts.ApiKey, plainPayload, ct);
            if (!fallbackResponse.Success)
            {
                throw new HttpRequestException($"text_to_speech chat_completions failed: {fallbackResponse.StatusCode} - {Trim(fallbackResponse.Body, 1600)}. The first audio-modalities attempt also failed: {ttsResponse.StatusCode} - {Trim(ttsResponse.Body, 900)}. This relay probably does not support TTS through chat/completions; try v1/tts or v1/audio/speech.");
            }

            body = fallbackResponse.Body;
        }
        else if (!ttsResponse.Success)
        {
            throw new HttpRequestException($"text_to_speech chat_completions failed: {ttsResponse.StatusCode} - {Trim(body, 2000)}");
        }

        using var document = JsonDocument.Parse(body);
        var audioPayload = ExtractAudioPayload(document.RootElement);
        if (audioPayload.Bytes == null && string.IsNullOrWhiteSpace(audioPayload.Url))
        {
            throw new InvalidOperationException("text_to_speech chat_completions returned no usable audio payload. If this relay only supports plain chat text, use v1/tts or v1/audio/speech instead.");
        }

        var bytes = audioPayload.Bytes ?? await GetByteArrayWithReconnectAsync(audioPayload.Url!, ct);
        var fileFormat = FirstValue(audioPayload.Format, GuessAudioFormat(audioPayload.Url), format);
        var filePath = ResolveGeneratedPath(agent, request.OutputPath, request.UseBrowserTemp, "audio", "speech", fileFormat, 1, 1);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, bytes, ct);
        return Result(agent, filePath, "audio", fileFormat, tts: tts);
    }

    private async Task<GeneratedFileResult> TextToSpeechViaAliyunQwenAsync(
        string agent,
        EffectiveTextToSpeechConfig tts,
        TextToSpeechRequest request,
        CancellationToken ct)
    {
        var input = new Dictionary<string, object?>
        {
            ["text"] = request.Text,
            ["voice"] = FirstValue(request.Voice, tts.Voice),
            ["language_type"] = FirstValue(tts.LanguageType, "Chinese")
        };

        if (!string.IsNullOrWhiteSpace(tts.Instructions))
        {
            input["instructions"] = tts.Instructions;
        }

        if (tts.OptimizeInstructions)
        {
            input["optimize_instructions"] = true;
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = tts.Model,
            ["input"] = input
        };

        var response = await SendJsonRequestAsync(Endpoint(tts.BaseUrl, TtsEndpoint(tts)), tts.ApiKey, payload, ct);
        if (!response.Success)
        {
            throw new HttpRequestException($"text_to_speech aliyun_qwen_tts failed: {response.StatusCode} - {Trim(response.Body, 2000)}");
        }

        using var document = JsonDocument.Parse(response.Body);
        var audioPayload = ExtractAudioPayload(document.RootElement);
        if (audioPayload.Bytes == null && string.IsNullOrWhiteSpace(audioPayload.Url))
        {
            throw new InvalidOperationException("text_to_speech aliyun_qwen_tts returned no usable output.audio.url or audio payload.");
        }

        var bytes = audioPayload.Bytes ?? await GetByteArrayWithReconnectAsync(audioPayload.Url!, ct);
        var fileFormat = FirstValue(audioPayload.Format, GuessAudioFormat(audioPayload.Url), "wav");
        var filePath = ResolveGeneratedPath(agent, request.OutputPath, request.UseBrowserTemp, "audio", "speech", fileFormat, 1, 1);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, bytes, ct);
        return Result(agent, filePath, "audio", fileFormat, tts: tts);
    }

    private static async Task<string> TranscribeViaChatCompletionsAsync(
        EffectiveSpeechToTextConfig stt,
        Stream audio,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await audio.CopyToAsync(buffer, ct);
        var audioData = Convert.ToBase64String(buffer.ToArray());
        var audioFormat = GuessAudioInputFormat(fileName, contentType);
        var payload = new
        {
            model = stt.Model,
            stream = false,
            temperature = 0,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Transcribe this audio. Return only the transcript text." },
                        new
                        {
                            type = "input_audio",
                            input_audio = new
                            {
                                data = audioData,
                                format = audioFormat
                            }
                        }
                    }
                }
            }
        };

        var sttResponse = await SendStringRequestWithReconnectAsync(() =>
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint(stt.BaseUrl, "chat/completions"));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", stt.ApiKey);
            httpRequest.Content = JsonContent(payload);
            return httpRequest;
        }, ct);
        var body = sttResponse.Body;
        if (!sttResponse.Success)
        {
            throw new HttpRequestException($"speech_to_text chat_completions failed: {sttResponse.StatusCode} - {Trim(body, 2000)}");
        }

        using var document = JsonDocument.Parse(body);
        if (TryGetString(document.RootElement, "text", out var directText))
        {
            return directText;
        }

        if (TryExtractFirstChoiceText(document.RootElement, out var text))
        {
            return text;
        }

        return body;
    }

    private static async Task<(bool Success, int StatusCode, string Body)> SendImageGenerationRequestAsync(
        string endpoint,
        string apiKey,
        Dictionary<string, object?> payload,
        bool useModelScopeAsync,
        CancellationToken ct)
    {
        return await SendStringRequestWithReconnectAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            if (useModelScopeAsync)
            {
                request.Headers.TryAddWithoutValidation("X-ModelScope-Async-Mode", "true");
            }

            request.Content = JsonContent(payload);
            return request;
        }, ct);
    }

    private static async Task<(bool Success, int StatusCode, string Body)> SendJsonRequestAsync(
        string endpoint,
        string apiKey,
        object payload,
        CancellationToken ct)
    {
        return await SendStringRequestWithReconnectAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent(payload);
            return request;
        }, ct);
    }

    private static async Task<IReadOnlyList<string>> PollModelScopeImageTaskAsync(string baseUrl, string apiKey, string taskId, CancellationToken ct)
    {
        var taskEndpoint = Endpoint(baseUrl, "tasks/" + Uri.EscapeDataString(taskId));
        const int maxAttempts = 300;
        var delay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var pollResponse = await SendStringRequestWithReconnectAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, taskEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Headers.TryAddWithoutValidation("X-ModelScope-Task-Type", "image_generation");
                return request;
            }, ct);
            var body = pollResponse.Body;
            if (!pollResponse.Success)
            {
                throw new HttpRequestException($"image_generation async task poll failed: {pollResponse.StatusCode} - {Trim(body, 2000)}");
            }

            using var document = JsonDocument.Parse(body);
            var urls = ExtractModelScopeImageUrls(document.RootElement);
            var status = FirstValue(
                TryGetString(document.RootElement, "task_status", out var rootStatus) ? rootStatus : null,
                TryGetString(document.RootElement, "status", out var statusValue) ? statusValue : null,
                TryGetNestedString(document.RootElement, "data", "task_status", out var dataStatus) ? dataStatus : null,
                TryGetNestedString(document.RootElement, "output", "task_status", out var outputStatus) ? outputStatus : null);

            if (IsTerminalSuccessStatus(status))
            {
                return urls.Count > 0
                    ? urls
                    : throw new InvalidOperationException("image_generation async task succeeded but returned no output_images.");
            }

            if (urls.Count > 0 && string.IsNullOrWhiteSpace(status))
            {
                return urls;
            }

            if (IsTerminalFailureStatus(status))
            {
                var message = FirstValue(
                    TryGetString(document.RootElement, "message", out var messageValue) ? messageValue : null,
                    TryGetNestedString(document.RootElement, "errors", "message", out var errorMessage) ? errorMessage : null,
                    body);
                throw new InvalidOperationException($"image_generation async task failed: {Trim(message, 2000)}");
            }

            await Task.Delay(delay, ct);
        }

        throw new TimeoutException("image_generation async task timed out waiting for ModelScope result.");
    }

    public static string ClassifyImageGenerationError(Exception? ex)
    {
        if (ex == null) return "unknown";
        var text = (ex.Message ?? string.Empty).ToLowerInvariant();
        if (ex is OperationCanceledException) return "canceled";
        if (ex is TimeoutException || text.Contains("timeout") || text.Contains("timed out")) return "timeout";
        if (text.Contains("401") || text.Contains("403") || text.Contains("unauthor") || text.Contains("forbidden") || text.Contains("api key") || text.Contains("apikey") || text.Contains("auth")) return "auth";
        if (text.Contains("quota") || text.Contains("credit") || text.Contains("balance") || text.Contains("billing") || text.Contains("insufficient") || text.Contains("429")) return "quota";
        if (text.Contains("model") && (text.Contains("not found") || text.Contains("unavailable") || text.Contains("disabled"))) return "model_unavailable";
        if (text.Contains("safety") || text.Contains("policy") || text.Contains("moderation") || text.Contains("unsafe") || text.Contains("content")) return "content_policy";
        if (ex is HttpRequestException || text.Contains("dns") || text.Contains("connection") || text.Contains("network") || text.Contains("temporarily")) return "network";
        return "unknown";
    }

    private static StringContent JsonContent(object payload) =>
        new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

    private static async Task<(bool Success, int StatusCode, string Body)> SendStringRequestWithReconnectAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken ct)
    {
        var maxAttempts = ReconnectRetryPolicy.TotalAttempts;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var request = requestFactory();
                using var response = await Http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                var statusCode = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode && IsReconnectRetryableStatus(statusCode) && attempt < maxAttempts)
                {
                    await DelayReconnectRetryAsync(attempt, ct);
                    continue;
                }

                return (response.IsSuccessStatusCode, statusCode, body);
            }
            catch (Exception ex) when (IsReconnectRetryableException(ex) && attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                await DelayReconnectRetryAsync(attempt, ct);
            }
        }

        throw new TimeoutException("HTTP request reconnect retry budget was exhausted.");
    }

    private static async Task<(bool Success, int StatusCode, byte[] Body)> SendBytesRequestWithReconnectAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken ct)
    {
        var maxAttempts = ReconnectRetryPolicy.TotalAttempts;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var request = requestFactory();
                using var response = await Http.SendAsync(request, ct);
                var body = await response.Content.ReadAsByteArrayAsync(ct);
                var statusCode = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode && IsReconnectRetryableStatus(statusCode) && attempt < maxAttempts)
                {
                    await DelayReconnectRetryAsync(attempt, ct);
                    continue;
                }

                return (response.IsSuccessStatusCode, statusCode, body);
            }
            catch (Exception ex) when (IsReconnectRetryableException(ex) && attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                await DelayReconnectRetryAsync(attempt, ct);
            }
        }

        throw new TimeoutException("HTTP bytes request reconnect retry budget was exhausted.");
    }

    private static async Task<byte[]> GetByteArrayWithReconnectAsync(string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Cannot download an empty URL.");

        var response = await SendBytesRequestWithReconnectAsync(() => new HttpRequestMessage(HttpMethod.Get, url), ct);
        if (!response.Success)
            throw new HttpRequestException($"download failed: {response.StatusCode} - {Trim(Encoding.UTF8.GetString(response.Body), 1200)}");

        return response.Body;
    }

    private static async Task DelayReconnectRetryAsync(int failedAttempt, CancellationToken ct)
    {
        var step = ReconnectRetryPolicy.GetStepAfterFailure(failedAttempt) ?? throw new TimeoutException("Reconnect retry budget was exhausted.");
        await Task.Delay(step.Delay, ct);
    }

    private static bool IsReconnectRetryableStatus(int status)
    {
        return status == 408 || status == 409 || status == 429 || status >= 500;
    }

    private static bool IsReconnectRetryableException(Exception ex)
    {
        return ex is HttpRequestException
            || ex is IOException
            || ex is TaskCanceledException
            || ex is TimeoutException;
    }

    private static void RequireEndpoint(string baseUrl, string apiKey, string name)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new InvalidOperationException($"{name} base_url is not configured.");
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException($"{name} api_key is not configured.");
    }

    private static IEnumerable<EffectiveImageGenerationConfig> ImageCandidates(
        EffectiveMultiModalConfig effective,
        string? requestedProfile,
        bool allowFallback)
    {
        var enabled = (effective.ImageModels.Count > 0 ? effective.ImageModels : new List<EffectiveImageGenerationConfig> { effective.Image })
            .Where(model => model.Enabled)
            .ToList();
        if (enabled.Count == 0) return enabled;

        if (string.IsNullOrWhiteSpace(requestedProfile))
        {
            return enabled;
        }

        var requested = requestedProfile.Trim();
        var selected = enabled
            .Where(model => MatchesImageProfile(model, requested))
            .ToList();
        if (selected.Count == 0)
        {
            return allowFallback ? enabled : Array.Empty<EffectiveImageGenerationConfig>();
        }

        return allowFallback
            ? selected.Concat(enabled.Where(model => selected.All(item => !item.Id.Equals(model.Id, StringComparison.OrdinalIgnoreCase))))
            : selected;
    }

    private static bool MatchesImageProfile(EffectiveImageGenerationConfig model, string requested) =>
        model.Id.Equals(requested, StringComparison.OrdinalIgnoreCase)
        || model.Name.Equals(requested, StringComparison.OrdinalIgnoreCase)
        || model.Model.Equals(requested, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<EffectiveTextToSpeechConfig> TtsCandidates(
        EffectiveMultiModalConfig effective,
        string? requestedProfile,
        bool allowFallback)
    {
        var enabled = (effective.TtsModels.Count > 0 ? effective.TtsModels : new List<EffectiveTextToSpeechConfig> { effective.Tts })
            .Where(model => !model.Mode.Equals("off", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (enabled.Count == 0) return enabled;

        if (string.IsNullOrWhiteSpace(requestedProfile))
        {
            return enabled;
        }

        var requested = requestedProfile.Trim();
        var selected = enabled
            .Where(model => MatchesTtsProfile(model, requested))
            .ToList();
        if (selected.Count == 0)
        {
            return allowFallback ? enabled : Array.Empty<EffectiveTextToSpeechConfig>();
        }

        return allowFallback
            ? selected.Concat(enabled.Where(model => selected.All(item => !item.Id.Equals(model.Id, StringComparison.OrdinalIgnoreCase))))
            : selected;
    }

    private static bool MatchesTtsProfile(EffectiveTextToSpeechConfig model, string requested) =>
        model.Id.Equals(requested, StringComparison.OrdinalIgnoreCase)
        || model.Name.Equals(requested, StringComparison.OrdinalIgnoreCase)
        || model.Model.Equals(requested, StringComparison.OrdinalIgnoreCase)
        || model.Voice.Equals(requested, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<EffectiveWebSearchConfig> SearchCandidates(
        EffectiveMultiModalConfig effective,
        string? requestedProfile,
        bool allowFallback)
    {
        var enabled = (effective.SearchModels.Count > 0 ? effective.SearchModels : new List<EffectiveWebSearchConfig> { effective.Search })
            .Where(model => model.Enabled)
            .ToList();
        if (enabled.Count == 0) return enabled;

        if (string.IsNullOrWhiteSpace(requestedProfile))
        {
            return enabled;
        }

        var requested = requestedProfile.Trim();
        var selected = enabled
            .Where(model => MatchesSearchProfile(model, requested))
            .ToList();
        if (selected.Count == 0)
        {
            return allowFallback ? enabled : Array.Empty<EffectiveWebSearchConfig>();
        }

        return allowFallback
            ? selected.Concat(enabled.Where(model => selected.All(item => !item.Id.Equals(model.Id, StringComparison.OrdinalIgnoreCase))))
            : selected;
    }

    private static bool MatchesSearchProfile(EffectiveWebSearchConfig model, string requested) =>
        model.Id.Equals(requested, StringComparison.OrdinalIgnoreCase)
        || model.Name.Equals(requested, StringComparison.OrdinalIgnoreCase)
        || model.Provider.Equals(requested, StringComparison.OrdinalIgnoreCase);

    private static bool IsChatCompletionsMode(string? endpointMode) =>
        endpointMode?.Equals("chat_completions", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsAliyunQwenTtsMode(string? endpointMode) =>
        endpointMode?.Equals("aliyun_qwen_tts", StringComparison.OrdinalIgnoreCase) == true;

    private static string TtsEndpoint(EffectiveTextToSpeechConfig tts) =>
        IsChatCompletionsMode(tts.EndpointMode) ? "chat/completions" :
        IsAliyunQwenTtsMode(tts.EndpointMode) ? "services/aigc/multimodal-generation/generation" :
        tts.EndpointMode.Equals("tts", StringComparison.OrdinalIgnoreCase) ? "tts" :
        FirstValue(tts.EndpointPath, "audio/speech");

    private static string SearchEndpoint(EffectiveWebSearchConfig search) =>
        FirstValue(search.EndpointPath, search.Provider.Equals("brave", StringComparison.OrdinalIgnoreCase) ? "res/v1/web/search" :
            search.Provider.Equals("firecrawl", StringComparison.OrdinalIgnoreCase) ? "v1/search" :
            "search");

    private static bool IsLikelyModelScope(string baseUrl) =>
        baseUrl.Contains("modelscope", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldRetryModelScopeAsync(string body) =>
        body.Contains("X-ModelScope-Async-Mode", StringComparison.OrdinalIgnoreCase)
        || body.Contains("does not support synchronous calls", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldRetryPlainChatTts(string body) =>
        body.Contains("invalid argument", StringComparison.OrdinalIgnoreCase)
        || body.Contains("upstream_error", StringComparison.OrdinalIgnoreCase)
        || body.Contains("modalities", StringComparison.OrdinalIgnoreCase)
        || body.Contains("\"audio\"", StringComparison.OrdinalIgnoreCase)
        || body.Contains("audio", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalSuccessStatus(string status) =>
        status.Equals("SUCCEED", StringComparison.OrdinalIgnoreCase)
        || status.Equals("SUCCEEDED", StringComparison.OrdinalIgnoreCase)
        || status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase)
        || status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase)
        || status.Equals("FINISHED", StringComparison.OrdinalIgnoreCase)
        || status.Equals("DONE", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalFailureStatus(string status) =>
        status.Equals("FAILED", StringComparison.OrdinalIgnoreCase)
        || status.Equals("FAIL", StringComparison.OrdinalIgnoreCase)
        || status.Equals("ERROR", StringComparison.OrdinalIgnoreCase)
        || status.Equals("CANCELED", StringComparison.OrdinalIgnoreCase)
        || status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        if (property.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
        {
            value = property.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private static string FirstJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetString(element, propertyName, out var value))
            {
                return Trim(value, propertyName is "markdown" or "content" ? 900 : 500);
            }
        }

        return string.Empty;
    }

    private static double? TryGetDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value))
        {
            return value;
        }

        if (property.ValueKind == JsonValueKind.String && double.TryParse(property.GetString(), out value))
        {
            return value;
        }

        return null;
    }

    private static WebSearchResult SearchResult(EffectiveWebSearchConfig search, string query) => new()
    {
        Query = query,
        Provider = search.Provider,
        SearchProfileId = search.Id,
        SearchProfileName = search.Name
    };

    private static WebSearchResult EnsureSearchItems(WebSearchResult result)
    {
        result.Items = result.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Url) || !string.IsNullOrWhiteSpace(item.Title) || !string.IsNullOrWhiteSpace(item.Snippet))
            .Select(item =>
            {
                item.Title = Trim(item.Title ?? string.Empty, 300);
                item.Url = Trim(item.Url ?? string.Empty, 1000);
                item.Snippet = Trim(item.Snippet ?? string.Empty, 1000);
                return item;
            })
            .ToList();
        if (result.Items.Count == 0)
        {
            throw new InvalidOperationException($"web_search {result.Provider} returned no usable result items.");
        }

        return result;
    }

    private static bool TryGetNestedString(JsonElement element, string parentName, string propertyName, out string value)
    {
        value = string.Empty;
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(parentName, out var parent)
            && TryGetString(parent, propertyName, out value);
    }

    private static IReadOnlyList<string> ExtractModelScopeImageUrls(JsonElement root)
    {
        var urls = new List<string>();
        AddImageUrls(root, urls);
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "data", "output", "result" })
            {
                if (root.TryGetProperty(propertyName, out var child))
                {
                    AddImageUrls(child, urls);
                }
            }
        }

        return urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<GeneratedFileResult>> SaveImagePayloadsAsync(
        string agent,
        ImageGenerationRequest request,
        IReadOnlyList<ImagePayload> payloads,
        string fallbackFormat,
        EffectiveImageGenerationConfig image,
        CancellationToken ct)
    {
        var results = new List<GeneratedFileResult>();
        var count = payloads.Count;
        var index = 0;
        foreach (var payload in payloads)
        {
            index++;
            var bytes = payload.Bytes ?? await GetByteArrayWithReconnectAsync(payload.Url!, ct);
            var fileFormat = FirstValue(payload.Format, GuessImageFormat(payload.Url), fallbackFormat);
            var filePath = ResolveGeneratedPath(agent, request.OutputPath, request.UseBrowserTemp, "images", "image", fileFormat, index, count);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllBytesAsync(filePath, bytes, ct);
            results.Add(Result(agent, filePath, "image", fileFormat, image: image, imageRequest: request));
        }

        if (results.Count == 0)
        {
            throw new InvalidOperationException("image_generation completed but returned no usable image payload.");
        }

        return results;
    }

    private static AudioPayload ExtractAudioPayload(JsonElement root)
    {
        var payload = FindAudioPayload(root);
        if (payload.Bytes != null || !string.IsNullOrWhiteSpace(payload.Url))
        {
            return payload;
        }

        foreach (var content in ExtractChoiceContentStrings(root))
        {
            payload = FindAudioPayloadInString(content);
            if (payload.Bytes != null || !string.IsNullOrWhiteSpace(payload.Url))
            {
                return payload;
            }
        }

        return payload;
    }

    private static AudioPayload FindAudioPayload(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return FindAudioPayloadInString(element.GetString());
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var itemPayload = FindAudioPayload(item);
                    if (itemPayload.Bytes != null || !string.IsNullOrWhiteSpace(itemPayload.Url)) return itemPayload;
                }

                return AudioPayload.Empty;
            case JsonValueKind.Object:
                break;
            default:
                return AudioPayload.Empty;
        }

        if (element.TryGetProperty("audio", out var audioObject))
        {
            var audioPayload = FindAudioPayload(audioObject);
            if (audioPayload.Bytes != null || !string.IsNullOrWhiteSpace(audioPayload.Url)) return audioPayload;
        }

        foreach (var propertyName in new[] { "data", "b64_json", "base64", "audio_base64" })
        {
            if (TryGetString(element, propertyName, out var b64) && TryDecodeBase64Payload(b64, out var bytes, out var format))
            {
                return AudioPayload.FromBytes(bytes, format);
            }
        }

        foreach (var propertyName in new[] { "url", "audio_url" })
        {
            if (TryGetString(element, propertyName, out var url) && IsHttpUrl(url))
            {
                return AudioPayload.FromUrl(url, GuessAudioFormat(url));
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("transcript")) continue;
            var childPayload = FindAudioPayload(property.Value);
            if (childPayload.Bytes != null || !string.IsNullOrWhiteSpace(childPayload.Url)) return childPayload;
        }

        return AudioPayload.Empty;
    }

    private static AudioPayload FindAudioPayloadInString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return AudioPayload.Empty;
        var trimmed = value.Trim();
        if ((trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal))
            && TryParseJson(trimmed, out var document))
        {
            using (document)
            {
                return FindAudioPayload(document.RootElement);
            }
        }

        if (TryDecodeBase64Payload(trimmed, out var bytes, out var format))
        {
            return AudioPayload.FromBytes(bytes, format);
        }

        foreach (Match match in UrlRegex.Matches(trimmed))
        {
            var url = match.Value.TrimEnd('.', ',', ';');
            return AudioPayload.FromUrl(url, GuessAudioFormat(url));
        }

        return AudioPayload.Empty;
    }

    private static void AddImageUrls(JsonElement element, List<string> urls)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            AddPotentialUrl(element.GetString(), urls);
            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                AddImageUrls(item, urls);
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var propertyName in new[] { "output_images", "images", "urls", "data" })
        {
            if (element.TryGetProperty(propertyName, out var child))
            {
                AddImageUrls(child, urls);
            }
        }

        foreach (var propertyName in new[] { "url", "image_url", "image" })
        {
            if (TryGetString(element, propertyName, out var url))
            {
                AddPotentialUrl(url, urls);
            }
        }
    }

    private static void AddPotentialUrl(string? value, List<string> urls)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var trimmed = value.Trim();
        if (IsHttpUrl(trimmed))
        {
            urls.Add(trimmed);
        }
    }

    private static IEnumerable<string> ExtractChoiceContentStrings(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var choice in choices.EnumerateArray())
        {
            if (!choice.TryGetProperty("message", out var message)) continue;

            if (TryGetString(message, "content", out var content))
            {
                yield return content;
            }
            else if (message.TryGetProperty("content", out var contentElement))
            {
                foreach (var item in FlattenStringValues(contentElement))
                {
                    yield return item;
                }
            }
        }
    }

    private static bool TryExtractFirstChoiceText(JsonElement root, out string text)
    {
        text = string.Empty;
        foreach (var content in ExtractChoiceContentStrings(root))
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                text = content.Trim();
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> FlattenStringValues(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                yield return element.GetString() ?? "";
                yield break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var value in FlattenStringValues(item))
                    {
                        yield return value;
                    }
                }

                yield break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var value in FlattenStringValues(property.Value))
                    {
                        yield return value;
                    }
                }

                yield break;
        }
    }

    private static bool TryDecodeBase64Payload(string value, out byte[] bytes, out string? format)
    {
        bytes = Array.Empty<byte>();
        format = null;
        var trimmed = value.Trim();
        var commaIndex = trimmed.IndexOf(',');
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex > 0)
        {
            var metadata = trimmed[..commaIndex];
            format = GuessFormatFromDataUri(metadata);
            trimmed = trimmed[(commaIndex + 1)..];
        }

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trimmed.Length < 64 || trimmed.Any(char.IsWhiteSpace))
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(trimmed);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryParseJson(string value, out JsonDocument document)
    {
        document = null!;
        try
        {
            document = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsHttpUrl(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string? GuessFormatFromDataUri(string metadata)
    {
        var slash = metadata.IndexOf('/');
        if (slash < 0) return null;
        var suffix = metadata[(slash + 1)..];
        var semicolon = suffix.IndexOf(';');
        if (semicolon >= 0) suffix = suffix[..semicolon];
        suffix = suffix.Trim().ToLowerInvariant();
        return suffix == "jpg" ? "jpeg" : suffix;
    }

    private static string? GuessImageFormat(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "jpg" => "jpeg",
            "jpeg" or "png" or "webp" or "gif" => extension,
            _ => null
        };
    }

    private static string? GuessAudioFormat(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "mp3" or "wav" or "webm" or "ogg" or "opus" or "m4a" or "aac" or "flac" => extension,
            _ => null
        };
    }

    private static string GuessAudioInputFormat(string fileName, string contentType)
    {
        var fromName = GuessAudioFormat(fileName);
        if (!string.IsNullOrWhiteSpace(fromName)) return fromName;
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var subtype = contentType.Split(';', 2)[0].Split('/').LastOrDefault()?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(subtype))
            {
                return subtype == "mpeg" ? "mp3" : subtype;
            }
        }

        return "webm";
    }

    private static bool ShouldAttemptTtsChunkFallback(Exception exception)
    {
        var message = FlattenExceptionMessage(exception).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var nonRetryable = new[]
        {
            "api_key", "api key", "unauthorized", "authentication", "forbidden", "401", "403",
            "not configured", "disabled", "profile", "model not found", "invalid model",
            "base_url", "base url", "invalid token", "access token"
        };
        if (nonRetryable.Any(marker => message.Contains(marker, StringComparison.Ordinal)))
            return false;

        var retryable = new[]
        {
            "413", "408", "504", "timeout", "timed out", "too long", "length", "payload",
            "request entity too large", "content too large", "input is too long", "exceed",
            "exceeded", "maximum", "max tokens", "context length", "body too large"
        };
        return retryable.Any(marker => message.Contains(marker, StringComparison.Ordinal));
    }

    private static string FlattenExceptionMessage(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
                messages.Add(current.Message);
        }

        return string.Join(" ", messages);
    }

    private static List<string> SplitTextIntoTtsChunks(string text, int maxChunks)
    {
        var normalized = Regex.Replace((text ?? string.Empty).Trim(), @"\s+", " ");
        if (string.IsNullOrWhiteSpace(normalized))
            return new List<string>();

        var sentences = Regex.Matches(normalized, @"[^。．.!?！？；;]+[。．.!?！？；;]+|[^。．.!?！？；;]+")
            .Select(match => match.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
        if (sentences.Count <= 1)
            return new List<string> { normalized };

        var limit = Math.Clamp(maxChunks, 1, 10);
        var targetLength = Math.Max(1, (int)Math.Ceiling(normalized.Length / (double)Math.Min(limit, sentences.Count)));
        var chunks = new List<string>();
        var current = new StringBuilder();

        for (var i = 0; i < sentences.Count; i++)
        {
            var sentence = sentences[i];
            var remainingSentences = sentences.Count - i;
            var remainingChunkSlots = limit - chunks.Count;
            var shouldFlush = current.Length > 0
                && remainingChunkSlots > 1
                && current.Length + 1 + sentence.Length > targetLength
                && remainingSentences >= remainingChunkSlots;

            if (shouldFlush)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }

            if (current.Length > 0)
                current.Append(' ');
            current.Append(sentence);
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        while (chunks.Count > limit)
        {
            var last = chunks[^1];
            chunks.RemoveAt(chunks.Count - 1);
            chunks[^1] = chunks[^1] + " " + last;
        }

        return chunks;
    }

    private static string? WithExtension(string? outputPath, string extension)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            return outputPath;

        var ext = "." + extension.TrimStart('.');
        return Path.HasExtension(outputPath) ? Path.ChangeExtension(outputPath, ext) : outputPath;
    }

    private static void MergeWaveFiles(IReadOnlyList<string> chunkPaths, string outputPath)
    {
        if (chunkPaths.Count == 0)
            throw new InvalidOperationException("No TTS wav chunks were generated.");

        var parts = chunkPaths.Select(ReadWavePart).ToList();
        var format = parts[0].FormatChunk;
        for (var i = 1; i < parts.Count; i++)
        {
            if (!parts[i].FormatChunk.SequenceEqual(format))
            {
                throw new InvalidOperationException($"Cannot merge TTS wav chunks because chunk {i + 1} has a different wav format.");
            }
        }

        var dataSize = parts.Sum(part => (long)part.DataChunk.Length);
        var riffSize = 4L + 8 + format.Length + (format.Length % 2) + 8 + dataSize + (dataSize % 2);
        if (dataSize > uint.MaxValue || riffSize > uint.MaxValue)
        {
            throw new InvalidOperationException("Merged TTS wav output is too large for a RIFF/WAVE file.");
        }

        using var stream = File.Create(outputPath);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write((uint)riffSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write((uint)format.Length);
        writer.Write(format);
        if (format.Length % 2 == 1)
            writer.Write((byte)0);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write((uint)dataSize);
        foreach (var part in parts)
            writer.Write(part.DataChunk);
        if (dataSize % 2 == 1)
            writer.Write((byte)0);
    }

    private static WavePart ReadWavePart(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 12
            || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF"
            || Encoding.ASCII.GetString(bytes, 8, 4) != "WAVE")
        {
            throw new InvalidOperationException($"TTS chunk is not a RIFF/WAVE file: {path}");
        }

        byte[]? format = null;
        byte[]? data = null;
        var offset = 12;
        while (offset + 8 <= bytes.Length)
        {
            var id = Encoding.ASCII.GetString(bytes, offset, 4);
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4, 4));
            offset += 8;
            if (size < 0 || offset + size > bytes.Length)
                throw new InvalidOperationException($"Invalid wav chunk size in TTS chunk: {path}");

            if (id == "fmt ")
                format = bytes.AsSpan(offset, size).ToArray();
            else if (id == "data")
                data = bytes.AsSpan(offset, size).ToArray();

            offset += size;
            if (size % 2 == 1)
                offset++;
        }

        if (format == null || data == null)
            throw new InvalidOperationException($"TTS chunk is missing fmt or data chunks: {path}");

        return new WavePart(format, data);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static string Endpoint(string baseUrl, string relative)
    {
        var trimmedBase = baseUrl.TrimEnd('/');
        var trimmedRelative = relative.Trim('/');
        return trimmedBase.EndsWith("/" + trimmedRelative, StringComparison.OrdinalIgnoreCase)
            ? trimmedBase
            : trimmedBase + "/" + trimmedRelative;
    }

    private string ResolveGeneratedPath(string agent, string? outputPath, bool useBrowserTemp, string kind, string prefix, string extension, int index, int count)
    {
        var root = useBrowserTemp
            ? Path.Combine(Directory.GetCurrentDirectory(), "browser_temp", "generated", kind)
            : Path.Combine(_path.GetWorkspacePath(agent), "generated", kind);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Path.Combine(root, $"{prefix}_{UserTimeZoneService.Now():yyyyMMdd_HHmmssfff}_{index}.{extension}");
        }

        var normalized = PathSafety.NormalizeSeparators(outputPath);
        if (PathSafety.ContainsParentTraversal(normalized))
        {
            throw new InvalidOperationException("output_path cannot contain path traversal.");
        }

        var candidate = Path.IsPathRooted(normalized)
            ? Path.GetFullPath(normalized)
            : Path.GetFullPath(Path.Combine(useBrowserTemp ? Path.Combine(Directory.GetCurrentDirectory(), "browser_temp") : _path.GetWorkspacePath(agent), normalized));

        var candidateExtension = Path.GetExtension(candidate);
        if (string.IsNullOrWhiteSpace(candidateExtension))
        {
            candidate = Path.Combine(candidate, $"{prefix}_{UserTimeZoneService.Now():yyyyMMdd_HHmmssfff}_{index}.{extension}");
        }
        else if (count > 1)
        {
            candidate = Path.Combine(
                Path.GetDirectoryName(candidate)!,
                Path.GetFileNameWithoutExtension(candidate) + $"_{index}" + candidateExtension);
        }

        var workspace = Path.GetFullPath(_path.GetWorkspacePath(agent));
        var browserTemp = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "browser_temp"));
        if (!IsUnderRoot(candidate, workspace) && !IsUnderRoot(candidate, browserTemp))
        {
            throw new InvalidOperationException("output_path must stay inside the agent workspace or browser_temp.");
        }

        return candidate;
    }

    private GeneratedFileResult Result(string agent, string filePath, string type, string format, EffectiveImageGenerationConfig? image = null, EffectiveTextToSpeechConfig? tts = null, ImageGenerationRequest? imageRequest = null)
    {
        var relative = PreviewPath(agent, filePath);
        return new GeneratedFileResult
        {
            Path = filePath,
            RelativePath = relative,
            Url = "/api/file?agent=" + Uri.EscapeDataString(agent) + "&path=" + Uri.EscapeDataString(relative),
            Type = type,
            Format = format,
            Size = new FileInfo(filePath).Length,
            JobId = imageRequest?.JobId,
            BatchId = imageRequest?.BatchId,
            Prompt = imageRequest?.Prompt,
            RequestedImageProfile = imageRequest?.ImageProfile,
            ImageProfileId = image?.Id,
            ImageProfileName = image?.Name,
            TtsProfileId = tts?.Id,
            TtsProfileName = tts?.Name,
            Model = image?.Model ?? tts?.Model
        };
    }

    private string PreviewPath(string agent, string filePath)
    {
        var workspace = Path.GetFullPath(_path.GetWorkspacePath(agent));
        var full = Path.GetFullPath(filePath);
        if (IsUnderRoot(full, workspace))
        {
            return NormalizeSlashes(Path.GetRelativePath(workspace, full));
        }

        var current = Path.GetFullPath(Directory.GetCurrentDirectory());
        if (IsUnderRoot(full, current))
        {
            return NormalizeSlashes(Path.GetRelativePath(current, full));
        }

        return full;
    }

    private static bool IsUnderRoot(string path, string root)
    {
        return PathSafety.IsUnderRoot(path, root);
    }

    private static string NormalizeSlashes(string value) => value.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

    private static string NormalizeFormat(string? primary, string? fallback, string defaultValue)
    {
        var value = FirstValue(primary, fallback, defaultValue).Trim().TrimStart('.').ToLowerInvariant();
        return value == "jpg" ? "jpeg" : value;
    }

    private static string FirstValue(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }

        return "";
    }

    private static string Trim(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    private sealed record ImagePayload(string? Url, byte[]? Bytes, string? Format)
    {
        public static ImagePayload FromUrl(string url, string? format = null) => new(url, null, format);
        public static ImagePayload FromBytes(byte[] bytes, string? format = null) => new(null, bytes, format);
    }

    private sealed record AudioPayload(string? Url, byte[]? Bytes, string? Format)
    {
        public static readonly AudioPayload Empty = new(null, null, null);
        public static AudioPayload FromUrl(string url, string? format = null) => new(url, null, format);
        public static AudioPayload FromBytes(byte[] bytes, string? format = null) => new(null, bytes, format);
    }

    private sealed record WavePart(byte[] FormatChunk, byte[] DataChunk);
}
