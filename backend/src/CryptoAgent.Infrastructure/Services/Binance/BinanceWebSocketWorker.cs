using CryptoAgent.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Binance;

/// <summary>
/// Background service managing the Binance WebSocket lifecycle.
/// Starts on application boot, reconnects on failure.
/// </summary>
public class BinanceWebSocketWorker : BackgroundService
{
    private readonly IBinanceService _binance;
    private readonly ILogger<BinanceWebSocketWorker> _logger;

    public BinanceWebSocketWorker(IBinanceService binance, ILogger<BinanceWebSocketWorker> logger)
    {
        _binance = binance;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BinanceWebSocketWorker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _binance.StartWebSocketAsync(stoppingToken);

                // Keep alive until cancellation
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("BinanceWebSocketWorker stopping gracefully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Binance WebSocket error. Reconnecting in 10 seconds...");
                await _binance.StopWebSocketAsync();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        await _binance.StopWebSocketAsync();
    }
}
