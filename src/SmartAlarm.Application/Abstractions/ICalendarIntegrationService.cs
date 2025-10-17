using SmartAlarm.Application.Services.External;
using SmartAlarm.Application.DTOs.Calendar;

namespace SmartAlarm.Application.Abstractions;

public interface ICalendarIntegrationService
{
    Task<List<CalendarEvent>> GetEventsFromAllProvidersAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<bool> HasVacationOrDayOffAsync(Guid userId, DateTime date, CancellationToken cancellationToken = default);
    Task<CalendarEvent?> GetNextEventAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> SyncAllCalendarsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetAuthorizedProvidersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> SyncBidirectionalAsync(Guid userId, CalendarEvent calendarEvent, CalendarSyncAction action, CancellationToken cancellationToken = default);
    Task<CalendarSyncStatus> GetSyncStatusAsync(Guid userId, CancellationToken cancellationToken = default);
}
