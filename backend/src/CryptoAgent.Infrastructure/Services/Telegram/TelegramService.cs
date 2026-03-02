using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CryptoAgent.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoAgent.Infrastructure.Services.Telegram;

/// <summary>
/// Sends messages to Telegram users via the Bot API.
/// Uses the sendMessage endpoint over HTTPS.
/// </summary>
public class TelegramService : ITelegramService
{
    private const string BaseUrl = "https://api.telegram.org";

    private readonly HttpClient _http;
    private readonly string _botToken;
    private readonly ILogger<TelegramService> _logger;

    public TelegramService(
        HttpClient http,
        IConfiguration configuration,
        ILogger<TelegramService> logger)
    {
        _http = http;
        _logger = logger;
        _botToken = configuration["ApiKeys:TelegramBotToken"]
            ?? throw new InvalidOperationException("TelegramBotToken is not configured.");
    }

    public async Task SendAlertAsync(
        string chatId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chatId))
        {
            _logger.LogWarning("SendAlertAsync called with empty chatId — skipping.");
            return;
        }

        var url = $"{BaseUrl}/bot{_botToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text = message,
            parse_mode = "Markdown"   // Allow *bold* and `code` formatting
        };

        try
        {
            var response = await _http.PostAsJsonAsync(url, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Telegram API error {StatusCode}: {Body}",
                    (int)response.StatusCode, body);
            }
            else
            {
                _logger.LogDebug("Telegram alert sent to {ChatId}.", chatId);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send Telegram alert to {ChatId}.", chatId);
            throw;
        }
    }
}
