using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Domain.Interfaces;

public interface IAgentDecisionRepository
{
    Task<AgentDecision?> GetByIdAsync(Guid id);
    Task<IEnumerable<AgentDecision>> GetRecentByAssetAsync(CryptoAsset asset, int limit = 20);
    Task<AgentDecision?> GetLatestByAssetAsync(CryptoAsset asset);
    Task AddAsync(AgentDecision decision);
}
