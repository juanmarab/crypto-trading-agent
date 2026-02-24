using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Application.Interfaces;

/// <summary>
/// Service for interacting with the LLM (Google Gemini) to generate trading decisions.
/// </summary>
public interface ILlmService
{
    Task<LlmDecisionResult> AnalyzeAndDecideAsync(string structuredPrompt, CancellationToken cancellationToken = default);
}

/// <summary>
/// The structured result from the LLM's analysis.
/// </summary>
public record LlmDecisionResult
{
    public string TechnicalVerdict { get; init; } = string.Empty;
    public string FundamentalVerdict { get; init; } = string.Empty;
    public TradeAction Action { get; init; }
    public decimal? SuggestedLeverage { get; init; }
    public decimal? Confidence { get; init; }
    public string RawOutput { get; init; } = string.Empty;
}
