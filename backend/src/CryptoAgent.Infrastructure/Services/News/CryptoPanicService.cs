using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.News;

/// <summary>
/// Fetches crypto news from the CryptoPanic public API, generates embeddings
/// for each headline, and persists new articles to the database.
/// 
/// CryptoPanic free-tier API: https://cryptopanic.com/api/free/v1/posts/
/// Docs: https://cryptopanic.com/developers/api/
/// </summary>
public class CryptoPanicService : ICryptoPanicService
{
    /// <summary>Maps a CryptoPanic currency symbol to our CryptoAsset enum.</summary>
    private static readonly Dictionary<string, CryptoAsset> SymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "BTC", CryptoAsset.BTC },
        { "ETH", CryptoAsset.ETH },
        { "SOL", CryptoAsset.SOL },
        { "BNB", CryptoAsset.BNB },
    };

    private readonly HttpClient _http;
    private readonly IEmbeddingService _embeddingService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CryptoPanicService> _logger;
    private readonly string _apiKey;

    public CryptoPanicService(
        HttpClient http,
        IEmbeddingService embeddingService,
        IServiceScopeFactory scopeFactory,
        CryptoPanicOptions options,
        ILogger<CryptoPanicService> logger)
    {
        _http = http;
        _embeddingService = embeddingService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _apiKey = options.ApiKey;
    }

    public async Task FetchAndStoreNewsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting CryptoPanic news ingestion cycle...");

        foreach (var (symbol, asset) in SymbolMap)
        {
            try
            {
                await FetchAndStoreForAssetAsync(symbol, asset, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch/store news for {Symbol}.", symbol);
            }
        }

        _logger.LogInformation("CryptoPanic ingestion cycle complete.");
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private async Task FetchAndStoreForAssetAsync(
        string symbol, CryptoAsset asset, CancellationToken ct)
    {
        // CryptoPanic free-tier endpoint
        var url = $"https://cryptopanic.com/api/free/v1/posts/?auth_token={_apiKey}" +
                  $"&currencies={symbol}&filter=news&public=true";

        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CryptoPanicResponse>(
            cancellationToken: ct);

        if (body?.Results == null || body.Results.Count == 0)
        {
            _logger.LogDebug("No news returned for {Symbol}.", symbol);
            return;
        }

        _logger.LogInformation("Fetched {Count} articles for {Symbol}.", body.Results.Count, symbol);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMarketNewsRepository>();

        var toInsert = new List<MarketNews>();

        foreach (var post in body.Results)
        {
            if (string.IsNullOrWhiteSpace(post.Title)) continue;

            // Skip duplicates (idempotent by headline + asset)
            var exists = await repo.ExistsByHeadlineAsync(post.Title, asset);
            if (exists)
            {
                _logger.LogDebug("Skipping duplicate headline: {Title}", post.Title);
                continue;
            }

            // Generate embedding for the headline (+ body if available)
            var textToEmbed = string.IsNullOrWhiteSpace(post.Body)
                ? post.Title
                : $"{post.Title}. {post.Body}";

            var embedding = await _embeddingService.GenerateEmbeddingAsync(textToEmbed, ct);

            var news = new MarketNews
            {
                Id = Guid.NewGuid(),
                Asset = asset,
                Headline = post.Title,
                Content = post.Body,
                SourceUrl = post.Url,
                PublishedAt = post.PublishedAt,
                Embedding = embedding,
                CreatedAt = DateTimeOffset.UtcNow
            };

            toInsert.Add(news);
        }

        if (toInsert.Count > 0)
        {
            await repo.AddRangeAsync(toInsert);
            _logger.LogInformation("✅ Inserted {Count} new articles for {Symbol}.", toInsert.Count, symbol);
        }
        else
        {
            _logger.LogDebug("No new articles to insert for {Symbol}.", symbol);
        }
    }

    // ── CryptoPanic API DTOs ─────────────────────────────────────────────────

    private sealed class CryptoPanicResponse
    {
        [JsonPropertyName("results")]
        public List<CryptoPanicPost> Results { get; set; } = [];
    }

    private sealed class CryptoPanicPost
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset PublishedAt { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        // Body is only present on paid tiers; null is fine
        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }
}

/// <summary>
/// Configuration options for the CryptoPanic service, populated from appsettings.json.
/// </summary>
public sealed class CryptoPanicOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public static CryptoPanicOptions FromConfiguration(
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return new CryptoPanicOptions
        {
            ApiKey = configuration["ApiKeys:CryptoPanic"] ?? string.Empty
        };
    }
}
