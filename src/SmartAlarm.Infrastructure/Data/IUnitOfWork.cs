using System;
using System.Threading.Tasks;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Infrastructure.Data
{
    /// <summary>
    /// Unit of Work interface for coordinating repository operations.
    /// Ensures transaction consistency across multiple repositories.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IAlarmRepository Alarms { get; }
        IUserRepository Users { get; }
        IScheduleRepository Schedules { get; }
        IRoutineRepository Routines { get; }
        IIntegrationRepository Integrations { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}