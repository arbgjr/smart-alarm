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
    /// In-memory implementation of IAlarmRepository for development and testing.
    /// Thread-safe and suitable for unit/integration tests.
    /// </summary>
    public class InMemoryAlarmRepository : IAlarmRepository
    {
        private readonly ConcurrentDictionary<Guid, Alarm> _alarms = new();

        public Task AddAsync(Alarm alarm, CancellationToken cancellationToken = default)
        {
            _alarms[alarm.Id] = alarm;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _alarms.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<Alarm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _alarms.TryGetValue(id, out var alarm);
            return Task.FromResult(alarm);
        }

        public Task<IEnumerable<Alarm>> GetByUserIdAsync(Guid userId)
        {
            var result = _alarms.Values.Where(a => a.UserId == userId);
            return Task.FromResult(result!);
        }

        public Task<IEnumerable<Alarm>> GetAllEnabledAsync()
        {
            var result = _alarms.Values.Where(a => a.Enabled);
            return Task.FromResult(result);
        }

        public Task<IEnumerable<Alarm>> GetDueForTriggeringAsync(DateTime now)
        {
            var enabledAlarms = _alarms.Values.Where(a => a.Enabled);
            var dueAlarms = enabledAlarms.Where(alarm =>
            {
                try
                {
                    return alarm.ShouldTriggerNow();
                }
                catch
                {
                    // Em caso de erro na verificação, ignorar o alarme
                    return false;
                }
            });

            return Task.FromResult(dueAlarms);
        }

        public Task UpdateAsync(Alarm alarm, CancellationToken cancellationToken = default)
        {
            _alarms[alarm.Id] = alarm;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Alarm>> GetMissedAlarmsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
        {
            var enabledAlarms = _alarms.Values.Where(a => a.Enabled);
            var missedAlarms = enabledAlarms.Where(alarm =>
            {
                try
                {
                    // Simple logic: alarm time is before cutoff and hasn't been triggered recently
                    return alarm.Time < cutoffTime.TimeOfDay;
                }
                catch
                {
                    return false;
                }
            });

            return Task.FromResult(missedAlarms);
        }
    }
}

