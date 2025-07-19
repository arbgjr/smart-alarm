using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Repositories.EntityFramework;

namespace SmartAlarm.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core implementation of Unit of Work pattern.
    /// Provides transaction coordination and ensures consistency across repositories.
    /// </summary>

    public class EfUnitOfWork : IUnitOfWork
    {
        protected readonly SmartAlarmDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        // Repositories from DI
        private readonly IAlarmRepository _alarms;
        private readonly IUserRepository _users;
        private readonly IScheduleRepository _schedules;
        private readonly IRoutineRepository _routines;
        private readonly IIntegrationRepository _integrations;

        public EfUnitOfWork(
            SmartAlarmDbContext context,
            IAlarmRepository alarms,
            IUserRepository users,
            IScheduleRepository schedules,
            IRoutineRepository routines,
            IIntegrationRepository integrations)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _alarms = alarms ?? throw new ArgumentNullException(nameof(alarms));
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _schedules = schedules ?? throw new ArgumentNullException(nameof(schedules));
            _routines = routines ?? throw new ArgumentNullException(nameof(routines));
            _integrations = integrations ?? throw new ArgumentNullException(nameof(integrations));
        }

        public virtual IAlarmRepository Alarms => _alarms;

        public virtual IUserRepository Users => _users;

        public virtual IScheduleRepository Schedules => _schedules;

        public virtual IRoutineRepository Routines => _routines;

        public virtual IIntegrationRepository Integrations => _integrations;

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}