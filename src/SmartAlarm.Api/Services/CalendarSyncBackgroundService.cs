using SmartAlarm.Application.Abstractions;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;

namespace SmartAlarm.Api.Services;

/// <summary>
/// Background service for periodic calendar synchronization
/// </summary>
public class CalendarSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CalendarSyncBackgroundService> _logger;
    private readonly SmartAlarmActivitySource _activitySource;
    private readonly IConfiguration _configuration;

    // Default to sync every 30 minutes
    private readonly TimeSpan _interval;
    private readonly bool _isEnabled;

    public CalendarSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CalendarSyncBackgroundService> logger,
        SmartAlarmActivitySource activitySource,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _activitySource = activitySource;
        _configuration = configuration;

        // Configure sync interval (default: 30 minutes)
        var intervalMinutes = _configuration.GetValue("CalendarSync:IntervalMinutes", 30);
        _interval = TimeSpan.FromMinutes(intervalMinutes);

        // Allow disabling calendar sync
        _isEnabled = _configuration.GetValue("CalendarSync:Enabled", true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Calendar Sync Background Service is disabled");
            return;
        }

        _logger.LogInformation("Calendar Sync Background Service started with {IntervalMinutes} minutes interval",
            _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = _activitySource.StartActivity("background.calendar_sync");
            activity?.SetTag("background.service", "CalendarSyncBackgroundService");
            activity?.SetTag("background.interval_minutes", _interval.TotalMinutes);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var calendarService = scope.ServiceProvider.GetRequiredService<ICalendarIntegrationService>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

                var stopwatch = Stopwatch.StartNew();

                // Get all users with calendar integrations
                var users = await GetUsersWithCalendarIntegrationsAsync(scope.ServiceProvider);
                var totalSynced = 0;
                var successCount = 0;
                var errorCount = 0;

                foreach (var userId in users)
                {
                    try
                    {
                        var syncedEvents = await calendarService.SyncAllCalendarsAsync(userId, stoppingToken);
                        totalSynced += syncedEvents;
                        successCount++;

                        _logger.LogDebug("Synced {EventCount} events for user {UserId}", syncedEvents, userId);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, "Failed to sync calendars for user {UserId}", userId);
                    }
                }

                stopwatch.Stop();

                activity?.SetTag("background.users_processed", users.Count);
                activity?.SetTag("background.total_events_synced", totalSynced);
                activity?.SetTag("background.success_count", successCount);
                activity?.SetTag("background.error_count", errorCount);
                activity?.SetTag("background.execution_time_ms", stopwatch.ElapsedMilliseconds);

                SmartAlarmActivitySource.SetSuccess(activity,
                    $"Calendar sync completed: {totalSynced} events from {successCount} users");

                _logger.LogInformation(
                    "Calendar sync completed in {ElapsedMs}ms: {TotalEvents} events synced from {SuccessCount}/{TotalUsers} users ({ErrorCount} errors)",
                    stopwatch.ElapsedMilliseconds, totalSynced, successCount, users.Count, errorCount);
            }
            catch (Exception ex)
            {
                SmartAlarmActivitySource.SetError(activity, ex);
                _logger.LogError(ex, "Error occurred during calendar synchronization");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Calendar Sync Background Service stopped");
    }

    private async Task<List<Guid>> GetUsersWithCalendarIntegrationsAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var integrationRepository = serviceProvider.GetRequiredService<IIntegrationRepository>();
            var userRepository = serviceProvider.GetRequiredService<IUserRepository>();

            // Get all integrations and find unique user IDs
            var allIntegrations = await integrationRepository.GetAllAsync();

            var calendarIntegrations = allIntegrations.Where(i =>
                i.IsEnabled &&
                !string.IsNullOrEmpty(i.AccessToken) &&
                (i.Type == Domain.Enums.IntegrationType.GoogleCalendar ||
                 i.Type == Domain.Enums.IntegrationType.OutlookCalendar));

            // Get user IDs through alarms (since integrations are linked to alarms)
            var userIds = new HashSet<Guid>();

            foreach (var integration in calendarIntegrations)
            {
                try
                {
                    // Get the alarm to find the user ID
                    if (integration.AlarmId.HasValue)
                    {
                        var alarmRepository = serviceProvider.GetRequiredService<IAlarmRepository>();
                        var alarm = await alarmRepository.GetByIdAsync(integration.AlarmId.Value);

                        if (alarm != null)
                        {
                            userIds.Add(alarm.UserId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get user ID for integration {IntegrationId}", integration.Id);
                }
            }

            return userIds.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users with calendar integrations");
            return new List<Guid>();
        }
    }
}
