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
    /// In-memory implementation of IRoutineRepository for development and testing.
    /// </summary>
    public class InMemoryRoutineRepository : IRoutineRepository
    {
        private readonly ConcurrentDictionary<Guid, Routine> _routines = new();

        public Task AddAsync(Routine routine)
        {
            _routines[routine.Id] = routine;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _routines.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<Routine> GetByIdAsync(Guid id)
        {
            _routines.TryGetValue(id, out var routine);
            return Task.FromResult(routine!);
        }

        public Task<IEnumerable<Routine>> GetByAlarmIdAsync(Guid alarmId)
        {
            var result = _routines.Values.Where(r => r.AlarmId == alarmId);
            return Task.FromResult(result!);
        }

        public Task UpdateAsync(Routine routine)
        {
            _routines[routine.Id] = routine;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Routine>> GetAllAsync()
        {
            return Task.FromResult(_routines.Values.AsEnumerable());
        }
    }
}

