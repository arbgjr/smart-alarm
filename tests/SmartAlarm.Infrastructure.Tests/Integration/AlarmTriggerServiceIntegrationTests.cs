using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.Services;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Services;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration;

public class AlarmTriggerServiceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IAlarmRepository> _mockAlarmRepository;
    private readonly Mock<IAlarmEventService> _mockAlarmEventService;
    private readonly Mock<IBackgroundJobService> _mockBackgroundJobService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IPushNotificationService> _mockPushNotificationService;
    private readonly AlarmTriggerService _alarmTriggerService;

    public AlarmTriggerServiceIntegrationTests()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add observability
        services.AddSingleton<SmartAlarmMeter>();
        services.AddSingleton<SmartAlarmActivitySource>();

        // Create mocks
        _mockAlarmRepository = new Mock<IAlarmRepository>();
        _mockAlarmEventService = new Mock<IAlarmEventService>();
        _mockBackgroundJobService = new Mock<IBackgroundJobService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockPushNotificationService = new Mock<IPushNotificationService>();

        // Register mocks
        services.AddSingleton(_mockAlarmRepository.Object);
        services.AddSingleton(_mockAlarmEventService.Object);
        services.AddSingleton(_mockBackgroundJobService.Object);
        services.AddSingleton(_mockNotificationService.Object);
        services.AddSingleton(_mockPushNotificationService.Object);

        // Register service under test
        services.AddScoped<AlarmTriggerService>();

        _serviceProvider = services.BuildServiceProvider();
        _alarmTriggerService = _serviceProvider.GetRequiredService<AlarmTriggerService>();
    }

    [Fact]
    public async Task ScheduleAlarmAsync_WithValidRecurringAlarm_ShouldScheduleSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var alarmId = Guid.NewGuid();
        var alarm = new Alarm
        {
            Id = alarmId,
            UserId = userId,
            Time = DateTime.UtcNow.AddHours(1),
            IsActive = true,
            IsRecurring = true,
            Title = "Test Alarm",
            Metadata = new Dictionary<string, object>()
        };

        var expectedJobId = "job_123";
        _mockBackgroundJobService
            .Setup(x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.IsAny<DateTimeOffset>()))
            .Returns(expectedJobId);

        _mockAlarmRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Alarm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _alarmTriggerService.ScheduleAlarmAsync(alarm);

        // Assert
        _mockBackgroundJobService.Verify(
            x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.IsAny<DateTimeOffset>()),
            Times.Once);

        _mockAlarmRepository.Verify(
            x => x.UpdateAsync(It.Is<Alarm>(a =>
                a.Id == alarmId &&
                a.Metadata.ContainsKey("HangfireJobId") &&
                a.Metadata["HangfireJobId"].ToString() == expectedJobId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        alarm.Metadata.Should().ContainKey("HangfireJobId");
        alarm.Metadata["HangfireJobId"].Should().Be(expectedJobId);
    }

    [Fact]
    public async Task ScheduleAlarmAsync_WithInactiveAlarm_ShouldNotSchedule()
    {
        // Arrange
        var alarm = new Alarm
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Time = DateTime.UtcNow.AddHours(1),
            IsActive = false,
            IsRecurring = true,
            Title = "Inactive Alarm"
        };

        // Act
        await _alarmTriggerService.ScheduleAlarmAsync(alarm);

        // Assert
        _mockBackgroundJobService.Verify(
            x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.IsAny<DateTimeOffset>()),
            Times.Never);

        _mockAlarmRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Alarm>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAlarmAsync_WithExistingJob_ShouldCancelSuccessfully()
    {
        // Arrange
        var alarmId = Guid.NewGuid();
        var jobId = "job_123";
        var alarm = new Alarm
        {
            Id = alarmId,
            UserId = Guid.NewGuid(),
            Time = DateTime.UtcNow.AddHours(1),
            IsActive = true,
            Metadata = new Dictionary<string, object> { { "HangfireJobId", jobId } }
        };

        _mockAlarmRepository
            .Setup(x => x.GetByIdAsync(alarmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alarm);

        _mockAlarmRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Alarm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _alarmTriggerService.CancelAlarmAsync(alarmId);

        // Assert
        _mockBackgroundJobService.Verify(x => x.DeleteJob(jobId), Times.Once);
        _mockAlarmRepository.Verify(
            x => x.UpdateAsync(It.Is<Alarm>(a =>
                a.Id == alarmId &&
                !a.Metadata.ContainsKey("HangfireJobId")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        alarm.Metadata.Should().NotContainKey("HangfireJobId");
    }

    [Fact]
    public async Task TriggerAlarmAsync_WithActiveAlarm_ShouldTriggerAndScheduleEscalation()
    {
        // Arrange
        var alarmId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var alarm = new Alarm
        {
            Id = alarmId,
            UserId = userId,
            Time = DateTime.UtcNow,
            IsActive = true,
            IsRecurring = false,
            Title = "Test Alarm"
        };

        _mockAlarmRepository
            .Setup(x => x.GetByIdAsync(alarmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alarm);

        _mockAlarmEventService
            .Setup(x => x.RecordAlarmTriggeredAsync(alarmId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _alarmTriggerService.TriggerAlarmAsync(alarmId);

        // Assert
        _mockAlarmEventService.Verify(
            x => x.RecordAlarmTriggeredAsync(alarmId, userId, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockBackgroundJobService.Verify(
            x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.Is<DateTimeOffset>(dt => dt > DateTimeOffset.UtcNow.AddMinutes(4))),
            Times.Once);
    }

    [Fact]
    public async Task TriggerAlarmAsync_WithRecurringAlarm_ShouldRescheduleNextOccurrence()
    {
        // Arrange
        var alarmId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var alarm = new Alarm
        {
            Id = alarmId,
            UserId = userId,
            Time = DateTime.UtcNow,
            IsActive = true,
            IsRecurring = true,
            Title = "Recurring Alarm",
            Metadata = new Dictionary<string, object>()
        };

        _mockAlarmRepository
            .Setup(x => x.GetByIdAsync(alarmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alarm);

        _mockAlarmEventService
            .Setup(x => x.RecordAlarmTriggeredAsync(alarmId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockBackgroundJobService
            .Setup(x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.IsAny<DateTimeOffset>()))
            .Returns("new_job_id");

        _mockAlarmRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Alarm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _alarmTriggerService.TriggerAlarmAsync(alarmId);

        // Assert
        _mockAlarmEventService.Verify(
            x => x.RecordAlarmTriggeredAsync(alarmId, userId, It.IsAny<CancellationToken>()),
            Times.Once);

        // Should schedule escalation + reschedule next occurrence
        _mockBackgroundJobService.Verify(
            x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.IsAny<DateTimeOffset>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task EscalateMissedAlarmAsync_WithUnhandledAlarm_ShouldSendNotifications()
    {
        // Arrange
        var alarmId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var escalationLevel = 1;
        var alarm = new Alarm
        {
            Id = alarmId,
            UserId = userId,
            Time = DateTime.UtcNow.AddMinutes(-10),
            IsActive = true,
            Title = "Missed Alarm"
        };

        _mockAlarmRepository
            .Setup(x => x.GetByIdAsync(alarmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alarm);

        _mockAlarmEventService
            .Setup(x => x.GetUserEventHistoryAsync(userId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlarmEvent>()); // No recent events

        _mockNotificationService
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<Application.DTOs.Notifications.NotificationDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _alarmTriggerService.EscalateMissedAlarmAsync(alarmId, escalationLevel);

        // Assert
        _mockNotificationService.Verify(
            x => x.SendNotificationAsync(
                userId.ToString(),
                It.Is<Application.DTOs.Notifications.NotificationDto>(n =>
                    n.Title.Contains("Escalation Level 1") &&
                    n.Type == Application.DTOs.Notifications.NotificationType.Error),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Should schedule next escalation
        _mockBackgroundJobService.Verify(
            x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.Is<DateTimeOffset>(dt => dt > DateTimeOffset.UtcNow.AddMinutes(9))),
            Times.Once);
    }

    [Fact]
    public async Task EscalateMissedAlarmAsync_WithHighEscalationLevel_ShouldSendPushNotification()
    {
        // Arrange
        var alarmId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var escalationLevel = 2;
        var alarm = new Alarm
        {
            Id = alarmId,
            UserId = userId,
            Time = DateTime.UtcNow.AddMinutes(-15),
            IsActive = true,
            Title = "Critical Missed Alarm"
        };

        _mockAlarmRepository
            .Setup(x => x.GetByIdAsync(alarmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alarm);

        _mockAlarmEventService
            .Setup(x => x.GetUserEventHistoryAsync(userId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlarmEvent>());

        _mockNotificationService
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<Application.DTOs.Notifications.NotificationDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockPushNotificationService
            .Setup(x => x.SendPushNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<Application.DTOs.Notifications.PushNotificationDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _alarmTriggerService.EscalateMissedAlarmAsync(alarmId, escalationLevel);

        // Assert
        _mockNotificationService.Verify(
            x => x.SendNotificationAsync(
                userId.ToString(),
                It.IsAny<Application.DTOs.Notifications.NotificationDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockPushNotificationService.Verify(
            x => x.SendPushNotificationAsync(
                userId.ToString(),
                It.Is<Application.DTOs.Notifications.PushNotificationDto>(p =>
                    p.Title == "Missed Alarm Alert" &&
                    p.Priority == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMissedAlarmsAsync_WithMissedAlarms_ShouldEscalateAll()
    {
        // Arrange
        var missedAlarms = new List<Alarm>
        {
            new Alarm { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Time = DateTime.UtcNow.AddMinutes(-15) },
            new Alarm { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Time = DateTime.UtcNow.AddMinutes(-20) }
        };

        _mockAlarmRepository
            .Setup(x => x.GetMissedAlarmsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(missedAlarms);

        foreach (var alarm in missedAlarms)
        {
            _mockAlarmRepository
                .Setup(x => x.GetByIdAsync(alarm.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(alarm);

            _mockAlarmEventService
                .Setup(x => x.GetUserEventHistoryAsync(alarm.UserId, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AlarmEvent>());

            _mockNotificationService
                .Setup(x => x.SendNotificationAsync(
                    alarm.UserId.ToString(),
                    It.IsAny<Application.DTOs.Notifications.NotificationDto>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        // Act
        await _alarmTriggerService.ProcessMissedAlarmsAsync();

        // Assert
        _mockNotificationService.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<Application.DTOs.Notifications.NotificationDto>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(missedAlarms.Count));
    }

    [Fact]
    public async Task RescheduleAlarmAsync_ShouldCancelAndScheduleNewJob()
    {
        // Arrange
        var alarmId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var oldJobId = "old_job_123";
        var newJobId = "new_job_456";

        var alarm = new Alarm
        {
            Id = alarmId,
            UserId = userId,
            Time = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            IsRecurring = true,
            Title = "Rescheduled Alarm",
            Metadata = new Dictionary<string, object> { { "HangfireJobId", oldJobId } }
        };

        _mockAlarmRepository
            .Setup(x => x.GetByIdAsync(alarmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alarm);

        _mockBackgroundJobService
            .Setup(x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.IsAny<DateTimeOffset>()))
            .Returns(newJobId);

        _mockAlarmRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Alarm>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _alarmTriggerService.RescheduleAlarmAsync(alarm);

        // Assert
        _mockBackgroundJobService.Verify(x => x.DeleteJob(oldJobId), Times.Once);
        _mockBackgroundJobService.Verify(
            x => x.ScheduleJob<IAlarmTriggerService>(
                It.IsAny<System.Linq.Expressions.Expression<Func<IAlarmTriggerService, Task>>>(),
                It.IsAny<DateTimeOffset>()),
            Times.Once);

        _mockAlarmRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Alarm>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Once for cancel, once for schedule
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
