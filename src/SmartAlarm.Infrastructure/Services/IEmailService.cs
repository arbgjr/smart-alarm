using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Interface for email service operations.
    /// Provides abstraction for sending emails in the Smart Alarm system.
    /// </summary>
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendEmailAsync(string to, string subject, string body, bool isHtml);
        Task SendAlarmNotificationAsync(string to, string alarmName, string message);
    }
}