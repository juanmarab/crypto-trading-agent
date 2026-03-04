using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Llm;

/// <summary>
/// Calls the Groq LPU inference API using the institutional quant analyst role.
///
/// Model: llama-3.3-70b-versatile
/// Free tier: 14,400 req/day | https://console.groq.com/keys
/// </summary>
public class GroqLlmService : ILlmService
{
    // Relative path — BaseAddress (https://api.groq.com/) is set by the DI HttpClient factory
    private const string ChatCompletionsUrl = "openai/v1/chat/completions";
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
        _logger.LogInformation("Calling Groq ({Model}) for quant analysis...", Model);

        const string systemMessage =
            """
            You are an expert quantitative analyst specialising in crypto futures trading.
            Evaluate probabilities — do not predict the future.

            Your task: Given the market data in the user message, determine:
            1. Direction: Long | Short | Neutral
               Choose based on 3+ confluent technical signals. No clear edge → Neutral.
            2. Confidence score (confluenceScore): 1–10
               Count aligned signals: EMA trend, RSI zone, MACD cross, BB position,
               S/R proximity, volume, news sentiment. sendTelegram = true when score >= 8.
            3. Suggested leverage: 1–10x (null if Neutral). Higher leverage only for high confluence (>=8).
            4. Holding period hours: estimated hours to hold this position (null if Neutral).
            5. Technical reasoning: Exactly 2 sentences quoting specific indicator values.
               Example: "RSI at 62.3 is rising toward overbought with MACD in bullish cross.
                         EMA20 (84200) > EMA50 (83100) > EMA200 (81400) confirming uptrend structure."

            CRITICAL: Return ONLY the JSON object — no markdown, no prose, no code fences.
            All numeric fields must be plain numbers or null. Never include formulas or math expressions.

            {
              "direction": "Long|Short|Neutral",
              "confluenceScore": 5,
              "sendTelegram": false,
              "suggestedLeverage": null,
              "holdingPeriodHours": null,
              "technicalReasoning": "2-sentence reasoning quoting actual indicator values."
            }
            """;

        var requestBody = new
        {
            model = Model,
            temperature = 0.10,
            max_tokens = 700,
            response_format = new { type = "json_object" },
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

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Groq API error {Status}: {Body}", (int)response.StatusCode, errorBody);
                response.EnsureSuccessStatusCode(); // throws HttpRequestException
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);

            var rawText = responseJson
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            _logger.LogDebug("Groq raw response: {Raw}", rawText);

            var parsed = JsonSerializer.Deserialize<GroqDecisionJson>(rawText, JsonOpts)
                ?? throw new JsonException("Groq returned null/unparseable JSON.");

            var direction = parsed.Direction?.ToLowerInvariant();
            var action = direction switch
            {
                "long"  => TradeAction.LONG,
                "short" => TradeAction.SHORT,
                _       => TradeAction.HOLD
            };

            var techVerdict = action == TradeAction.LONG ? "BULLISH"
                : action == TradeAction.SHORT ? "BEARISH" : "NEUTRAL";

            _logger.LogInformation(
                "Groq decision: {Dir} | Score: {Score}/10 | Telegram: {Tg}",
                parsed.Direction, parsed.ConfluenceScore, parsed.SendTelegram);

            return new LlmDecisionResult
            {
                TechnicalVerdict   = techVerdict,
                FundamentalVerdict = "NEUTRAL",
                Action             = action,
                SuggestedLeverage  = parsed.SuggestedLeverage,
                Confidence         = parsed.ConfluenceScore.HasValue
                                       ? (decimal)parsed.ConfluenceScore.Value / 10m
                                       : null,
                // Numeric trade parameters are NOT set here —
                // they are computed by AgentOrchestrationService from real indicator data
                TechnicalReasoning = parsed.TechnicalReasoning ?? string.Empty,
                HoldingPeriodHours = parsed.HoldingPeriodHours,
                ConfluenceScore    = parsed.ConfluenceScore,
                SendTelegramAlert  = parsed.SendTelegram,
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

    private sealed class GroqDecisionJson
    {
        [JsonPropertyName("direction")]          public string? Direction          { get; set; }
        [JsonPropertyName("confluenceScore")]    public int?    ConfluenceScore    { get; set; }
        [JsonPropertyName("sendTelegram")]       public bool    SendTelegram       { get; set; }
        [JsonPropertyName("suggestedLeverage")]  public decimal? SuggestedLeverage { get; set; }
        [JsonPropertyName("holdingPeriodHours")] public int?    HoldingPeriodHours { get; set; }
        [JsonPropertyName("technicalReasoning")] public string? TechnicalReasoning { get; set; }
    }
}
