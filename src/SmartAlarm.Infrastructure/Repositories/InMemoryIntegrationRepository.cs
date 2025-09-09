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

        public Task<Integration?> GetByIdAsync(Guid id)
        {
            _integrations.TryGetValue(id, out var integration);
            return Task.FromResult(integration);
        }

        public Task<IEnumerable<Integration>> GetByAlarmIdAsync(Guid alarmId)
        {
            var result = _integrations.Values.Where(i => i.AlarmId == alarmId);
            return Task.FromResult(result!);
        }

        public Task<IEnumerable<Integration>> GetByUserIdAsync(Guid userId)
        {
            // Para encontrar integrações por usuário, precisamos buscar via alarmes
            // Em uma implementação real, isso seria feito com JOIN no banco
            var result = _integrations.Values.Where(i => 
                // Para este exemplo, usamos uma lógica simples baseada no hash do userId
                // Em produção, haveria uma relação real User -> Alarm -> Integration
                (i.AlarmId.GetHashCode() % 100) == (userId.GetHashCode() % 100)
            );
            return Task.FromResult(result!);
        }

        public Task<IEnumerable<Integration>> GetActiveByUserIdAsync(Guid userId)
        {
            // Similar ao anterior, mas filtrando apenas integrações ativas
            var result = _integrations.Values.Where(i => 
                i.IsActive && 
                (i.AlarmId.GetHashCode() % 100) == (userId.GetHashCode() % 100)
            );
            return Task.FromResult(result!);
        }

        public Task<Integration?> GetByUserAndTypeAsync(Guid userId, string integrationType)
        {
            // Busca integração por tipo e usuário
            var result = _integrations.Values.FirstOrDefault(i =>
                i.Provider == integrationType &&
                (i.AlarmId.GetHashCode() % 100) == (userId.GetHashCode() % 100)
            );
            return Task.FromResult(result);
        }

        public Task UpdateAsync(Integration integration)
        {
            _integrations[integration.Id] = integration;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Integration>> GetAllAsync()
        {
            return Task.FromResult(_integrations.Values.AsEnumerable());
        }
    }
}

