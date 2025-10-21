using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;

using SmartAlarm.Infrastructure.Services;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration;

public class NotificationSystemIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IHubContext<Hub>> _mockHubContext;
    private readonly Mock<IHubClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IPushNotificationService> _mockPushNotificationService;
    private readonly SmartAlarm.Application.Abstractions.INotificationService _notificationService;

    public NotificationSystemIntegrationTests()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Create SignalR mocks
        _mockHubContext = new Mock<IHubContext<Hub>>();
        _mockClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockPushNotificationService = new Mock<IPushNotificationService>();

        // Setup SignalR mock chain
        _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(x => x.All).Returns(_mockClientProxy.Object);

        // Register mocks
        services.AddSingleton(_mockHubContext.Object);
        services.AddSingleton(_mockPushNotificationService.Object);

        // Register service under test
        services.AddScoped<SmartAlarm.Application.Abstractions.INotificationService, SignalRNotificationService>();

        _serviceProvider = services.BuildServiceProvider();
        _notificationService = _serviceProvider.GetRequiredService<SmartAlarm.Application.Abstractions.INotificationService>();
    }
    [Fact]
    public async Task SendNotificationAsync_WithValidUser_ShouldSendToUserGroup()
    {
        // Arrange
        var userId = "user_123";
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test Notification",
            Message = "This is a test notification",
            Type = NotificationType.Info,
            Priority = 1,
            Timestamp = DateTime.UtcNow
        };

        _mockClientProxy
            .Setup(x => x.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(args => args.Length == 1 && args[0] == notification),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _notificationService.SendNotificationAsync(userId, notification);

        // Assert
        _mockClients.Verify(x => x.Group($"user_{userId}"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(args => args.Length == 1 && args[0] == notification),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationToGroupAsync_WithValidGroup_ShouldSendToGroup()
    {
        // Arrange
        var groupName = "admin_group";
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Admin Notification",
            Message = "System maintenance scheduled",
            Type = NotificationType.Warning,
            Priority = 2
        };

        _mockClientProxy
            .Setup(x => x.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(args => args.Length == 1 && args[0] == notification),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _notificationService.SendNotificationToGroupAsync(groupName, notification);

        // Assert
        _mockClients.Verify(x => x.Group(groupName), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(args => args.Length == 1 && args[0] == notification),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task SendBroadcastNotificationAsync_ShouldSendToAllUsers()
    {
        // Arrange
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = "System Announcement",
            Message = "System will be updated tonight",
            Type = NotificationType.Info,
            Priority = 1
        };

        _mockClientProxy
            .Setup(x => x.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(args => args.Length == 1 && args[0] == notification),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _notificationService.SendBroadcastNotificationAsync(notification);

        // Assert
        _mockClients.Verify(x => x.All, Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(args => args.Length == 1 && args[0] == notification),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WithSignalRException_ShouldThrowException()
    {
        // Arrange
        var userId = "user_123";
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test Notification",
            Message = "This will fail",
            Type = NotificationType.Error
        };

        _mockClientProxy
            .Setup(x => x.SendCoreAsync(
                "ReceiveNotification",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR connection failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _notificationService.SendNotificationAsync(userId, notification));

        exception.Message.Should().Be("SignalR connection failed");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
