using SmartAlarm.Application.Services.External;

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

public enum CalendarSyncAction
{
    Create,
    Update,
    Delete
}

public class CalendarSyncStatus
{
    public Guid UserId { get; set; }
    public DateTime LastSyncTime { get; set; }
    public List<ProviderSyncStatus> Providers { get; set; } = new();
    public int TotalEvents { get; set; }
    public bool HasErrors { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ProviderSyncStatus
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public int EventCount { get; set; }
    public string? LastError { get; set; }
}
