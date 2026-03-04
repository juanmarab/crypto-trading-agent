using System.Text.RegularExpressions;
using System.Xml.Linq;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.News;

/// <summary>
/// Fetches crypto news from free public RSS feeds (no API key required).
/// Sources: CoinTelegraph, CoinDesk, Decrypt, TheBlock.
/// </summary>
public class CryptoPanicService : ICryptoPanicService
{
    // ── RSS feeds ─────────────────────────────────────────────────────────────
    private static readonly string[] RssFeeds =
    [
        "https://cointelegraph.com/rss",
        "https://decrypt.co/feed",
        "https://www.coindesk.com/arc/outboundfeeds/rss/?outputType=xml",
    ];

    // ── Asset keyword mapping ────────────────────────────────────────────────
    private static readonly Dictionary<CryptoAsset, string[]> AssetKeywords = new()
    {
        [CryptoAsset.BTC] = ["bitcoin", "btc", "satoshi"],
        [CryptoAsset.ETH] = ["ethereum", "eth", "ether", "vitalik"],
        [CryptoAsset.SOL] = ["solana", "sol"],
        [CryptoAsset.BNB] = ["binance", "bnb", "bsc"],
    };

    private readonly HttpClient _http;
    private readonly IEmbeddingService _embeddingService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CryptoPanicService> _logger;

    public CryptoPanicService(
        HttpClient http,
        IEmbeddingService embeddingService,
        IServiceScopeFactory scopeFactory,
        ILogger<CryptoPanicService> logger)
    {
        _http            = http;
        _embeddingService = embeddingService;
        _scopeFactory    = scopeFactory;
        _logger          = logger;
    }

    // ── ICryptoPanicService ──────────────────────────────────────────────────

    public async Task FetchAndStoreNewsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting RSS crypto news ingestion cycle...");

        var rawItems = new List<(string Title, string? Url, DateTimeOffset Published)>();

        foreach (var feedUrl in RssFeeds)
        {
            try
            {
                var items = await FetchRssAsync(feedUrl, cancellationToken);
                rawItems.AddRange(items);
                _logger.LogInformation("Fetched {Count} items from {Feed}", items.Count, feedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RSS feed {Feed} unavailable: {Msg}", feedUrl, ex.Message);
            }
        }

        // Deduplicate by normalised title
        rawItems = rawItems
            .GroupBy(x => x.Title.ToLowerInvariant().Trim())
            .Select(g => g.First())
            .ToList();

        _logger.LogInformation("Processing {Total} unique articles.", rawItems.Count);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMarketNewsRepository>();

        var inserted = 0;

        foreach (var (title, url, published) in rawItems)
        {
            var assets = Classify(title);
            if (assets.Count == 0) continue;

            foreach (var asset in assets)
            {
                try
                {
                    if (await repo.ExistsByHeadlineAsync(title, asset)) continue;

                    var embedding = await _embeddingService.GenerateEmbeddingAsync(title, cancellationToken);

                    await repo.AddRangeAsync(
                    [
                        new MarketNews
                        {
                            Id          = Guid.NewGuid(),
                            Asset       = asset,
                            Headline    = title,
                            Content     = null,
                            SourceUrl   = url,
                            PublishedAt = published,
                            Embedding   = embedding,
                            CreatedAt   = DateTimeOffset.UtcNow,
                        }
                    ]);
                    inserted++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not store [{Title}] for {Asset}: {Msg}",
                        title[..Math.Min(60, title.Length)], asset, ex.Message);
                }
            }
        }

        _logger.LogInformation("✅ Inserted {Count} new news articles.", inserted);
        _logger.LogInformation("RSS ingestion cycle complete.");
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<List<(string Title, string? Url, DateTimeOffset Published)>>
        FetchRssAsync(string feedUrl, CancellationToken ct)
    {
        using var response = await _http.GetAsync(feedUrl, ct);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(ct);
        var doc = XDocument.Parse(xml);

        XNamespace dc  = "http://purl.org/dc/elements/1.1/";
        XNamespace atom = "http://www.w3.org/2005/Atom";

        var items = doc.Descendants("item")
            .Concat(doc.Descendants(atom + "entry"))
            .Take(50)
            .Select(item =>
            {
                var title = StripHtml(
                    item.Element("title")?.Value ??
                    item.Element(atom + "title")?.Value ?? "").Trim();

                var link =
                    item.Element("link")?.Value?.Trim() ??
                    item.Element(atom + "link")?.Attribute("href")?.Value;

                var pubStr =
                    item.Element("pubDate")?.Value ??
                    item.Element("published")?.Value ??
                    item.Element(atom + "published")?.Value ??
                    item.Element(dc + "date")?.Value;

                var published = DateTimeOffset.TryParse(pubStr, out var dt) ? dt : DateTimeOffset.UtcNow;

                return (title, link, published);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.title))
            .ToList();

        return items;
    }

    private static List<CryptoAsset> Classify(string title)
    {
        var lower  = title.ToLowerInvariant();
        var result = new List<CryptoAsset>();

        foreach (var (asset, kws) in AssetKeywords)
            if (kws.Any(kw => lower.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                result.Add(asset);

        return result;
    }

    private static string StripHtml(string text) =>
        Regex.Replace(text, "<[^>]+>", string.Empty);
}

/// <summary>
/// Kept for DI compatibility — options are no longer used since we use RSS feeds.
/// </summary>
public sealed class CryptoPanicOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public static CryptoPanicOptions FromConfiguration(
        Microsoft.Extensions.Configuration.IConfiguration cfg) =>
        new() { ApiKey = cfg["ApiKeys:CryptoPanic"] ?? string.Empty };
}
