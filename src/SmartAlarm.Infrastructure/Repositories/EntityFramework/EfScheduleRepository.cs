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
    /// Entity Framework Core implementation of IScheduleRepository with full observability instrumentation.
    /// </summary>
    public class EfScheduleRepository : IScheduleRepository
    {
        private readonly SmartAlarmDbContext _context;
        private readonly ILogger<EfScheduleRepository> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public EfScheduleRepository(
            SmartAlarmDbContext context,
            ILogger<EfScheduleRepository> logger,
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

        public async Task<Schedule?> GetByIdAsync(Guid id)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfScheduleRepository.GetByIdAsync");
            activity?.SetTag("schedule.id", id.ToString());
            activity?.SetTag("operation", "schedule.get.by.id");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "GetScheduleById", 
                correlationId, 
                id);

            try
            {
                var schedule = await _context.Schedules.FindAsync(id);
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "schedule", "get_by_id");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "GetScheduleById",
                    stopwatch.ElapsedMilliseconds,
                    schedule != null ? "found" : "not_found");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return schedule;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Schedules", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetScheduleById",
                    "Schedules",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<IEnumerable<Schedule>> GetByAlarmIdAsync(Guid alarmId)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfScheduleRepository.GetByAlarmIdAsync");
            activity?.SetTag("alarm.id", alarmId.ToString());
            activity?.SetTag("operation", "schedule.get.by.alarm.id");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "GetSchedulesByAlarmId", 
                correlationId, 
                alarmId);

            try
            {
                var schedules = await _context.Schedules
                    .Where(s => s.AlarmId == alarmId)
                    .ToListAsync();
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "schedule", "get_by_alarm_id");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "GetSchedulesByAlarmId",
                    stopwatch.ElapsedMilliseconds,
                    $"returned {schedules.Count()} schedules");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return schedules;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Schedules", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetSchedulesByAlarmId",
                    "Schedules",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<IEnumerable<Schedule>> GetActiveSchedulesAsync()
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfScheduleRepository.GetActiveSchedulesAsync");
            activity?.SetTag("operation", "schedule.get.active");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "GetActiveSchedules", 
                correlationId, 
                "active_only");

            try
            {
                var schedules = await _context.Schedules
                    .Where(s => s.IsActive)
                    .ToListAsync();
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "schedule", "get_active");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "GetActiveSchedules",
                    stopwatch.ElapsedMilliseconds,
                    $"returned {schedules.Count()} active schedules");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return schedules;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Schedules", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetActiveSchedules",
                    "Schedules",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task AddAsync(Schedule schedule)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfScheduleRepository.AddAsync");
            activity?.SetTag("schedule.id", schedule.Id.ToString());
            activity?.SetTag("alarm.id", schedule.AlarmId.ToString());
            activity?.SetTag("operation", "schedule.add");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "AddSchedule", 
                correlationId, 
                schedule.Id);

            try
            {
                await _context.Schedules.AddAsync(schedule);
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "schedule", "add");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "AddSchedule",
                    stopwatch.ElapsedMilliseconds,
                    "schedule added successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Schedules", "InsertError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "AddSchedule",
                    "Schedules",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public Task UpdateAsync(Schedule schedule)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfScheduleRepository.UpdateAsync");
            activity?.SetTag("schedule.id", schedule.Id.ToString());
            activity?.SetTag("alarm.id", schedule.AlarmId.ToString());
            activity?.SetTag("operation", "schedule.update");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "UpdateSchedule", 
                correlationId, 
                schedule.Id);

            try
            {
                _context.Schedules.Update(schedule);
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "schedule", "update");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "UpdateSchedule",
                    stopwatch.ElapsedMilliseconds,
                    "schedule updated successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Schedules", "UpdateError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "UpdateSchedule",
                    "Schedules",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public Task DeleteAsync(Guid id)
        {
            var correlationId = _correlationContext.CorrelationId;
            var stopwatch = Stopwatch.StartNew();
            
            using var activity = _activitySource.StartActivity("EfScheduleRepository.DeleteAsync");
            activity?.SetTag("schedule.id", id.ToString());
            activity?.SetTag("operation", "schedule.delete");

            _logger.LogInformation(LogTemplates.DatabaseQueryStarted, 
                "DeleteSchedule", 
                correlationId, 
                id);

            try
            {
                var schedule = _context.Schedules.Find(id);
                var wasDeleted = schedule != null;
                
                if (schedule != null)
                {
                    _context.Schedules.Remove(schedule);
                }
                
                stopwatch.Stop();
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "schedule", "delete");

                _logger.LogInformation(LogTemplates.DatabaseQueryExecuted,
                    "DeleteSchedule",
                    stopwatch.ElapsedMilliseconds,
                    wasDeleted ? "schedule deleted successfully" : "schedule not found");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Schedules", "DeleteError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "DeleteSchedule",
                    "Schedules",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
