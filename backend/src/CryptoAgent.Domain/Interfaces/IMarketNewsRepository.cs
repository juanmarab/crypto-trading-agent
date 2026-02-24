using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using Pgvector;

namespace CryptoAgent.Domain.Interfaces;

public interface IMarketNewsRepository
{
    Task<MarketNews?> GetByIdAsync(Guid id);
    Task<IEnumerable<MarketNews>> GetRecentByAssetAsync(CryptoAsset asset, int hours = 24, int limit = 20);
    Task<IEnumerable<MarketNews>> SearchSimilarAsync(Vector embedding, CryptoAsset asset, int hours = 24, int limit = 5);
    Task AddAsync(MarketNews news);
    Task AddRangeAsync(IEnumerable<MarketNews> newsList);
    Task<bool> ExistsByHeadlineAsync(string headline, CryptoAsset asset);
}
