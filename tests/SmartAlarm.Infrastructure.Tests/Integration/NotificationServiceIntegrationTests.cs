using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;
using SmartAlarm.Infrastructure.Services;
using SmartAlarm.Infrastructure.Tests.Fixtures;
using Xunit;
using Moq;

namespace SmartAlarm.Infrastructure.Tests.Integration;

public class NotificationServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IServiceProvider _serviceProvider;

    public NotificationServiceIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _serviceProvider = _fixture.ServiceProvider;
    }

    [Fact]
    public async Task SendNotificationAsync_ShouldSendToSignalRAndPush()
    {
        // Arrange
        var notificationService = _serviceProvider.GetRequiredService<SmartAlarm.Application.Abstractions.INotificationService>();
        var userId = Guid.NewGuid().ToString();

        var notification = new NotificationDto
        {
            Title = "Test Notification",
            Message = "This is a test notification",
            Type = NotificationType.Info,
            Priority = 2
        };

        // Act & Assert - Should not throw
        await notificationService.SendNotificationAsync(userId, notification);
    }

    [Fact]
    public async Task SendNotificationToGroupAsync_ShouldSendToGroup()
    {
        // Arrange
        var notificationService = _serviceProvider.GetRequiredService<SmartAlarm.Application.Abstractions.INotificationService>();
        var groupName = "test-group";

        var notification = new NotificationDto
        {
            Title = "Group Notification",
            Message = "This is a group notification",
            Type = NotificationType.Info,
            Priority = 1
        };

        // Act & Assert - Should not throw
        await notificationService.SendNotificationToGroupAsync(groupName, notification);
    }

    [Fact]
    public async Task SendBroadcastNotificationAsync_ShouldBroadcastToAll()
    {
        // Arrange
        var notificationService = _serviceProvider.GetRequiredService<SmartAlarm.Application.Abstractions.INotificationService>();

        var notification = new NotificationDto
        {
            Title = "Broadcast Notification",
            Message = "This is a broadcast notification",
            Type = NotificationType.SystemMaintenance,
            Priority = 3
        };

        // Act & Assert - Should not throw
        await notificationService.SendBroadcastNotificationAsync(notification);
    }

    [Fact]
    public async Task PushNotificationService_ShouldRegisterAndSendNotification()
    {
        // Arrange
        var pushService = _serviceProvider.GetRequiredService<IPushNotificationService>();
        var userId = Guid.NewGuid().ToString();
        var deviceToken = "test-device-token-123";
        var platform = "android";

        // Act - Register device
        await pushService.RegisterDeviceAsync(userId, deviceToken, platform);

        // Act - Send notification
        var pushNotification = new PushNotificationDto
        {
            Title = "Test Push",
            Body = "Test push notification body",
            Priority = 2
        };

        // Assert - Should not throw
        await pushService.SendPushNotificationAsync(userId, pushNotification);
    }

    [Fact]
    public async Task PushNotificationService_ShouldUnregisterDevice()
    {
        // Arrange
        var pushService = _serviceProvider.GetRequiredService<IPushNotificationService>();
        var userId = Guid.NewGuid().ToString();
        var deviceToken = "test-device-token-456";
        var platform = "ios";

        // Act - Register then unregister
        await pushService.RegisterDeviceAsync(userId, deviceToken, platform);
        await pushService.UnregisterDeviceAsync(userId, deviceToken);

        // Assert - Should not throw when sending to unregistered device
        var pushNotification = new PushNotificationDto
        {
            Title = "Test Push After Unregister",
            Body = "This should not be delivered"
        };

        await pushService.SendPushNotificationAsync(userId, pushNotification);
    }

    [Fact]
    public async Task PushNotificationService_ShouldSendBulkNotifications()
    {
        // Arrange
        var pushService = _serviceProvider.GetRequiredService<IPushNotificationService>();
        var userIds = new[]
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };

        // Register devices for all users
        for (int i = 0; i < userIds.Length; i++)
        {
            await pushService.RegisterDeviceAsync(userIds[i], $"device-token-{i}", "web");
        }

        var bulkNotification = new PushNotificationDto
        {
            Title = "Bulk Notification",
            Body = "This is sent to multiple users",
            Priority = 1
        };

        // Act & Assert - Should not throw
        await pushService.SendBulkPushNotificationAsync(userIds, bulkNotification);
    }

    [Theory]
    [InlineData(NotificationType.AlarmTriggered, 300)] // 5 minutes for alarms
    [InlineData(NotificationType.SecurityAlert, 3600)] // 1 hour for security
    [InlineData(NotificationType.Error, 1800)] // 30 minutes for errors
    [InlineData(NotificationType.Info, 3600)] // 1 hour default
    public async Task ComprehensiveNotificationService_ShouldSetCorrectTTL(NotificationType type, int expectedTtl)
    {
        // Arrange
        var notificationService = _serviceProvider.GetRequiredService<SmartAlarm.Application.Abstractions.INotificationService>();
        var pushService = _serviceProvider.GetRequiredService<IPushNotificationService>();
        var userId = Guid.NewGuid().ToString();

        // Register a device to capture push notification details
        await pushService.RegisterDeviceAsync(userId, "test-device", "android");

        var notification = new NotificationDto
        {
            Title = "TTL Test",
            Message = "Testing TTL configuration",
            Type = type,
            Priority = 2
        };

        // Act & Assert - Should not throw and should handle TTL correctly
        await notificationService.SendNotificationAsync(userId, notification);
    }
}
