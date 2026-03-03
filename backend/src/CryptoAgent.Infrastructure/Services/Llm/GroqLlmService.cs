using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Llm;

/// <summary>
/// Calls the Groq LPU inference API using its OpenAI-compatible endpoint.
///
/// Model: llama-3.3-70b-versatile
///   — Best-in-class open-source reasoning, excellent at structured JSON.
///   — Groq delivers it at 100+ tokens/sec (far faster than any cloud API).
///
/// Free tier: https://console.groq.com/keys
///   — 14,400 requests / day  |  30 requests / minute  |  fully free.
///   — Our agent runs every 15 min × 4 assets = 16 calls/hour — well within limits.
///
/// API docs: https://console.groq.com/docs/openai
/// </summary>
public class GroqLlmService : ILlmService
{
    private const string ChatCompletionsUrl = "https://api.groq.com/openai/v1/chat/completions";

    // Best model for structured reasoning on Groq (Dec 2024 → current)
    private const string Model = "llama-3.3-70b-versatile";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ILogger<GroqLlmService> _logger;

    public GroqLlmService(HttpClient http, ILogger<GroqLlmService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<LlmDecisionResult> AnalyzeAndDecideAsync(
        string structuredPrompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling Groq ({Model}) for trading decision...", Model);

        // System message that enforces strict JSON output.
        const string systemMessage =
            """
            You are an expert crypto trading analyst. 
            You MUST respond ONLY with a single, valid JSON object — no markdown, no explanation, no code fences.
            The JSON must contain exactly these fields:
            {
              "technicalVerdict": "<BULLISH | BEARISH | NEUTRAL>",
              "fundamentalVerdict": "<BULLISH | BEARISH | NEUTRAL>",
              "action": "<Long | Short | Hold>",
              "suggestedLeverage": <integer 1-10, or null if Hold>,
              "confidence": <float 0.0 - 1.0>,
              "reasoning": "<1-3 concise sentences explaining the decision>"
            }
            Only recommend Long or Short when there is strong multi-indicator confluence.
            Leverage > 3 only when confidence > 0.75. Otherwise use Hold and null leverage.
            """;

        var requestBody = new
        {
            model = Model,
            temperature = 0.15,     // Low temp → consistent, deterministic analysis
            max_tokens = 400,       // Our JSON is small; keep it tight
            response_format = new { type = "json_object" }, // Enforce JSON output at API level
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user",   content = structuredPrompt }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody, JsonOpts);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(ChatCompletionsUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);

            // Extract the text from the OpenAI-compatible response envelope
            var rawText = responseJson
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            _logger.LogDebug("Groq raw response: {Raw}", rawText);

            // Parse the structured decision from the model's JSON output
            var parsed = JsonSerializer.Deserialize<GroqDecisionJson>(rawText, JsonOpts)
                ?? throw new JsonException("Groq returned null/unparseable JSON.");

            _logger.LogInformation(
                "Groq decision parsed — Action: {Action}, Confidence: {Conf:P0}, Leverage: {Lev}x",
                parsed.Action, parsed.Confidence, parsed.SuggestedLeverage);

            return new LlmDecisionResult
            {
                TechnicalVerdict   = parsed.TechnicalVerdict   ?? "NEUTRAL",
                FundamentalVerdict = parsed.FundamentalVerdict ?? "NEUTRAL",
                Action             = ParseAction(parsed.Action),
                SuggestedLeverage  = parsed.SuggestedLeverage,
                Confidence         = parsed.Confidence,
                RawOutput          = rawText
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Groq API HTTP request failed.");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Groq response as JSON.");
            throw;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TradeAction ParseAction(string? action) => action?.ToLowerInvariant() switch
    {
        "long"  => TradeAction.LONG,
        "short" => TradeAction.SHORT,
        _       => TradeAction.HOLD
    };

    // ── Internal DTO matching Groq/Llama JSON output ──────────────────────────

    private sealed class GroqDecisionJson
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
