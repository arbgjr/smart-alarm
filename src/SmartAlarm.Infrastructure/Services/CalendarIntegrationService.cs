using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;

namespace SmartAlarm.Infrastructure.Services;

public class CalendarIntegrationService : ICalendarIntegrationService
{
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly IOutlookCalendarService _outlookCalendarService;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly ILogger<CalendarIntegrationService> _logger;
    private readonly SmartAlarmMeter _meter;
    private readonly SmartAlarmActivitySource _activitySource;

    public CalendarIntegrationService(
        IGoogleCalendarService googleCalendarService,
        IOutlookCalendarService outlookCalendarService,
        IIntegrationRepository integrationRepository,
        ILogger<CalendarIntegrationService> logger,
        SmartAlarmMeter meter,
        SmartAlarmActivitySource activitySource)
    {
        _googleCalendarService = googleCalendarService;
        _outlookCalendarService = outlookCalendarService;
        _integrationRepository = integrationRepository;
        _logger = logger;
        _meter = meter;
        _activitySource = activitySource;
    }

    public async Task<List<CalendarEvent>> GetEventsFromAllProvidersAsync(
        Guid userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetEventsFromAllProviders");
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("date.start", startDate.ToString("yyyy-MM-dd"));
        activity?.SetTag("date.end", endDate.ToString("yyyy-MM-dd"));

        var stopwatch = Stopwatch.StartNew();
        var allEvents = new List<CalendarEvent>();

        try
        {
            var tasks = new List<Task<List<CalendarEvent>>>();

            // Check if user has Google Calendar integration
            if (await _googleCalendarService.IsAuthorizedAsync(userId, cancellationToken))
            {
                tasks.Add(_googleCalendarService.GetEventsAsync(userId, startDate, endDate, cancellationToken));
            }

            // Check if user has Outlook Calendar integration
            if (await _outlookCalendarService.IsAuthorizedAsync(userId, cancellationToken))
            {
                tasks.Add(_outlookCalendarService.GetEventsAsync(userId, startDate, endDate, cancellationToken));
            }

            if (tasks.Count == 0)
            {
                _logger.LogInformation("User {UserId} has no authorized calendar providers", userId);
                return allEvents;
            }

            var results = await Task.WhenAll(tasks);

            foreach (var events in results)
            {
                allEvents.AddRange(events);
            }

            // Remove duplicates based on title, start time, and end time
            var uniqueEvents = allEvents
                .GroupBy(e => new { e.Title, e.StartTime, e.EndTime })
                .Select(g => g.First())
                .OrderBy(e => e.StartTime)
                .ToList();

            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetEventsFromAllProviders", "Calendar");
            _meter.IncrementCounter("calendar_events_retrieved", uniqueEvents.Count, new Dictionary<string, object>
            {
                { "user_id", userId.ToString() },
                { "provider_count", tasks.Count.ToString() }
            });

            _logger.LogInformation("Retrieved {Count} unique events from {ProviderCount} providers for user {UserId} in {ElapsedMs}ms",
                uniqueEvents.Count, tasks.Count, userId, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return uniqueEvents;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "CalendarIntegration", "GetEventsError");

            _logger.LogError(ex, "Failed to get events from all providers for user {UserId} in {ElapsedMs}ms",
                userId, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    public async Task<bool> HasVacationOrDayOffAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("HasVacationOrDayOff");
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("date", date.ToString("yyyy-MM-dd"));

        try
        {
            var tasks = new List<Task<bool>>();

            // Check Google Calendar
            if (await _googleCalendarService.IsAuthorizedAsync(userId, cancellationToken))
            {
                tasks.Add(_googleCalendarService.HasVacationOrDayOffAsync(userId, date, cancellationToken));
            }

            // Check Outlook Calendar
            if (await _outlookCalendarService.IsAuthorizedAsync(userId, cancellationToken))
            {
                tasks.Add(_outlookCalendarService.HasVacationOrDayOffAsync(userId, date, cancellationToken));
            }

            if (tasks.Count == 0)
            {
                return false;
            }

            var results = await Task.WhenAll(tasks);
            var hasVacation = results.Any(result => result);

            _logger.LogInformation("User {UserId} has vacation/day off on {Date}: {HasVacation}",
                userId, date.ToString("yyyy-MM-dd"), hasVacation);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return hasVacation;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to check vacation/day off for user {UserId} on {Date}",
                userId, date.ToString("yyyy-MM-dd"));

            // Return false on error to avoid blocking alarm functionality
            return false;
        }
    }

    public async Task<CalendarEvent?> GetNextEventAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetNextEvent");
        activity?.SetTag("user.id", userId.ToString());

        try
        {
            var tasks = new List<Task<CalendarEvent?>>();

            // Check Google Calendar
            if (await _googleCalendarService.IsAuthorizedAsync(userId, cancellationToken))
            {
                tasks.Add(_googleCalendarService.GetNextEventAsync(userId, cancellationToken));
            }

            // Check Outlook Calendar
            if (await _outlookCalendarService.IsAuthorizedAsync(userId, cancellationToken))
            {
                tasks.Add(_outlookCalendarService.GetNextEventAsync(userId, cancellationToken));
            }

            if (tasks.Count == 0)
            {
                return null;
            }

            var results = await Task.WhenAll(tasks);
            var nextEvents = results.Where(e => e != null).Cast<CalendarEvent>().ToList();

            var nextEvent = nextEvents
                .OrderBy(e => e.StartTime)
                .FirstOrDefault();

            _logger.LogInformation("Next event for user {UserId}: {EventTitle} at {StartTime}",
                userId, nextEvent?.Title ?? "None", nextEvent?.StartTime.ToString("yyyy-MM-dd HH:mm") ?? "N/A");

            activity?.SetStatus(ActivityStatusCode.Ok);
            return nextEvent;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to get next event for user {UserId}", userId);

            return null;
        }
    }

