using CryptoAgent.Application.DTOs.TechnicalAnalysis;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TechnicalAnalysisController : ControllerBase
{
    private readonly IBinanceService _binance;
    private readonly ITechnicalAnalysisService _taService;
    private readonly ITechnicalSnapshotRepository _snapshotRepo;

    public TechnicalAnalysisController(
        IBinanceService binance,
        ITechnicalAnalysisService taService,
        ITechnicalSnapshotRepository snapshotRepo)
    {
        _binance = binance;
        _taService = taService;
        _snapshotRepo = snapshotRepo;
    }

    /// <summary>
    /// Get live prices for all 4 monitored assets.
    /// </summary>
    [HttpGet("prices")]
    public IActionResult GetPrices()
    {
        var prices = _binance.GetAllPrices();
        return Ok(prices);
    }

    /// <summary>
    /// Get the live price for a specific asset.
    /// </summary>
    [HttpGet("prices/{asset}")]
    public IActionResult GetPrice(CryptoAsset asset)
    {
        var price = _binance.GetLatestPrice(asset);
        if (price == null) return NotFound(new { message = $"No price data available for {asset}" });
        return Ok(price);
    }

    /// <summary>
    /// Calculate fresh indicators on-demand for an asset and timeframe.
    /// </summary>
    [HttpGet("indicators/{asset}")]
    public async Task<IActionResult> GetIndicators(
        CryptoAsset asset,
        [FromQuery] string timeframe = "15m",
        CancellationToken ct = default)
    {
        var klines = await _binance.GetKlinesAsync(asset, timeframe, 250, ct);
        if (klines.Count < 50)
            return BadRequest(new { message = "Insufficient kline data for indicator calculation." });

        var indicators = _taService.CalculateIndicators(asset, timeframe, klines);
        return Ok(indicators);
    }

    /// <summary>
    /// Get the full technical analysis summary for an asset (both 5m and 15m).
    /// </summary>
    [HttpGet("analysis/{asset}")]
    public async Task<IActionResult> GetAnalysis(CryptoAsset asset, CancellationToken ct = default)
    {
        var klines5m = await _binance.GetKlinesAsync(asset, "5m", 250, ct);
        var klines15m = await _binance.GetKlinesAsync(asset, "15m", 250, ct);

        var indicators5m = klines5m.Count >= 50
            ? _taService.CalculateIndicators(asset, "5m", klines5m)
            : null;

        var indicators15m = klines15m.Count >= 50
            ? _taService.CalculateIndicators(asset, "15m", klines15m)
            : null;

        var analysis = new TechnicalAnalysisDto
        {
            Asset = asset,
            CurrentPrice = _binance.GetLatestPrice(asset),
            Indicators5m = indicators5m,
            Indicators15m = indicators15m,
            OverallVerdict = DeriveOverallVerdict(indicators5m, indicators15m),
            LastUpdated = DateTimeOffset.UtcNow
        };

        return Ok(analysis);
    }

    /// <summary>
    /// Get the latest stored snapshots from the database for an asset.
    /// </summary>
    [HttpGet("snapshots/{asset}")]
    public async Task<IActionResult> GetSnapshots(
        CryptoAsset asset,
        [FromQuery] string timeframe = "15m",
        [FromQuery] int limit = 20)
    {
        var snapshots = await _snapshotRepo.GetRecentByAssetAsync(asset, timeframe, limit);
        return Ok(snapshots);
    }

    /// <summary>
    /// Get raw OHLCV kline data from Binance for charting.
    /// </summary>
    [HttpGet("klines/{asset}")]
    public async Task<IActionResult> GetKlines(
        CryptoAsset asset,
        [FromQuery] string interval = "15m",
        [FromQuery] int limit = 250,
        CancellationToken ct = default)
    {
        var klines = await _binance.GetKlinesAsync(asset, interval, limit, ct);
        return Ok(klines);
    }

    // ── Private Helpers ───────────────────────────────────────────────────

    private static string DeriveOverallVerdict(IndicatorSet? ind5m, IndicatorSet? ind15m)
    {
        if (ind15m == null) return "INSUFFICIENT_DATA";

        int bullishScore = 0, bearishScore = 0;

        // Weight 15m indicators more (x2) for convergence
        ScoreIndicators(ind15m, ref bullishScore, ref bearishScore, weight: 2);

        if (ind5m != null)
            ScoreIndicators(ind5m, ref bullishScore, ref bearishScore, weight: 1);

        if (bullishScore > bearishScore + 2) return "BULLISH";
        if (bearishScore > bullishScore + 2) return "BEARISH";
        return "NEUTRAL";
    }

    private static void ScoreIndicators(IndicatorSet ind, ref int bull, ref int bear, int weight)
    {
        if (ind.EmaTrend?.Contains("BULLISH") == true) bull += weight;
        if (ind.EmaTrend?.Contains("BEARISH") == true) bear += weight;

        if (ind.RsiSignal == "OVERSOLD") bull += weight;
        if (ind.RsiSignal == "OVERBOUGHT") bear += weight;

        if (ind.MacdSignalType == "BULLISH_CROSS") bull += weight;
        if (ind.MacdSignalType == "BEARISH_CROSS") bear += weight;

        if (ind.BbPosition == "BELOW_LOWER") bull += weight;
        if (ind.BbPosition == "ABOVE_UPPER") bear += weight;
    }
}
