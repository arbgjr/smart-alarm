using Microsoft.EntityFrameworkCore;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework;

/// <summary>
/// Entity Framework Core implementation of IHolidayRepository.
/// Seguindo padrões estabelecidos no projeto para repositórios EF.
/// </summary>
public class EfHolidayRepository : IHolidayRepository
{
    private readonly SmartAlarmDbContext _context;

    public EfHolidayRepository(SmartAlarmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(holiday);
        
        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Holiday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Holiday>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .OrderBy(h => h.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Holiday>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        
        return await _context.Holidays
            .Where(h => h.Date.Date == dateTime.Date || 
                       (h.Date.Year == 1 && h.Date.Month == dateTime.Month && h.Date.Day == dateTime.Day))
            .OrderBy(h => h.Date.Year == 1 ? 0 : 1) // Feriados recorrentes primeiro
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Holiday>> GetRecurringAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .Where(h => h.Date.Year == 1)
            .OrderBy(h => h.Date.Month)
            .ThenBy(h => h.Date.Day)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(holiday);

        var existingHoliday = await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == holiday.Id, cancellationToken);

        if (existingHoliday == null)
        {
            throw new InvalidOperationException($"Holiday with ID {holiday.Id} not found for update.");
        }

        // Atualizar propriedades
        existingHoliday.UpdateDescription(holiday.Description);
        
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var holiday = await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (holiday == null)
        {
            return false;
        }

        _context.Holidays.Remove(holiday);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExistsOnDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        
        return await _context.Holidays
            .AnyAsync(h => h.Date.Date == dateTime.Date || 
                          (h.Date.Year == 1 && h.Date.Month == dateTime.Month && h.Date.Day == dateTime.Day),
                     cancellationToken);
    }
}
