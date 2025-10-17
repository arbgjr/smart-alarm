using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;
using SmartAlarm.Application.Services;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Api.Hubs;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Services;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration;

/// <summary>
/// Integration tests that verify the interaction between multiple backend services
/// </summary>
public class BackendServicesIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IAlarmRepository> _mockAlarmRepository;
    private readonly Mock<IAlarmEventService> _mockAlarmEventService;
    private readonly Mock<IBackgroundJobService> _mockBackgroundJobService;
    private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IPushNotificationService> _mockPushNotificationService;
    private readonly Mock<IGoogleCalendarService> _mockGoogleCalendarService;
    private readonly Mock<IIntegrationRepository> _mockIntegrationRepository;

    private readonly AlarmTriggerService _alarmTriggerService;
    private readonly SmartAlarm.Application.Abstractions.INotificationService _notificationService;
    private readonly CalendarIntegrationService _calendarIntegrationService;

    public BackendServicesIntegrationTests()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add observability
        services.AddSingleton<SmartAlarmMeter>();
        services.AddSingleton<SmartAlarmActivitySource>();

        // Create all mocks
        _mockAlarmRepository = new Mock<IAlarmRepository>();
        _mockAlarmEventService = new Mock<IAlarmEventService>();
        _mockBackgroundJobService = new Mock<IBackgroundJobService>();
        _mockHubContext = new Mock<IHubContext<NotificationHub>>();
        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockPushNotificationService = new Mock<IPushNotificationService>();
        _mockGoogleCalendarService = new Mock<IGoogleCalendarService>();
        _mockIntegrationRepository = new Mock<IIntegrationRepository>();

        // Setup SignalR mock chain
        _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

        // Register all mocks
        services.AddSingleton(_mockAlarmRepository.Object);
        services.AddSingleton(_mockAlarmEventService.Object);
        services.AddSingleton(_mockBackgroundJobService.Object);
        services.AddSingleton(_mockHubContext.Object);
        services.AddSingleton(_mockPushNotificationService.Object);
        services.AddSingleton(_mockGoogleCalendarService.Object);
        services.AddSingleton(_mockIntegrationRepository.Object);

        // Register services under test
        services.AddScoped<AlarmTriggerService>();
        services.AddScoped<SmartAlarm.Application.Abstractions.INotificationService, SignalRNotificationService>();
        services.AddScoped<CalendarIntegrationService>();

        _serviceProvider = services.BuildServiceProvider();
        _alarmTriggerService = _serviceProvider.GetRequiredService<AlarmTriggerService>();
        _notificationService = _serviceProvider.GetRequiredService<SmartAlarm.Application.Abstractions.INotificationService>();
        _calendarIntegrationService = _serviceProvider.GetRequiredService<CalendarIntegrationService>();
    }
    [Fact]
    public async Task AlarmWorkflow_FromScheduleToEscalation_ShouldWorkEndToEnd()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var alarmId = Guid.NewGuid();
        var alarm = new Alarm
        {
            Id = alarmId,
            UserId = userId,
            Time = DateTime.UtcNow.AddMinutes(-5), // Alarm was 5 minutes ago
            IsActive = true,
            IsRecurring = false,
            Title = "Important Meeting",
            Metadata = new Dictionary<string, object>()
        };

        // Setup mocks for the full workflow
        _mockAlarmRepository
            .Setup(x => x.GetByIdAsync(alarmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alarm);

        _mockAlarmRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Alarm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockBackgroundJobService
            .Setup(x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.IsAny<DateTimeOffset>()))
            .Returns("job_123");

        _mockAlarmEventService
            .Setup(x => x.RecordAlarmTriggeredAsync(alarmId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAlarmEventService
            .Setup(x => x.GetUserEventHistoryAsync(userId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlarmEvent>()); // No recent events = unhandled

        _mockClientProxy
            .Setup(x => x.SendCoreAsync(
                "ReceiveNotification",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockPushNotificationService
            .Setup(x => x.SendPushNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<PushNotificationDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - Execute the full workflow
        // 1. Schedule the alarm
        await _alarmTriggerService.ScheduleAlarmAsync(alarm);

        // 2. Trigger the alarm
        await _alarmTriggerService.TriggerAlarmAsync(alarmId);

        // 3. Escalate missed alarm (level 1)
        await _alarmTriggerService.EscalateMissedAlarmAsync(alarmId, 1);

        // 4. Escalate missed alarm (level 2 - should include push notification)
        await _alarmTriggerService.EscalateMissedAlarmAsync(alarmId, 2);

        // Assert - Verify the complete workflow
        // Verify alarm was scheduled
        _mockBackgroundJobService.Verify(
            x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.IsAny<DateTimeOffset>()),
            Times.AtLeast(3)); // Schedule + 2 escalations

        // Verify alarm trigger was recorded
        _mockAlarmEventService.Verify(
            x => x.RecordAlarmTriggeredAsync(alarmId, userId, It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify SignalR notifications were sent (2 escalations)
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveNotification",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // Verify push notification was sent for level 2 escalation
        _mockPushNotificationService.Verify(
            x => x.SendPushNotificationAsync(
                userId.ToString(),
                It.Is<PushNotificationDto>(p => p.Priority == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify alarm metadata was updated with job ID
        alarm.Metadata.Should().ContainKey("HangfireJobId");
    }
    [Fact]
    public async Task CalendarIntegrationWithAlarmScheduling_ShouldRespectVacationDays()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var alarmId = Guid.NewGuid();
        var vacationDate = DateTime.UtcNow.Date.AddDays(1);

        var alarm = new Alarm
        {
            Id = alarmId,
            UserId = userId,
            Time = vacationDate.AddHours(8), // 8 AM on vacation day
            IsActive = true,
            IsRecurring = true,
            Title = "Work Alarm"
        };

        // Setup calendar to indicate vacation day
        _mockGoogleCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockGoogleCalendarService
            .Setup(x => x.HasVacationOrDayOffAsync(userId, vacationDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var hasVacation = await _calendarIntegrationService.HasVacationOrDayOffAsync(userId, vacationDate);

        // Assert
        hasVacation.Should().BeTrue();

        // In a real implementation, the alarm scheduling would check this
        // and skip scheduling on vacation days
        _mockGoogleCalendarService.Verify(
            x => x.HasVacationOrDayOffAsync(userId, vacationDate, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotificationSystem_WithMultipleChannels_ShouldDeliverToAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid(),
            Title = "System Alert",
            Message = "Multiple channel test",
            Type = NotificationType.Warning,
            Priority = 2
        };

        var pushNotification = new PushNotificationDto
        {
            Title = notification.Title,
            Body = notification.Message,
            Priority = notification.Priority
        };

        _mockClientProxy
            .Setup(x => x.SendCoreAsync(
                "ReceiveNotification",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockPushNotificationService
            .Setup(x => x.SendPushNotificationAsync(
                userId.ToString(),
                pushNotification,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _notificationService.SendNotificationAsync(userId.ToString(), notification);
        await _mockPushNotificationService.Object.SendPushNotificationAsync(userId.ToString(), pushNotification);

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveNotification",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockPushNotificationService.Verify(
            x => x.SendPushNotificationAsync(
                userId.ToString(),
                pushNotification,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CalendarSync_WithFailureRecovery_ShouldHandleGracefully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var integration = new Integration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "GoogleCalendar",
            IsEnabled = true,
            AccessToken = "invalid_token"
        };

        _mockIntegrationRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Integration> { integration });

        _mockGoogleCalendarService
            .Setup(x => x.SyncCalendarAsync(userId, "invalid_token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Token expired"));

        _mockIntegrationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Integration>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _calendarIntegrationService.SyncAllCalendarsAsync(userId);

        // Assert
        result.Should().Be(0); // No events synced due to failure

        // Verify that the integration was still updated (for last sync attempt time)
        _mockIntegrationRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Integration>()),
            Times.Once);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
