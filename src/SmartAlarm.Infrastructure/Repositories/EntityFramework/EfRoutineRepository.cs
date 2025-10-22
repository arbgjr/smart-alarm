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
    /// Entity Framework Core implementation of IRoutineRepository.
    /// </summary>
    public class EfRoutineRepository : IRoutineRepository
    {
        private readonly SmartAlarmDbContext _context;
        private readonly ILogger<EfRoutineRepository> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public EfRoutineRepository(
            SmartAlarmDbContext context,
            ILogger<EfRoutineRepository> logger,
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

        public async Task<Routine?> GetByIdAsync(Guid id)
        {
            using var activity = _activitySource.StartActivity("GetRoutineById");
            activity?.SetTag("routine.id", id.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetRoutineById",
                new { Id = id });

            try
            {
                var routine = await _context.Routines.FindAsync(id);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetById", "Routines");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetRoutineById",
                    stopwatch.ElapsedMilliseconds,
                    routine != null ? 1 : 0);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return routine;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetRoutineById",
                    "Routines",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<IEnumerable<Routine>> GetByAlarmIdAsync(Guid alarmId)
        {
            using var activity = _activitySource.StartActivity("GetRoutinesByAlarmId");
            activity?.SetTag("alarm.id", alarmId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetRoutinesByAlarmId",
                new { AlarmId = alarmId });

            try
            {
                var routines = await _context.Routines
                    .Where(r => r.AlarmId == alarmId)
                    .ToListAsync();
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByAlarmId", "Routines");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetRoutinesByAlarmId",
                    stopwatch.ElapsedMilliseconds,
                    routines.Count());

                activity?.SetStatus(ActivityStatusCode.Ok);
                return routines;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetRoutinesByAlarmId",
                    "Routines",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<IEnumerable<Routine>> GetAllAsync()
        {
            using var activity = _activitySource.StartActivity("GetAllRoutines");

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetAllRoutines",
                new { });

            try
            {
                var routines = await _context.Routines.ToListAsync();
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetAll", "Routines");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetAllRoutines",
                    stopwatch.ElapsedMilliseconds,
                    routines.Count());

                activity?.SetStatus(ActivityStatusCode.Ok);
                return routines;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetAllRoutines",
                    "Routines",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task AddAsync(Routine routine)
        {
            using var activity = _activitySource.StartActivity("AddRoutine");
            activity?.SetTag("routine.id", routine.Id.ToString());
            activity?.SetTag("alarm.id", routine.AlarmId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "AddRoutine",
                new { RoutineId = routine.Id, AlarmId = routine.AlarmId });

            try
            {
                await _context.Routines.AddAsync(routine);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "AddAsync", "Routines");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "AddRoutine",
                    stopwatch.ElapsedMilliseconds,
                    "routine added successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "InsertError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "AddRoutine",
                    "Routines",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public Task UpdateAsync(Routine routine)
        {
            using var activity = _activitySource.StartActivity("UpdateRoutine");
            activity?.SetTag("routine.id", routine.Id.ToString());
            activity?.SetTag("alarm.id", routine.AlarmId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "UpdateRoutine",
                new { RoutineId = routine.Id, AlarmId = routine.AlarmId });

            try
            {
                _context.Routines.Update(routine);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "UpdateAsync", "Routines");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "UpdateRoutine",
                    stopwatch.ElapsedMilliseconds,
                    "routine updated successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "UpdateError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "UpdateRoutine",
                    "Routines",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<Routine?> GetByIdAndUserIdAsync(Guid routineId, Guid userId, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetRoutineByIdAndUserId");
            activity?.SetTag("routine.id", routineId.ToString());
            activity?.SetTag("user.id", userId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetRoutineByIdAndUserId",
                new { RoutineId = routineId, UserId = userId });

            try
            {
                var routine = await _context.Routines
                    .Where(r => r.Id == routineId && r.AlarmId == userId) // Note: Assuming UserId correlates with AlarmId, adjust as needed
                    .FirstOrDefaultAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByIdAndUserId", "Routines");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetRoutineByIdAndUserId",
                    stopwatch.ElapsedMilliseconds,
                    routine != null ? 1 : 0);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return routine;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetRoutineByIdAndUserId",
                    "Routines",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<List<Routine>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetRoutinesByUserId");
            activity?.SetTag("user.id", userId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetRoutinesByUserId",
                new { UserId = userId });

            try
            {
                var routines = await _context.Routines
                    .Where(r => r.AlarmId == userId) // Note: Assuming UserId correlates with AlarmId, adjust as needed
                    .ToListAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByUserId", "Routines");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetRoutinesByUserId",
                    stopwatch.ElapsedMilliseconds,
                    routines.Count);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return routines;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetRoutinesByUserId",
                    "Routines",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<List<Routine>> GetByIdsAndUserIdAsync(List<Guid> routineIds, Guid userId, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetRoutinesByIdsAndUserId");
            activity?.SetTag("user.id", userId.ToString());
            activity?.SetTag("routine.count", routineIds.Count.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetRoutinesByIdsAndUserId",
                new { UserId = userId, RoutineCount = routineIds.Count });

            try
            {
                var routines = await _context.Routines
                    .Where(r => routineIds.Contains(r.Id) && r.AlarmId == userId) // Note: Assuming UserId correlates with AlarmId, adjust as needed
                    .ToListAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByIdsAndUserId", "Routines");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetRoutinesByIdsAndUserId",
                    stopwatch.ElapsedMilliseconds,
                    routines.Count);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return routines;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetRoutinesByIdsAndUserId",
                    "Routines",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public void Update(Routine routine)
        {
            using var activity = _activitySource.StartActivity("UpdateRoutineSync");
            activity?.SetTag("routine.id", routine.Id.ToString());

            _logger.LogInformation(LogTemplates.QueryStarted,
                "UpdateRoutineSync",
                new { RoutineId = routine.Id });

            try
            {
                _context.Routines.Update(routine);
                activity?.SetStatus(ActivityStatusCode.Ok);

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "UpdateRoutineSync",
                    0,
                    "routine marked for update");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "UpdateError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "UpdateRoutineSync",
                    "Routines",
                    0,
                    ex.Message);

                throw;
            }
        }

        public IQueryable<Routine> GetByUserIdQueryable(Guid userId)
        {
            using var activity = _activitySource.StartActivity("GetRoutinesQueryableByUserId");
            activity?.SetTag("user.id", userId.ToString());

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetRoutinesQueryableByUserId",
                new { UserId = userId });

            try
            {
                var queryable = _context.Routines
                    .Where(r => r.AlarmId == userId) // Note: Assuming UserId correlates with AlarmId, adjust as needed
                    .AsQueryable();

                activity?.SetStatus(ActivityStatusCode.Ok);

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetRoutinesQueryableByUserId",
                    0,
                    "queryable returned");

                return queryable;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetRoutinesQueryableByUserId",
                    "Routines",
                    0,
                    ex.Message);

                throw;
            }
        }

        public Task DeleteAsync(Guid id)
        {
            using var activity = _activitySource.StartActivity("DeleteRoutine");
            activity?.SetTag("routine.id", id.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "DeleteRoutine",
                new { RoutineId = id });

            try
            {
                var routine = _context.Routines.Find(id);
                bool wasDeleted = false;
                if (routine != null)
                {
                    _context.Routines.Remove(routine);
                    wasDeleted = true;
                }

                stopwatch.Stop();
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "DeleteAsync", "Routines");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "DeleteRoutine",
                    stopwatch.ElapsedMilliseconds,
                    wasDeleted ? "routine deleted successfully" : "routine not found");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Routines", "DeleteError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "DeleteRoutine",
                    "Routines",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
