using CryptoAgent.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.News;

/// <summary>
/// Background worker that triggers CryptoPanic news ingestion every 15 minutes.
/// Runs on startup after a short warm-up delay, then on a fixed interval.
/// </summary>
public class NewsIngestionWorker : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan WarmUpDelay = TimeSpan.FromSeconds(15);

    private readonly ICryptoPanicService _cryptoPanic;
    private readonly ILogger<NewsIngestionWorker> _logger;

    public NewsIngestionWorker(
        ICryptoPanicService cryptoPanic,
        ILogger<NewsIngestionWorker> logger)
    {
        _cryptoPanic = cryptoPanic;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NewsIngestionWorker starting. First run in {Delay}s, then every {Interval} minutes.",
            WarmUpDelay.TotalSeconds, RunInterval.TotalMinutes);

        // Short warm-up so the app is fully started before the first API call
        await Task.Delay(WarmUpDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _cryptoPanic.FetchAndStoreNewsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in NewsIngestionWorker.");
            }

            try
            {
                await Task.Delay(RunInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("NewsIngestionWorker stopped.");
    }
}
