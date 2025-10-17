using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;
using SmartAlarm.Application.Services;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;

namespace SmartAlarm.Infrastructure.Services;

/// <summary>
/// Service responsible for handling alarm escalation policies and retry logic
/// </summary>
public class AlarmEscalationService
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IAlarmEventService _alarmEventService;
    private readonly INotificationService _notificationService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlarmEscalationService> _logger;
    private readonly SmartAlarmMeter _meter;
    private readonly SmartAlarmActivitySource _activitySource;

    // Escalation configuration
    private readonly TimeSpan _initialEscalationDelay;
    private readonly TimeSpan _subsequentEscalationDelay;
    private readonly int _maxEscalationLevel;
    private readonly int _maxRetryAttempts;

    public AlarmEscalationService(
        IAlarmRepository alarmRepository,
        IAlarmEventService alarmEventService,
        INotificationService notificationService,
        IPushNotificationService pushNotificationService,
        IBackgroundJobService backgroundJobService,
        IConfiguration configuration,
        ILogger<AlarmEscalationService> logger,
        SmartAlarmMeter meter,
        SmartAlarmActivitySource activitySource)
    {
        _alarmRepository = alarmRepository;
        _alarmEventService = alarmEventService;
        _notificationService = notificationService;
        _pushNotificationService = pushNotificationService;
        _backgroundJobService = backgroundJobService;
        _configuration = configuration;
        _logger = logger;
        _meter = meter;
        _activitySource = activitySource;

        // Load configuration with defaults
        _initialEscalationDelay = TimeSpan.FromMinutes(configuration.GetValue("AlarmEscalation:InitialDelayMinutes", 5));
        _subsequentEscalationDelay = TimeSpan.FromMinutes(configuration.GetValue("AlarmEscalation:SubsequentDelayMinutes", 10));
        _maxEscalationLevel = configuration.GetValue("AlarmEscalation:MaxLevel", 3);
        _maxRetryAttempts = configuration.GetValue("AlarmEscalation:MaxRetryAttempts", 3);
    }

    public async Task HandleAlarmTriggerAsync(Guid alarmId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("AlarmEscalationService.HandleAlarmTrigger");
        activity?.SetTag("alarm.id", alarmId.ToString());

        try
        {
            var alarm = await _alarmRepository.GetByIdAsync(alarmId, cancellationToken);
            if (alarm == null)
            {
                _logger.LogWarning("Alarm {AlarmId} not found for trigger handling", alarmId);
                return;
            }

            if (!alarm.IsActive)
            {
                _logger.LogInformation("Alarm {AlarmId} is not active, skipping trigger", alarmId);
                return;
            }

            // Send initial notification with retry logic
            await SendNotificationWithRetryAsync(alarm, 0, cancellationToken);

            // Schedule first escalation
            ScheduleEscalation(alarmId, 1, _initialEscalationDelay);

            _logger.LogInformation("Handled alarm trigger for {AlarmId}, scheduled first escalation", alarmId);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to handle alarm trigger for {AlarmId}", alarmId);
            throw;
        }
    }

    public async Task HandleEscalationAsync(Guid alarmId, int escalationLevel, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("AlarmEscalationService.HandleEscalation");
        activity?.SetTag("alarm.id", alarmId.ToString());
        activity?.SetTag("escalation.level", escalationLevel.ToString());

        try
        {
            var alarm = await _alarmRepository.GetByIdAsync(alarmId, cancellationToken);
            if (alarm == null)
            {
                _logger.LogWarning("Alarm {AlarmId} not found for escalation level {Level}", alarmId, escalationLevel);
                return;
            }

            // Check if alarm was already handled (snoozed or dismissed)
            if (await IsAlarmHandledAsync(alarmId, alarm.UserId, cancellationToken))
            {
                _logger.LogInformation("Alarm {AlarmId} was already handled, stopping escalation", alarmId);
                return;
            }

            // Send escalated notification
            await SendEscalatedNotificationAsync(alarm, escalationLevel, cancellationToken);

            // Schedule next escalation if not at max level
            if (escalationLevel < _maxEscalationLevel)
            {
                ScheduleEscalation(alarmId, escalationLevel + 1, _subsequentEscalationDelay);
            }
            else
            {
                _logger.LogWarning("Alarm {AlarmId} reached maximum escalation level {MaxLevel}", alarmId, _maxEscalationLevel);
                await HandleMaxEscalationReachedAsync(alarm, cancellationToken);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to handle escalation for alarm {AlarmId} at level {Level}", alarmId, escalationLevel);
            throw;
        }
    }

    private async Task SendNotificationWithRetryAsync(Alarm alarm, int retryAttempt, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("AlarmEscalationService.SendNotificationWithRetry");
        activity?.SetTag("alarm.id", alarm.Id.ToString());
        activity?.SetTag("retry.attempt", retryAttempt.ToString());

        try
        {
            var notification = CreateAlarmNotification(alarm, 0);

            // Try SignalR notification first
            await _notificationService.SendNotificationAsync(alarm.UserId.ToString(), notification.Title, notification.Message, cancellationToken);

            // Send push notification for immediate delivery
            var pushNotification = CreatePushNotification(alarm, 0);
            await _pushNotificationService.SendPushNotificationAsync(alarm.UserId.ToString(), pushNotification, cancellationToken);

            // Record successful trigger
            await _alarmEventService.RecordAlarmTriggeredAsync(alarm.Id, alarm.UserId, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully sent notifications for alarm {AlarmId} on attempt {Attempt}",
                alarm.Id, retryAttempt + 1);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to send notification for alarm {AlarmId} on attempt {Attempt}",
                alarm.Id, retryAttempt + 1);

            // Retry if we haven't exceeded max attempts
            if (retryAttempt < _maxRetryAttempts - 1)
            {
                var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * 30); // Exponential backoff
                _backgroundJobService.ScheduleJob<AlarmEscalationService>(
                    service => service.RetryNotificationAsync(alarm.Id, retryAttempt + 1, CancellationToken.None),
                    DateTimeOffset.UtcNow.Add(retryDelay));

                _logger.LogInformation("Scheduled retry {NextAttempt} for alarm {AlarmId} in {Delay}",
                    retryAttempt + 2, alarm.Id, retryDelay);
            }
            else
            {
                _logger.LogError("Exhausted all retry attempts for alarm {AlarmId}", alarm.Id);
                throw;
            }
        }
    }

    public async Task RetryNotificationAsync(Guid alarmId, int retryAttempt, CancellationToken cancellationToken = default)
    {
        var alarm = await _alarmRepository.GetByIdAsync(alarmId, cancellationToken);
        if (alarm != null && !await IsAlarmHandledAsync(alarmId, alarm.UserId, cancellationToken))
        {
            await SendNotificationWithRetryAsync(alarm, retryAttempt, cancellationToken);
        }
    }

    private async Task SendEscalatedNotificationAsync(Alarm alarm, int escalationLevel, CancellationToken cancellationToken)
    {
        var notification = CreateAlarmNotification(alarm, escalationLevel);
        var pushNotification = CreatePushNotification(alarm, escalationLevel);

        // Send both SignalR and push notifications for escalated alarms
        var signalRTask = _notificationService.SendNotificationAsync(alarm.UserId.ToString(), notification.Title, notification.Message, cancellationToken);
        var pushTask = _pushNotificationService.SendPushNotificationAsync(alarm.UserId.ToString(), pushNotification, cancellationToken);

        await Task.WhenAll(signalRTask, pushTask);

        _logger.LogInformation("Sent escalated notifications for alarm {AlarmId} at level {Level}",
            alarm.Id, escalationLevel);
    }

    private async Task<bool> IsAlarmHandledAsync(Guid alarmId, Guid userId, CancellationToken cancellationToken)
    {
        var recentEvents = await _alarmEventService.GetUserEventHistoryAsync(userId, 10, cancellationToken);
        return recentEvents.Any(e =>
            e.AlarmId == alarmId &&
            (e.EventType == AlarmEventType.Snoozed || e.EventType == AlarmEventType.Dismissed) &&
            e.Timestamp > DateTime.UtcNow.AddMinutes(-30)); // Check last 30 minutes
    }

    private async Task HandleMaxEscalationReachedAsync(Alarm alarm, CancellationToken cancellationToken)
    {
        // Send critical alert
        var criticalNotification = new NotificationDto
        {
            Title = "CRITICAL: Alarm Requires Immediate Attention",
            Message = $"Alarm '{alarm.Name}' has reached maximum escalation level and requires immediate attention.",
            Type = NotificationType.Error,
            Priority = 4, // Critical
            Data = new Dictionary<string, object>
            {
                { "alarmId", alarm.Id.ToString() },
                { "escalationLevel", _maxEscalationLevel },
                { "isCritical", true }
            }
        };

        await _notificationService.SendNotificationAsync(alarm.UserId.ToString(), criticalNotification.Title, criticalNotification.Message, cancellationToken);

        // Log critical event
        _logger.LogCritical("Alarm {AlarmId} for user {UserId} reached maximum escalation level {MaxLevel}",
            alarm.Id, alarm.UserId, _maxEscalationLevel);

        // Record critical event
        await _alarmEventService.RecordAlarmEscalatedAsync(alarm.Id, alarm.UserId, _maxEscalationLevel, cancellationToken: cancellationToken);
    }

    private void ScheduleEscalation(Guid alarmId, int escalationLevel, TimeSpan delay)
    {
        _backgroundJobService.ScheduleJob<AlarmEscalationService>(
            service => service.HandleEscalationAsync(alarmId, escalationLevel, CancellationToken.None),
            DateTimeOffset.UtcNow.Add(delay));

        _logger.LogDebug("Scheduled escalation level {Level} for alarm {AlarmId} in {Delay}",
            escalationLevel, alarmId, delay);
    }

    private static NotificationDto CreateAlarmNotification(Alarm alarm, int escalationLevel)
    {
        var title = escalationLevel switch
        {
            0 => $"Alarm: {alarm.Name}",
            1 => $"Reminder: {alarm.Name}",
            2 => $"URGENT: {alarm.Name}",
            _ => $"CRITICAL: {alarm.Name}"
        };

        var message = escalationLevel switch
        {
            0 => "Your alarm is now active.",
            1 => "Your alarm is still active and needs attention.",
            2 => "URGENT: Your alarm has been active for several minutes!",
            _ => "CRITICAL: Your alarm requires immediate attention!"
        };

        return new NotificationDto
        {
            Title = title,
            Message = message,
            Type = escalationLevel >= 2 ? NotificationType.Error : NotificationType.AlarmTriggered,
            Priority = Math.Min(4, escalationLevel + 2),
            Data = new Dictionary<string, object>
            {
                { "alarmId", alarm.Id.ToString() },
                { "escalationLevel", escalationLevel },
                { "alarmName", alarm.Name.Value }
            }
        };
    }

    private static PushNotificationDto CreatePushNotification(Alarm alarm, int escalationLevel)
    {
        var title = escalationLevel switch
        {
            0 => "Alarm",
            1 => "Alarm Reminder",
            2 => "URGENT Alarm",
            _ => "CRITICAL Alarm"
        };

        var body = escalationLevel switch
        {
            0 => $"{alarm.Name} is now active",
            1 => $"{alarm.Name} needs attention",
            2 => $"URGENT: {alarm.Name} requires immediate attention",
            _ => $"CRITICAL: {alarm.Name} - immediate action required"
        };

        return new PushNotificationDto
        {
            Title = title,
            Body = body,
            Sound = escalationLevel >= 2 ? "urgent_alarm" : "default_alarm",
            Priority = Math.Min(3, escalationLevel + 1),
            Data = new Dictionary<string, string>
            {
                { "alarmId", alarm.Id.ToString() },
                { "escalationLevel", escalationLevel.ToString() },
                { "action", "view_alarm" }
            },
            TimeToLive = escalationLevel >= 2 ? 300 : 600 // Shorter TTL for urgent alarms
        };
    }
}
