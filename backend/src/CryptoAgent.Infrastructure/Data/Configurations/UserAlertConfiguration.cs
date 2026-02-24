using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoAgent.Infrastructure.Data.Configurations;

public class UserAlertConfiguration : IEntityTypeConfiguration<UserAlert>
{
    public void Configure(EntityTypeBuilder<UserAlert> builder)
    {
        builder.ToTable("user_alerts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.TelegramChatId).HasColumnName("telegram_chat_id")
            .HasMaxLength(100).IsRequired();

        builder.Property(e => e.AlertOnlyAssets).HasColumnName("alert_only_assets");
        builder.Property(e => e.MinConfidenceThreshold).HasColumnName("min_confidence_threshold")
            .HasPrecision(5, 4).HasDefaultValue(0.7m);

        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(e => e.TelegramChatId).IsUnique()
            .HasDatabaseName("idx_user_alerts_chat_id");
    }
}
