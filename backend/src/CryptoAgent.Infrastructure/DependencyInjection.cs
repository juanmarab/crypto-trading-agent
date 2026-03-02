using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Interfaces;
using CryptoAgent.Infrastructure.Data;
using CryptoAgent.Infrastructure.Repositories;
using CryptoAgent.Infrastructure.Services.Binance;
using CryptoAgent.Infrastructure.Services.Embedding;
using CryptoAgent.Infrastructure.Services.Llm;
using CryptoAgent.Infrastructure.Services.News;
using CryptoAgent.Infrastructure.Services.Orchestration;
using CryptoAgent.Infrastructure.Services.Telegram;
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

        // ── Phase 3: Embedding Service (Gemini text-embedding-004) ────────
        services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── Phase 3: CryptoPanic News Service ─────────────────────────────
        services.AddSingleton(CryptoPanicOptions.FromConfiguration(configuration));
        services.AddHttpClient<ICryptoPanicService, CryptoPanicService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ── Phase 4: LLM Service ──────────────────────────────────────────
        services.AddHttpClient<ILlmService, GeminiLlmService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // ── Phase 4: Telegram Notification Service ────────────────────────
        services.AddHttpClient<ITelegramService, TelegramService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // ── Phase 4: AI Orchestrator ──────────────────────────────────────
        // Scoped because it depends on scoped repositories
        services.AddScoped<AgentOrchestrationService>();

        // ── Background Workers ────────────────────────────────────────────
        services.AddHostedService<BinanceWebSocketWorker>();
        services.AddHostedService<IndicatorCalculationWorker>();
        services.AddHostedService<NewsIngestionWorker>();
        services.AddHostedService<AgentOrchestrationWorker>();

        return services;
    }
}
