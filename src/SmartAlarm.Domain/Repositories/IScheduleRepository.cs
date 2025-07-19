using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para persistência e consulta de Schedules.
    /// </summary>
    public interface IScheduleRepository
    {
        Task<Schedule?> GetByIdAsync(Guid id);
        Task<IEnumerable<Schedule>> GetByAlarmIdAsync(Guid alarmId);
        Task<IEnumerable<Schedule>> GetActiveSchedulesAsync();
        Task AddAsync(Schedule schedule);
        Task UpdateAsync(Schedule schedule);
        Task DeleteAsync(Guid id);
    }
}