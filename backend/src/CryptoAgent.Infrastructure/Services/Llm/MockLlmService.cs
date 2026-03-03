using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Llm;

/// <summary>
/// Local dev fallback — generates realistic trading decisions without any API calls.
/// Activated automatically when no LLM API key is configured.
/// </summary>
public class MockLlmService : ILlmService
{
    private readonly ILogger<MockLlmService> _logger;
    private static readonly Random Rng = new();

    public MockLlmService(ILogger<MockLlmService> logger) => _logger = logger;

    public Task<LlmDecisionResult> AnalyzeAndDecideAsync(
        string structuredPrompt, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[MockLlmService] No LLM API key configured. Returning a synthetic decision. " +
            "Set GROQ_API_KEY in your .env file for real analysis.");

        var action     = PickRandom(TradeAction.LONG, TradeAction.SHORT, TradeAction.HOLD, TradeAction.HOLD);
        var confidence = (decimal)(Rng.NextDouble() * 0.4 + 0.5); // 0.50 – 0.90
        int? leverage  = action == TradeAction.HOLD ? null : Rng.Next(1, 6);

        string techVerdict = action switch
        {
            TradeAction.LONG  => "BULLISH",
            TradeAction.SHORT => "BEARISH",
            _                 => "NEUTRAL"
        };

        string reasoning = action switch
        {
            TradeAction.LONG  => "Price is above EMA20 and EMA50 with bullish MACD crossover. RSI is rising but not overbought.",
            TradeAction.SHORT => "Price rejected at the upper Bollinger Band. MACD histogram is declining and RSI is overbought.",
            _                 => "No clear trend confluence across timeframes. ATR is contracting — waiting for a breakout."
        };

        return Task.FromResult(new LlmDecisionResult
        {
            TechnicalVerdict   = techVerdict,
            FundamentalVerdict = "NEUTRAL",
            Action             = action,
            SuggestedLeverage  = leverage,
            Confidence         = confidence,
            RawOutput          = $"{{\"action\":\"{action}\",\"reasoning\":\"{reasoning}\",\"mock\":true}}"
        });
    }

    private static T PickRandom<T>(params T[] options) => options[Rng.Next(options.Length)];
}
