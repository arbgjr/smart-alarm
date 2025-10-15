using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.Services;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;

namespace SmartAlarm.Infrastructure.Services;

public class AlarmTriggerService : IAlarmTriggerService
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IAlarmEventService _alarmEventService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly INotificationService _notificationService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<AlarmTriggerService> _logger;
    private readonly SmartAlarmMeter _meter;
    private readonly SmartAlarmActivitySource _activitySource;

    public AlarmTriggerService(
        IAlarmRepository alarmRepository,
        IAlarmEventService alarmEventService,
        IBackgroundJobService backgroundJobService,
        INotificationService notificationService,
        IPushNotificationService pushNotificationService,
        ILogger<AlarmTriggerService> logger,
        SmartAlarmMeter meter,
        SmartAlarmActivitySource activitySource)
    {
        _alarmRepository = alarmRepository;
        _alarmEventService = alarmEventService;
        _backgroundJobService = backgroundJobService;
        _notificationService = notificationService;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
        _meter = meter;
        _activitySource = activitySource;
    }

    public async Task ScheduleAlarmAsync(Alarm alarm, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ScheduleAlarm");
        activity?.SetTag("alarm.id", alarm.Id.ToString());
        activity?.SetTag("user.id", alarm.UserId.ToString());

        try
        {
            var nextTriggerTime = CalculateNextTriggerTime(alarm);
            if (nextTriggerTime.HasValue)
            {
                var jobId = _backgroundJobService.ScheduleJob<IAlarmTriggerService>(
                    service => service.TriggerAlarmAsync(alarm.Id, CancellationToken.None),
                    nextTriggerTime.Value);

                // Store job ID in alarm metadata for later cancellation
                alarm.Metadata = alarm.Metadata ?? new Dictionary<string, object>();
                alarm.Metadata["HangfireJobId"] = jobId;

                await _alarmRepository.UpdateAsync(alarm, cancellationToken);

                _logger.LogInformation("Scheduled alarm {AlarmId} to trigger at {TriggerTime} with job {JobId}",
                    alarm.Id, nextTriggerTime.Value, jobId);

                _meter.IncrementCounter("alarms_scheduled", 1, new Dictionary<string, object>
                {
                    { "user_id", alarm.UserId.ToString() }
                });
            }
            else
            {
                _logger.LogWarning("Could not calculate next trigger time for alarm {AlarmId}", alarm.Id);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to schedule alarm {AlarmId}", alarm.Id);
            throw;
        }
    }

    public async Task CancelAlarmAsync(Guid alarmId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("CancelAlarm");
        activity?.SetTag("alarm.id", alarmId.ToString());

        try
        {
            var alarm = await _alarmRepository.GetByIdAsync(alarmId, cancellationToken);
            if (alarm?.Metadata?.ContainsKey("HangfireJobId") == true)
            {
                var jobId = alarm.Metadata["HangfireJobId"].ToString();
                if (!string.IsNullOrEmpty(jobId))
                {
                    _backgroundJobService.DeleteJob(jobId);
                    alarm.Metadata.Remove("HangfireJobId");
                    await _alarmRepository.UpdateAsync(alarm, cancellationToken);

                    _logger.LogInformation("Cancelled alarm {AlarmId} with job {JobId}", alarmId, jobId);
                }
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to cancel alarm {AlarmId}", alarmId);
            throw;
        }
    }

    public async Task RescheduleAlarmAsync(Alarm alarm, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("RescheduleAlarm");
        activity?.SetTag("alarm.id", alarm.Id.ToString());

        try
        {
            // Cancel existing job
            await CancelAlarmAsync(alarm.Id, cancellationToken);

            // Schedule new job
            await ScheduleAlarmAsync(alarm, cancellationToken);

            _logger.LogInformation("Rescheduled alarm {AlarmId}", alarm.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to reschedule alarm {AlarmId}", alarm.Id);
            throw;
        }
    }

    public async Task TriggerAlarmAsync(Guid alarmId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("TriggerAlarm");
        activity?.SetTag("alarm.id", alarmId.ToString());

        try
        {
            var alarm = await _alarmRepository.GetByIdAsync(alarmId, cancellationToken);
            if (alarm == null)
            {
                _logger.LogWarning("Alarm {AlarmId} not found for triggering", alarmId);
                return;
            }

            if (!alarm.IsActive)
            {
                _logger.LogInformation("Alarm {AlarmId} is not active, skipping trigger", alarmId);
                return;
            }

            // Record the alarm triggered event
            await _alarmEventService.RecordAlarmTriggeredAsync(alarmId, alarm.UserId, cancellationToken: cancellationToken);

            // Schedule escalation if no response within 5 minutes
            _backgroundJobService.ScheduleJob<IAlarmTriggerService>(
                service => service.EscalateMissedAlarmAsync(alarmId, 1, CancellationToken.None),
                DateTimeOffset.UtcNow.AddMinutes(5));

            // If this is a recurring alarm, schedule the next occurrence
            if (alarm.IsRecurring)
            {
                await ScheduleAlarmAsync(alarm, cancellationToken);
            }

            _logger.LogInformation("Triggered alarm {AlarmId} for user {UserId}", alarmId, alarm.UserId);

            _meter.IncrementCounter("alarms_triggered", 1, new Dictionary<string, object>
            {
                { "user_id", alarm.UserId.ToString() },
                { "alarm_id", alarmId.ToString() }
            });

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to trigger alarm {AlarmId}", alarmId);
            throw;
        }
    }

    public async Task ProcessMissedAlarmsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ProcessMissedAlarms");

        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-10); // Consider alarms missed after 10 minutes
            var missedAlarms = await _alarmRepository.GetMissedAlarmsAsync(cutoffTime, cancellationToken);

            foreach (var alarm in missedAlarms)
            {
                await EscalateMissedAlarmAsync(alarm.Id, 1, cancellationToken);
            }

            _logger.LogInformation("Processed {Count} missed alarms", missedAlarms.Count());
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to process missed alarms");
            throw;
        }
    }

    public async Task EscalateMissedAlarmAsync(Guid alarmId, int escalationLevel, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EscalateMissedAlarm");
        activity?.SetTag("alarm.id", alarmId.ToString());
        activity?.SetTag("escalation.level", escalationLevel.ToString());

        try
        {
            var alarm = await _alarmRepository.GetByIdAsync(alarmId, cancellationToken);
            if (alarm == null)
            {
                _logger.LogWarning("Alarm {AlarmId} not found for escalation", alarmId);
                return;
            }

            // Check if alarm was already handled (snoozed or dismissed)
            var recentEvents = await _alarmEventService.GetUserEventHistoryAsync(alarm.UserId, 1, cancellationToken);
            var alarmHandled = recentEvents.Any(e =>
                e.AlarmId == alarmId &&
                (e.EventType == AlarmEventType.Snoozed || e.EventType == AlarmEventType.Dismissed) &&
                e.Timestamp > DateTime.UtcNow.AddMinutes(-10));

            if (alarmHandled)
            {
                _logger.LogInformation("Alarm {AlarmId} was already handled, skipping escalation", alarmId);
                return;
            }

            var escalationMessage = escalationLevel switch
            {
                1 => "Your alarm is still active and needs attention!",
                2 => "URGENT: Your alarm has been active for 10 minutes!",
                3 => "CRITICAL: Your alarm has been ignored for 20 minutes!",
                _ => "Your alarm requires immediate attention!"
            };

            // Send escalated notification
            var notification = new Application.DTOs.Notifications.NotificationDto
            {
                Title = $"Missed Alarm - Escalation Level {escalationLevel}",
                Message = escalationMessage,
                Type = Application.DTOs.Notifications.NotificationType.Error,
                Priority = Math.Min(4, escalationLevel + 1), // Increase priority with escalation
                Data = new Dictionary<string, object>
                {
                    { "alarmId", alarmId.ToString() },
                    { "escalationLevel", escalationLevel }
                }
            };

            await _notificationService.SendNotificationAsync(alarm.UserId.ToString(), notification, cancellationToken);

            // Send push notification for higher escalation levels
            if (escalationLevel >= 2)
            {
                var pushNotification = new Application.DTOs.Notifications.PushNotificationDto
                {
                    Title = "Missed Alarm Alert",
                    Body = escalationMessage,
                    Sound = "urgent_alarm",
                    Priority = 3, // High priority
                    Data = new Dictionary<string, string>
                    {
                        { "alarmId", alarmId.ToString() },
                        { "escalationLevel", escalationLevel.ToString() }
                    }
                };

                await _pushNotificationService.SendPushNotificationAsync(alarm.UserId.ToString(), pushNotification, cancellationToken);
            }

            // Schedule next escalation if level is less than 3
            if (escalationLevel < 3)
            {
                _backgroundJobService.ScheduleJob<IAlarmTriggerService>(
                    service => service.EscalateMissedAlarmAsync(alarmId, escalationLevel + 1, CancellationToken.None),
                    DateTimeOffset.UtcNow.AddMinutes(10));
            }

            _logger.LogInformation("Escalated missed alarm {AlarmId} to level {EscalationLevel}", alarmId, escalationLevel);

            _meter.IncrementCounter("alarms_escalated", 1, new Dictionary<string, object>
            {
                { "user_id", alarm.UserId.ToString() },
                { "escalation_level", escalationLevel.ToString() }
            });

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to escalate missed alarm {AlarmId} at level {EscalationLevel}", alarmId, escalationLevel);
            throw;
        }
    }

    private DateTimeOffset? CalculateNextTriggerTime(Alarm alarm)
    {
        if (!alarm.IsActive)
            return null;

        var now = DateTimeOffset.UtcNow;
        var alarmTime = new DateTimeOffset(alarm.Time, TimeSpan.Zero);

        if (alarm.IsRecurring)
        {
            // For recurring alarms, find the next occurrence
            var nextOccurrence = alarmTime;
            while (nextOccurrence <= now)
            {
                nextOccurrence = nextOccurrence.AddDays(1);
            }
            return nextOccurrence;
        }
        else
        {
            // For one-time alarms, only schedule if in the future
            return alarmTime > now ? alarmTime : null;
        }
    }
}
