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
    /// Entity Framework Core implementation of IScheduleRepository.
    /// </summary>
    public class EfScheduleRepository : IScheduleRepository
    {
        private readonly SmartAlarmDbContext _context;

        public EfScheduleRepository(SmartAlarmDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Schedule> GetByIdAsync(Guid id)
        {
            return await _context.Schedules.FindAsync(id);
        }

        public async Task<IEnumerable<Schedule>> GetByAlarmIdAsync(Guid alarmId)
        {
            return await _context.Schedules
                .Where(s => s.AlarmId == alarmId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Schedule>> GetActiveSchedulesAsync()
        {
            return await _context.Schedules
                .Where(s => s.IsActive)
                .ToListAsync();
        }

        public async Task AddAsync(Schedule schedule)
        {
            await _context.Schedules.AddAsync(schedule);
        }

        public Task UpdateAsync(Schedule schedule)
        {
            _context.Schedules.Update(schedule);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            var schedule = _context.Schedules.Find(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
            }
            return Task.CompletedTask;
        }
    }
}