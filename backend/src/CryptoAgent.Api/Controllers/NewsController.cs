using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly IMarketNewsRepository _newsRepo;
    private readonly ICryptoPanicService _cryptoPanic;

    public NewsController(
        IMarketNewsRepository newsRepo,
        ICryptoPanicService cryptoPanic)
    {
        _newsRepo = newsRepo;
        _cryptoPanic = cryptoPanic;
    }

    /// <summary>
    /// Get recent news for an asset (last N hours, up to 50 articles).
    /// </summary>
    [HttpGet("{asset}")]
    public async Task<IActionResult> GetRecent(
        CryptoAsset asset,
        [FromQuery] int hours = 24,
        [FromQuery] int limit = 20)
    {
        var news = await _newsRepo.GetRecentByAssetAsync(asset, hours, limit);
        return Ok(news.Select(n => new
        {
            n.Id,
            n.Asset,
            n.Headline,
            n.Content,
            n.SourceUrl,
            n.PublishedAt,
            n.SentimentScore,
            HasEmbedding = n.Embedding != null
        }));
    }

    /// <summary>
    /// Manually trigger a news ingestion cycle (useful for testing / dev).
    /// </summary>
    [HttpPost("ingest")]
    public async Task<IActionResult> TriggerIngestion(CancellationToken ct)
    {
        await _cryptoPanic.FetchAndStoreNewsAsync(ct);
        return Ok(new { message = "News ingestion cycle completed." });
    }
}
