using CryptoAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoAgent.Infrastructure.Data.Configurations;

public class AgentDecisionConfiguration : IEntityTypeConfiguration<AgentDecision>
{
    public void Configure(EntityTypeBuilder<AgentDecision> builder)
    {
        builder.ToTable("agent_decisions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Asset).HasColumnName("asset")
            .HasConversion<string>().HasMaxLength(10);

        builder.Property(e => e.DecidedAt).HasColumnName("decided_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.TechnicalVerdict).HasColumnName("technical_verdict").IsRequired();
        builder.Property(e => e.FundamentalVerdict).HasColumnName("fundamental_verdict").IsRequired();

        builder.Property(e => e.Action).HasColumnName("action")
            .HasConversion<string>().HasMaxLength(10);

        builder.Property(e => e.SuggestedLeverage).HasColumnName("suggested_leverage").HasPrecision(4, 1);
        builder.Property(e => e.Confidence).HasColumnName("confidence").HasPrecision(5, 4);

        // ── Quant trade parameters ────────────────────────────────────────
        builder.Property(e => e.EntryPrice).HasColumnName("entry_price").HasPrecision(20, 8);
        builder.Property(e => e.TakeProfit).HasColumnName("take_profit").HasPrecision(20, 8);
        builder.Property(e => e.TakeProfit2).HasColumnName("take_profit2").HasPrecision(20, 8);
        builder.Property(e => e.StopLoss).HasColumnName("stop_loss").HasPrecision(20, 8);
        builder.Property(e => e.PositionSizeUsd).HasColumnName("position_size_usd").HasPrecision(12, 4);
        builder.Property(e => e.HoldingPeriodHours).HasColumnName("holding_period_hours");
        builder.Property(e => e.TechnicalReasoning).HasColumnName("technical_reasoning")
            .HasDefaultValue(string.Empty).IsRequired();

        // ── Confluence & alerts ───────────────────────────────────────────
        builder.Property(e => e.ConfluenceScore).HasColumnName("confluence_score");
        builder.Property(e => e.SendTelegramAlert).HasColumnName("send_telegram_alert")
            .HasDefaultValue(false);

        builder.Property(e => e.RawLlmOutput).HasColumnName("raw_llm_output").IsRequired();
        builder.Property(e => e.SnapshotId).HasColumnName("snapshot_id");
        builder.Property(e => e.NewsIds).HasColumnName("news_ids").HasDefaultValueSql("'{}'");

        builder.HasOne(e => e.Snapshot)
            .WithMany(s => s.Decisions)
            .HasForeignKey(e => e.SnapshotId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.Asset, e.DecidedAt })
            .HasDatabaseName("idx_agent_decisions_asset_time")
            .IsDescending(false, true);
    }
}
