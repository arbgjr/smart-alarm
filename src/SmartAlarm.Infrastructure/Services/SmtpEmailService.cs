using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Diagnostics;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Implementação de produção do IEmailService usando SMTP
    /// Suporta autenticação, TLS/SSL e observabilidade completa
    /// </summary>
    public class SmtpEmailService : IEmailService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ActivitySource _activitySource;
        private readonly SmtpClient _smtpClient;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private bool _disposed = false;

        public SmtpEmailService(
            IConfiguration configuration,
            ILogger<SmtpEmailService> logger,
            SmartAlarmMeter meter,
            ActivitySource activitySource)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));

            // Configurações SMTP obrigatórias
            var smtpHost = _configuration["Email:Smtp:Host"] 
                ?? throw new InvalidOperationException("Email:Smtp:Host não configurado");
            var smtpPort = int.Parse(_configuration["Email:Smtp:Port"] ?? "587");
            var smtpUser = _configuration["Email:Smtp:Username"] 
                ?? throw new InvalidOperationException("Email:Smtp:Username não configurado");
            var smtpPassword = _configuration["Email:Smtp:Password"] 
                ?? throw new InvalidOperationException("Email:Smtp:Password não configurado");
            var enableSsl = bool.Parse(_configuration["Email:Smtp:EnableSsl"] ?? "true");

            _fromEmail = _configuration["Email:From:Address"] 
                ?? throw new InvalidOperationException("Email:From:Address não configurado");
            _fromName = _configuration["Email:From:Name"] ?? "Smart Alarm";

            // Configurar cliente SMTP
            _smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000 // 30 segundos
            };

            _logger.LogInformation("SmtpEmailService initialized with host {Host}:{Port}, SSL: {EnableSsl}", 
                smtpHost, smtpPort, enableSsl);
        }

        public Task SendEmailAsync(string to, string subject, string body)
        {
            return SendEmailAsync(to, subject, body, false);
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml)
        {
            using var activity = _activitySource.StartActivity("Email.Send");
            activity?.SetTag("email.operation", "send");
            activity?.SetTag("email.provider", "smtp");
            activity?.SetTag("email.to", to);
            activity?.SetTag("email.subject", subject);
            activity?.SetTag("email.is_html", isHtml.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "SendEmail",
                new { To = to, Subject = subject, IsHtml = isHtml });

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_fromEmail, _fromName);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = isHtml;
                message.BodyEncoding = Encoding.UTF8;
                message.SubjectEncoding = Encoding.UTF8;

                // Adicionar headers de rastreamento
                message.Headers.Add("X-Smart-Alarm-Service", "true");
                message.Headers.Add("X-Trace-Id", Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString());

                await _smtpClient.SendMailAsync(message);

                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "SMTP", "SendEmail", true);

                _logger.LogInformation(LogTemplates.ExternalServiceCall,
                    "SMTP",
                    "SEND",
                    _smtpClient.Host,
                    "250", // SMTP success code
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("EMAIL", "SMTP", "SendEmailError");

                _logger.LogError(LogTemplates.ExternalServiceCallFailed,
                    "SMTP",
                    _smtpClient.Host,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw new InvalidOperationException($"Erro ao enviar email para {to}", ex);
            }
        }

        public async Task SendAlarmNotificationAsync(string to, string alarmName, string message)
        {
            using var activity = _activitySource.StartActivity("Email.SendAlarmNotification");
            activity?.SetTag("email.operation", "send_alarm_notification");
            activity?.SetTag("email.alarm_name", alarmName);

            var subject = $"🚨 Smart Alarm: {alarmName}";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Smart Alarm Notification</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; border-left: 4px solid #dc3545;'>
            <h2 style='color: #dc3545; margin-top: 0;'>🚨 Alarm Triggered</h2>
            <h3>Alarm: {alarmName}</h3>
            <p><strong>Message:</strong> {message}</p>
            <p><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        </div>
        <div style='margin-top: 20px; padding: 15px; background: #e9ecef; border-radius: 4px;'>
            <p style='margin: 0; font-size: 12px; color: #6c757d;'>
                This notification was sent by Smart Alarm System.<br>
                Trace ID: {Activity.Current?.TraceId.ToString() ?? "N/A"}
            </p>
        </div>
    </div>
</body>
</html>";

            _logger.LogInformation("Sending alarm notification email to {To} for alarm '{AlarmName}'",
                to, alarmName);

            await SendEmailAsync(to, subject, htmlBody, true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _smtpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
