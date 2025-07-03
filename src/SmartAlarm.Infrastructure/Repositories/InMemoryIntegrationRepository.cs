using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Infrastructure.Repositories
{
    /// <summary>
    /// In-memory implementation of IIntegrationRepository for development and testing.
    /// </summary>
    public class InMemoryIntegrationRepository : IIntegrationRepository
    {
        private readonly ConcurrentDictionary<Guid, Integration> _integrations = new();

        public Task AddAsync(Integration integration)
        {
            _integrations[integration.Id] = integration;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _integrations.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<Integration> GetByIdAsync(Guid id)
        {
            _integrations.TryGetValue(id, out var integration);
            return Task.FromResult(integration);
        }

        public Task<IEnumerable<Integration>> GetByAlarmIdAsync(Guid alarmId)
        {
            var result = _integrations.Values.Where(i => i.AlarmId == alarmId);
            return Task.FromResult(result);
        }

        public Task UpdateAsync(Integration integration)
        {
            _integrations[integration.Id] = integration;
            return Task.CompletedTask;
        }
    }
}
