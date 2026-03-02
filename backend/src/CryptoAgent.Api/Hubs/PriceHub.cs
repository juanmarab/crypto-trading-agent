using CryptoAgent.Application.DTOs.TechnicalAnalysis;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace CryptoAgent.Api.Hubs;

/// <summary>
/// SignalR hub for real-time price streaming to connected frontend clients.
///
/// Clients should connect to: /hubs/price
///
/// Events pushed to clients:
///   - "PriceTick"  → { asset, price, volume24h, priceChangePercent24h, timestamp }
/// </summary>
public class PriceHub : Hub
{
    /// <summary>
    /// Called by the client to request the current snapshot of all prices.
    /// Useful on reconnect — gets all 4 prices without waiting for the next tick.
    /// </summary>
    public async Task GetAllPrices(IBinanceService binance)
    {
        var prices = binance.GetAllPrices();
        await Clients.Caller.SendAsync("AllPrices", prices);
    }
}

/// <summary>
/// Singleton dispatcher that bridges the BinanceService price event
/// to all connected SignalR clients.
///
/// Registered as a hosted service — starts immediately and stays alive
/// for the lifetime of the app.
/// </summary>
public class PriceHubDispatcher : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IBinanceService _binance;
    private readonly IHubContext<PriceHub> _hub;
    private readonly Microsoft.Extensions.Logging.ILogger<PriceHubDispatcher> _logger;

    public PriceHubDispatcher(
        IBinanceService binance,
        IHubContext<PriceHub> hub,
        Microsoft.Extensions.Logging.ILogger<PriceHubDispatcher> logger)
    {
        _binance = binance;
        _hub = hub;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _binance.OnPriceTick += async (tick) =>
        {
            try
            {
                await _hub.Clients.All.SendAsync("PriceTick", tick, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast price tick for {Asset}.", tick.Asset);
            }
        };

        _logger.LogInformation("PriceHubDispatcher started — broadcasting price ticks to SignalR clients.");

        // Keep alive until cancellation
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
