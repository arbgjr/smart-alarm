using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;
using System.Text.Json;
using System.Net.Http;
using System.Text;

namespace SmartAlarm.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, List<DeviceRegistration>> _userDevices;

    public PushNotificationService(
        ILogger<PushNotificationService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _userDevices = new Dictionary<string, List<DeviceRegistration>>();
    }

    public async Task SendPushNotificationAsync(string userId, PushNotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending push notification to user {UserId}", userId);

            if (!_userDevices.TryGetValue(userId, out var devices) || !devices.Any())
            {
                _logger.LogWarning("No registered devices found for user {UserId}", userId);
                return;
            }

            var tasks = devices.Select(device => SendToDevice(device, notification, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogDebug("Successfully sent push notification to {DeviceCount} devices for user {UserId}",
                devices.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
            throw;
        }
    }

    public async Task RegisterDeviceAsync(string userId, string deviceToken, string platform, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registering device for user {UserId} on platform {Platform}", userId, platform);

            if (!_userDevices.ContainsKey(userId))
            {
                _userDevices[userId] = new List<DeviceRegistration>();
            }

            var existingDevice = _userDevices[userId].FirstOrDefault(d => d.DeviceToken == deviceToken);
            if (existingDevice != null)
            {
                existingDevice.LastUpdated = DateTime.UtcNow;
                existingDevice.Platform = platform;
            }
            else
            {
                _userDevices[userId].Add(new DeviceRegistration
                {
                    DeviceToken = deviceToken,
                    Platform = platform,
                    RegisteredAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsActive = true
                });
            }

            _logger.LogDebug("Device registered successfully for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device for user {UserId}", userId);
            throw;
        }
    }

    public async Task UnregisterDeviceAsync(string userId, string deviceToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unregistering device for user {UserId}", userId);

            if (_userDevices.TryGetValue(userId, out var devices))
            {
                var deviceToRemove = devices.FirstOrDefault(d => d.DeviceToken == deviceToken);
                if (deviceToRemove != null)
                {
                    devices.Remove(deviceToRemove);
                    _logger.LogDebug("Device unregistered successfully for user {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("Device token not found for user {UserId}", userId);
                }
            }
            else
            {
                _logger.LogWarning("No devices registered for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister device for user {UserId}", userId);
            throw;
        }
    }

    public async Task SendBulkPushNotificationAsync(IEnumerable<string> userIds, PushNotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending bulk push notification to {UserCount} users", userIds.Count());

            var tasks = userIds.Select(userId => SendPushNotificationAsync(userId, notification, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogDebug("Successfully sent bulk push notification to {UserCount} users", userIds.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk push notification");
            throw;
        }
    }

    private async Task SendToDevice(DeviceRegistration device, PushNotificationDto notification, CancellationToken cancellationToken)
    {
        try
        {
            switch (device.Platform.ToLowerInvariant())
            {
                case "android":
                case "fcm":
                    await SendFcmNotification(device.DeviceToken, notification, cancellationToken);
                    break;
                case "ios":
                case "apns":
                    await SendApnsNotification(device.DeviceToken, notification, cancellationToken);
                    break;
                case "web":
                    await SendWebPushNotification(device.DeviceToken, notification, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unsupported platform: {Platform}", device.Platform);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to device {DeviceToken} on platform {Platform}",
                device.DeviceToken, device.Platform);
        }
    }

    private async Task SendFcmNotification(string deviceToken, PushNotificationDto notification, CancellationToken cancellationToken)
    {
        var fcmServerKey = _configuration["PushNotifications:FCM:ServerKey"];
        if (string.IsNullOrEmpty(fcmServerKey))
        {
            _logger.LogWarning("FCM Server Key not configured");
            return;
        }

        var payload = new
        {
            to = deviceToken,
            notification = new
            {
                title = notification.Title,
                body = notification.Body,
                icon = notification.Icon,
                sound = notification.Sound,
                click_action = notification.ClickAction
            },
            data = notification.Data,
            priority = notification.Priority switch
            {
                3 => "high",
                2 => "normal",
                _ => "normal"
            },
            time_to_live = notification.TimeToLive
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"key={fcmServerKey}");

        var response = await _httpClient.PostAsync("https://fcm.googleapis.com/fcm/send", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("FCM notification failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
        }
    }

    private async Task SendApnsNotification(string deviceToken, PushNotificationDto notification, CancellationToken cancellationToken)
    {
        // APNS implementation would go here
        // This is a placeholder for Apple Push Notification Service integration
        _logger.LogInformation("APNS notification would be sent to device {DeviceToken}", deviceToken);
        await Task.CompletedTask;
    }

    private async Task SendWebPushNotification(string deviceToken, PushNotificationDto notification, CancellationToken cancellationToken)
    {
        // Web Push implementation would go here
        // This is a placeholder for Web Push Protocol integration
        _logger.LogInformation("Web Push notification would be sent to device {DeviceToken}", deviceToken);
        await Task.CompletedTask;
    }

    private class DeviceRegistration
    {
        public string DeviceToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; }
    }
}
