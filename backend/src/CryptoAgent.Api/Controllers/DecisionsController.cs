using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DecisionsController : ControllerBase
{
    private readonly IAgentDecisionRepository _repo;

    public DecisionsController(IAgentDecisionRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Get the latest AI trading decision for a specific asset.
    /// </summary>
    [HttpGet("latest/{asset}")]
    public async Task<IActionResult> GetLatest(CryptoAsset asset)
    {
        var decision = await _repo.GetLatestByAssetAsync(asset);
        if (decision == null)
            return NotFound(new { message = $"No decision found for {asset}." });

        return Ok(Map(decision));
    }

    /// <summary>
    /// Get the most recent N AI decisions for an asset.
    /// </summary>
    [HttpGet("{asset}")]
    public async Task<IActionResult> GetRecent(
        CryptoAsset asset,
        [FromQuery] int limit = 20)
    {
        var decisions = await _repo.GetRecentByAssetAsync(asset, limit);
        return Ok(decisions.Select(Map));
    }

    /// <summary>
    /// Get a specific AI decision by its ID (includes linked snapshot).
    /// </summary>
    [HttpGet("detail/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var decision = await _repo.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { message = $"Decision {id} not found." });

        return Ok(Map(decision));
    }

    // ── Private mapper ─────────────────────────────────────────────────────

    private static object Map(Domain.Entities.AgentDecision d) => new
    {
        d.Id,
        d.Asset,
        d.DecidedAt,
        d.TechnicalVerdict,
        d.FundamentalVerdict,
        Action = d.Action.ToString(),
        d.SuggestedLeverage,
        d.Confidence,
        d.SnapshotId,
        // Omit RawLlmOutput for list views to keep payloads small
    };
}
