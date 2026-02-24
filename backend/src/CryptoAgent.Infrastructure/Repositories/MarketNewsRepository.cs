using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using CryptoAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace CryptoAgent.Infrastructure.Repositories;

public class MarketNewsRepository : IMarketNewsRepository
{
    private readonly AppDbContext _db;

    public MarketNewsRepository(AppDbContext db) => _db = db;

    public async Task<MarketNews?> GetByIdAsync(Guid id) =>
        await _db.MarketNews.FindAsync(id);

    public async Task<IEnumerable<MarketNews>> GetRecentByAssetAsync(CryptoAsset asset, int hours = 24, int limit = 20)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-hours);
        return await _db.MarketNews
            .Where(n => n.Asset == asset && n.PublishedAt >= cutoff)
            .OrderByDescending(n => n.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<MarketNews>> SearchSimilarAsync(Vector embedding, CryptoAsset asset, int hours = 24, int limit = 5)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-hours);
        return await _db.MarketNews
            .Where(n => n.Asset == asset && n.PublishedAt >= cutoff && n.Embedding != null)
            .OrderBy(n => n.Embedding!.CosineDistance(embedding))
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddAsync(MarketNews news)
    {
        await _db.MarketNews.AddAsync(news);
        await _db.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<MarketNews> newsList)
    {
        await _db.MarketNews.AddRangeAsync(newsList);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExistsByHeadlineAsync(string headline, CryptoAsset asset) =>
        await _db.MarketNews.AnyAsync(n => n.Headline == headline && n.Asset == asset);
}
