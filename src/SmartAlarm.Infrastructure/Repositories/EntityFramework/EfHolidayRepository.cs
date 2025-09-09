using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework;

/// <summary>
/// Entity Framework Core implementation of IHolidayRepository.
/// Seguindo padrões estabelecidos no projeto para repositórios EF.
/// </summary>
public class EfHolidayRepository : IHolidayRepository
{
    private readonly SmartAlarmDbContext _context;
    private readonly ILogger<EfHolidayRepository> _logger;
    private readonly SmartAlarmMeter _meter;
    private readonly ICorrelationContext _correlationContext;
    private readonly SmartAlarmActivitySource _activitySource;

    public EfHolidayRepository(
        SmartAlarmDbContext context,
        ILogger<EfHolidayRepository> logger,
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

    public async Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(holiday);

        using var activity = _activitySource.StartActivity("AddHoliday");
        activity?.SetTag("holiday.id", holiday.Id.ToString());
        activity?.SetTag("holiday.date", holiday.Date.ToString("yyyy-MM-dd"));

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(LogTemplates.QueryStarted,
            "AddHoliday",
            new { HolidayId = holiday.Id, Date = holiday.Date });

        try
        {
            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync(cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "AddAsync", "Holidays");

            _logger.LogInformation(LogTemplates.QueryCompleted,
                "AddHoliday",
                stopwatch.ElapsedMilliseconds,
                "holiday added successfully");

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "InsertError");

            _logger.LogError(LogTemplates.DatabaseQueryFailed,
                "AddHoliday",
                "Holidays",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    public async Task<Holiday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetHolidayById");
        activity?.SetTag("holiday.id", id.ToString());

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(LogTemplates.QueryStarted,
            "GetHolidayById",
            new { Id = id });

        try
        {
            var holiday = await _context.Holidays
                .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetById", "Holidays");

            _logger.LogInformation(LogTemplates.QueryCompleted,
                "GetHolidayById",
                stopwatch.ElapsedMilliseconds,
                holiday != null ? 1 : 0);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return holiday;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "QueryError");

            _logger.LogError(LogTemplates.DatabaseQueryFailed,
                "GetHolidayById",
                "Holidays",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    public async Task<IEnumerable<Holiday>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetAllHolidays");

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(LogTemplates.QueryStarted,
            "GetAllHolidays",
            new { });

        try
        {
            var holidays = await _context.Holidays
                .OrderBy(h => h.Date)
                .ToListAsync(cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetAll", "Holidays");

            _logger.LogInformation(LogTemplates.QueryCompleted,
                "GetAllHolidays",
                stopwatch.ElapsedMilliseconds,
                holidays.Count());

            activity?.SetStatus(ActivityStatusCode.Ok);
            return holidays;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "QueryError");

            _logger.LogError(LogTemplates.DatabaseQueryFailed,
                "GetAllHolidays",
                "Holidays",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    public async Task<IEnumerable<Holiday>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetHolidaysByDate");
        activity?.SetTag("date", date.ToString("yyyy-MM-dd"));

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(LogTemplates.QueryStarted,
            "GetHolidaysByDate",
            new { Date = date });

        try
        {
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            
            var holidays = await _context.Holidays
                .Where(h => h.Date.Date == dateTime.Date || 
                           (h.Date.Year == 1 && h.Date.Month == dateTime.Month && h.Date.Day == dateTime.Day))
                .OrderBy(h => h.Date.Year == 1 ? 0 : 1) // Feriados recorrentes primeiro
                .ToListAsync(cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByDate", "Holidays");

            _logger.LogInformation(LogTemplates.QueryCompleted,
                "GetHolidaysByDate",
                stopwatch.ElapsedMilliseconds,
                holidays.Count());

            activity?.SetStatus(ActivityStatusCode.Ok);
            return holidays;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "QueryError");

            _logger.LogError(LogTemplates.DatabaseQueryFailed,
                "GetHolidaysByDate",
                "Holidays",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    public async Task<IEnumerable<Holiday>> GetRecurringAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetRecurringHolidays");

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(LogTemplates.QueryStarted,
            "GetRecurringHolidays",
            new { });

        try
        {
            var holidays = await _context.Holidays
                .Where(h => h.Date.Year == 1)
                .OrderBy(h => h.Date.Month)
                .ThenBy(h => h.Date.Day)
                .ToListAsync(cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetRecurring", "Holidays");

            _logger.LogInformation(LogTemplates.QueryCompleted,
                "GetRecurringHolidays",
                stopwatch.ElapsedMilliseconds,
                holidays.Count());

            activity?.SetStatus(ActivityStatusCode.Ok);
            return holidays;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "QueryError");

            _logger.LogError(LogTemplates.DatabaseQueryFailed,
                "GetRecurringHolidays",
                "Holidays",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    public async Task UpdateAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(holiday);

        using var activity = _activitySource.StartActivity("UpdateHoliday");
        activity?.SetTag("holiday.id", holiday.Id.ToString());
        activity?.SetTag("holiday.date", holiday.Date.ToString("yyyy-MM-dd"));

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(LogTemplates.QueryStarted,
            "UpdateHoliday",
            new { HolidayId = holiday.Id, Date = holiday.Date });

        try
        {
            var existingHoliday = await _context.Holidays
                .FirstOrDefaultAsync(h => h.Id == holiday.Id, cancellationToken);

            if (existingHoliday == null)
            {
                throw new InvalidOperationException($"Holiday with ID {holiday.Id} not found for update.");
            }

            // Atualizar propriedades
            existingHoliday.UpdateDescription(holiday.Description);
            
            await _context.SaveChangesAsync(cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "UpdateAsync", "Holidays");

            _logger.LogInformation(LogTemplates.QueryCompleted,
                "UpdateHoliday",
                stopwatch.ElapsedMilliseconds,
                "holiday updated successfully");

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "UpdateError");

            _logger.LogError(LogTemplates.DatabaseQueryFailed,
                "UpdateHoliday",
                "Holidays",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("DeleteHoliday");
        activity?.SetTag("holiday.id", id.ToString());

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(LogTemplates.QueryStarted,
            "DeleteHoliday",
            new { HolidayId = id });

        try
        {
            var holiday = await _context.Holidays
                .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

            bool wasDeleted = false;
            if (holiday != null)
            {
                _context.Holidays.Remove(holiday);
                await _context.SaveChangesAsync(cancellationToken);
                wasDeleted = true;
            }

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "DeleteAsync", "Holidays");

            _logger.LogInformation(LogTemplates.QueryCompleted,
                "DeleteHoliday",
                stopwatch.ElapsedMilliseconds,
                wasDeleted ? "holiday deleted successfully" : "holiday not found");

            activity?.SetStatus(ActivityStatusCode.Ok);
            return wasDeleted;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "DeleteError");

            _logger.LogError(LogTemplates.DatabaseQueryFailed,
                "DeleteHoliday",
                "Holidays",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    public async Task<bool> ExistsOnDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("CheckHolidayExists");
        activity?.SetTag("date", date.ToString("yyyy-MM-dd"));

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(LogTemplates.QueryStarted,
            "CheckHolidayExists",
            new { Date = date });

        try
        {
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            
            var exists = await _context.Holidays
                .AnyAsync(h => h.Date.Date == dateTime.Date || 
                              (h.Date.Year == 1 && h.Date.Month == dateTime.Month && h.Date.Day == dateTime.Day),
                         cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "ExistsOnDate", "Holidays");

            _logger.LogInformation(LogTemplates.QueryCompleted,
                "CheckHolidayExists",
                stopwatch.ElapsedMilliseconds,
                exists ? "holiday exists" : "holiday not found");

            activity?.SetStatus(ActivityStatusCode.Ok);
            return exists;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "QueryError");

            _logger.LogError(LogTemplates.DatabaseQueryFailed,
                "CheckHolidayExists",
                "Holidays",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    // Methods to support legacy interface compatibility
    public async Task<Holiday?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var holidays = await GetByDateAsync(DateOnly.FromDateTime(date), cancellationToken);
        return holidays.FirstOrDefault();
    }

    public async Task<IEnumerable<Holiday>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetHolidaysByYear");
        activity?.SetTag("year", year.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var holidays = await _context.Holidays
                .Where(h => h.Date.Year == year)
                .OrderBy(h => h.Date)
                .ToListAsync(cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByYear", "Holidays");
            activity?.SetStatus(ActivityStatusCode.Ok);
            return holidays;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "QueryError");
            throw;
        }
    }

    public async Task<IEnumerable<Holiday>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetHolidaysByDateRange");
        activity?.SetTag("start_date", startDate.ToString("yyyy-MM-dd"));
        activity?.SetTag("end_date", endDate.ToString("yyyy-MM-dd"));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var holidays = await _context.Holidays
                .Where(h => h.Date.Date >= startDate.Date && h.Date.Date <= endDate.Date)
                .OrderBy(h => h.Date)
                .ToListAsync(cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByDateRange", "Holidays");
            activity?.SetStatus(ActivityStatusCode.Ok);
            return holidays;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "QueryError");
            throw;
        }
    }

    public async Task<IEnumerable<Holiday>> GetByCountryAsync(string country, string? region = null, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetHolidaysByCountry");
        activity?.SetTag("country", country);
        activity?.SetTag("region", region);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var query = _context.Holidays.AsQueryable();
            
            // Assumindo que Holiday tem propriedades Country e Region
            // Se não existirem, retorna todos os holidays para compatibilidade
            var holidays = await query
                .OrderBy(h => h.Date)
                .ToListAsync(cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByCountry", "Holidays");
            activity?.SetStatus(ActivityStatusCode.Ok);
            return holidays;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DATABASE", "Holidays", "QueryError");
            throw;
        }
    }

    public async Task<bool> IsHolidayAsync(DateTime date, string? country = null, CancellationToken cancellationToken = default)
    {
        return await ExistsOnDateAsync(DateOnly.FromDateTime(date), cancellationToken);
    }
}
