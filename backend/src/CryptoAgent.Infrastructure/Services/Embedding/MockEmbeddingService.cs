using CryptoAgent.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace CryptoAgent.Infrastructure.Services.Embedding;

/// <summary>
/// Local dev fallback — generates random 768-dim vectors when no Gemini key is configured.
/// The RAG pipeline will function but similarity scores won't be semantically meaningful.
/// </summary>
public class MockEmbeddingService : IEmbeddingService
{
    private readonly ILogger<MockEmbeddingService> _logger;
    private static readonly Random Rng = new();

    public MockEmbeddingService(ILogger<MockEmbeddingService> logger) => _logger = logger;

    public Task<Vector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[MockEmbeddingService] Generating dummy 768-dim vector.");

        // Gemini text-embedding-004 produces 768 dimensions.
        float[] values = new float[768];
        for (int i = 0; i < values.Length; i++)
            values[i] = (float)(Rng.NextDouble() * 2.0 - 1.0); // range [-1, 1]

        return Task.FromResult(new Vector(values));
    }
}
