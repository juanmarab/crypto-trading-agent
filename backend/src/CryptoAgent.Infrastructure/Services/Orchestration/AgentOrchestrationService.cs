using System.Text;
using CryptoAgent.Application.DTOs.TechnicalAnalysis;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Orchestration;

/// <summary>
/// Core AI orchestration logic:
///   1. Fetches the latest technical indicators (via Binance REST + TA engine + S/R detector)
///   2. Retrieves recent news for fundamental context
///   3. Builds an institutional quant-analyst prompt
///   4. Calls the LLM and parses the structured decision
///   5. Persists the AgentDecision and returns it
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
    private readonly decimal _accountCapitalUsd;

    public AgentOrchestrationService(
        IBinanceService binance,
        ITechnicalAnalysisService taService,
        IMarketNewsRepository newsRepo,
        IAgentDecisionRepository decisionRepo,
        ITechnicalSnapshotRepository snapshotRepo,
        ILlmService llm,
        IConfiguration configuration,
        ILogger<AgentOrchestrationService> logger)
    {
        _binance      = binance;
        _taService    = taService;
        _newsRepo     = newsRepo;
        _decisionRepo = decisionRepo;
        _snapshotRepo = snapshotRepo;
        _llm          = llm;
        _logger       = logger;
        _accountCapitalUsd = configuration.GetValue<decimal>("Trading:AccountCapitalUsd", 1000m);
    }

    /// <summary>
    /// Runs the full analysis pipeline for a single asset.
    /// Returns the persisted AgentDecision, or null if there was insufficient data.
    /// </summary>
    public async Task<AgentDecision?> RunForAssetAsync(
        CryptoAsset asset,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🤖 Orchestrating quant analysis for {Asset}...", asset);

        // ── Step 1: Fetch klines and calculate indicators ─────────────────
        IReadOnlyList<KlineData> klines5m, klines15m;
        try
        {
            klines5m  = await _binance.GetKlinesAsync(asset, "5m",  250, cancellationToken);
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

        var ind5m  = klines5m.Count >= 50 ? _taService.CalculateIndicators(asset, "5m",  klines5m)  : null;
        var ind15m = _taService.CalculateIndicators(asset, "15m", klines15m);

        // ── Step 2: Get the persisted snapshot for FK linkage ─────────────
        var latestSnapshot = await _snapshotRepo.GetLatestByAssetAsync(asset, "15m");

        // ── Step 3: Retrieve recent news (last 24 hours) ──────────────────
        var recentNews = await _newsRepo.GetRecentByAssetAsync(asset, hours: 24, limit: 10);

        // ── Step 4: Build the quant analyst prompt ────────────────────────
        var prompt = BuildPrompt(asset, ind5m, ind15m, recentNews, _accountCapitalUsd);
        _logger.LogDebug("Prompt for {Asset}:\n{Prompt}", asset, prompt);

        // ── Step 5: Call the LLM ─────────────────────────────────────────
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
            "✅ {Asset} — {Dir} | Score: {Score}/10 | Telegram: {Tg} | Reasoning: {Reasoning}",
            asset, llmResult.Action, llmResult.ConfluenceScore, llmResult.SendTelegramAlert,
            llmResult.TechnicalReasoning?[..Math.Min(60, llmResult.TechnicalReasoning.Length)]);

        // ── Step 6: Compute numeric trade parameters ─────────────────────
        var tradeParams = ComputeTradeParameters(llmResult.Action, ind15m, _accountCapitalUsd);

        _logger.LogInformation(
            "📐 {Asset} — Entry: {Entry} | TP1: {TP1} | TP2: {TP2} | SL: {SL} | Size: ${Size} USD",
            asset, tradeParams.Entry, tradeParams.Tp1, tradeParams.Tp2, tradeParams.Sl, tradeParams.SizeUsd);

        // ── Step 7: Persist the AgentDecision ────────────────────────────
        var decision = new AgentDecision
        {
            Id                 = Guid.NewGuid(),
            Asset              = asset,
            DecidedAt          = DateTimeOffset.UtcNow,
            TechnicalVerdict   = llmResult.TechnicalVerdict,
            FundamentalVerdict = llmResult.FundamentalVerdict,
            Action             = llmResult.Action,
            SuggestedLeverage  = llmResult.SuggestedLeverage,
            Confidence         = llmResult.Confidence ?? 0m,
            EntryPrice         = tradeParams.Entry,
            TakeProfit         = tradeParams.Tp1,
            TakeProfit2        = tradeParams.Tp2,
            StopLoss           = tradeParams.Sl,
            PositionSizeUsd    = tradeParams.SizeUsd,
            HoldingPeriodHours = llmResult.HoldingPeriodHours,
            TechnicalReasoning = llmResult.TechnicalReasoning ?? string.Empty,
            ConfluenceScore    = llmResult.ConfluenceScore,
            SendTelegramAlert  = llmResult.SendTelegramAlert,
            RawLlmOutput       = llmResult.RawOutput,
            SnapshotId         = latestSnapshot?.Id
        };

        await _decisionRepo.AddAsync(decision);
        return decision;
    }

    // ── Quant Trade Parameter Calculator ─────────────────────────────────────

    private static (decimal? Entry, decimal? Tp1, decimal? Tp2, decimal? Sl, decimal? SizeUsd)
        ComputeTradeParameters(TradeAction action, IndicatorSet ind, decimal accountCapital)
    {
        if (action == TradeAction.HOLD)
            return (ind.Close, null, null, null, null);

        var entry = ind.Close;
        var atr   = ind.Atr ?? (entry * 0.005m); // fallback: 0.5% of price
        var buf   = atr * 1.5m;                  // 1.5× ATR buffer

        decimal? sl;
        if (action == TradeAction.LONG)
        {
            var nearestSupport = ind.Support1 ?? entry - atr * 2m;
            sl = Math.Round(nearestSupport - buf, 4);
        }
        else // SHORT
        {
            var nearestResistance = ind.Resistance1 ?? entry + atr * 2m;
            sl = Math.Round(nearestResistance + buf, 4);
        }

        var risk   = Math.Abs(entry - sl.Value);
        if (risk <= 0) return (entry, null, null, sl, null);

        decimal? tp1 = action == TradeAction.LONG
            ? Math.Round(entry + risk * 1.5m, 4)
            : Math.Round(entry - risk * 1.5m, 4);

        decimal? tp2 = action == TradeAction.LONG
            ? Math.Round(entry + risk * 3.0m, 4)
            : Math.Round(entry - risk * 3.0m, 4);

        // positionSizeUsd = riskUsd / (riskDistance / entry)
        var riskUsd     = accountCapital * 0.01m;
        var riskPct     = risk / entry;
        var sizeUsd     = riskPct > 0 ? Math.Round(riskUsd / riskPct, 2) : (decimal?)null;

        return (entry, tp1, tp2, sl, sizeUsd);
    }

    // ── Prompt Builder ────────────────────────────────────────────────────────

    private static string BuildPrompt(
        CryptoAsset asset,
        IndicatorSet? ind5m,
        IndicatorSet ind15m,
        IEnumerable<MarketNews> news,
        decimal accountCapital)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"## Datos del Mercado — {asset}/USDT — {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();
        sb.AppendLine($"**Activo:** {asset}/USDT");
        sb.AppendLine($"**Timeframe Principal:** 15m");
        sb.AppendLine($"**Precio Actual (Close 15m):** {ind15m.Close:F4} USDT");
        sb.AppendLine($"**ATR (14 periodos, 15m):** {FormatNullable(ind15m.Atr)}");
        sb.AppendLine($"**Capital Total de la Cuenta:** {accountCapital:F2} USD");
        sb.AppendLine($"**Riesgo por operación:** 1%  (= {accountCapital * 0.01m:F2} USD)");
        sb.AppendLine();

        // S/R levels
        sb.AppendLine("### Niveles de Estructura");
        sb.AppendLine($"- **Soporte 1 (más cercano):** {FormatNullable(ind15m.Support1)}");
        sb.AppendLine($"- **Soporte 2:**               {FormatNullable(ind15m.Support2)}");
        sb.AppendLine($"- **Resistencia 1 (más cercana):** {FormatNullable(ind15m.Resistance1)}");
        sb.AppendLine($"- **Resistencia 2:**               {FormatNullable(ind15m.Resistance2)}");
        sb.AppendLine();

        // Technical indicators
        sb.AppendLine("### Indicadores Técnicos — 15m (Principal)");
        AppendIndicators(sb, ind15m);

        if (ind5m != null)
        {
            sb.AppendLine();
            sb.AppendLine("### Indicadores Técnicos — 5m (Confirmación)");
            AppendIndicators(sb, ind5m);
        }

        // News
        sb.AppendLine();
        sb.AppendLine("### Noticias Recientes (últimas 24h)");
        var newsList = news.ToList();
        if (newsList.Count == 0)
            sb.AppendLine("Sin noticias significativas en las últimas 24 horas.");
        else
            foreach (var item in newsList)
                sb.AppendLine($"- [{item.PublishedAt:HH:mm}] {item.Headline}");

        return sb.ToString();
    }

    private static void AppendIndicators(StringBuilder sb, IndicatorSet ind)
    {
        sb.AppendLine($"- Close: {ind.Close:F4} | High: {ind.High:F4} | Low: {ind.Low:F4}");
        sb.AppendLine($"- EMA20: {FormatNullable(ind.Ema20)} | EMA50: {FormatNullable(ind.Ema50)} | EMA200: {FormatNullable(ind.Ema200)} → {ind.EmaTrend ?? "N/A"}");
        sb.AppendLine($"- RSI(14): {FormatNullable(ind.Rsi, "F2")} → {ind.RsiSignal ?? "N/A"}");
        sb.AppendLine($"- MACD Line: {FormatNullable(ind.MacdLine)} | Signal: {FormatNullable(ind.MacdSignal)} | Histogram: {FormatNullable(ind.MacdHistogram)} → {ind.MacdSignalType ?? "N/A"}");
        sb.AppendLine($"- BB Upper: {FormatNullable(ind.BbUpper)} | Middle: {FormatNullable(ind.BbMiddle)} | Lower: {FormatNullable(ind.BbLower)} → {ind.BbPosition ?? "N/A"}");
        sb.AppendLine($"- ATR(14): {FormatNullable(ind.Atr)} | Volume: {ind.Volume:F2}");
    }

    private static string FormatNullable(decimal? value, string format = "F4") =>
        value.HasValue ? value.Value.ToString(format) : "N/A";
}
