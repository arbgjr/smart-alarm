using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para persistência e consulta de Alarmes.
    /// </summary>
    public interface IAlarmRepository
    {
        Task<Alarm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Alarm>> GetAllEnabledAsync();
        Task<IEnumerable<Alarm>> GetDueForTriggeringAsync(DateTime now);
        Task<IEnumerable<Alarm>> GetMissedAlarmsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
        Task AddAsync(Alarm alarm, CancellationToken cancellationToken = default);
        Task UpdateAsync(Alarm alarm, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id);
    }
}
