using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DecisionsController : ControllerBase
{
    private readonly IAgentDecisionRepository _repo;

    public DecisionsController(IAgentDecisionRepository repo) => _repo = repo;

    /// <summary>Get the latest AI trading decision for a specific asset.</summary>
    [HttpGet("latest/{asset}")]
    public async Task<IActionResult> GetLatest(CryptoAsset asset)
    {
        var decision = await _repo.GetLatestByAssetAsync(asset);
        if (decision == null)
            return NotFound(new { message = $"No decision found for {asset}." });
        return Ok(Map(decision));
    }

    /// <summary>Get the most recent N AI decisions for an asset.</summary>
    [HttpGet("{asset}")]
    public async Task<IActionResult> GetRecent(CryptoAsset asset, [FromQuery] int limit = 20)
    {
        var decisions = await _repo.GetRecentByAssetAsync(asset, limit);
        return Ok(decisions.Select(Map));
    }

    /// <summary>Get a specific AI decision by its ID (includes linked snapshot).</summary>
    [HttpGet("detail/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var decision = await _repo.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { message = $"Decision {id} not found." });
        return Ok(Map(decision));
    }

    // ── Private mapper ─────────────────────────────────────────────────────

    private static object Map(Domain.Entities.AgentDecision d)
    {
        // Compute both RR ratios from ATR multiples baked into the quant prompt
        // SL = 1.5×ATR, TP1 = |entry−SL|×1.5, TP2 = |entry−SL|×3
        // → RR1 = 1.5, RR2 = 3.0
        string? rrRatio = d.Action != TradeAction.HOLD && d.TakeProfit.HasValue && d.StopLoss.HasValue
            ? "1:1.5 / 1:3"
            : null;

        return new
        {
            d.Id,
            d.Asset,
            d.DecidedAt,
            d.TechnicalVerdict,
            d.FundamentalVerdict,
            Action            = d.Action.ToString(),
            d.SuggestedLeverage,
            d.Confidence,
            // ── Quant trade parameters ───────────────────────────────────
            d.EntryPrice,
            d.TakeProfit,
            d.TakeProfit2,
            d.StopLoss,
            d.PositionSizeUsd,
            d.HoldingPeriodHours,
            d.TechnicalReasoning,
            // ── Confluence ───────────────────────────────────────────────
            d.ConfluenceScore,
            d.SendTelegramAlert,
            RiskRewardRatio   = rrRatio,
            d.SnapshotId,
        };
    }
}
