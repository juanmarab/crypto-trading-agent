using CryptoAgent.Domain.Entities;

namespace CryptoAgent.Domain.Interfaces;

public interface IUserAlertRepository
{
    Task<UserAlert?> GetByIdAsync(Guid id);
    Task<UserAlert?> GetByChatIdAsync(string telegramChatId);
    Task<IEnumerable<UserAlert>> GetActiveAlertsAsync();
    Task AddAsync(UserAlert alert);
    Task UpdateAsync(UserAlert alert);
    Task DeleteAsync(Guid id);
}
