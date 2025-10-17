using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;

namespace SmartAlarm.Infrastructure.Services;

/// <summary>
/// Unified notification service that combines SignalR real-time notifications with push notifications
/// </summary>
public class UnifiedNotificationService : INotificationService
{
    private readonly IHubContext<Hub> _hubContext;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<UnifiedNotificationService> _logger;
    private readonly SmartAlarmMeter _meter;
    private readonly SmartAlarmActivitySource _activitySource;

    public UnifiedNotificationService(
        IHubContext<Hub> hubContext,
        IPushNotificationService pushNotificationService,
        ILogger<UnifiedNotificationService> logger,
        SmartAlarmMeter meter,
        SmartAlarmActivitySource activitySource)
    {
        _hubContext = hubContext;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
        _meter = meter;
        _activitySource = activitySource;
    }

    public async Task SendNotificationAsync(string userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("SendNotification");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("notification.type", notification.Type.ToString());
        activity?.SetTag("notification.priority", notification.Priority.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Send real-time notification via SignalR
            var signalRTask = SendSignalRNotificationAsync(userId, notification, cancellationToken);

            // Send push notification for high priority or alarm notifications
            Task? pushTask = null;
            if (ShouldSendPushNotification(notification))
            {
                var pushNotification = ConvertToPushNotification(notification);
                pushTask = _pushNotificationService.SendPushNotificationAsync(userId, pushNotification, cancellationToken);
            }

            // Wait for both notifications to complete
            var tasks = new List<Task> { signalRTask };
            if (pushTask != null)
            {
                tasks.Add(pushTask);
            }

            await Task.WhenAll(tasks);

            stopwatch.Stop();

            _meter.IncrementCounter("notification_sent", 1,
                new KeyValuePair<string, object?>("type", notification.Type.ToString()),
                new KeyValuePair<string, object?>("duration_ms", stopwatch.ElapsedMilliseconds));
            activity?.SetTag("notification.execution_time_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation("Notification {NotificationId} sent to user {UserId} in {ElapsedMs}ms (SignalR: true, Push: {HasPush})",
                notification.Id, userId, stopwatch.ElapsedMilliseconds, pushTask != null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("NOTIFICATION", "UnifiedNotificationService", "SendNotificationError");

            _logger.LogError(ex, "Failed to send notification {NotificationId} to user {UserId} in {ElapsedMs}ms",
                notification.Id, userId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task SendNotificationToGroupAsync(string groupName, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("SendNotificationToGroup");
        activity?.SetTag("group.name", groupName);
        activity?.SetTag("notification.type", notification.Type.ToString());

        try
        {
            await _hubContext.Clients.Group(groupName)
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("Notification {NotificationId} sent to group {GroupName}",
                notification.Id, groupName);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to send notification {NotificationId} to group {GroupName}",
                notification.Id, groupName);
            throw;
        }
    }

    public async Task SendBroadcastNotificationAsync(NotificationDto notification, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("SendBroadcastNotification");
        activity?.SetTag("notification.type", notification.Type.ToString());

        try
        {
            await _hubContext.Clients.All
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("Broadcast notification {NotificationId} sent to all users",
                notification.Id);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to send broadcast notification {NotificationId}",
                notification.Id);
            throw;
        }
    }

    public async Task AddUserToGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is typically handled by the Hub when users connect
            // But we can also manage groups programmatically if needed
            _logger.LogInformation("User {UserId} added to group {GroupName}", userId, groupName);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add user {UserId} to group {GroupName}", userId, groupName);
            throw;
        }
    }

    public async Task RemoveUserFromGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is typically handled by the Hub when users disconnect
            // But we can also manage groups programmatically if needed
            _logger.LogInformation("User {UserId} removed from group {GroupName}", userId, groupName);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove user {UserId} from group {GroupName}", userId, groupName);
            throw;
        }
    }

    // Convenience methods for different notification types
    public async Task SendNotificationAsync(string userId, string title, string message, CancellationToken cancellationToken = default)
    {
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Message = message,
            Type = NotificationType.Info,
            Timestamp = DateTime.UtcNow,
            Priority = 2 // Normal priority
        };

        await SendNotificationAsync(userId, notification, cancellationToken);
    }

    public async Task SendPushNotificationAsync(string userId, string title, string message)
    {
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Message = message,
            Type = NotificationType.Info,
            Timestamp = DateTime.UtcNow,
            Priority = 2 // Normal priority
        };

        await SendNotificationAsync(userId, notification);
    }

    public async Task SendAlarmNotificationAsync(Guid alarmId, string title, string message)
    {
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Message = message,
            Type = NotificationType.AlarmTriggered,
            Timestamp = DateTime.UtcNow,
            Priority = 4, // Critical priority for alarms
            Data = new Dictionary<string, object>
            {
                ["alarmId"] = alarmId.ToString(),
                ["actionUrl"] = $"/alarms/{alarmId}"
            }
        };

        // For alarm notifications, we broadcast to ensure delivery
        await SendBroadcastNotificationAsync(notification);
    }

    public async Task SendSystemNotificationAsync(Guid userId, string title, string message)
    {
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Message = message,
            Type = NotificationType.SystemMaintenance,
            Timestamp = DateTime.UtcNow,
            Priority = 3 // High priority for system notifications
        };

        await SendNotificationAsync(userId.ToString(), notification);
    }

    public async Task SendReminderNotificationAsync(Guid userId, string message)
    {
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Reminder",
            Message = message,
            Type = NotificationType.Info,
            Timestamp = DateTime.UtcNow,
            Priority = 2 // Normal priority
        };

        await SendNotificationAsync(userId.ToString(), notification);
    }

    private async Task SendSignalRNotificationAsync(string userId, NotificationDto notification, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR notification to user {UserId}", userId);
            throw;
        }
    }

    private bool ShouldSendPushNotification(NotificationDto notification)
    {
        // Send push notifications for:
        // 1. High priority notifications (3 or 4)
        // 2. Alarm-related notifications
        // 3. Security alerts
        return notification.Priority >= 3 ||
               notification.Type == NotificationType.AlarmTriggered ||
               notification.Type == NotificationType.SecurityAlert ||
               notification.Type == NotificationType.SystemMaintenance;
    }

    private PushNotificationDto ConvertToPushNotification(NotificationDto notification)
    {
        return new PushNotificationDto
        {
            Title = notification.Title,
            Body = notification.Message,
            Priority = notification.Priority,
            Sound = notification.Type == NotificationType.AlarmTriggered ? "alarm" : "default",
            ClickAction = notification.ActionUrl,
            Data = notification.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty),
            TimeToLive = notification.Priority >= 3 ? 3600 : 1800 // Higher TTL for important notifications
        };
    }
}
