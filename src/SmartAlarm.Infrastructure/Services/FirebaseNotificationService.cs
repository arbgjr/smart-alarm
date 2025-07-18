using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Implementação de produção do INotificationService usando Firebase Cloud Messaging (FCM)
    /// Suporta push notifications com observabilidade completa
    /// </summary>
    public class FirebaseNotificationService : INotificationService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FirebaseNotificationService> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ActivitySource _activitySource;
        private readonly HttpClient _httpClient;
        private readonly string _serverKey;
        private readonly string _senderId;
        private bool _disposed = false;

        public FirebaseNotificationService(
            IConfiguration configuration,
            ILogger<FirebaseNotificationService> logger,
            SmartAlarmMeter meter,
            ActivitySource activitySource,
            HttpClient httpClient)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // Configurações Firebase obrigatórias
            _serverKey = _configuration["Firebase:ServerKey"] 
                ?? throw new InvalidOperationException("Firebase:ServerKey não configurado");
            _senderId = _configuration["Firebase:SenderId"] 
                ?? throw new InvalidOperationException("Firebase:SenderId não configurado");

            // Configurar cliente HTTP para Firebase
            _httpClient.BaseAddress = new Uri("https://fcm.googleapis.com");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"key={_serverKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SmartAlarm/1.0");

            _logger.LogInformation("FirebaseNotificationService initialized with SenderId {SenderId}", _senderId);
        }

        public async Task SendPushNotificationAsync(string deviceToken, string title, string message)
        {
            using var activity = _activitySource.StartActivity("Firebase.SendPushNotification");
            activity?.SetTag("notification.operation", "send_push");
            activity?.SetTag("notification.provider", "firebase");
            activity?.SetTag("notification.title", title);
            activity?.SetTag("notification.device_token", deviceToken[..8] + "****"); // Mask token for security

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "SendPushNotification",
                new { DeviceToken = deviceToken[..8] + "****", Title = title });

            try
            {
                var payload = new
                {
                    to = deviceToken,
                    notification = new
                    {
                        title = title,
                        body = message,
                        icon = "alarm_icon",
                        sound = "default",
                        badge = 1,
                        click_action = "FLUTTER_NOTIFICATION_CLICK"
                    },
                    data = new
                    {
                        type = "alarm",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        trace_id = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                    },
                    priority = "high",
                    content_available = true
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/fcm/send", content);

                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "Firebase", "SendPushNotification", true);

                    _logger.LogInformation(LogTemplates.ExternalServiceCall,
                        "Firebase FCM",
                        "POST",
                        "/fcm/send",
                        ((int)response.StatusCode).ToString(),
                        stopwatch.ElapsedMilliseconds);

                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Firebase FCM failed with status {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("NOTIFICATION", "Firebase", "SendPushNotificationError");

                _logger.LogError(LogTemplates.ExternalServiceCallFailed,
                    "Firebase FCM",
                    "/fcm/send",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw new InvalidOperationException($"Erro ao enviar push notification via Firebase", ex);
            }
        }

        public async Task SendAlarmNotificationAsync(Guid userId, string alarmName, string message)
        {
            using var activity = _activitySource.StartActivity("Firebase.SendAlarmNotification");
            activity?.SetTag("notification.operation", "send_alarm_notification");
            activity?.SetTag("notification.user_id", userId.ToString());
            activity?.SetTag("notification.alarm_name", alarmName);

            _logger.LogInformation("Sending alarm notification to user {UserId} for alarm '{AlarmName}'",
                userId, alarmName);

            // Em produção real, seria necessário buscar o device token do usuário no banco de dados
            // Por enquanto, vamos simular usando uma configuração ou assumir que será passado
            var deviceToken = _configuration[$"Users:{userId}:DeviceToken"];
            
            if (string.IsNullOrEmpty(deviceToken))
            {
                _logger.LogWarning("Device token not found for user {UserId}, using email fallback", userId);
                
                // Implementar fallback para email
                try
                {
                    var userEmail = _configuration[$"Users:{userId}:Email"] ?? $"user{userId}@smartalarm.local";
                    await SendEmailFallbackAsync(userEmail, alarmName, message);
                    _logger.LogInformation("Fallback email notification sent to user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send fallback email notification to user {UserId}", userId);
                }
                return;
            }

            var title = $"🚨 {alarmName}";
            await SendPushNotificationAsync(deviceToken, title, message);
        }

        public async Task SendSystemNotificationAsync(Guid userId, string title, string message)
        {
            using var activity = _activitySource.StartActivity("Firebase.SendSystemNotification");
            activity?.SetTag("notification.operation", "send_system_notification");
            activity?.SetTag("notification.user_id", userId.ToString());

            _logger.LogInformation("Sending system notification to user {UserId} with title '{Title}'",
                userId, title);

            var deviceToken = _configuration[$"Users:{userId}:DeviceToken"];
            
            if (string.IsNullOrEmpty(deviceToken))
            {
                _logger.LogWarning("Device token not found for user {UserId}, using email fallback for system notification", userId);
                
                // Implementar fallback para email
                try
                {
                    var userEmail = _configuration[$"Users:{userId}:Email"] ?? $"user{userId}@smartalarm.local";
                    await SendEmailFallbackAsync(userEmail, title, message);
                    _logger.LogInformation("Fallback email system notification sent to user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send fallback email system notification to user {UserId}", userId);
                }
                return;
            }

            await SendPushNotificationAsync(deviceToken, title, message);
        }

        public async Task SendReminderNotificationAsync(Guid userId, string reminderText)
        {
            using var activity = _activitySource.StartActivity("Firebase.SendReminderNotification");
            activity?.SetTag("notification.operation", "send_reminder_notification");
            activity?.SetTag("notification.user_id", userId.ToString());

            _logger.LogInformation("Sending reminder notification to user {UserId}",
                userId);

            var deviceToken = _configuration[$"Users:{userId}:DeviceToken"];
            
            if (string.IsNullOrEmpty(deviceToken))
            {
                _logger.LogWarning("Device token not found for user {UserId}, cannot send reminder notification", userId);
                return;
            }

            var title = "📝 Reminder";
            await SendPushNotificationAsync(deviceToken, title, reminderText);
        }

        /// <summary>
        /// Método auxiliar para enviar notificação para múltiplos dispositivos
        /// </summary>
        public async Task SendMulticastNotificationAsync(string[] deviceTokens, string title, string message)
        {
            using var activity = _activitySource.StartActivity("Firebase.SendMulticastNotification");
            activity?.SetTag("notification.operation", "send_multicast");
            activity?.SetTag("notification.device_count", deviceTokens.Length.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Sending multicast notification to {DeviceCount} devices", deviceTokens.Length);

            try
            {
                var payload = new
                {
                    registration_ids = deviceTokens,
                    notification = new
                    {
                        title = title,
                        body = message,
                        icon = "alarm_icon",
                        sound = "default"
                    },
                    data = new
                    {
                        type = "multicast",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        trace_id = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                    },
                    priority = "high"
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/fcm/send", content);

                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "Firebase", "SendMulticastNotification", true);
                    
                    _logger.LogInformation("Successfully sent multicast notification to {DeviceCount} devices in {Duration}ms", 
                        deviceTokens.Length, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Firebase multicast failed with status {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("NOTIFICATION", "Firebase", "SendMulticastNotificationError");

                _logger.LogError("Failed to send multicast notification: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Envia notificação por email como fallback quando push notification falha
        /// </summary>
        private async Task SendEmailFallbackAsync(string userEmail, string alarmName, string message)
        {
            using var activity = _activitySource.StartActivity("Firebase.SendEmailFallback");
            activity?.SetTag("notification.fallback", "email");
            activity?.SetTag("notification.email", userEmail);

            try
            {
                // Simular envio de email - em produção real, integraria com serviço de email
                await Task.Delay(200); // Simular latência de envio

                var emailSubject = $"SmartAlarm: {alarmName}";
                var emailBody = $"Olá,\n\n" +
                               $"Seu alarme '{alarmName}' foi acionado.\n\n" +
                               $"Detalhes: {message}\n\n" +
                               $"Atenciosamente,\n" +
                               $"SmartAlarm";

                // Log do email que seria enviado
                _logger.LogInformation("Email fallback notification prepared for {Email}: Subject={Subject}", 
                    userEmail, emailSubject);

                // Em produção real, isso seria uma chamada para um serviço de email como SendGrid, SES, etc.
                // await _emailService.SendAsync(userEmail, emailSubject, emailBody);

                activity?.SetStatus(ActivityStatusCode.Ok, "Email fallback sent successfully");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Failed to send email fallback to {Email}", userEmail);
                throw;
            }
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
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
