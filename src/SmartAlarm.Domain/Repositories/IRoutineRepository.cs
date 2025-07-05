using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para persistência e consulta de Rotinas.
    /// </summary>
    public interface IRoutineRepository
    {
        Task<Routine> GetByIdAsync(Guid id);
        Task<IEnumerable<Routine>> GetByAlarmIdAsync(Guid alarmId);
        Task<IEnumerable<Routine>> GetAllAsync();
        Task AddAsync(Routine routine);
        Task UpdateAsync(Routine routine);
        Task DeleteAsync(Guid id);
    }
}
