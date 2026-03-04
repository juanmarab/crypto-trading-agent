using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Domain.Entities;

/// <summary>
/// A historical record of the AI agent's trading decision.
/// Stores the full "glass box" reasoning for auditing win-rate over time.
/// </summary>
public class AgentDecision
{
    public Guid Id { get; set; }
    public CryptoAsset Asset { get; set; }
    public DateTimeOffset DecidedAt { get; set; } = DateTimeOffset.UtcNow;

    // ── Direction & sizing ────────────────────────────────────────────────
    public string TechnicalVerdict { get; set; } = string.Empty;
    public string FundamentalVerdict { get; set; } = string.Empty;
    public TradeAction Action { get; set; }
    public decimal? SuggestedLeverage { get; set; }
    public decimal? Confidence { get; set; }

    // ── Quant trade parameters ─────────────────────────────────────────────
    /// <summary>Entry price at decision time (absolute USDT).</summary>
    public decimal? EntryPrice { get; set; }

    /// <summary>TP1 — next S/R level, minimum Risk:Reward 1:1.5 (absolute USDT).</summary>
    public decimal? TakeProfit { get; set; }

    /// <summary>TP2 — extended target, minimum Risk:Reward 1:3 (absolute USDT).</summary>
    public decimal? TakeProfit2 { get; set; }

    /// <summary>Stop-loss placed beyond nearest S/R ± 1.5× ATR to avoid stop hunts.</summary>
    public decimal? StopLoss { get; set; }

    /// <summary>USD position size so that a loss at SL equals exactly 1% of account capital.</summary>
    public decimal? PositionSizeUsd { get; set; }

    /// <summary>Estimated number of hours to hold the position.</summary>
    public int? HoldingPeriodHours { get; set; }

    /// <summary>2-line LLM justification referencing specific indicator values.</summary>
    public string TechnicalReasoning { get; set; } = string.Empty;

    // ── Confluence & alerts ───────────────────────────────────────────────
    /// <summary>Signal strength score 1–10. Score ≥ 8 = high probability setup.</summary>
    public int? ConfluenceScore { get; set; }

    /// <summary>True when ConfluenceScore ≥ 8. Used to trigger Telegram alerts.</summary>
    public bool SendTelegramAlert { get; set; }

    // ── Raw output for debugging ──────────────────────────────────────────
    public string RawLlmOutput { get; set; } = string.Empty;

    // ── References ────────────────────────────────────────────────────────
    public Guid? SnapshotId { get; set; }
    public TechnicalSnapshot? Snapshot { get; set; }
    public Guid[] NewsIds { get; set; } = [];
}
