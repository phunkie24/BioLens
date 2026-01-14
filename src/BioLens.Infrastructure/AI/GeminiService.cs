using System.Net.Http.Json;
using System.Text.Json;
using BioLens.Agents.Agents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace BioLens.Infrastructure.AI;

public class GeminiConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public string Model { get; set; } = "gemini-3-pro";
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
}

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private readonly GeminiConfiguration _config;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public GeminiService(
        HttpClient httpClient,
        ILogger<GeminiService> logger,
        IOptions<GeminiConfiguration> config)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));

        ConfigureHttpClient();
        _resiliencePolicy = BuildResiliencePolicy();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _config.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    private IAsyncPolicy<HttpResponseMessage> BuildResiliencePolicy()
    {
        // Retry with exponential backoff
        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetries,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s. Status: {StatusCode}",
                        retryCount,
                        timespan.TotalSeconds,
                        outcome.Result?.StatusCode);
                });

        // Circuit breaker
        var circuitBreakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (result, duration) =>
                {
                    _logger.LogError(
                        "Circuit breaker opened for {Duration}s",
                        duration.TotalSeconds);
                },
                onReset: () => _logger.LogInformation("Circuit breaker reset"));

        // Timeout
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(_config.TimeoutSeconds));

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }

    public async Task<GeminiResponse> AnalyzeImagesAsync(
        List<byte[]> images,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = BuildMultiModalRequest(images, null, prompt);

            var response = await _resiliencePolicy.ExecuteAsync(
                async ct => await _httpClient.PostAsJsonAsync(
                    $"/v1/models/{_config.Model}:generateContent",
                    request,
                    ct),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error: {Error}", error);
                
                return new GeminiResponse
                {
                    IsSuccess = false,
                    Error = $"API returned {response.StatusCode}: {error}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(
                cancellationToken: cancellationToken);

            if (result?.Candidates == null || result.Candidates.Count == 0)
            {
                return new GeminiResponse
                {
                    IsSuccess = false,
                    Error = "No response from Gemini API"
                };
            }

            var content = result.Candidates[0].Content.Parts
                .Where(p => p.Text != null)
                .Select(p => p.Text)
                .FirstOrDefault() ?? string.Empty;

            return new GeminiResponse
            {
                IsSuccess = true,
                Content = content,
                Metadata = new Dictionary<string, object>
                {
                    ["model"] = _config.Model,
                    ["finishReason"] = result.Candidates[0].FinishReason ?? "unknown"
                }
            };
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open");
            return new GeminiResponse
            {
                IsSuccess = false,
                Error = "Service temporarily unavailable. Please try offline mode."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Gemini API");
            return new GeminiResponse
            {
                IsSuccess = false,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<GeminiResponse> AnalyzeAudioAsync(
        byte[] audioData,
        string languageCode,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var request = BuildMultiModalRequest(null, audioData, prompt);
        
        // Similar implementation to AnalyzeImagesAsync
        return await AnalyzeImagesAsync(new List<byte[]>(), prompt, cancellationToken);
    }

    public async Task<GeminiResponse> GenerateTextAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                topP = 0.95,
                topK = 40,
                maxOutputTokens = 2048
            }
        };

        try
        {
            var response = await _resiliencePolicy.ExecuteAsync(
                async ct => await _httpClient.PostAsJsonAsync(
                    $"/v1/models/{_config.Model}:generateContent",
                    request,
                    ct),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(
                cancellationToken: cancellationToken);

            var content = result?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? string.Empty;

            return new GeminiResponse
            {
                IsSuccess = true,
                Content = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text with Gemini");
            return new GeminiResponse
            {
                IsSuccess = false,
                Error = ex.Message
            };
        }
    }

    private object BuildMultiModalRequest(
        List<byte[]>? images,
        byte[]? audioData,
        string prompt)
    {
        var parts = new List<object> { new { text = prompt } };

        // Add images
        if (images != null)
        {
            foreach (var image in images)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = "image/jpeg",
                        data = Convert.ToBase64String(image)
                    }
                });
            }
        }

        // Add audio
        if (audioData != null)
        {
            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = "audio/wav",
                    data = Convert.ToBase64String(audioData)
                }
            });
        }

        return new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = parts.ToArray()
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                topP = 0.95,
                topK = 40,
                maxOutputTokens = 2048,
                responseMimeType = "application/json"
            },
            safetySettings = new[]
            {
                new
                {
                    category = "HARM_CATEGORY_MEDICAL",
                    threshold = "BLOCK_NONE"
                }
            }
        };
    }
}

// Gemini API Response Models
internal class GeminiApiResponse
{
    public List<Candidate> Candidates { get; set; } = new();
}

internal class Candidate
{
    public Content Content { get; set; } = new();
    public string? FinishReason { get; set; }
}

internal class Content
{
    public List<Part> Parts { get; set; } = new();
}

internal class Part
{
    public string? Text { get; set; }
}
