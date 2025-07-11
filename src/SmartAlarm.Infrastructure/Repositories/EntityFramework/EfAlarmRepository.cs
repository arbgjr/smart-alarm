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
    /// Entity Framework Core implementation of IAlarmRepository.
    /// </summary>
    public class EfAlarmRepository : IAlarmRepository
    {
        private readonly SmartAlarmDbContext _context;

        public EfAlarmRepository(SmartAlarmDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Alarm?> GetByIdAsync(Guid id)
        {
            return await _context.Alarms
                .Include(a => a.Schedules)
                .Include(a => a.Routines)
                .Include(a => a.Integrations)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Alarm>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Alarms
                .Include(a => a.Schedules)
                .Include(a => a.Routines)
                .Include(a => a.Integrations)
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(Alarm alarm)
        {
            await _context.Alarms.AddAsync(alarm);
        }

        public Task UpdateAsync(Alarm alarm)
        {
            _context.Alarms.Update(alarm);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            var alarm = _context.Alarms.Find(id);
            if (alarm != null)
            {
                _context.Alarms.Remove(alarm);
            }
            return Task.CompletedTask;
        }
    }
}
