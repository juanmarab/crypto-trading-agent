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

    // The "Glass Box" reasoning
    public string TechnicalVerdict { get; set; } = string.Empty;
    public string FundamentalVerdict { get; set; } = string.Empty;
    public TradeAction Action { get; set; }
    public decimal? SuggestedLeverage { get; set; }
    public decimal? Confidence { get; set; }

    // Raw LLM output for debugging and auditing
    public string RawLlmOutput { get; set; } = string.Empty;

    // References
    public Guid? SnapshotId { get; set; }
    public TechnicalSnapshot? Snapshot { get; set; }
    public Guid[] NewsIds { get; set; } = [];
}
