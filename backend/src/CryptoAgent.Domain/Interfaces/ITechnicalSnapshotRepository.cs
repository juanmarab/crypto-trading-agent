using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Domain.Interfaces;

public interface ITechnicalSnapshotRepository
{
    Task<TechnicalSnapshot?> GetByIdAsync(Guid id);
    Task<TechnicalSnapshot?> GetLatestByAssetAsync(CryptoAsset asset, string timeframe = "15m");
    Task<IEnumerable<TechnicalSnapshot>> GetRecentByAssetAsync(CryptoAsset asset, string timeframe = "15m", int limit = 50);
    Task AddAsync(TechnicalSnapshot snapshot);
}
