using System.Text;
using CryptoAgent.Application.DTOs.TechnicalAnalysis;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Orchestration;

/// <summary>
/// Core AI orchestration logic:
///   1. Fetches the latest technical indicators (via Binance REST + TA engine)
///   2. Retrieves recent news for fundamental context
///   3. Builds a rich structured prompt for the LLM
///   4. Calls Gemini and parses the structured decision
///   5. Persists the AgentDecision and returns it
///
/// This is NOT a background worker — it is pure business logic invoked by the worker.
/// </summary>
public class AgentOrchestrationService
{
    private readonly IBinanceService _binance;
    private readonly ITechnicalAnalysisService _taService;
    private readonly IMarketNewsRepository _newsRepo;
    private readonly IAgentDecisionRepository _decisionRepo;
    private readonly ITechnicalSnapshotRepository _snapshotRepo;
    private readonly ILlmService _llm;
    private readonly ILogger<AgentOrchestrationService> _logger;

    public AgentOrchestrationService(
        IBinanceService binance,
        ITechnicalAnalysisService taService,
        IMarketNewsRepository newsRepo,
        IAgentDecisionRepository decisionRepo,
        ITechnicalSnapshotRepository snapshotRepo,
        ILlmService llm,
        ILogger<AgentOrchestrationService> logger)
    {
        _binance = binance;
        _taService = taService;
        _newsRepo = newsRepo;
        _decisionRepo = decisionRepo;
        _snapshotRepo = snapshotRepo;
        _llm = llm;
        _logger = logger;
    }

