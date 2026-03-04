using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Application.Interfaces;

/// <summary>
/// Service for interacting with an LLM to generate structured trading decisions.
/// </summary>
public interface ILlmService
{
    Task<LlmDecisionResult> AnalyzeAndDecideAsync(string structuredPrompt, CancellationToken cancellationToken = default);
}

/// <summary>
/// The structured result from the LLM quant analysis.
/// All price fields are in absolute USDT terms.
/// </summary>
public record LlmDecisionResult
{
    // ── Verdicts ──────────────────────────────────────────────────────────
    public string TechnicalVerdict   { get; init; } = string.Empty;
    public string FundamentalVerdict { get; init; } = string.Empty;
    public TradeAction Action        { get; init; }
    public decimal? SuggestedLeverage { get; init; }
    public decimal? Confidence       { get; init; }

    // ── Quant trade parameters ─────────────────────────────────────────────
    /// <summary>Entry price stated by LLM (echoes current close).</summary>
    public decimal? EntryPrice       { get; init; }

    /// <summary>TP1 — next S/R, RR ≥ 1:1.5.</summary>
    public decimal? TakeProfit       { get; init; }

    /// <summary>TP2 — extended target, RR ≥ 1:3.</summary>
    public decimal? TakeProfit2      { get; init; }

    /// <summary>SL placed beyond S/R ± 1.5× ATR.</summary>
    public decimal? StopLoss         { get; init; }

    /// <summary>USD position size for exactly 1% account risk.</summary>
    public decimal? PositionSizeUsd  { get; init; }

    /// <summary>Estimated hold duration in hours.</summary>
    public int? HoldingPeriodHours   { get; init; }

    /// <summary>2-line justification referencing actual indicator values.</summary>
    public string TechnicalReasoning { get; init; } = string.Empty;

    // ── Confluence & alerts ────────────────────────────────────────────────
    /// <summary>Signal strength 1–10. ≥ 8 triggers Telegram.</summary>
    public int? ConfluenceScore      { get; init; }

    /// <summary>True when ConfluenceScore ≥ 8.</summary>
    public bool SendTelegramAlert    { get; init; }

    public string RawOutput          { get; init; } = string.Empty;
}
