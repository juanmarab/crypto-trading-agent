using System.Net.Http.Json;
using System.Text.Json;
using CryptoAgent.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace CryptoAgent.Infrastructure.Services.Embedding;

/// <summary>
/// Generates text embeddings using Google Gemini text-embedding-004 (768 dimensions).
/// Uses the lightweight REST API — no SDK required.
/// </summary>
public class GeminiEmbeddingService : IEmbeddingService
{
    private const string ModelId = "text-embedding-004";
    private const string TaskType = "RETRIEVAL_DOCUMENT";

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    public GeminiEmbeddingService(
        HttpClient http,
        IConfiguration configuration,
        ILogger<GeminiEmbeddingService> logger)
    {
        _http = http;
        _apiKey = configuration["ApiKeys:GoogleGemini"]
            ?? throw new InvalidOperationException("GoogleGemini API key is not configured.");
        _logger = logger;
    }

    public async Task<Vector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty.", nameof(text));

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelId}:embedContent?key={_apiKey}";

        var requestBody = new
        {
            model = $"models/{ModelId}",
            content = new
            {
                parts = new[] { new { text } }
            },
            taskType = TaskType
        };

        try
        {
            var response = await _http.PostAsJsonAsync(url, requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var values = json
                .GetProperty("embedding")
                .GetProperty("values")
                .EnumerateArray()
                .Select(v => v.GetSingle())
                .ToArray();

            _logger.LogDebug("Generated embedding with {Dimensions} dimensions.", values.Length);
            return new Vector(values);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Gemini embedding API request failed.");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini embedding response.");
            throw;
        }
    }
}