    /// <summary>
    /// Runs the full analysis pipeline for a single asset.
    /// Returns the persisted AgentDecision, or null if there was insufficient data.
    /// </summary>
    public async Task<AgentDecision?> RunForAssetAsync(
        CryptoAsset asset,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🤖 Orchestrating analysis for {Asset}...", asset);

        // ── Step 1: Fetch klines and calculate indicators ─────────────────
        IReadOnlyList<KlineData> klines5m, klines15m;
        try
        {
            klines5m = await _binance.GetKlinesAsync(asset, "5m", 250, cancellationToken);
            klines15m = await _binance.GetKlinesAsync(asset, "15m", 250, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch klines for {Asset}.", asset);
            return null;
        }

        if (klines15m.Count < 50)
        {
            _logger.LogWarning("Insufficient klines for {Asset} 15m. Skipping.", asset);
            return null;
        }

        var ind5m = klines5m.Count >= 50
            ? _taService.CalculateIndicators(asset, "5m", klines5m)
            : null;
        var ind15m = _taService.CalculateIndicators(asset, "15m", klines15m);

        // ── Step 2: Get the persisted snapshot for FK linkage ─────────────
        var latestSnapshot = await _snapshotRepo.GetLatestByAssetAsync(asset, "15m");

        // ── Step 3: Retrieve recent news (last 24 hours) ──────────────────
        var recentNews = await _newsRepo.GetRecentByAssetAsync(asset, hours: 24, limit: 10);

        // ── Step 4: Build the LLM prompt ──────────────────────────────────
        var prompt = BuildPrompt(asset, ind5m, ind15m, recentNews);
        _logger.LogDebug("Prompt for {Asset}:\n{Prompt}", asset, prompt);

        // ── Step 5: Call the LLM ──────────────────────────────────────────
        LlmDecisionResult llmResult;
        try
        {
            llmResult = await _llm.AnalyzeAndDecideAsync(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM call failed for {Asset}.", asset);
            return null;
        }

        _logger.LogInformation(
            "✅ {Asset} — Action: {Action}, Confidence: {Confidence:P0}, Leverage: {Leverage}x",
            asset, llmResult.Action, llmResult.Confidence, llmResult.SuggestedLeverage);

        // ── Step 6: Persist the AgentDecision ────────────────────────────
        var decision = new AgentDecision
        {
            Id = Guid.NewGuid(),
            Asset = asset,
            DecidedAt = DateTimeOffset.UtcNow,
            TechnicalVerdict = llmResult.TechnicalVerdict,
            FundamentalVerdict = llmResult.FundamentalVerdict,
            Action = llmResult.Action,
            SuggestedLeverage = llmResult.SuggestedLeverage,
            Confidence = llmResult.Confidence ?? 0m,
            RawLlmOutput = llmResult.RawOutput,
            SnapshotId = latestSnapshot?.Id
        };

        await _decisionRepo.AddAsync(decision);

        return decision;
    }

    // ── Prompt Builder ────────────────────────────────────────────────────────

    private static string BuildPrompt(
        CryptoAsset asset,
        IndicatorSet? ind5m,
        IndicatorSet ind15m,
        IEnumerable<MarketNews> news)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"## {asset}/USDT Trading Analysis — {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();

        // ── Technical Analysis Section ────────────────────────────────────
        sb.AppendLine("### TECHNICAL ANALYSIS");
        sb.AppendLine();

        AppendIndicators(sb, "15-Minute Timeframe (Primary)", ind15m);

        if (ind5m != null)
        {
            sb.AppendLine();
            AppendIndicators(sb, "5-Minute Timeframe (Confirmation)", ind5m);
        }

        // ── Fundamental / News Section ────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("### RECENT NEWS (last 24h)");
        sb.AppendLine();

        var newsList = news.ToList();
        if (newsList.Count == 0)
        {
            sb.AppendLine("No significant news in the last 24 hours.");
        }
        else
        {
            foreach (var item in newsList)
            {
                sb.AppendLine($"- [{item.PublishedAt:HH:mm}] {item.Headline}");
            }
        }

        // ── Instructions ───────────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("### INSTRUCTIONS");
        sb.AppendLine("Based on the technical data and recent news above, provide a trading decision for a leveraged futures position.");
        sb.AppendLine("Consider trend alignment across timeframes, momentum, volatility (ATR), and news sentiment.");
        sb.AppendLine("Only recommend Long/Short if there is strong confluence. Otherwise, recommend Hold.");
        sb.AppendLine("Leverage must be between 1x and 10x. Only suggest leverage > 3x if confidence is high (>0.75).");

        return sb.ToString();
    }

    private static void AppendIndicators(StringBuilder sb, string label, IndicatorSet ind)
    {
        sb.AppendLine($"**{label}**");
        sb.AppendLine($"- Close: {ind.Close:F4} | High: {ind.High:F4} | Low: {ind.Low:F4}");
        sb.AppendLine($"- EMA20: {FormatNullable(ind.Ema20)} | EMA50: {FormatNullable(ind.Ema50)} | EMA200: {FormatNullable(ind.Ema200)}");
        sb.AppendLine($"- EMA Trend: {ind.EmaTrend ?? "N/A"}");
        sb.AppendLine($"- RSI(14): {FormatNullable(ind.Rsi, "F2")} → {ind.RsiSignal ?? "N/A"}");
        sb.AppendLine($"- MACD Line: {FormatNullable(ind.MacdLine)} | Signal: {FormatNullable(ind.MacdSignal)} | Histogram: {FormatNullable(ind.MacdHistogram)} → {ind.MacdSignalType ?? "N/A"}");
        sb.AppendLine($"- BB Upper: {FormatNullable(ind.BbUpper)} | Middle: {FormatNullable(ind.BbMiddle)} | Lower: {FormatNullable(ind.BbLower)} → {ind.BbPosition ?? "N/A"}");
        sb.AppendLine($"- ATR(14): {FormatNullable(ind.Atr)}");
        sb.AppendLine($"- Volume: {ind.Volume:F2}");
    }

    private static string FormatNullable(decimal? value, string format = "F4") =>
        value.HasValue ? value.Value.ToString(format) : "N/A";
}
