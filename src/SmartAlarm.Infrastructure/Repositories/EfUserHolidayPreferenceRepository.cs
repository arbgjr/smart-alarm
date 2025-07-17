using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework implementation of IUserHolidayPreferenceRepository.
    /// Provides optimized database access for user holiday preferences using EF Core with joins.
    /// </summary>
    public class EfUserHolidayPreferenceRepository : IUserHolidayPreferenceRepository
    {
        private readonly SmartAlarmDbContext _context;
        private readonly ILogger<EfUserHolidayPreferenceRepository> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public EfUserHolidayPreferenceRepository(
            SmartAlarmDbContext context, 
            ILogger<EfUserHolidayPreferenceRepository> logger,
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

        public async Task AddAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfUserHolidayPreferenceRepository.AddAsync");
            activity?.SetTag("userholidaypreference.user_id", preference.UserId.ToString());
            activity?.SetTag("userholidaypreference.holiday_id", preference.HolidayId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "AddAsync");
            activity?.SetTag("table", "UserHolidayPreferences");

            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "AddAsync", 
                    "UserHolidayPreferences", 
                    new { UserId = preference.UserId, HolidayId = preference.HolidayId });
                
                await _context.UserHolidayPreferences.AddAsync(preference, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "AddAsync", "UserHolidayPreferences");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "AddAsync",
                    stopwatch.ElapsedMilliseconds,
                    1);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "UserHolidayPreferences", "QueryError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "AddAsync",
                    "UserHolidayPreferences",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }

        public async Task<UserHolidayPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfUserHolidayPreferenceRepository.GetByIdAsync");
            activity?.SetTag("userholidaypreference.id", id.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "GetByIdAsync");
            activity?.SetTag("table", "UserHolidayPreferences");

            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "GetByIdAsync", 
                    "UserHolidayPreferences", 
                    new { PreferenceId = id });
                
                var result = await _context.UserHolidayPreferences
                    .AsNoTracking()
                    .Include(uhp => uhp.User)
                    .Include(uhp => uhp.Holiday)
                    .FirstOrDefaultAsync(uhp => uhp.Id == id, cancellationToken);
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByIdAsync", "UserHolidayPreferences");
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
                _meter.IncrementErrorCount("DATABASE", "UserHolidayPreferences", "QueryError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetByIdAsync",
                    "UserHolidayPreferences",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }

        public async Task<UserHolidayPreference?> GetByUserAndHolidayAsync(Guid userId, Guid holidayId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting user holiday preference for User {UserId} and Holiday {HolidayId}", 
                    userId, holidayId);
                
                return await _context.UserHolidayPreferences
                    .AsNoTracking()
                    .Include(uhp => uhp.User)
                    .Include(uhp => uhp.Holiday)
                    .FirstOrDefaultAsync(uhp => uhp.UserId == userId && uhp.HolidayId == holidayId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user holiday preference for User {UserId} and Holiday {HolidayId}", 
                    userId, holidayId);
                throw;
            }
        }

        public async Task<IEnumerable<UserHolidayPreference>> GetByUserIdAsync(Guid userId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting all holiday preferences for User {UserId}", userId);
                
                return await _context.UserHolidayPreferences
                    .AsNoTracking()
                    .Include(uhp => uhp.Holiday)
                    .Where(uhp => uhp.UserId == userId)
                    .OrderBy(uhp => uhp.Holiday.Date)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting holiday preferences for User {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserHolidayPreference>> GetActiveByUserIdAsync(Guid userId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting active holiday preferences for User {UserId}", userId);
                
                return await _context.UserHolidayPreferences
                    .AsNoTracking()
                    .Include(uhp => uhp.Holiday)
                    .Where(uhp => uhp.UserId == userId && uhp.IsEnabled)
                    .OrderBy(uhp => uhp.Holiday.Date)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active holiday preferences for User {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserHolidayPreference>> GetApplicableForDateAsync(Guid userId, DateTime date, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting applicable holiday preferences for User {UserId} on date {Date}", 
                    userId, date.Date);
                
                var targetDate = date.Date;
                
                return await _context.UserHolidayPreferences
                    .AsNoTracking()
                    .Include(uhp => uhp.Holiday)
                    .Where(uhp => uhp.UserId == userId 
                                  && uhp.IsEnabled 
                                  && (uhp.Holiday.Date.Date == targetDate 
                                      || (uhp.Holiday.Date.Year == 1 && uhp.Holiday.Date.Month == targetDate.Month && uhp.Holiday.Date.Day == targetDate.Day)))
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applicable holiday preferences for User {UserId} on date {Date}", 
                    userId, date.Date);
                throw;
            }
        }

        public async Task<IEnumerable<UserHolidayPreference>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting all user holiday preferences");
                
                return await _context.UserHolidayPreferences
                    .AsNoTracking()
                    .Include(uhp => uhp.User)
                    .Include(uhp => uhp.Holiday)
                    .OrderBy(uhp => uhp.UserId)
                    .ThenBy(uhp => uhp.Holiday.Date)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all user holiday preferences");
                throw;
            }
        }

        public async Task UpdateAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfUserHolidayPreferenceRepository.UpdateAsync");
            activity?.SetTag("userholidaypreference.id", preference.Id.ToString());
            activity?.SetTag("userholidaypreference.user_id", preference.UserId.ToString());
            activity?.SetTag("userholidaypreference.holiday_id", preference.HolidayId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "UpdateAsync");
            activity?.SetTag("table", "UserHolidayPreferences");

            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "UpdateAsync", 
                    "UserHolidayPreferences", 
                    new { PreferenceId = preference.Id });
                
                _context.UserHolidayPreferences.Update(preference);
                await _context.SaveChangesAsync(cancellationToken);
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "UpdateAsync", "UserHolidayPreferences");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "UpdateAsync",
                    stopwatch.ElapsedMilliseconds,
                    1);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "UserHolidayPreferences", "QueryError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "UpdateAsync",
                    "UserHolidayPreferences",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }

        public async Task DeleteAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("EfUserHolidayPreferenceRepository.DeleteAsync");
            activity?.SetTag("userholidaypreference.id", preference.Id.ToString());
            activity?.SetTag("userholidaypreference.user_id", preference.UserId.ToString());
            activity?.SetTag("userholidaypreference.holiday_id", preference.HolidayId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "DeleteAsync");
            activity?.SetTag("table", "UserHolidayPreferences");

            try
            {
                _logger.LogDebug(LogTemplates.DatabaseQueryStarted,
                    "DeleteAsync", 
                    "UserHolidayPreferences", 
                    new { PreferenceId = preference.Id });
                
                _context.UserHolidayPreferences.Remove(preference);
                await _context.SaveChangesAsync(cancellationToken);
                
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "DeleteAsync", "UserHolidayPreferences");
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                
                _logger.LogDebug(LogTemplates.DatabaseQueryExecuted,
                    "DeleteAsync",
                    stopwatch.ElapsedMilliseconds,
                    1);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "UserHolidayPreferences", "QueryError");
                
                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "DeleteAsync",
                    "UserHolidayPreferences",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid holidayId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Checking if user holiday preference exists for User {UserId} and Holiday {HolidayId}", 
                    userId, holidayId);
                
                return await _context.UserHolidayPreferences
                    .AsNoTracking()
                    .AnyAsync(uhp => uhp.UserId == userId && uhp.HolidayId == holidayId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of user holiday preference for User {UserId} and Holiday {HolidayId}", 
                    userId, holidayId);
                throw;
            }
        }

        public async Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Counting active holiday preferences for User {UserId}", userId);
                
                return await _context.UserHolidayPreferences
                    .AsNoTracking()
                    .CountAsync(uhp => uhp.UserId == userId && uhp.IsEnabled, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting active holiday preferences for User {UserId}", userId);
                throw;
            }
        }
    }
}
