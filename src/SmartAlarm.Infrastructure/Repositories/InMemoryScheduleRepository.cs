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
    /// In-memory implementation of IScheduleRepository for development and testing.
    /// Thread-safe and suitable for unit/integration tests.
    /// </summary>
    public class InMemoryScheduleRepository : IScheduleRepository
    {
        private readonly ConcurrentDictionary<Guid, Schedule> _schedules = new();

        public Task AddAsync(Schedule schedule)
        {
            _schedules[schedule.Id] = schedule;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _schedules.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<Schedule?> GetByIdAsync(Guid id)
        {
            _schedules.TryGetValue(id, out var schedule);
            return Task.FromResult(schedule);
        }

        public Task<IEnumerable<Schedule>> GetByAlarmIdAsync(Guid alarmId)
        {
            var result = _schedules.Values.Where(s => s.AlarmId == alarmId);
            return Task.FromResult(result!);
        }

        public Task<IEnumerable<Schedule>> GetActiveSchedulesAsync()
        {
            var result = _schedules.Values.Where(s => s.IsActive);
            return Task.FromResult(result!);
        }

        public Task UpdateAsync(Schedule schedule)
        {
            _schedules[schedule.Id] = schedule;
            return Task.CompletedTask;
        }
    }
}
