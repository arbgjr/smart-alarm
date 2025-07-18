using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Api.Tests.Controllers
{
    /// <summary>
    /// Implementação em memória do IWebhookRepository para testes de integração
    /// </summary>
    public class InMemoryWebhookRepository : IWebhookRepository
    {
        private readonly ConcurrentDictionary<Guid, Webhook> _webhooks = new();

        public Task<Webhook> CreateAsync(Webhook webhook)
        {
            webhook.Id = Guid.NewGuid();
            webhook.CreatedAt = DateTime.UtcNow;
            _webhooks[webhook.Id] = webhook;
            return Task.FromResult(webhook);
        }

        public Task<Webhook?> GetByIdAsync(Guid id)
        {
            _webhooks.TryGetValue(id, out var webhook);
            return Task.FromResult(webhook);
        }

        public Task<IEnumerable<Webhook>> GetByUserIdAsync(Guid userId)
        {
            var webhooks = _webhooks.Values
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt);

            return Task.FromResult(webhooks);
        }

        public Task<IEnumerable<Webhook>> GetActiveWebhooksAsync()
        {
            var activeWebhooks = _webhooks.Values.Where(w => w.IsActive);
            return Task.FromResult(activeWebhooks);
        }

        public Task<Webhook> UpdateAsync(Webhook webhook)
        {
            if (_webhooks.ContainsKey(webhook.Id))
            {
                webhook.UpdatedAt = DateTime.UtcNow;
                _webhooks[webhook.Id] = webhook;
            }
            return Task.FromResult(webhook);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            var removed = _webhooks.TryRemove(id, out _);
            return Task.FromResult(removed);
        }

        public Task<bool> ExistsAsync(Guid id)
        {
            return Task.FromResult(_webhooks.ContainsKey(id));
        }

        public Task<IEnumerable<Webhook>> GetByEventTypeAsync(string eventType)
        {
            var webhooks = _webhooks.Values
                .Where(w => w.Events.Contains(eventType) && w.IsActive);
            return Task.FromResult(webhooks);
        }
    }
}
