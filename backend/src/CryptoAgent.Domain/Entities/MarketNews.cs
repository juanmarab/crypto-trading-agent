using CryptoAgent.Domain.Enums;
using Pgvector;

namespace CryptoAgent.Domain.Entities;

/// <summary>
/// A financial news article ingested via the RAG pipeline.
/// Stores the vector embedding for semantic similarity search.
/// </summary>
public class MarketNews
{
    public Guid Id { get; set; }
    public CryptoAsset Asset { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? SourceUrl { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public Vector? Embedding { get; set; }
    public decimal? SentimentScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
