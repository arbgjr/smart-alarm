using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
    /// Entity Framework Core implementation of IAlarmEventRepository
    /// </summary>
    public class EfAlarmEventRepository : IAlarmEventRepository
    {
        private readonly SmartAlarmDbContext _context;
        private readonly ILogger<EfAlarmEventRepository> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public EfAlarmEventRepository(
            SmartAlarmDbContext context,
            ILogger<EfAlarmEventRepository> logger,
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

        public async Task AddAsync(AlarmEvent alarmEvent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(alarmEvent);

            using var activity = _activitySource.StartActivity("AddAlarmEvent");
            activity?.SetTag("alarm_event.id", alarmEvent.Id.ToString());
            activity?.SetTag("alarm_event.type", alarmEvent.EventType.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "AddAlarmEvent",
                new { AlarmEventId = alarmEvent.Id, Type = alarmEvent.EventType, AlarmId = alarmEvent.AlarmId });

            try
            {
                _context.AlarmEvents.Add(alarmEvent);
                await _context.SaveChangesAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "AddAsync", "AlarmEvents");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "AddAlarmEvent",
                    stopwatch.ElapsedMilliseconds,
                    "alarm event added successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "InsertError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "AddAlarmEvent",
                    "AlarmEvents",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task AddRangeAsync(IEnumerable<AlarmEvent> alarmEvents, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(alarmEvents);

            var eventList = alarmEvents.ToList();
            if (!eventList.Any()) return;

            using var activity = _activitySource.StartActivity("AddAlarmEventRange");
            activity?.SetTag("alarm_events.count", eventList.Count.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "AddAlarmEventRange",
                new { Count = eventList.Count });

            try
            {
                _context.AlarmEvents.AddRange(eventList);
                await _context.SaveChangesAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "AddRangeAsync", "AlarmEvents");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "AddAlarmEventRange",
                    stopwatch.ElapsedMilliseconds,
                    $"{eventList.Count} alarm events added successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "BatchInsertError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "AddAlarmEventRange",
                    "AlarmEvents",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<AlarmEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetAlarmEventById");
            activity?.SetTag("alarm_event.id", id.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetAlarmEventById",
                new { Id = id });

            try
            {
                var alarmEvent = await _context.AlarmEvents
                    .Include(ae => ae.Alarm)
                    .FirstOrDefaultAsync(ae => ae.Id == id, cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByIdAsync", "AlarmEvents");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetAlarmEventById",
                    stopwatch.ElapsedMilliseconds,
                    alarmEvent != null ? 1 : 0);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return alarmEvent;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetAlarmEventById",
                    "AlarmEvents",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<List<AlarmEvent>> GetUserEventsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetUserEvents");
            activity?.SetTag("user.id", userId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetUserEvents",
                new { UserId = userId });

            try
            {
                var events = await _context.AlarmEvents
                    .Where(ae => ae.UserId == userId)
                    .Include(ae => ae.Alarm)
                    .OrderByDescending(ae => ae.Timestamp)
                    .ToListAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetUserEventsAsync", "AlarmEvents");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetUserEvents",
                    stopwatch.ElapsedMilliseconds,
                    events.Count);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return events;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetUserEvents",
                    "AlarmEvents",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<List<AlarmEvent>> GetUserEventsAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetUserEventsByDateRange");
            activity?.SetTag("user.id", userId.ToString());
            activity?.SetTag("date.start", startDate.ToString("yyyy-MM-dd"));
            activity?.SetTag("date.end", endDate.ToString("yyyy-MM-dd"));

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetUserEventsByDateRange",
                new { UserId = userId, StartDate = startDate, EndDate = endDate });

            try
            {
                var events = await _context.AlarmEvents
                    .Where(ae => ae.UserId == userId && 
                                ae.Timestamp >= startDate && 
                                ae.Timestamp <= endDate)
                    .Include(ae => ae.Alarm)
                    .OrderByDescending(ae => ae.Timestamp)
                    .ToListAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetUserEventsByDateRange", "AlarmEvents");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetUserEventsByDateRange",
                    stopwatch.ElapsedMilliseconds,
                    events.Count);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return events;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetUserEventsByDateRange",
                    "AlarmEvents",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<List<AlarmEvent>> GetUserEventsByTypeAsync(
            Guid userId, 
            AlarmEventType eventType, 
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetUserEventsByType");
            activity?.SetTag("user.id", userId.ToString());
            activity?.SetTag("event.type", eventType.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetUserEventsByType",
                new { UserId = userId, EventType = eventType });

            try
            {
                var events = await _context.AlarmEvents
                    .Where(ae => ae.UserId == userId && ae.EventType == eventType)
                    .Include(ae => ae.Alarm)
                    .OrderByDescending(ae => ae.Timestamp)
                    .ToListAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetUserEventsByType", "AlarmEvents");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetUserEventsByType",
                    stopwatch.ElapsedMilliseconds,
                    events.Count);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return events;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetUserEventsByType",
                    "AlarmEvents",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<List<AlarmEvent>> GetUserEventsByTypeAsync(
            Guid userId, 
            AlarmEventType eventType, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetUserEventsByTypeAndDateRange");
            activity?.SetTag("user.id", userId.ToString());
            activity?.SetTag("event.type", eventType.ToString());
            activity?.SetTag("date.start", startDate.ToString("yyyy-MM-dd"));
            activity?.SetTag("date.end", endDate.ToString("yyyy-MM-dd"));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var events = await _context.AlarmEvents
                    .Where(ae => ae.UserId == userId && 
                                ae.EventType == eventType &&
                                ae.Timestamp >= startDate && 
                                ae.Timestamp <= endDate)
                    .Include(ae => ae.Alarm)
                    .OrderByDescending(ae => ae.Timestamp)
                    .ToListAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetUserEventsByTypeAndDateRange", "AlarmEvents");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return events;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");
                throw;
            }
        }

        public async Task<List<AlarmEvent>> GetAlarmEventsAsync(Guid alarmId, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetAlarmEvents");
            activity?.SetTag("alarm.id", alarmId.ToString());

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var events = await _context.AlarmEvents
                    .Where(ae => ae.AlarmId == alarmId)
                    .Include(ae => ae.Alarm)
                    .OrderByDescending(ae => ae.Timestamp)
                    .ToListAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetAlarmEvents", "AlarmEvents");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return events;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");
                throw;
            }
        }

        public async Task<List<AlarmEvent>> GetAlarmEventsAsync(
            Guid alarmId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetAlarmEventsByDateRange");
            activity?.SetTag("alarm.id", alarmId.ToString());
            activity?.SetTag("date.start", startDate.ToString("yyyy-MM-dd"));
            activity?.SetTag("date.end", endDate.ToString("yyyy-MM-dd"));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var events = await _context.AlarmEvents
                    .Where(ae => ae.AlarmId == alarmId && 
                                ae.Timestamp >= startDate && 
                                ae.Timestamp <= endDate)
                    .Include(ae => ae.Alarm)
                    .OrderByDescending(ae => ae.Timestamp)
                    .ToListAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetAlarmEventsByDateRange", "AlarmEvents");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return events;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");
                throw;
            }
        }

        public async Task<Dictionary<AlarmEventType, int>> GetUserEventStatsAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetUserEventStats");
            activity?.SetTag("user.id", userId.ToString());

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var stats = await _context.AlarmEvents
                    .Where(ae => ae.UserId == userId && 
                                ae.Timestamp >= startDate && 
                                ae.Timestamp <= endDate)
                    .GroupBy(ae => ae.EventType)
                    .Select(g => new { EventType = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.EventType, x => x.Count, cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetUserEventStats", "AlarmEvents");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return stats;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");
                throw;
            }
        }

        public async Task<List<AlarmEvent>> GetRecentUserEventsAsync(
            Guid userId, 
            int count = 100, 
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetRecentUserEvents");
            activity?.SetTag("user.id", userId.ToString());
            activity?.SetTag("count", count.ToString());

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var events = await _context.AlarmEvents
                    .Where(ae => ae.UserId == userId)
                    .Include(ae => ae.Alarm)
                    .OrderByDescending(ae => ae.Timestamp)
                    .Take(count)
                    .ToListAsync(cancellationToken);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetRecentUserEvents", "AlarmEvents");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return events;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");
                throw;
            }
        }

        public async Task<Dictionary<DayOfWeek, List<AlarmEvent>>> GetEventPatternsByDayOfWeekAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GetEventPatternsByDayOfWeek");
            activity?.SetTag("user.id", userId.ToString());

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var events = await _context.AlarmEvents
                    .Where(ae => ae.UserId == userId && 
                                ae.Timestamp >= startDate && 
                                ae.Timestamp <= endDate)
                    .Include(ae => ae.Alarm)
                    .OrderBy(ae => ae.DayOfWeek)
                    .ThenBy(ae => ae.Time)
                    .ToListAsync(cancellationToken);

                var patterns = events
                    .GroupBy(ae => ae.DayOfWeek)
                    .ToDictionary(g => g.Key, g => g.ToList());

                stopwatch.Stop();
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetEventPatternsByDayOfWeek", "AlarmEvents");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return patterns;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");
                throw;
            }
        }

        public async Task<int> CountConsecutiveEventsAsync(
            Guid userId, 
            Guid alarmId, 
            AlarmEventType eventType, 
            DayOfWeek dayOfWeek, 
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("CountConsecutiveEvents");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var events = await _context.AlarmEvents
                    .Where(ae => ae.UserId == userId && 
                                ae.AlarmId == alarmId &&
                                ae.EventType == eventType &&
                                ae.DayOfWeek == dayOfWeek)
                    .OrderByDescending(ae => ae.Timestamp)
                    .Select(ae => ae.Timestamp.Date)
                    .ToListAsync(cancellationToken);

                if (!events.Any()) return 0;

                var consecutive = 1;
                var maxConsecutive = 1;

                for (int i = 1; i < events.Count; i++)
                {
                    // Verifica se é o mesmo dia da semana na semana anterior (7 dias)
                    var daysDiff = (events[i - 1] - events[i]).Days;
                    if (daysDiff == 7)
                    {
                        consecutive++;
                    }
                    else
                    {
                        maxConsecutive = Math.Max(maxConsecutive, consecutive);
                        consecutive = 1;
                    }
                }

                stopwatch.Stop();
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "CountConsecutiveEvents", "AlarmEvents");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Math.Max(maxConsecutive, consecutive);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "QueryError");
                throw;
            }
        }

        public async Task DeleteOldEventsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("DeleteOldEvents");
            activity?.SetTag("older_than", olderThan.ToString("yyyy-MM-dd"));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var oldEvents = await _context.AlarmEvents
                    .Where(ae => ae.Timestamp < olderThan)
                    .ToListAsync(cancellationToken);

                if (oldEvents.Any())
                {
                    _context.AlarmEvents.RemoveRange(oldEvents);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                stopwatch.Stop();
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "DeleteOldEvents", "AlarmEvents");

                _logger.LogInformation("Deleted {Count} old alarm events older than {Date}", 
                    oldEvents.Count, olderThan);

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "AlarmEvents", "DeleteError");
                throw;
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}