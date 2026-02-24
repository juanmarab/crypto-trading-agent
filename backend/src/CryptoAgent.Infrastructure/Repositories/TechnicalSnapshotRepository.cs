using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using CryptoAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CryptoAgent.Infrastructure.Repositories;

public class TechnicalSnapshotRepository : ITechnicalSnapshotRepository
{
    private readonly AppDbContext _db;

    public TechnicalSnapshotRepository(AppDbContext db) => _db = db;

    public async Task<TechnicalSnapshot?> GetByIdAsync(Guid id) =>
        await _db.TechnicalSnapshots.FindAsync(id);

    public async Task<TechnicalSnapshot?> GetLatestByAssetAsync(CryptoAsset asset, string timeframe = "15m") =>
        await _db.TechnicalSnapshots
            .Where(s => s.Asset == asset && s.Timeframe == timeframe)
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<TechnicalSnapshot>> GetRecentByAssetAsync(CryptoAsset asset, string timeframe = "15m", int limit = 50) =>
        await _db.TechnicalSnapshots
            .Where(s => s.Asset == asset && s.Timeframe == timeframe)
            .OrderByDescending(s => s.CapturedAt)
            .Take(limit)
            .ToListAsync();

    public async Task AddAsync(TechnicalSnapshot snapshot)
    {
        await _db.TechnicalSnapshots.AddAsync(snapshot);
        await _db.SaveChangesAsync();
    }
}
