using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Domain.Entities;

/// <summary>
/// User alert configuration for Telegram push notifications.
/// </summary>
public class UserAlert
{
    public Guid Id { get; set; }
    public string TelegramChatId { get; set; } = string.Empty;
    public CryptoAsset[] AlertOnlyAssets { get; set; } = [CryptoAsset.BTC, CryptoAsset.ETH, CryptoAsset.SOL, CryptoAsset.BNB];
    public decimal MinConfidenceThreshold { get; set; } = 0.7m;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
