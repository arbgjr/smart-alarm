using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Development and testing implementation of IEmailService.
    /// Logs email operations instead of sending actual emails.
    /// </summary>
    public class LoggingEmailService : IEmailService
    {
        private readonly ILogger<LoggingEmailService> _logger;

        public LoggingEmailService(ILogger<LoggingEmailService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SendEmailAsync(string to, string subject, string body)
        {
            return SendEmailAsync(to, subject, body, false);
        }

        public Task SendEmailAsync(string to, string subject, string body, bool isHtml)
        {
            _logger.LogInformation("Sending email to {To} with subject '{Subject}'. IsHtml: {IsHtml}. Body: {Body}",
                to, subject, isHtml, body);
            return Task.CompletedTask;
        }

        public Task SendAlarmNotificationAsync(string to, string alarmName, string message)
        {
            var subject = $"Smart Alarm: {alarmName}";
            var body = $"Your alarm '{alarmName}' was triggered.\n\nMessage: {message}";
            
            _logger.LogInformation("Sending alarm notification email to {To} for alarm '{AlarmName}' with message '{Message}'",
                to, alarmName, message);
            
            return SendEmailAsync(to, subject, body);
        }
    }
}