using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Interface for notification service operations.
    /// Provides abstraction for sending various types of notifications.
    /// </summary>
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string title, string message, CancellationToken cancellationToken = default);
        Task SendPushNotificationAsync(string deviceToken, string title, string message);
        Task SendAlarmNotificationAsync(Guid userId, string alarmName, string message);
        Task SendSystemNotificationAsync(Guid userId, string title, string message);
        Task SendReminderNotificationAsync(Guid userId, string reminderText);
    }
}
