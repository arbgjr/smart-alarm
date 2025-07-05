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

        // Lazy-loaded repositories
        protected IAlarmRepository? _alarms;
        protected IUserRepository? _users;
        protected IScheduleRepository? _schedules;
        protected IRoutineRepository? _routines;
        protected IIntegrationRepository? _integrations;

        public EfUnitOfWork(SmartAlarmDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public virtual IAlarmRepository Alarms
        {
            get { return _alarms ??= new EfAlarmRepository(_context); }
        }

        public virtual IUserRepository Users
        {
            get { return _users ??= new EfUserRepository(_context); }
        }

        public virtual IScheduleRepository Schedules
        {
            get { return _schedules ??= new EfScheduleRepository(_context); }
        }

        public virtual IRoutineRepository Routines
        {
            get { return _routines ??= new EfRoutineRepository(_context); }
        }

        public virtual IIntegrationRepository Integrations
        {
            get { return _integrations ??= new EfIntegrationRepository(_context); }
        }

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