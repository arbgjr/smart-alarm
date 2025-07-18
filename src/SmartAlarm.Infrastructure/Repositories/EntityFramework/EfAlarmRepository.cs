using System;
using System.Collections.Generic;
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
    /// Entity Framework Core implementation of IAlarmRepository.
    /// </summary>
    public class EfAlarmRepository : IAlarmRepository
    {
        private readonly SmartAlarmDbContext _context;
        private readonly ILogger<EfAlarmRepository> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public EfAlarmRepository(
            SmartAlarmDbContext context,
            ILogger<EfAlarmRepository> logger,
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

        public async Task<Alarm?> GetByIdAsync(Guid id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfAlarmRepository.GetByIdAsync");
            activity?.SetTag("alarm.id", id.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "GetByIdAsync");
            activity?.SetTag("table", "Alarms");
            
            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "GetByIdAsync", 
                    "Alarms", 
                    new { AlarmId = id });

                var result = await _context.Alarms
                    .Include(a => a.Schedules)
                    .Include(a => a.Routines)
                    .Include(a => a.Integrations)
                    .FirstOrDefaultAsync(a => a.Id == id);

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByIdAsync", "Alarms");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "GetByIdAsync",
                    stopwatch.ElapsedMilliseconds,
                    result != null ? 1 : 0);

                return result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Alarms", "QueryError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetByIdAsync",
                    "Alarms", 
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }

        public async Task<IEnumerable<Alarm>> GetByUserIdAsync(Guid userId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfAlarmRepository.GetByUserIdAsync");
            activity?.SetTag("user.id", userId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "GetByUserIdAsync");
            activity?.SetTag("table", "Alarms");
            
            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "GetByUserIdAsync", 
                    "Alarms", 
                    new { UserId = userId });

                var result = await _context.Alarms
                    .Include(a => a.Schedules)
                    .Include(a => a.Routines)
                    .Include(a => a.Integrations)
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByUserIdAsync", "Alarms");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                activity?.SetTag("result.count", result.Count());
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "GetByUserIdAsync",
                    stopwatch.ElapsedMilliseconds,
                    result.Count());

                return result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Alarms", "QueryError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetByUserIdAsync",
                    "Alarms", 
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }

        public async Task AddAsync(Alarm alarm)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfAlarmRepository.AddAsync");
            activity?.SetTag("alarm.id", alarm.Id.ToString());
            activity?.SetTag("user.id", alarm.UserId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "AddAsync");
            activity?.SetTag("table", "Alarms");
            
            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "AddAsync", 
                    "Alarms", 
                    new { AlarmId = alarm.Id, UserId = alarm.UserId });

                await _context.Alarms.AddAsync(alarm);

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "AddAsync", "Alarms");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "AddAsync",
                    stopwatch.ElapsedMilliseconds,
                    1);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Alarms", "InsertError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "AddAsync",
                    "Alarms", 
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }

        public Task UpdateAsync(Alarm alarm)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfAlarmRepository.UpdateAsync");
            activity?.SetTag("alarm.id", alarm.Id.ToString());
            activity?.SetTag("user.id", alarm.UserId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "UpdateAsync");
            activity?.SetTag("table", "Alarms");
            
            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "UpdateAsync", 
                    "Alarms", 
                    new { AlarmId = alarm.Id, UserId = alarm.UserId });

                _context.Alarms.Update(alarm);

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "UpdateAsync", "Alarms");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "UpdateAsync",
                    stopwatch.ElapsedMilliseconds,
                    1);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Alarms", "UpdateError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "UpdateAsync",
                    "Alarms", 
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }

        public async Task<IEnumerable<Alarm>> GetAllEnabledAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfAlarmRepository.GetAllEnabledAsync");
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "GetAllEnabledAsync");
            activity?.SetTag("table", "Alarms");
            
            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "GetAllEnabledAsync", 
                    "Alarms", 
                    new { });

                var result = await _context.Alarms
                    .Include(a => a.Schedules)
                    .Include(a => a.Routines)
                    .Include(a => a.Integrations)
                    .Where(a => a.Enabled)
                    .ToListAsync();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetAllEnabledAsync", "Alarms");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                activity?.SetTag("result.count", result.Count);
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "GetAllEnabledAsync",
                    stopwatch.ElapsedMilliseconds,
                    result.Count);

                return result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Alarms", "QueryError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetAllEnabledAsync",
                    "Alarms", 
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }

        public async Task<IEnumerable<Alarm>> GetDueForTriggeringAsync(DateTime now)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfAlarmRepository.GetDueForTriggeringAsync");
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "GetDueForTriggeringAsync");
            activity?.SetTag("table", "Alarms");
            activity?.SetTag("query.time", now.ToString("HH:mm"));
            
            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "GetDueForTriggeringAsync", 
                    "Alarms", 
                    new { Time = now });

                // Query otimizada para buscar alarmes que devem ser disparados agora
                var result = await _context.Alarms
                    .Include(a => a.Schedules)
                    .Include(a => a.Routines)
                    .Include(a => a.Integrations)
                    .Where(a => a.Enabled && 
                               a.Schedules.Any(s => s.IsActive &&
                                                   s.Time.Hour == now.Hour &&
                                                   s.Time.Minute == now.Minute))
                    .ToListAsync();

                // Filtro adicional em memória para validar regras de negócio complexas
                var dueAlarms = result.Where(alarm => 
                {
                    try
                    {
                        return alarm.ShouldTriggerNow();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error checking if alarm {AlarmId} should trigger: {Error}", alarm.Id, ex.Message);
                        return false;
                    }
                }).ToList();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetDueForTriggeringAsync", "Alarms");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                activity?.SetTag("result.count", dueAlarms.Count);
                activity?.SetTag("candidates.count", result.Count);
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "GetDueForTriggeringAsync",
                    stopwatch.ElapsedMilliseconds,
                    dueAlarms.Count);

                return dueAlarms;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Alarms", "QueryError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetDueForTriggeringAsync",
                    "Alarms", 
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }

        public Task DeleteAsync(Guid id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfAlarmRepository.DeleteAsync");
            activity?.SetTag("alarm.id", id.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "DeleteAsync");
            activity?.SetTag("table", "Alarms");
            
            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "DeleteAsync", 
                    "Alarms", 
                    new { AlarmId = id });

                var alarm = _context.Alarms.Find(id);
                if (alarm != null)
                {
                    _context.Alarms.Remove(alarm);
                }

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "DeleteAsync", "Alarms");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                activity?.SetTag("entity.found", alarm != null);
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "DeleteAsync",
                    stopwatch.ElapsedMilliseconds,
                    alarm != null ? 1 : 0);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Alarms", "DeleteError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "DeleteAsync",
                    "Alarms", 
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
