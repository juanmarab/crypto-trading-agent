using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoAgent.Infrastructure.Data.Configurations;

public class MarketNewsConfiguration : IEntityTypeConfiguration<MarketNews>
{
    public void Configure(EntityTypeBuilder<MarketNews> builder)
    {
        builder.ToTable("market_news");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Asset).HasColumnName("asset")
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(e => e.Headline).HasColumnName("headline").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Content).HasColumnName("content");
        builder.Property(e => e.SourceUrl).HasColumnName("source_url").HasMaxLength(1000);
        builder.Property(e => e.PublishedAt).HasColumnName("published_at").IsRequired();
        builder.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(768)");
        builder.Property(e => e.SentimentScore).HasColumnName("sentiment_score").HasPrecision(5, 4);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(e => new { e.Asset, e.PublishedAt })
            .HasDatabaseName("idx_market_news_asset_published")
            .IsDescending(false, true);
    }
}
