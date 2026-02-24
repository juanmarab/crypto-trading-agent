using System.Collections.Concurrent;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoAgent.Application.DTOs.TechnicalAnalysis;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;
using CryptoExchange.Net.Objects.Sockets;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Binance;

public class BinanceService : IBinanceService, IDisposable
{
    private readonly ILogger<BinanceService> _logger;
    private readonly BinanceRestClient _restClient;
    private readonly BinanceSocketClient _socketClient;
    private readonly ConcurrentDictionary<CryptoAsset, PriceTick> _latestPrices = new();
    private UpdateSubscription? _subscription;

    public event Action<PriceTick>? OnPriceTick;

    private static readonly Dictionary<CryptoAsset, string> SymbolMap = new()
    {
        { CryptoAsset.BTC, "BTCUSDT" },
        { CryptoAsset.ETH, "ETHUSDT" },
        { CryptoAsset.SOL, "SOLUSDT" },
        { CryptoAsset.BNB, "BNBUSDT" },
    };

    private static readonly Dictionary<string, CryptoAsset> ReverseSymbolMap =
        SymbolMap.ToDictionary(kv => kv.Value, kv => kv.Key);

    public BinanceService(ILogger<BinanceService> logger)
    {
        _logger = logger;
        _restClient = new BinanceRestClient();
        _socketClient = new BinanceSocketClient();
    }

    // ── WebSocket ─────────────────────────────────────────────────────────

    public async Task StartWebSocketAsync(CancellationToken cancellationToken)
    {
        var symbols = SymbolMap.Values.ToArray();
        _logger.LogInformation("Subscribing to Binance WebSocket for: {Symbols}", string.Join(", ", symbols));

        var result = await _socketClient.SpotApi.ExchangeData.SubscribeToTickerUpdatesAsync(
            symbols,
            data =>
            {
                var symbol = data.Data.Symbol;
                if (!ReverseSymbolMap.TryGetValue(symbol, out var asset)) return;

                var tick = new PriceTick
                {
                    Asset = asset,
                    Price = data.Data.LastPrice,
                    Volume24h = data.Data.Volume,
                    PriceChangePercent24h = data.Data.PriceChangePercent,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _latestPrices[asset] = tick;
                OnPriceTick?.Invoke(tick);
            },
            cancellationToken);

        if (result.Success)
        {
            _subscription = result.Data;
            _logger.LogInformation("Binance WebSocket connected successfully.");
        }
        else
        {
            _logger.LogError("Failed to connect Binance WebSocket: {Error}", result.Error?.Message);
        }
    }

    public async Task StopWebSocketAsync()
    {
        if (_subscription != null)
        {
            await _socketClient.UnsubscribeAsync(_subscription);
            _logger.LogInformation("Binance WebSocket disconnected.");
        }
    }

    // ── REST API (Klines) ─────────────────────────────────────────────────

    public PriceTick? GetLatestPrice(CryptoAsset asset) =>
        _latestPrices.TryGetValue(asset, out var tick) ? tick : null;

    public IReadOnlyDictionary<CryptoAsset, PriceTick> GetAllPrices() =>
        _latestPrices;

    public async Task<IReadOnlyList<KlineData>> GetKlinesAsync(
        CryptoAsset asset, string interval, int limit = 250, CancellationToken cancellationToken = default)
    {
        var symbol = SymbolMap[asset];
        var klineInterval = ParseInterval(interval);

        _logger.LogInformation("Fetching {Limit} klines for {Symbol} @ {Interval}", limit, symbol, interval);

        var result = await _restClient.SpotApi.ExchangeData.GetKlinesAsync(
            symbol, klineInterval, limit: limit, ct: cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Failed to fetch klines for {Symbol}: {Error}", symbol, result.Error?.Message);
            return Array.Empty<KlineData>();
        }

        return result.Data.Select(k => new KlineData
        {
            OpenTime = k.OpenTime,
            CloseTime = k.CloseTime,
            Open = k.OpenPrice,
            High = k.HighPrice,
            Low = k.LowPrice,
            Close = k.ClosePrice,
            Volume = k.Volume
        }).ToList();
    }

    private static global::Binance.Net.Enums.KlineInterval ParseInterval(string interval) => interval switch
    {
        "1m" => global::Binance.Net.Enums.KlineInterval.OneMinute,
        "5m" => global::Binance.Net.Enums.KlineInterval.FiveMinutes,
        "15m" => global::Binance.Net.Enums.KlineInterval.FifteenMinutes,
        "1h" => global::Binance.Net.Enums.KlineInterval.OneHour,
        "4h" => global::Binance.Net.Enums.KlineInterval.FourHour,
        "1d" => global::Binance.Net.Enums.KlineInterval.OneDay,
        _ => global::Binance.Net.Enums.KlineInterval.FifteenMinutes,
    };

    public void Dispose()
    {
        _restClient.Dispose();
        _socketClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
