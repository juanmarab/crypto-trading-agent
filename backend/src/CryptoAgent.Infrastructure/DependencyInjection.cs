using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Interfaces;
using CryptoAgent.Infrastructure.Data;
using CryptoAgent.Infrastructure.Repositories;
using CryptoAgent.Infrastructure.Services.Binance;
using CryptoAgent.Infrastructure.Services.TechnicalAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseVector();
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        // ── Repositories ──────────────────────────────────────────────────
        services.AddScoped<IMarketNewsRepository, MarketNewsRepository>();
        services.AddScoped<ITechnicalSnapshotRepository, TechnicalSnapshotRepository>();
        services.AddScoped<IAgentDecisionRepository, AgentDecisionRepository>();
        services.AddScoped<IUserAlertRepository, UserAlertRepository>();

        // ── Services ──────────────────────────────────────────────────────
        services.AddSingleton<IBinanceService, BinanceService>();
        services.AddSingleton<ITechnicalAnalysisService, TechnicalAnalysisService>();

        // ── Background Workers ────────────────────────────────────────────
        services.AddHostedService<BinanceWebSocketWorker>();
        services.AddHostedService<IndicatorCalculationWorker>();

        return services;
    }
}
