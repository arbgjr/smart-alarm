using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Infrastructure implementation of INotificationService.
    /// Provides basic notification functionality for the Infrastructure layer.
    /// </summary>
    public class InfrastructureNotificationService : INotificationService
    {
        private readonly ILogger<InfrastructureNotificationService> _logger;

        public InfrastructureNotificationService(ILogger<InfrastructureNotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SendNotificationAsync(string userId, string title, string message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending notification to user {UserId} with title '{Title}' and message '{Message}'",
                userId, title, message);
            return Task.CompletedTask;
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
