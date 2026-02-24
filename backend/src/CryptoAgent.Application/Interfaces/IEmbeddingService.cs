using Pgvector;

namespace CryptoAgent.Application.Interfaces;

/// <summary>
/// Service for generating text embeddings using an AI model.
/// </summary>
public interface IEmbeddingService
{
    Task<Vector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
