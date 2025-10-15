using SmartAlarm.Application.DTOs.Notifications;

namespace SmartAlarm.Application.Abstractions;

public interface IPushNotificationService
{
    Task SendPushNotificationAsync(string userId, PushNotificationDto notification, CancellationToken cancellationToken = default);
    Task RegisterDeviceAsync(string userId, string deviceToken, string platform, CancellationToken cancellationToken = default);
    Task UnregisterDeviceAsync(string userId, string deviceToken, CancellationToken cancellationToken = default);
    Task SendBulkPushNotificationAsync(IEnumerable<string> userIds, PushNotificationDto notification, CancellationToken cancellationToken = default);
}
