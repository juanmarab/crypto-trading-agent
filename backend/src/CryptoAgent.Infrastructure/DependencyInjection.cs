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
        // ── Database ──────────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseVector();
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IMarketNewsRepository, MarketNewsRepository>();
        services.AddScoped<ITechnicalSnapshotRepository, TechnicalSnapshotRepository>();
        services.AddScoped<IAgentDecisionRepository, AgentDecisionRepository>();
        services.AddScoped<IUserAlertRepository, UserAlertRepository>();

        // ── Core Services ─────────────────────────────────────────────────────
        services.AddSingleton<IBinanceService, BinanceService>();
        services.AddSingleton<ITechnicalAnalysisService, TechnicalAnalysisService>();

        // ── LLM Service ───────────────────────────────────────────────────────
        // Priority: Groq (fastest, free) → Gemini (free tier) → Mock (no key, local dev)
        var groqKey   = configuration["ApiKeys:Groq"];
        var geminiKey = configuration["ApiKeys:GoogleGemini"];

        if (!string.IsNullOrWhiteSpace(groqKey))
        {
            services.AddHttpClient<ILlmService, GroqLlmService>(client =>
            {
                client.BaseAddress = new Uri("https://api.groq.com/");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqKey}");
                client.Timeout = TimeSpan.FromSeconds(30); // Groq is very fast
            });
        }
        else if (!string.IsNullOrWhiteSpace(geminiKey))
        {
            services.AddHttpClient<ILlmService, GeminiLlmService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
        }
        else
        {
            // Local dev / testing: realistic mocked decisions, zero API calls
            services.AddSingleton<ILlmService, MockLlmService>();
        }

        // ── Embedding Service ─────────────────────────────────────────────────
        // Groq does not offer embeddings. Gemini → Mock fallback.
        if (!string.IsNullOrWhiteSpace(geminiKey))
        {
            services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }
        else
        {
            // Dummy 768-dim vectors — RAG relevance won't be semantic, but the pipeline works
            services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
        }

        // ── News Ingestion ────────────────────────────────────────────────────
        var panicKey = configuration["ApiKeys:CryptoPanic"];
        services.AddSingleton(new CryptoPanicOptions { ApiKey = panicKey ?? string.Empty });

        if (!string.IsNullOrWhiteSpace(panicKey))
        {
            services.AddHttpClient<ICryptoPanicService, CryptoPanicService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
        }
        else
        {
            // Generates synthetic headlines so the news feed and RAG pipeline populate locally
            services.AddSingleton<ICryptoPanicService, MockNewsService>();
        }

        // ── Telegram Notifications ────────────────────────────────────────────
        services.AddHttpClient<ITelegramService, TelegramService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // ── AI Orchestrator (scoped — depends on scoped repos) ────────────────
        services.AddScoped<AgentOrchestrationService>();

        // ── Background Workers ────────────────────────────────────────────────
        services.AddHostedService<BinanceWebSocketWorker>();
        services.AddHostedService<IndicatorCalculationWorker>();
        services.AddHostedService<NewsIngestionWorker>();
        services.AddHostedService<AgentOrchestrationWorker>();

        return services;
    }
}
