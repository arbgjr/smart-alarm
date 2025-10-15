using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAlarm.Api.Services;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;

namespace SmartAlarm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        IPushNotificationService pushNotificationService,
        ICurrentUserService currentUserService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _pushNotificationService = pushNotificationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var notification = new NotificationDto
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Data = request.Data ?? new Dictionary<string, object>(),
                ActionUrl = request.ActionUrl,
                Priority = request.Priority
            };

            await _notificationService.SendNotificationAsync(userId, notification);

            _logger.LogInformation("Notification sent to user {UserId}", userId);
            return Ok(new { success = true, notificationId = notification.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
            return StatusCode(500, new { error = "Failed to send notification" });
        }
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> SendBroadcastNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            var notification = new NotificationDto
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Data = request.Data ?? new Dictionary<string, object>(),
                ActionUrl = request.ActionUrl,
                Priority = request.Priority
            };

            await _notificationService.SendBroadcastNotificationAsync(notification);

            _logger.LogInformation("Broadcast notification sent");
            return Ok(new { success = true, notificationId = notification.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send broadcast notification");
            return StatusCode(500, new { error = "Failed to send broadcast notification" });
        }
    }

    [HttpPost("push")]
    public async Task<IActionResult> SendPushNotification([FromBody] SendPushNotificationRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var pushNotification = new PushNotificationDto
            {
                Title = request.Title,
                Body = request.Body,
                Icon = request.Icon,
                Image = request.Image,
                Sound = request.Sound,
                Data = request.Data ?? new Dictionary<string, string>(),
                ClickAction = request.ClickAction,
                Priority = request.Priority,
                TimeToLive = request.TimeToLive
            };

            await _pushNotificationService.SendPushNotificationAsync(userId, pushNotification);

            _logger.LogInformation("Push notification sent to user {UserId}", userId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification");
            return StatusCode(500, new { error = "Failed to send push notification" });
        }
    }

    [HttpPost("register-device")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _pushNotificationService.RegisterDeviceAsync(userId, request.DeviceToken, request.Platform);

            _logger.LogInformation("Device registered for user {UserId}", userId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device");
            return StatusCode(500, new { error = "Failed to register device" });
        }
    }
}

public class SendNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public Dictionary<string, object>? Data { get; set; }
    public string? ActionUrl { get; set; }
    public int Priority { get; set; } = 1;
}

public class SendPushNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Image { get; set; }
    public string? Sound { get; set; } = "default";
    public Dictionary<string, string>? Data { get; set; }
    public string? ClickAction { get; set; }
    public int Priority { get; set; } = 1;
    public int TimeToLive { get; set; } = 3600;
}

public class RegisterDeviceRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
}
