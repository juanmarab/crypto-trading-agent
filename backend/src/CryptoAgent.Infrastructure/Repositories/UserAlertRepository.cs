using CryptoAgent.Domain.Entities;
using CryptoAgent.Domain.Interfaces;
using CryptoAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CryptoAgent.Infrastructure.Repositories;

public class UserAlertRepository : IUserAlertRepository
{
    private readonly AppDbContext _db;

    public UserAlertRepository(AppDbContext db) => _db = db;

    public async Task<UserAlert?> GetByIdAsync(Guid id) =>
        await _db.UserAlerts.FindAsync(id);

    public async Task<UserAlert?> GetByChatIdAsync(string telegramChatId) =>
        await _db.UserAlerts.FirstOrDefaultAsync(u => u.TelegramChatId == telegramChatId);

    public async Task<IEnumerable<UserAlert>> GetActiveAlertsAsync() =>
        await _db.UserAlerts
            .Where(u => u.IsActive)
            .ToListAsync();

    public async Task AddAsync(UserAlert alert)
    {
        await _db.UserAlerts.AddAsync(alert);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserAlert alert)
    {
        alert.UpdatedAt = DateTimeOffset.UtcNow;
        _db.UserAlerts.Update(alert);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var alert = await _db.UserAlerts.FindAsync(id);
        if (alert != null)
        {
            _db.UserAlerts.Remove(alert);
            await _db.SaveChangesAsync();
        }
    }
}
