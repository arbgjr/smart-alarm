using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Entity Framework Core implementation of IUserRepository with full observability instrumentation.
    /// </summary>
    public class EfUserRepository : IUserRepository
    {
        private readonly SmartAlarmDbContext _context;
        private readonly ILogger<EfUserRepository> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public EfUserRepository(
            SmartAlarmDbContext context,
            ILogger<EfUserRepository> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfUserRepository.GetByIdAsync");
            activity?.SetTag("user.id", id.ToString());
            activity?.SetTag("operation", "user.get.by.id");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "GetUserById", 
                correlationId, 
                id);

            try
            {
                var user = await _context.Users.FindAsync(id);
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "user", "get_by_id");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "GetUserById",
                    stopwatch.ElapsedMilliseconds,
                    user != null ? "found" : "not_found");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return user;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Users", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetUserById",
                    correlationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfUserRepository.GetByEmailAsync");
            activity?.SetTag("user.email", email);
            activity?.SetTag("operation", "user.get.by.email");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "GetUserByEmail", 
                correlationId, 
                email);

            try
            {
                // Filtra em memória para evitar problemas de conversão de Value Object
                var user = await Task.FromResult(
                    _context.Users
                        .AsEnumerable()
                        .FirstOrDefault(u => u.Email.Address == email)
                );
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "user", "get_by_email");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "GetUserByEmail",
                    stopwatch.ElapsedMilliseconds,
                    user != null ? "found" : "not_found");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return user;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Users", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetUserByEmail",
                    correlationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfUserRepository.GetAllAsync");
            activity?.SetTag("operation", "user.get.all");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "GetAllUsers", 
                correlationId, 
                "all");

            try
            {
                var users = await _context.Users.ToListAsync();
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "user", "get_all");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "GetAllUsers",
                    stopwatch.ElapsedMilliseconds,
                    $"returned {users.Count()} users");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return users;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Users", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetAllUsers",
                    correlationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task AddAsync(User user)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfUserRepository.AddAsync");
            activity?.SetTag("user.id", user.Id.ToString());
            activity?.SetTag("user.email", user.Email.Address);
            activity?.SetTag("operation", "user.add");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "AddUser", 
                correlationId, 
                user.Id);

            try
            {
                await _context.Users.AddAsync(user);
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "user", "add");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "AddUser",
                    stopwatch.ElapsedMilliseconds,
                    "user added successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Users", "InsertError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "AddUser",
                    correlationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public Task UpdateAsync(User user)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfUserRepository.UpdateAsync");
            activity?.SetTag("user.id", user.Id.ToString());
            activity?.SetTag("user.email", user.Email.Address);
            activity?.SetTag("operation", "user.update");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "UpdateUser", 
                correlationId, 
                user.Id);

            try
            {
                _context.Users.Update(user);
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "user", "update");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "UpdateUser",
                    stopwatch.ElapsedMilliseconds,
                    "user updated successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Users", "UpdateError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "UpdateUser",
                    correlationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public Task DeleteAsync(Guid id)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfUserRepository.DeleteAsync");
            activity?.SetTag("user.id", id.ToString());
            activity?.SetTag("operation", "user.delete");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "DeleteUser", 
                correlationId, 
                id);

            try
            {
                var user = _context.Users.Find(id);
                var wasDeleted = user != null;
                
                if (user != null)
                {
                    _context.Users.Remove(user);
                }
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "user", "delete");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "DeleteUser",
                    stopwatch.ElapsedMilliseconds,
                    wasDeleted ? "user deleted successfully" : "user not found");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Users", "DeleteError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "DeleteUser",
                    correlationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
