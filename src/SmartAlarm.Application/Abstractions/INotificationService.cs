using SmartAlarm.Application.DTOs.Notifications;

namespace SmartAlarm.Application.Abstractions;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, NotificationDto notification, CancellationToken cancellationToken = default);
    Task SendNotificationToGroupAsync(string groupName, NotificationDto notification, CancellationToken cancellationToken = default);
    Task SendBroadcastNotificationAsync(NotificationDto notification, CancellationToken cancellationToken = default);
    Task AddUserToGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default);
    Task RemoveUserFromGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default);

    // Additional methods for different notification types
    Task SendNotificationAsync(string userId, string title, string message, CancellationToken cancellationToken = default);
    Task SendPushNotificationAsync(string userId, string title, string message);
    Task SendAlarmNotificationAsync(Guid alarmId, string title, string message);
    Task SendSystemNotificationAsync(Guid userId, string title, string message);
    Task SendReminderNotificationAsync(Guid userId, string message);
}
