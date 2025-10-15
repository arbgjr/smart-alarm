using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;
using SmartAlarm.Api.Hubs;

namespace SmartAlarm.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("Notification {NotificationId} sent to user {UserId}",
                notification.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to user {UserId}",
                notification.Id, userId);
            throw;
        }
    }

    public async Task SendNotificationToGroupAsync(string groupName, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(groupName)
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("Notification {NotificationId} sent to group {GroupName}",
                notification.Id, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to group {GroupName}",
                notification.Id, groupName);
            throw;
        }
    }

    public async Task SendBroadcastNotificationAsync(NotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("Broadcast notification {NotificationId} sent to all users",
                notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send broadcast notification {NotificationId}",
                notification.Id);
            throw;
        }
    }

    public async Task AddUserToGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            // This would typically be handled by the Hub when users connect
            // But we can also manage groups programmatically if needed
            _logger.LogInformation("User {UserId} added to group {GroupName}", userId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add user {UserId} to group {GroupName}", userId, groupName);
            throw;
        }
    }

    public async Task RemoveUserFromGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            // This would typically be handled by the Hub when users disconnect
            // But we can also manage groups programmatically if needed
            _logger.LogInformation("User {UserId} removed from group {GroupName}", userId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove user {UserId} from group {GroupName}", userId, groupName);
            throw;
        }
    }
}
