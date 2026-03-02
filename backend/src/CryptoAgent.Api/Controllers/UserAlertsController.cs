using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Enums;
using CryptoAgent.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoAgent.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public class UserAlertsController : ControllerBase
{
    private readonly IUserAlertRepository _repo;

    public UserAlertsController(IUserAlertRepository repo) => _repo = repo;

    // ── DTOs ──────────────────────────────────────────────────────────────

    public sealed record RegisterAlertRequest(
        string TelegramChatId,
        string[]? AlertOnlyAssets,
        decimal MinConfidenceThreshold = 0.7m);

    public sealed record UpdateAlertRequest(
        string[]? AlertOnlyAssets,
        decimal? MinConfidenceThreshold,
        bool? IsActive);

    // ── Endpoints ─────────────────────────────────────────────────────────

    /// <summary>
    /// Register a new Telegram alert subscription.
    /// If a subscription with this chat ID already exists, returns 409.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterAlertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TelegramChatId))
            return BadRequest(new { message = "TelegramChatId is required." });

        if (request.MinConfidenceThreshold is < 0m or > 1m)
            return BadRequest(new { message = "MinConfidenceThreshold must be between 0.0 and 1.0." });

        // Idempotency — don't create duplicates
        var existing = await _repo.GetByChatIdAsync(request.TelegramChatId);
        if (existing != null)
            return Conflict(new { message = $"A subscription for chat ID '{request.TelegramChatId}' already exists." });

        // Parse asset filter (null or empty = all assets)
        var assets = ParseAssets(request.AlertOnlyAssets)
            ?? [CryptoAsset.BTC, CryptoAsset.ETH, CryptoAsset.SOL, CryptoAsset.BNB];

        var alert = new UserAlert
        {
            Id = Guid.NewGuid(),
            TelegramChatId = request.TelegramChatId,
            AlertOnlyAssets = assets,
            MinConfidenceThreshold = request.MinConfidenceThreshold,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _repo.AddAsync(alert);

        return CreatedAtAction(nameof(GetByChatId),
            new { chatId = alert.TelegramChatId },
            Map(alert));
    }

    /// <summary>
    /// Get an existing alert subscription by Telegram Chat ID.
    /// </summary>
    [HttpGet("{chatId}")]
    public async Task<IActionResult> GetByChatId(string chatId)
    {
        var alert = await _repo.GetByChatIdAsync(chatId);
        if (alert == null)
            return NotFound(new { message = $"No subscription found for chat ID '{chatId}'." });

        return Ok(Map(alert));
    }

    /// <summary>
    /// Update alert settings (assets filter, confidence threshold, active status).
    /// </summary>
    [HttpPut("{chatId}")]
    public async Task<IActionResult> Update(string chatId, [FromBody] UpdateAlertRequest request)
    {
        var alert = await _repo.GetByChatIdAsync(chatId);
        if (alert == null)
            return NotFound(new { message = $"No subscription found for chat ID '{chatId}'." });

        if (request.MinConfidenceThreshold is < 0m or > 1m)
            return BadRequest(new { message = "MinConfidenceThreshold must be between 0.0 and 1.0." });

        // Apply partial updates
        if (request.AlertOnlyAssets != null)
        {
            var parsed = ParseAssets(request.AlertOnlyAssets);
            if (parsed != null) alert.AlertOnlyAssets = parsed;
        }

        if (request.MinConfidenceThreshold.HasValue)
            alert.MinConfidenceThreshold = request.MinConfidenceThreshold.Value;

        if (request.IsActive.HasValue)
            alert.IsActive = request.IsActive.Value;

        alert.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(alert);
        return Ok(Map(alert));
    }

    /// <summary>
    /// Unsubscribe / delete an alert by Telegram Chat ID.
    /// </summary>
    [HttpDelete("{chatId}")]
    public async Task<IActionResult> Delete(string chatId)
    {
        var alert = await _repo.GetByChatIdAsync(chatId);
        if (alert == null)
            return NotFound(new { message = $"No subscription found for chat ID '{chatId}'." });

        await _repo.DeleteAsync(alert.Id);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static CryptoAsset[]? ParseAssets(string[]? raw)
    {
        if (raw == null || raw.Length == 0) return null;

        var parsed = new List<CryptoAsset>();
        foreach (var s in raw)
        {
            if (Enum.TryParse<CryptoAsset>(s, ignoreCase: true, out var asset))
                parsed.Add(asset);
        }
        return parsed.Count > 0 ? parsed.ToArray() : null;
    }

    private static object Map(UserAlert a) => new
    {
        a.Id,
        a.TelegramChatId,
        AlertOnlyAssets = a.AlertOnlyAssets.Select(x => x.ToString()).ToArray(),
        a.MinConfidenceThreshold,
        a.IsActive,
        a.CreatedAt,
        a.UpdatedAt
    };
}
