using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace BioLens.Infrastructure.AI;

public interface IGeminiAIService
{
    Task<string> GenerateContentAsync(
        string prompt,
        List<byte[]>? images = null,
        byte[]? audio = null,
        CancellationToken cancellationToken = default);
}

public class GeminiAIService : IGeminiAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAIService> _logger;
    private readonly GeminiConfiguration _config;

    public GeminiAIService(
        HttpClient httpClient,
        ILogger<GeminiAIService> logger,
        IOptions<GeminiConfiguration> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<string> GenerateContentAsync(
        string prompt,
        List<byte[]>? images = null,
        byte[]? audio = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = BuildRequest(prompt, images, audio);
            
            var response = await _httpClient.PostAsJsonAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro:generateContent?key={_config.ApiKey}",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(
                cancellationToken: cancellationToken);

            return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed");
            throw;
        }
    }

    private object BuildRequest(string prompt, List<byte[]>? images, byte[]? audio)
    {
        var parts = new List<object> { new { text = prompt } };

        if (images != null)
        {
            parts.AddRange(images.Select(img => new
            {
                inline_data = new
                {
                    mime_type = "image/jpeg",
                    data = Convert.ToBase64String(img)
                }
            }));
        }

        if (audio != null)
        {
            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = "audio/wav",
                    data = Convert.ToBase64String(audio)
                }
            });
        }

        return new
        {
            contents = new[]
            {
                new { role = "user", parts }
            },
            generationConfig = new
            {
                temperature = 0.2,
                topP = 0.95,
                topK = 40,
                maxOutputTokens = 4096,
                responseMimeType = "application/json"
            }
        };
    }
}

public class GeminiConfiguration
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
}

public record GeminiResponse(Candidate[]? Candidates);
public record Candidate(Content? Content);
public record Content(Part[]? Parts);
public record Part(string? Text);
