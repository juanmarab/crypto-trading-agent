using CryptoAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoAgent.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<MarketNews> MarketNews => Set<MarketNews>();
    public DbSet<TechnicalSnapshot> TechnicalSnapshots => Set<TechnicalSnapshot>();
    public DbSet<AgentDecision> AgentDecisions => Set<AgentDecision>();
    public DbSet<UserAlert> UserAlerts => Set<UserAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
