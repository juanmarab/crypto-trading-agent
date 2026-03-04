using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Llm;

/// <summary>
/// Local dev fallback — generates realistic quant trading decisions without any API calls.
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
            "[MockLlmService] No GROQ_API_KEY set — returning synthetic quant decision. " +
            "Add your key to the .env file for real analysis.");

        var action        = PickRandom(TradeAction.LONG, TradeAction.SHORT, TradeAction.HOLD, TradeAction.HOLD);
        var confluenceScore = action == TradeAction.HOLD ? Rng.Next(3, 7) : Rng.Next(5, 10);
        var confidence    = (decimal)confluenceScore / 10m;
        int? leverage     = action == TradeAction.HOLD ? null : Rng.Next(1, 5);

        // Mock entry around BTC price approximation
        var entry  = 84_000m + (decimal)(Rng.NextDouble() * 2000 - 1000);
        var atr    = entry * 0.008m; // ~0.8%

        decimal? tp1 = null, tp2 = null, sl = null, size = null;
        int? holdHours = null;
        string reasoning;

        decimal risk = 10m; // 1% of $1000 account

        switch (action)
        {
            case TradeAction.LONG:
                var supportMock = entry - atr * 2m;
                sl   = Math.Round(supportMock - atr * 1.5m, 2);
                tp1  = Math.Round(entry + Math.Abs(entry - sl.Value) * 1.5m, 2);
                tp2  = Math.Round(entry + Math.Abs(entry - sl.Value) * 3.0m, 2);
                size = Math.Round(risk / (Math.Abs(entry - sl.Value) / entry), 2);
                holdHours = PickRandom(4, 8, 12);
                reasoning = $"Price above EMA20 ({entry - 200:F0}) and EMA50, MACD histogram positive and rising — BULLISH_CROSS confirmed. " +
                            $"RSI at ~58 has room before overbought; BB position WITHIN with room to upper band, supporting long continuation.";
                break;

            case TradeAction.SHORT:
                var resMock = entry + atr * 2m;
                sl   = Math.Round(resMock + atr * 1.5m, 2);
                tp1  = Math.Round(entry - Math.Abs(sl.Value - entry) * 1.5m, 2);
                tp2  = Math.Round(entry - Math.Abs(sl.Value - entry) * 3.0m, 2);
                size = Math.Round(risk / (Math.Abs(sl.Value - entry) / entry), 2);
                holdHours = PickRandom(2, 4, 8);
                reasoning = $"Price rejected at EMA20 ({entry + 150:F0}) with bearish MACD divergence and RSI at 71 (OVERBOUGHT). " +
                            $"BB position ABOVE_UPPER signals overextension; nearest resistance at {resMock:F0} acted as barrier, favoring short.";
                break;

            default:
                reasoning = $"No clear directional confluence — RSI neutral (~50), MACD histogram amplitude low and flat. " +
                            $"Price inside Bollinger Bands with ATR contracting; waiting for breakout with volume confirmation before committing.";
                break;
        }

        return Task.FromResult(new LlmDecisionResult
        {
            TechnicalVerdict   = action == TradeAction.LONG ? "BULLISH" : action == TradeAction.SHORT ? "BEARISH" : "NEUTRAL",
            FundamentalVerdict = "NEUTRAL",
            Action             = action,
            SuggestedLeverage  = leverage,
            Confidence         = confidence,
            EntryPrice         = Math.Round(entry, 2),
            TakeProfit         = tp1,
            TakeProfit2        = tp2,
            StopLoss           = sl,
            PositionSizeUsd    = size,
            HoldingPeriodHours = holdHours,
            TechnicalReasoning = reasoning,
            ConfluenceScore    = confluenceScore,
            SendTelegramAlert  = confluenceScore >= 8,
            RawOutput          = $"{{\"direction\":\"{action}\",\"confluenceScore\":{confluenceScore},\"mock\":true}}"
        });
    }

    private static T PickRandom<T>(params T[] options) => options[Rng.Next(options.Length)];
}
