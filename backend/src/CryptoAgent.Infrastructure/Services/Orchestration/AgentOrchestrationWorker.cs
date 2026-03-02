using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using CryptoAgent.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Orchestration;

/// <summary>
/// Background worker that runs the full AI analysis pipeline on a configurable interval.
///
/// Pipeline per cycle:
///   For each asset → AgentOrchestrationService.RunForAssetAsync()
///   If action != Hold AND confidence is above user threshold → ITelegramService alert
///
/// Runs every 15 minutes (aligned with the primary timeframe).
/// Starts with a 60s delay to let the indicator worker populate snapshots first.
/// </summary>
public class AgentOrchestrationWorker : BackgroundService
{
    private static readonly CryptoAsset[] Assets =
        [CryptoAsset.BTC, CryptoAsset.ETH, CryptoAsset.SOL, CryptoAsset.BNB];

    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentOrchestrationWorker> _logger;

    public AgentOrchestrationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AgentOrchestrationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AgentOrchestrationWorker starting. Waiting {Delay}s for data to stabilize...",
            StartupDelay.TotalSeconds);

        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in orchestration cycle.");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        _logger.LogInformation("🧠 Starting AI orchestration cycle...");

        using var scope = _scopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<AgentOrchestrationService>();
        var userAlertRepo = scope.ServiceProvider.GetRequiredService<IUserAlertRepository>();
        var telegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();

        // Fetch all active alert subscriptions once per cycle
        var activeAlerts = (await userAlertRepo.GetActiveAlertsAsync()).ToList();

        foreach (var asset in Assets)
        {
            try
            {
                var decision = await orchestrator.RunForAssetAsync(asset, ct);
                if (decision == null) continue;

                // Only alert on actionable decisions (Long or Short)
                if (decision.Action == Domain.Enums.TradeAction.HOLD)
                {
                    _logger.LogDebug("{Asset}: Hold — no Telegram alert sent.", asset);
                    continue;
                }

                // Notify each eligible subscriber
                foreach (var alert in activeAlerts)
                {
                    // Skip if subscriber only wants specific assets and this isn't one
                    if (alert.AlertOnlyAssets?.Length > 0
                        && !alert.AlertOnlyAssets.Contains(asset))
                        continue;

                    // Skip if confidence is below user's threshold
                    if (decision.Confidence.GetValueOrDefault() < alert.MinConfidenceThreshold)
                    {
                        _logger.LogDebug(
                            "Skipping alert for {Asset} to chat {ChatId}: confidence {Conf:P0} < threshold {Threshold:P0}",
                            asset, alert.TelegramChatId, decision.Confidence, alert.MinConfidenceThreshold);
                        continue;
                    }

                    var message = FormatAlertMessage(decision);

                    try
                    {
                        await telegram.SendAlertAsync(alert.TelegramChatId, message, ct);
                        _logger.LogInformation(
                            "📱 Alert sent to {ChatId} for {Asset} ({Action}).",
                            alert.TelegramChatId, asset, decision.Action);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send Telegram alert to {ChatId}.", alert.TelegramChatId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestration failed for {Asset}.", asset);
            }
        }

        _logger.LogInformation("🧠 Orchestration cycle complete.");
    }

    private static string FormatAlertMessage(Domain.Entities.AgentDecision decision)
    {
        var actionEmoji = decision.Action switch
        {
            TradeAction.LONG => "🟢",
            TradeAction.SHORT => "🔴",
            _ => "⚪"
        };

        var leverageStr = decision.SuggestedLeverage.HasValue
            ? $"{decision.SuggestedLeverage:F0}x"
            : "—";

        return $"""
            {actionEmoji} *{decision.Asset}/USDT — {decision.Action.ToString().ToUpper()}*

            📊 Technical: `{decision.TechnicalVerdict}`
            📰 Fundamental: `{decision.FundamentalVerdict}`
            💪 Confidence: `{decision.Confidence:P0}`
            ⚡ Suggested Leverage: `{leverageStr}`
            🕐 Time: `{decision.DecidedAt:yyyy-MM-dd HH:mm} UTC`

            _Powered by CryptoAgent AI — Not financial advice._
            """;
    }
}
