using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para repositório de webhooks
    /// </summary>
    public interface IWebhookRepository
    {
        Task<Webhook> CreateAsync(Webhook webhook);
        Task<Webhook?> GetByIdAsync(Guid id);
        Task<IEnumerable<Webhook>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Webhook>> GetActiveWebhooksAsync();
        Task<Webhook> UpdateAsync(Webhook webhook);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<IEnumerable<Webhook>> GetByEventTypeAsync(string eventType);
    }
}
