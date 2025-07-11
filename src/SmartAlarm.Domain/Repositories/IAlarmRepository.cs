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
        Task<Alarm?> GetByIdAsync(Guid id);
        Task<IEnumerable<Alarm>> GetByUserIdAsync(Guid userId);
        Task AddAsync(Alarm alarm);
        Task UpdateAsync(Alarm alarm);
        Task DeleteAsync(Guid id);
    }
}
