using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Llm;

/// <summary>
/// Calls Google Gemini (gemini-2.0-flash) with a structured prompt and parses
/// the response as a strict JSON trading decision.
/// 
/// The model is instructed to ALWAYS return valid JSON matching LlmDecisionResult.
/// </summary>
public class GeminiLlmService : ILlmService
{
    private const string Model = "gemini-2.0-flash";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<GeminiLlmService> _logger;

    public GeminiLlmService(
        HttpClient http,
        IConfiguration configuration,
        ILogger<GeminiLlmService> logger)
    {
        _http = http;
        _apiKey = configuration["ApiKeys:GoogleGemini"]
            ?? throw new InvalidOperationException("GoogleGemini API key is not configured.");
        _logger = logger;
    }

    public async Task<LlmDecisionResult> AnalyzeAndDecideAsync(
        string structuredPrompt, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/models/{Model}:generateContent?key={_apiKey}";

        // System instruction: enforce strict JSON output
        var systemInstruction = """
            You are a professional crypto trading analyst AI.
            You MUST respond ONLY with a single valid JSON object — no markdown, no explanation, no code fences.
            The JSON must have exactly these fields:
            {
              "technicalVerdict": string (BULLISH | BEARISH | NEUTRAL),
              "fundamentalVerdict": string (BULLISH | BEARISH | NEUTRAL),
              "action": string (Long | Short | Hold),
              "suggestedLeverage": number | null (1x–10x only, null if Hold),
              "confidence": number (0.0 to 1.0),
              "reasoning": string (1–3 concise sentences explaining the decision)
            }
            """;

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = structuredPrompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,          // Low temperature = deterministic, consistent
                maxOutputTokens = 512,
                responseMimeType = "application/json"
            }
        };

        try
        {
            var response = await _http.PostAsJsonAsync(url, requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);

            // Extract text from Gemini response envelope
            var rawText = json
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            _logger.LogDebug("Gemini raw response: {Raw}", rawText);

            // Parse our structured decision
            var parsed = JsonSerializer.Deserialize<GeminiDecisionJson>(rawText, JsonOpts)
                ?? throw new JsonException("Gemini returned a null/unparseable JSON body.");

            return new LlmDecisionResult
            {
                TechnicalVerdict = parsed.TechnicalVerdict ?? "NEUTRAL",
                FundamentalVerdict = parsed.FundamentalVerdict ?? "NEUTRAL",
                Action = ParseAction(parsed.Action),
                SuggestedLeverage = parsed.SuggestedLeverage,
                Confidence = parsed.Confidence,
                RawOutput = rawText
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Gemini LLM API request failed.");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini LLM response.");
            throw;
        }
    }

    private static TradeAction ParseAction(string? action) => action?.ToLowerInvariant() switch
    {
        "long" => TradeAction.LONG,
        "short" => TradeAction.SHORT,
        _ => TradeAction.HOLD
    };

    // ── Internal DTO matching Gemini's JSON output ────────────────────────────

    private sealed class GeminiDecisionJson
    {
        [JsonPropertyName("technicalVerdict")]
        public string? TechnicalVerdict { get; set; }

        [JsonPropertyName("fundamentalVerdict")]
        public string? FundamentalVerdict { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("suggestedLeverage")]
        public decimal? SuggestedLeverage { get; set; }

        [JsonPropertyName("confidence")]
        public decimal? Confidence { get; set; }

        [JsonPropertyName("reasoning")]
        public string? Reasoning { get; set; }
    }
}
