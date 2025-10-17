using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Infrastructure.Tests.Fixtures;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration;

public class AlarmTriggerServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IServiceProvider _serviceProvider;

    public AlarmTriggerServiceIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _serviceProvider = _fixture.ServiceProvider;
    }

    [Fact]
    public async Task ScheduleAlarmAsync_ShouldScheduleAlarmSuccessfully()
    {
        // Arrange
        var alarmTriggerService = _serviceProvider.GetRequiredService<IAlarmTriggerService>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        var alarm = CreateTestAlarm();
        await alarmRepository.AddAsync(alarm);

        // Act
        await alarmTriggerService.ScheduleAlarmAsync(alarm);

        // Assert
        var updatedAlarm = await alarmRepository.GetByIdAsync(alarm.Id);
        Assert.NotNull(updatedAlarm);
        Assert.True(updatedAlarm.Metadata.ContainsKey("HangfireJobId"));
    }

    [Fact]
    public async Task CancelAlarmAsync_ShouldCancelScheduledAlarm()
    {
        // Arrange
        var alarmTriggerService = _serviceProvider.GetRequiredService<IAlarmTriggerService>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        var alarm = CreateTestAlarm();
        await alarmRepository.AddAsync(alarm);

        // Schedule first
        await alarmTriggerService.ScheduleAlarmAsync(alarm);

        // Act
        await alarmTriggerService.CancelAlarmAsync(alarm.Id);

        // Assert
        var updatedAlarm = await alarmRepository.GetByIdAsync(alarm.Id);
        Assert.NotNull(updatedAlarm);
        Assert.False(updatedAlarm.Metadata.ContainsKey("HangfireJobId"));
    }

    [Fact]
    public async Task RescheduleAlarmAsync_ShouldRescheduleAlarm()
    {
        // Arrange
        var alarmTriggerService = _serviceProvider.GetRequiredService<IAlarmTriggerService>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        var alarm = CreateTestAlarm();
        await alarmRepository.AddAsync(alarm);

        // Schedule first
        await alarmTriggerService.ScheduleAlarmAsync(alarm);
        var originalJobId = alarm.Metadata["HangfireJobId"];

        // Modify alarm time
        alarm.UpdateTime(TimeOnly.FromDateTime(DateTime.Now.AddHours(2)));

        // Act
        await alarmTriggerService.RescheduleAlarmAsync(alarm);

        // Assert
        var updatedAlarm = await alarmRepository.GetByIdAsync(alarm.Id);
        Assert.NotNull(updatedAlarm);
        Assert.True(updatedAlarm.Metadata.ContainsKey("HangfireJobId"));
        Assert.NotEqual(originalJobId, updatedAlarm.Metadata["HangfireJobId"]);
    }

    [Fact]
    public async Task TriggerAlarmAsync_ShouldTriggerActiveAlarm()
    {
        // Arrange
        var alarmTriggerService = _serviceProvider.GetRequiredService<IAlarmTriggerService>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        var alarm = CreateTestAlarm();
        alarm.Activate(); // Ensure alarm is active
        await alarmRepository.AddAsync(alarm);

        // Act & Assert - Should not throw
        await alarmTriggerService.TriggerAlarmAsync(alarm.Id);
    }

    [Fact]
    public async Task TriggerAlarmAsync_ShouldSkipInactiveAlarm()
    {
        // Arrange
        var alarmTriggerService = _serviceProvider.GetRequiredService<IAlarmTriggerService>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        var alarm = CreateTestAlarm();
        alarm.Deactivate(); // Ensure alarm is inactive
        await alarmRepository.AddAsync(alarm);

        // Act & Assert - Should not throw and should skip processing
        await alarmTriggerService.TriggerAlarmAsync(alarm.Id);
    }

    [Fact]
    public async Task ProcessMissedAlarmsAsync_ShouldProcessMissedAlarms()
    {
        // Arrange
        var alarmTriggerService = _serviceProvider.GetRequiredService<IAlarmTriggerService>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        // Create an alarm that should be considered "missed"
        var missedAlarm = CreateTestAlarm();
        missedAlarm.UpdateTime(TimeOnly.FromDateTime(DateTime.Now.AddMinutes(-15))); // 15 minutes ago
        await alarmRepository.AddAsync(missedAlarm);

        // Act & Assert - Should not throw
        await alarmTriggerService.ProcessMissedAlarmsAsync();
    }

    [Fact]
    public async Task EscalateMissedAlarmAsync_ShouldEscalateAlarm()
    {
        // Arrange
        var alarmTriggerService = _serviceProvider.GetRequiredService<IAlarmTriggerService>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        var alarm = CreateTestAlarm();
        await alarmRepository.AddAsync(alarm);

        // Act & Assert - Should not throw
        await alarmTriggerService.EscalateMissedAlarmAsync(alarm.Id, 1);
        await alarmTriggerService.EscalateMissedAlarmAsync(alarm.Id, 2);
        await alarmTriggerService.EscalateMissedAlarmAsync(alarm.Id, 3);
    }

    [Fact]
    public async Task TriggerAlarmAsync_WithRecurringAlarm_ShouldScheduleNextOccurrence()
    {
        // Arrange
        var alarmTriggerService = _serviceProvider.GetRequiredService<IAlarmTriggerService>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        var alarm = CreateTestAlarm();
        alarm.Activate();
        // Make it recurring by adding metadata
        alarm.UpdateMetadata("IsRecurring", "true");
        await alarmRepository.AddAsync(alarm);

        // Act
        await alarmTriggerService.TriggerAlarmAsync(alarm.Id);

        // Assert - Should have scheduled next occurrence
        var updatedAlarm = await alarmRepository.GetByIdAsync(alarm.Id);
        Assert.NotNull(updatedAlarm);
        // For recurring alarms, it should have a job ID for the next occurrence
        Assert.True(updatedAlarm.Metadata.ContainsKey("HangfireJobId"));
    }

    [Fact]
    public async Task TriggerAlarmAsync_WithNonExistentAlarm_ShouldHandleGracefully()
    {
        // Arrange
        var alarmTriggerService = _serviceProvider.GetRequiredService<IAlarmTriggerService>();
        var nonExistentAlarmId = Guid.NewGuid();

        // Act & Assert - Should not throw
        await alarmTriggerService.TriggerAlarmAsync(nonExistentAlarmId);
    }

    private Alarm CreateTestAlarm()
    {
        var userId = Guid.NewGuid();
        var alarmName = new Name("Test Alarm");
        var alarmTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(1));

        return new Alarm(
            Guid.NewGuid(),
            userId,
            alarmName,
            alarmTime,
            enabled: true
        );
    }
}
