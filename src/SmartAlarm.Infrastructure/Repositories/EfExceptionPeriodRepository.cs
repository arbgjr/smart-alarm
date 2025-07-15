using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;

namespace SmartAlarm.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework implementation of IExceptionPeriodRepository.
    /// Provides optimized database access for exception periods using EF Core.
    /// </summary>
    public class EfExceptionPeriodRepository : IExceptionPeriodRepository
    {
        private readonly SmartAlarmDbContext _context;
        private readonly ILogger<EfExceptionPeriodRepository> _logger;

        public EfExceptionPeriodRepository(SmartAlarmDbContext context, ILogger<EfExceptionPeriodRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExceptionPeriod?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("Getting exception period by Id: {ExceptionPeriodId}", id);
                return await _context.ExceptionPeriods
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ep => ep.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exception period by Id: {ExceptionPeriodId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ExceptionPeriod>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                _logger.LogDebug("Getting exception periods for user: {UserId}", userId);
                return await _context.ExceptionPeriods
                    .AsNoTracking()
                    .Where(ep => ep.UserId == userId)
                    .OrderBy(ep => ep.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exception periods for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ExceptionPeriod>> GetActivePeriodsOnDateAsync(Guid userId, DateTime date)
        {
            try
            {
                var dateOnly = date.Date;
                _logger.LogDebug("Getting active exception periods for user {UserId} on date {Date}", userId, dateOnly);
                
                return await _context.ExceptionPeriods
                    .AsNoTracking()
                    .Where(ep => ep.UserId == userId 
                        && ep.IsActive 
                        && ep.StartDate <= dateOnly 
                        && ep.EndDate >= dateOnly)
                    .OrderBy(ep => ep.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active exception periods for user {UserId} on date {Date}", userId, date);
                throw;
            }
        }

        public async Task<IEnumerable<ExceptionPeriod>> GetOverlappingPeriodsAsync(Guid userId, DateTime startDate, DateTime endDate, Guid? excludeId = null)
        {
            try
            {
                var startDateOnly = startDate.Date;
                var endDateOnly = endDate.Date;
                
                _logger.LogDebug("Getting overlapping exception periods for user {UserId} between {StartDate} and {EndDate}, excluding {ExcludeId}", 
                    userId, startDateOnly, endDateOnly, excludeId);

                var query = _context.ExceptionPeriods
                    .AsNoTracking()
                    .Where(ep => ep.UserId == userId 
                        && ep.StartDate <= endDateOnly 
                        && ep.EndDate >= startDateOnly);

                if (excludeId.HasValue)
                {
                    query = query.Where(ep => ep.Id != excludeId.Value);
                }

                return await query
                    .OrderBy(ep => ep.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overlapping exception periods for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ExceptionPeriod>> GetByTypeAsync(Guid userId, ExceptionPeriodType type)
        {
            try
            {
                _logger.LogDebug("Getting exception periods of type {Type} for user: {UserId}", type, userId);
                
                return await _context.ExceptionPeriods
                    .AsNoTracking()
                    .Where(ep => ep.UserId == userId && ep.Type == type)
                    .OrderBy(ep => ep.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exception periods of type {Type} for user: {UserId}", type, userId);
                throw;
            }
        }

        public async Task<int> CountByUserIdAsync(Guid userId)
        {
            try
            {
                _logger.LogDebug("Counting exception periods for user: {UserId}", userId);
                
                return await _context.ExceptionPeriods
                    .AsNoTracking()
                    .CountAsync(ep => ep.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting exception periods for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> HasActivePeriodOnDateAsync(Guid userId, DateTime date)
        {
            try
            {
                var dateOnly = date.Date;
                _logger.LogDebug("Checking if user {UserId} has active exception period on date {Date}", userId, dateOnly);
                
                return await _context.ExceptionPeriods
                    .AsNoTracking()
                    .AnyAsync(ep => ep.UserId == userId 
                        && ep.IsActive 
                        && ep.StartDate <= dateOnly 
                        && ep.EndDate >= dateOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active exception period for user {UserId} on date {Date}", userId, date);
                throw;
            }
        }

        public async Task AddAsync(ExceptionPeriod exceptionPeriod)
        {
            try
            {
                if (exceptionPeriod == null)
                    throw new ArgumentNullException(nameof(exceptionPeriod));

                _logger.LogDebug("Adding exception period: {ExceptionPeriodId} for user: {UserId}", 
                    exceptionPeriod.Id, exceptionPeriod.UserId);
                
                await _context.ExceptionPeriods.AddAsync(exceptionPeriod);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Exception period {ExceptionPeriodId} added successfully", exceptionPeriod.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding exception period: {ExceptionPeriodId}", exceptionPeriod?.Id);
                throw;
            }
        }

        public async Task UpdateAsync(ExceptionPeriod exceptionPeriod)
        {
            try
            {
                if (exceptionPeriod == null)
                    throw new ArgumentNullException(nameof(exceptionPeriod));

                _logger.LogDebug("Updating exception period: {ExceptionPeriodId}", exceptionPeriod.Id);
                
                _context.ExceptionPeriods.Update(exceptionPeriod);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Exception period {ExceptionPeriodId} updated successfully", exceptionPeriod.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exception period: {ExceptionPeriodId}", exceptionPeriod?.Id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("Deleting exception period: {ExceptionPeriodId}", id);
                
                var exceptionPeriod = await _context.ExceptionPeriods
                    .FirstOrDefaultAsync(ep => ep.Id == id);
                
                if (exceptionPeriod != null)
                {
                    _context.ExceptionPeriods.Remove(exceptionPeriod);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Exception period {ExceptionPeriodId} deleted successfully", id);
                }
                else
                {
                    _logger.LogWarning("Exception period {ExceptionPeriodId} not found for deletion", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exception period: {ExceptionPeriodId}", id);
                throw;
            }
        }
    }
}