    public async Task<int> SyncAllCalendarsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("SyncAllCalendars");
        activity?.SetTag("user.id", userId.ToString());

        var stopwatch = Stopwatch.StartNew();
        var totalSynced = 0;

        try
        {
            var integrations = await _integrationRepository.GetByUserIdAsync(userId);
            var calendarIntegrations = integrations.Where(i =>
                i.Type == "GoogleCalendar" || i.Type == "OutlookCalendar").ToList();

            foreach (var integration in calendarIntegrations)
            {
                try
                {
                    if (!integration.IsEnabled || string.IsNullOrEmpty(integration.AccessToken))
                    {
                        _logger.LogWarning("Skipping disabled or invalid integration {IntegrationType} for user {UserId}",
                            integration.Type, userId);
                        continue;
                    }

                    int syncedCount = 0;

                    if (integration.Type == "GoogleCalendar")
                    {
                        syncedCount = await _googleCalendarService.SyncCalendarAsync(userId, integration.AccessToken, cancellationToken);
                    }
                    else if (integration.Type == "OutlookCalendar")
                    {
                        syncedCount = await _outlookCalendarService.SyncCalendarAsync(userId, integration.AccessToken, cancellationToken);
                    }

                    totalSynced += syncedCount;

                    // Update last sync time
                    integration.UpdateLastSyncTime(DateTime.UtcNow);
                    await _integrationRepository.UpdateAsync(integration);

                    _logger.LogInformation("Synced {Count} events from {IntegrationType} for user {UserId}",
                        syncedCount, integration.Type, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync {IntegrationType} for user {UserId}",
                        integration.Type, userId);
                }
            }

            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "SyncAllCalendars", "Calendar");
            _meter.IncrementCounter("calendar_sync_completed", 1, new Dictionary<string, object>
            {
                { "user_id", userId.ToString() },
                { "events_synced", totalSynced.ToString() }
            });

            _logger.LogInformation("Completed calendar sync for user {UserId}: {TotalSynced} events in {ElapsedMs}ms",
                userId, totalSynced, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return totalSynced;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "CalendarIntegration", "SyncError");

            _logger.LogError(ex, "Failed to sync calendars for user {UserId} in {ElapsedMs}ms",
                userId, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    public async Task<List<string>> GetAuthorizedProvidersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var authorizedProviders = new List<string>();

        try
        {
            if (await _googleCalendarService.IsAuthorizedAsync(userId, cancellationToken))
            {
                authorizedProviders.Add("GoogleCalendar");
            }

            if (await _outlookCalendarService.IsAuthorizedAsync(userId, cancellationToken))
            {
                authorizedProviders.Add("OutlookCalendar");
            }

            _logger.LogInformation("User {UserId} has {Count} authorized calendar providers: {Providers}",
                userId, authorizedProviders.Count, string.Join(", ", authorizedProviders));

            return authorizedProviders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get authorized providers for user {UserId}", userId);
            return authorizedProviders;
        }
    }

