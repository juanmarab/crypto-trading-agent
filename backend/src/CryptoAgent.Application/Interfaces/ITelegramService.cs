namespace CryptoAgent.Application.Interfaces;

/// <summary>
/// Service for sending notifications via the Telegram Bot API.
/// </summary>
public interface ITelegramService
{
    Task SendAlertAsync(string chatId, string message, CancellationToken cancellationToken = default);
}
