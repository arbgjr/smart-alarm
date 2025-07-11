using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Entity Framework Core implementation of IIntegrationRepository.
    /// </summary>
    public class EfIntegrationRepository : IIntegrationRepository
    {
        public async Task<IEnumerable<Integration>> GetAllAsync()
        {
            return await _context.Integrations.ToListAsync();
        }
        private readonly SmartAlarmDbContext _context;

        public EfIntegrationRepository(SmartAlarmDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Integration?> GetByIdAsync(Guid id)
        {
            return await _context.Integrations.FindAsync(id);
        }

        public async Task<IEnumerable<Integration>> GetByAlarmIdAsync(Guid alarmId)
        {
            return await _context.Integrations
                .Where(i => i.AlarmId == alarmId)
                .ToListAsync();
        }

        public async Task AddAsync(Integration integration)
        {
            await _context.Integrations.AddAsync(integration);
        }

        public Task UpdateAsync(Integration integration)
        {
            _context.Integrations.Update(integration);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            var integration = _context.Integrations.Find(id);
            if (integration != null)
            {
                _context.Integrations.Remove(integration);
            }
            return Task.CompletedTask;
        }
    }
}
