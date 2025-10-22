using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para persistência e consulta de Rotinas.
    /// </summary>
    public interface IRoutineRepository
    {
        Task<Routine?> GetByIdAsync(Guid id);
        Task<Routine?> GetByIdAndUserIdAsync(Guid routineId, Guid userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Routine>> GetByAlarmIdAsync(Guid alarmId);
        Task<List<Routine>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<Routine>> GetByIdsAndUserIdAsync(List<Guid> routineIds, Guid userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Routine>> GetAllAsync();
        Task AddAsync(Routine routine);
        Task UpdateAsync(Routine routine);
        void Update(Routine routine);
        Task DeleteAsync(Guid id);
        IQueryable<Routine> GetByUserIdQueryable(Guid userId);
    }
}
