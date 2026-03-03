using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.News;

/// <summary>
/// Local dev fallback — inserts synthetic market headlines so the RAG feed
/// and news pipeline are populated even without a CryptoPanic API key.
/// </summary>
public class MockNewsService : ICryptoPanicService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MockNewsService> _logger;
    private static readonly Random Rng = new();

    private static readonly string[] HeadlineTemplates =
    [
        "{0} breaks key resistance as institutional DCA volume accelerates",
        "On-chain data shows whale accumulation of {0} near current support levels",
        "{0} open interest surges 40% — options traders position for a major move",
        "Bearish divergence spotted on {0} daily RSI amid broader market uncertainty",
        "{0} funding rates turn negative, hinting at potential short squeeze ahead",
        "Analysts divided on {0} outlook after CPI print: bulls vs. bears weigh in"
    ];

    public MockNewsService(
        IEmbeddingService embeddingService,
        IServiceScopeFactory scopeFactory,
        ILogger<MockNewsService> logger)
    {
        _embeddingService = embeddingService;
        _scopeFactory     = scopeFactory;
        _logger           = logger;
    }

    public async Task FetchAndStoreNewsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[MockNewsService] No CryptoPanic key configured. Generating synthetic headlines.");

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMarketNewsRepository>();

        var assets = new[] { CryptoAsset.BTC, CryptoAsset.ETH, CryptoAsset.SOL, CryptoAsset.BNB };

        foreach (var asset in assets)
        {
            // ~50 % chance per asset per cycle — keeps the feed varied
            if (Rng.Next(2) != 0) continue;

            var headline = string.Format(
                HeadlineTemplates[Rng.Next(HeadlineTemplates.Length)],
                asset.ToString());

            if (await repo.ExistsByHeadlineAsync(headline, asset))
                continue;

            var embedding = await _embeddingService.GenerateEmbeddingAsync(headline, cancellationToken);

            var news = new MarketNews
            {
                Id          = Guid.NewGuid(),
                Asset       = asset,
                Headline    = headline,
                Content     = "Synthetic article generated for local development. The market summary " +
                              $"indicates notable activity for {asset}/USDT on multiple timeframes.",
                SourceUrl   = "https://example.com/mock-news",
                PublishedAt = DateTimeOffset.UtcNow,
                Embedding   = embedding,
                CreatedAt   = DateTimeOffset.UtcNow
            };

            await repo.AddRangeAsync(new[] { news });
            _logger.LogInformation("✅ Mock article inserted for {Asset}: \"{Headline}\"", asset, headline);
        }
    }
}
