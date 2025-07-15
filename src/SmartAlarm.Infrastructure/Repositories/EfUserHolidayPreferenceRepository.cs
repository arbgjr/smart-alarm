using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;

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

        public EfUserHolidayPreferenceRepository(SmartAlarmDbContext context, ILogger<EfUserHolidayPreferenceRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Adding user holiday preference for User {UserId} and Holiday {HolidayId}", 
                    preference.UserId, preference.HolidayId);
                
                await _context.UserHolidayPreferences.AddAsync(preference, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("User holiday preference added successfully with Id {PreferenceId}", preference.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user holiday preference for User {UserId} and Holiday {HolidayId}", 
                    preference.UserId, preference.HolidayId);
                throw;
            }
        }

        public async Task<UserHolidayPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting user holiday preference by Id: {PreferenceId}", id);
                
                return await _context.UserHolidayPreferences
                    .AsNoTracking()
                    .Include(uhp => uhp.User)
                    .Include(uhp => uhp.Holiday)
                    .FirstOrDefaultAsync(uhp => uhp.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user holiday preference by Id: {PreferenceId}", id);
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
            try
            {
                _logger.LogDebug("Updating user holiday preference {PreferenceId}", preference.Id);
                
                _context.UserHolidayPreferences.Update(preference);
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("User holiday preference {PreferenceId} updated successfully", preference.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user holiday preference {PreferenceId}", preference.Id);
                throw;
            }
        }

        public async Task DeleteAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Deleting user holiday preference {PreferenceId}", preference.Id);
                
                _context.UserHolidayPreferences.Remove(preference);
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("User holiday preference {PreferenceId} deleted successfully", preference.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user holiday preference {PreferenceId}", preference.Id);
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
