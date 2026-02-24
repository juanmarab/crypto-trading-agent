using CryptoAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoAgent.Infrastructure.Data.Configurations;

public class TechnicalSnapshotConfiguration : IEntityTypeConfiguration<TechnicalSnapshot>
{
    public void Configure(EntityTypeBuilder<TechnicalSnapshot> builder)
    {
        builder.ToTable("technical_snapshots");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Asset).HasColumnName("asset")
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(e => e.Timeframe).HasColumnName("timeframe").HasMaxLength(10).IsRequired();
        builder.Property(e => e.CapturedAt).HasColumnName("captured_at").HasDefaultValueSql("NOW()");

        // OHLCV
        builder.Property(e => e.OpenPrice).HasColumnName("open_price").HasPrecision(18, 8);
        builder.Property(e => e.HighPrice).HasColumnName("high_price").HasPrecision(18, 8);
        builder.Property(e => e.LowPrice).HasColumnName("low_price").HasPrecision(18, 8);
        builder.Property(e => e.ClosePrice).HasColumnName("close_price").HasPrecision(18, 8);
        builder.Property(e => e.Volume).HasColumnName("volume").HasPrecision(24, 8);

        // EMAs
        builder.Property(e => e.Ema20).HasColumnName("ema_20").HasPrecision(18, 8);
        builder.Property(e => e.Ema50).HasColumnName("ema_50").HasPrecision(18, 8);
        builder.Property(e => e.Ema200).HasColumnName("ema_200").HasPrecision(18, 8);

        // RSI
        builder.Property(e => e.Rsi).HasColumnName("rsi").HasPrecision(8, 4);

        // MACD
        builder.Property(e => e.MacdLine).HasColumnName("macd_line").HasPrecision(18, 8);
        builder.Property(e => e.MacdSignal).HasColumnName("macd_signal").HasPrecision(18, 8);
        builder.Property(e => e.MacdHistogram).HasColumnName("macd_histogram").HasPrecision(18, 8);

        // Bollinger Bands
        builder.Property(e => e.BbUpper).HasColumnName("bb_upper").HasPrecision(18, 8);
        builder.Property(e => e.BbMiddle).HasColumnName("bb_middle").HasPrecision(18, 8);
        builder.Property(e => e.BbLower).HasColumnName("bb_lower").HasPrecision(18, 8);

        // ATR
        builder.Property(e => e.Atr).HasColumnName("atr").HasPrecision(18, 8);

        builder.HasIndex(e => new { e.Asset, e.CapturedAt })
            .HasDatabaseName("idx_tech_snapshots_asset_time")
            .IsDescending(false, true);
    }
}