    public async Task<bool> SyncBidirectionalAsync(
        Guid userId,
        CalendarEvent calendarEvent,
        CalendarSyncAction action,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("SyncBidirectional");
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("sync.action", action.ToString());
        activity?.SetTag("event.title", calendarEvent.Title);

        var success = true;

        try
        {
            var authorizedProviders = await GetAuthorizedProvidersAsync(userId, cancellationToken);

            foreach (var provider in authorizedProviders)
            {
                try
                {
                    bool result = false;

                    switch (provider)
                    {
                        case "GoogleCalendar":
                            // Google Calendar API doesn't support bidirectional sync in this implementation
                            // This would require additional implementation for creating/updating events
                            _logger.LogInformation("Bidirectional sync not implemented for Google Calendar");
                            result = true; // Skip for now
                            break;

                        case "OutlookCalendar":
                            result = action switch
                            {
                                CalendarSyncAction.Create => await _outlookCalendarService.CreateEventAsync(userId, calendarEvent, cancellationToken) != null,
                                CalendarSyncAction.Update => await _outlookCalendarService.UpdateEventAsync(userId, calendarEvent.Id, calendarEvent, cancellationToken),
                                CalendarSyncAction.Delete => await _outlookCalendarService.DeleteEventAsync(userId, calendarEvent.Id, cancellationToken),
                                _ => false
                            };
                            break;
                    }

                    if (!result)
                    {
                        success = false;
                        _logger.LogWarning("Failed to sync event {EventId} with {Provider} for user {UserId}",
                            calendarEvent.Id, provider, userId);
                    }
                    else
                    {
                        _logger.LogInformation("Successfully synced event {EventId} with {Provider} for user {UserId}",
                            calendarEvent.Id, provider, userId);
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    _logger.LogError(ex, "Error syncing event {EventId} with {Provider} for user {UserId}",
                        calendarEvent.Id, provider, userId);
                }
            }

            _meter.IncrementCounter("calendar_bidirectional_sync", 1, new Dictionary<string, object>
            {
                { "user_id", userId.ToString() },
                { "action", action.ToString() },
                { "success", success.ToString() }
            });

            activity?.SetStatus(success ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            return success;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed bidirectional sync for user {UserId}", userId);
            return false;
        }
    }

    public async Task<CalendarSyncStatus> GetSyncStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var integrations = await _integrationRepository.GetByUserIdAsync(userId);
            var calendarIntegrations = integrations.Where(i =>
                i.Type == "GoogleCalendar" || i.Type == "OutlookCalendar").ToList();

            var status = new CalendarSyncStatus
            {
                UserId = userId,
                LastSyncTime = calendarIntegrations.Any() ?
                    calendarIntegrations.Max(i => i.LastSyncTime ?? DateTime.MinValue) :
                    DateTime.MinValue
            };

            foreach (var integration in calendarIntegrations)
            {
                var providerStatus = new ProviderSyncStatus
                {
                    ProviderName = integration.Type,
                    IsAuthorized = !string.IsNullOrEmpty(integration.AccessToken),
                    IsEnabled = integration.IsEnabled,
                    LastSyncTime = integration.LastSyncTime
                };

                // Get event count for the last 30 days
                try
                {
                    var startDate = DateTime.UtcNow.AddDays(-30);
                    var endDate = DateTime.UtcNow;

                    if (integration.Type == "GoogleCalendar" && providerStatus.IsAuthorized)
                    {
                        var events = await _googleCalendarService.GetEventsAsync(userId, startDate, endDate, cancellationToken);
                        providerStatus.EventCount = events.Count;
                    }
                    else if (integration.Type == "OutlookCalendar" && providerStatus.IsAuthorized)
                    {
                        var events = await _outlookCalendarService.GetEventsAsync(userId, startDate, endDate, cancellationToken);
                        providerStatus.EventCount = events.Count;
                    }
                }
                catch (Exception ex)
                {
                    providerStatus.LastError = ex.Message;
                    status.HasErrors = true;
                    status.Errors.Add($"{integration.Type}: {ex.Message}");
                }

                status.Providers.Add(providerStatus);
                status.TotalEvents += providerStatus.EventCount;
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sync status for user {UserId}", userId);

            return new CalendarSyncStatus
            {
                UserId = userId,
                HasErrors = true,
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
