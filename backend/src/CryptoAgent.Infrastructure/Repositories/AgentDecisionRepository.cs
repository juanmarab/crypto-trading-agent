using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using CryptoAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CryptoAgent.Infrastructure.Repositories;

public class AgentDecisionRepository : IAgentDecisionRepository
{
    private readonly AppDbContext _db;

    public AgentDecisionRepository(AppDbContext db) => _db = db;

    public async Task<AgentDecision?> GetByIdAsync(Guid id) =>
        await _db.AgentDecisions
            .Include(d => d.Snapshot)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<IEnumerable<AgentDecision>> GetRecentByAssetAsync(CryptoAsset asset, int limit = 20) =>
        await _db.AgentDecisions
            .Where(d => d.Asset == asset)
            .OrderByDescending(d => d.DecidedAt)
            .Include(d => d.Snapshot)
            .Take(limit)
            .ToListAsync();

    public async Task<AgentDecision?> GetLatestByAssetAsync(CryptoAsset asset) =>
        await _db.AgentDecisions
            .Where(d => d.Asset == asset)
            .OrderByDescending(d => d.DecidedAt)
            .Include(d => d.Snapshot)
            .FirstOrDefaultAsync();

    public async Task AddAsync(AgentDecision decision)
    {
        await _db.AgentDecisions.AddAsync(decision);
        await _db.SaveChangesAsync();
    }
}
