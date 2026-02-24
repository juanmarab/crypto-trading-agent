using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.TechnicalAnalysis;

/// <summary>
/// Background worker that fetches klines and calculates indicators
/// every 5 and 15 minutes for all 4 assets, persisting snapshots to the database.
/// </summary>
public class IndicatorCalculationWorker : BackgroundService
{
    private readonly IBinanceService _binance;
    private readonly ITechnicalAnalysisService _taService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IndicatorCalculationWorker> _logger;

    private static readonly CryptoAsset[] Assets = [CryptoAsset.BTC, CryptoAsset.ETH, CryptoAsset.SOL, CryptoAsset.BNB];
    private static readonly string[] Timeframes = ["5m", "15m"];

    public IndicatorCalculationWorker(
        IBinanceService binance,
        ITechnicalAnalysisService taService,
        IServiceScopeFactory scopeFactory,
        ILogger<IndicatorCalculationWorker> logger)
    {
        _binance = binance;
        _taService = taService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IndicatorCalculationWorker starting. Waiting 30s for WebSocket to stabilize...");
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CalculateAllIndicatorsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during indicator calculation cycle.");
            }

            // Run every 5 minutes (aligned with the smallest timeframe)
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task CalculateAllIndicatorsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting indicator calculation cycle...");

        foreach (var asset in Assets)
        {
            foreach (var timeframe in Timeframes)
            {
                try
                {
                    var klines = await _binance.GetKlinesAsync(asset, timeframe, 250, ct);
                    if (klines.Count < 50)
                    {
                        _logger.LogWarning("Insufficient klines for {Asset}@{Tf}: {Count}", asset, timeframe, klines.Count);
                        continue;
                    }

                    var indicators = _taService.CalculateIndicators(asset, timeframe, klines);

                    // Persist snapshot
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<ITechnicalSnapshotRepository>();

                    var snapshot = new TechnicalSnapshot
                    {
                        Asset = asset,
                        Timeframe = timeframe,
                        CapturedAt = indicators.Timestamp,
                        OpenPrice = indicators.Open,
                        HighPrice = indicators.High,
                        LowPrice = indicators.Low,
                        ClosePrice = indicators.Close,
                        Volume = indicators.Volume,
                        Ema20 = indicators.Ema20,
                        Ema50 = indicators.Ema50,
                        Ema200 = indicators.Ema200,
                        Rsi = indicators.Rsi,
                        MacdLine = indicators.MacdLine,
                        MacdSignal = indicators.MacdSignal,
                        MacdHistogram = indicators.MacdHistogram,
                        BbUpper = indicators.BbUpper,
                        BbMiddle = indicators.BbMiddle,
                        BbLower = indicators.BbLower,
                        Atr = indicators.Atr,
                    };

                    await repo.AddAsync(snapshot);

                    _logger.LogInformation(
                        "✅ {Asset}@{Tf} | RSI: {Rsi:F2} | MACD: {Macd:F8} | ATR: {Atr:F8} | Close: {Close}",
                        asset, timeframe, indicators.Rsi, indicators.MacdLine, indicators.Atr, indicators.Close);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to calculate indicators for {Asset}@{Timeframe}", asset, timeframe);
                }
            }
        }

        _logger.LogInformation("Indicator calculation cycle complete.");
    }
}
