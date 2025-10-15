using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para persistência e consulta de Integrações.
    /// </summary>
    public interface IIntegrationRepository
    {
        Task<Integration?> GetByIdAsync(Guid id);
        Task<IEnumerable<Integration>> GetByAlarmIdAsync(Guid alarmId);
        Task<IEnumerable<Integration>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Integration>> GetActiveByUserIdAsync(Guid userId);
        Task<Integration?> GetByUserAndTypeAsync(Guid userId, string integrationType);
        Task<IEnumerable<Integration>> GetAllAsync();
        Task AddAsync(Integration integration);
        Task UpdateAsync(Integration integration);
        Task DeleteAsync(Guid id);
    }
}
