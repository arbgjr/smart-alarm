using System.Collections.Concurrent;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Infrastructure.Repositories
{
    /// <summary>
    /// Implementação em memória do repositório de webhooks para desenvolvimento/teste
    /// </summary>
    public class InMemoryWebhookRepository : IWebhookRepository
    {
        private readonly ConcurrentDictionary<Guid, Webhook> _webhooks = new();

        public Task<Webhook> CreateAsync(Webhook webhook)
        {
            webhook.Id = webhook.Id == Guid.Empty ? Guid.NewGuid() : webhook.Id;
            _webhooks.TryAdd(webhook.Id, webhook);
            return Task.FromResult(webhook);
        }

        public Task<Webhook?> GetByIdAsync(Guid id)
        {
            _webhooks.TryGetValue(id, out var webhook);
            return Task.FromResult(webhook);
        }

        public Task<IEnumerable<Webhook>> GetByUserIdAsync(Guid userId)
        {
            var userWebhooks = _webhooks.Values.Where(w => w.UserId == userId);
            return Task.FromResult(userWebhooks);
        }

        public Task<IEnumerable<Webhook>> GetActiveWebhooksAsync()
        {
            var activeWebhooks = _webhooks.Values.Where(w => w.IsActive);
            return Task.FromResult(activeWebhooks);
        }

        public Task<Webhook> UpdateAsync(Webhook webhook)
        {
            webhook.UpdatedAt = DateTime.UtcNow;
            _webhooks.AddOrUpdate(webhook.Id, webhook, (_, _) => webhook);
            return Task.FromResult(webhook);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            return Task.FromResult(_webhooks.TryRemove(id, out _));
        }

        public Task<bool> ExistsAsync(Guid id)
        {
            return Task.FromResult(_webhooks.ContainsKey(id));
        }

        public Task<IEnumerable<Webhook>> GetByEventTypeAsync(string eventType)
        {
            var webhooks = _webhooks.Values.Where(w => w.Events.Contains(eventType) && w.IsActive);
            return Task.FromResult(webhooks);
        }
    }
}
