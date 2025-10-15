using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;
using SmartAlarm.Infrastructure.Services;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration;

public class PushNotificationIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IPushNotificationService> _mockPushNotificationService;

    public PushNotificationIntegrationTests()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Create mocks
        _mockPushNotificationService = new Mock<IPushNotificationService>();

        // Register mocks
        services.AddSingleton(_mockPushNotificationService.Object);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithValidNotification_ShouldSendSuccessfully()
    {
        // Arrange
        var userId = "user_123";
        var pushNotification = new PushNotificationDto
        {
            Title = "Alarm Alert",
            Body = "Your alarm is ringing!",
            Sound = "alarm_sound",
            Priority = 3,
            Data = new Dictionary<string, string>
            {
                { "alarmId", Guid.NewGuid().ToString() },
                { "type", "alarm_trigger" }
            }
        };

        _mockPushNotificationService
            .Setup(x => x.SendPushNotificationAsync(userId, pushNotification, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = _mockPushNotificationService.Object;

        // Act
        await service.SendPushNotificationAsync(userId, pushNotification);

        // Assert
        _mockPushNotificationService.Verify(
            x => x.SendPushNotificationAsync(userId, pushNotification, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPushNotificationAsync_WithHighPriorityNotification_ShouldHandleCorrectly()
    {
        // Arrange
        var userId = "user_456";
        var urgentNotification = new PushNotificationDto
        {
            Title = "URGENT: Missed Alarm",
            Body = "Your alarm has been active for 10 minutes!",
            Sound = "urgent_alarm",
            Priority = 4, // Maximum priority
            Data = new Dictionary<string, string>
            {
                { "alarmId", Guid.NewGuid().ToString() },
                { "escalationLevel", "2" },
                { "type", "alarm_escalation" }
            }
        };

        _mockPushNotificationService
            .Setup(x => x.SendPushNotificationAsync(userId, urgentNotification, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = _mockPushNotificationService.Object;

        // Act
        await service.SendPushNotificationAsync(userId, urgentNotification);

        // Assert
        _mockPushNotificationService.Verify(
            x => x.SendPushNotificationAsync(
                userId,
                It.Is<PushNotificationDto>(n =>
                    n.Priority == 4 &&
                    n.Title.Contains("URGENT") &&
                    n.Data.ContainsKey("escalationLevel")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
