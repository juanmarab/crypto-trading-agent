using CryptoAgent.Application.DTOs.TechnicalAnalysis;
using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Application.Interfaces;

/// <summary>
/// Service for connecting to Binance WebSocket and REST APIs.
/// Provides live prices and OHLCV kline data for BTC, ETH, SOL, BNB.
/// </summary>
public interface IBinanceService
{
    /// <summary>
    /// Start the WebSocket connection for all 4 asset tickers.
    /// </summary>
    Task StartWebSocketAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stop WebSocket connections.
    /// </summary>
    Task StopWebSocketAsync();

    /// <summary>
    /// Get the latest price tick for a specific asset.
    /// </summary>
    PriceTick? GetLatestPrice(CryptoAsset asset);

    /// <summary>
    /// Get all latest price ticks.
    /// </summary>
    IReadOnlyDictionary<CryptoAsset, PriceTick> GetAllPrices();

    /// <summary>
    /// Fetch historical kline (OHLCV) data from Binance REST API.
    /// </summary>
    Task<IReadOnlyList<KlineData>> GetKlinesAsync(CryptoAsset asset, string interval, int limit = 250, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a new price tick arrives from the WebSocket.
    /// </summary>
    event Action<PriceTick>? OnPriceTick;
}
