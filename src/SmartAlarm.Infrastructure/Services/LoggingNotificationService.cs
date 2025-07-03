using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Development and testing implementation of INotificationService.
    /// Logs notification operations instead of sending actual notifications.
    /// </summary>
    public class LoggingNotificationService : INotificationService
    {
        private readonly ILogger<LoggingNotificationService> _logger;

        public LoggingNotificationService(ILogger<LoggingNotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SendPushNotificationAsync(string deviceToken, string title, string message)
        {
            _logger.LogInformation("Sending push notification to device {DeviceToken} with title '{Title}' and message '{Message}'",
                deviceToken, title, message);
            return Task.CompletedTask;
        }

        public Task SendAlarmNotificationAsync(Guid userId, string alarmName, string message)
        {
            _logger.LogInformation("Sending alarm notification to user {UserId} for alarm '{AlarmName}' with message '{Message}'",
                userId, alarmName, message);
            return Task.CompletedTask;
        }

        public Task SendSystemNotificationAsync(Guid userId, string title, string message)
        {
            _logger.LogInformation("Sending system notification to user {UserId} with title '{Title}' and message '{Message}'",
                userId, title, message);
            return Task.CompletedTask;
        }

        public Task SendReminderNotificationAsync(Guid userId, string reminderText)
        {
            _logger.LogInformation("Sending reminder notification to user {UserId} with text '{ReminderText}'",
                userId, reminderText);
            return Task.CompletedTask;
        }
    }
}