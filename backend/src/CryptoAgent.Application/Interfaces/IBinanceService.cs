namespace CryptoAgent.Application.Interfaces;

/// <summary>
/// Service for connecting to Binance WebSocket and REST APIs.
/// Provides live prices and OHLCV kline data for BTC, ETH, SOL, BNB.
/// </summary>
public interface IBinanceService
{
    Task StartWebSocketAsync(CancellationToken cancellationToken);
    Task StopWebSocketAsync();
}
