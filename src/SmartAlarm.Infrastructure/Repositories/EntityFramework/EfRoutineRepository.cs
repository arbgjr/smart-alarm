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
    /// Entity Framework Core implementation of IRoutineRepository.
    /// </summary>
    public class EfRoutineRepository : IRoutineRepository
    {
        public async Task<IEnumerable<Routine>> GetAllAsync()
        {
            return await _context.Routines.ToListAsync();
        }
        private readonly SmartAlarmDbContext _context;

        public EfRoutineRepository(SmartAlarmDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Routine> GetByIdAsync(Guid id)
        {
            return await _context.Routines.FindAsync(id);
        }

        public async Task<IEnumerable<Routine>> GetByAlarmIdAsync(Guid alarmId)
        {
            return await _context.Routines
                .Where(r => r.AlarmId == alarmId)
                .ToListAsync();
        }

        public async Task AddAsync(Routine routine)
        {
            await _context.Routines.AddAsync(routine);
        }

        public Task UpdateAsync(Routine routine)
        {
            _context.Routines.Update(routine);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            var routine = _context.Routines.Find(id);
            if (routine != null)
            {
                _context.Routines.Remove(routine);
            }
            return Task.CompletedTask;
        }
    }
}
