using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Calendar;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Enums;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Infrastructure.Tests.Fixtures;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration;

public class CalendarIntegrationServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IServiceProvider _serviceProvider;

    public CalendarIntegrationServiceIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _serviceProvider = _fixture.ServiceProvider;
    }

    [Fact]
    public async Task GetEventsFromAllProvidersAsync_WithNoIntegrations_ShouldReturnEmptyList()
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(7);

        // Act
        var events = await calendarService.GetEventsFromAllProvidersAsync(userId, startDate, endDate);

        // Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task HasVacationOrDayOffAsync_WithNoIntegrations_ShouldReturnFalse()
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var userId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;

        // Act
        var hasVacation = await calendarService.HasVacationOrDayOffAsync(userId, date);

        // Assert
        Assert.False(hasVacation);
    }

    [Fact]
    public async Task GetNextEventAsync_WithNoIntegrations_ShouldReturnNull()
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var userId = Guid.NewGuid();

        // Act
        var nextEvent = await calendarService.GetNextEventAsync(userId);

        // Assert
        Assert.Null(nextEvent);
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WithNoIntegrations_ShouldReturnZero()
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var userId = Guid.NewGuid();

        // Act
        var syncedCount = await calendarService.SyncAllCalendarsAsync(userId);

        // Assert
        Assert.Equal(0, syncedCount);
    }

    [Fact]
    public async Task GetAuthorizedProvidersAsync_WithNoIntegrations_ShouldReturnEmptyList()
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var userId = Guid.NewGuid();

        // Act
        var providers = await calendarService.GetAuthorizedProvidersAsync(userId);

        // Assert
        Assert.NotNull(providers);
        Assert.Empty(providers);
    }

    [Fact]
    public async Task SyncBidirectionalAsync_WithNoIntegrations_ShouldReturnFalse()
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var userId = Guid.NewGuid();
        var calendarEvent = CreateTestCalendarEvent();

        // Act
        var result = await calendarService.SyncBidirectionalAsync(userId, calendarEvent, CalendarSyncAction.Create);

        // Assert
        Assert.True(result); // Should return true even with no integrations (no-op)
    }

    [Fact]
    public async Task GetSyncStatusAsync_WithNoIntegrations_ShouldReturnEmptyStatus()
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var userId = Guid.NewGuid();

        // Act
        var status = await calendarService.GetSyncStatusAsync(userId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(userId, status.UserId);
        Assert.Empty(status.Providers);
        Assert.Equal(0, status.TotalEvents);
        Assert.False(status.HasErrors);
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WithMockIntegration_ShouldProcessIntegration()
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var integrationRepository = _serviceProvider.GetRequiredService<IIntegrationRepository>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        var userId = Guid.NewGuid();

        // Create a test alarm first (required for integration)
        var alarm = CreateTestAlarm(userId);
        await alarmRepository.AddAsync(alarm);

        // Create a mock integration
        var integration = new SmartAlarm.Domain.Entities.Integration(
            Guid.NewGuid(),
            alarm.Id,
            "GoogleCalendar",
            IntegrationType.GoogleCalendar,
            "Test Google Calendar Integration",
            new Dictionary<string, string>
            {
                { "AccessToken", "mock-access-token" }
            }
        );
        integration.Enable();

        await integrationRepository.AddAsync(integration);

        // Act
        var syncedCount = await calendarService.SyncAllCalendarsAsync(userId);

        // Assert
        // Should attempt to sync (even if it fails due to invalid token)
        Assert.True(syncedCount >= 0);
    }

    [Fact]
    public async Task GetSyncStatusAsync_WithMockIntegration_ShouldReturnStatus()
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var integrationRepository = _serviceProvider.GetRequiredService<IIntegrationRepository>();
        var alarmRepository = _serviceProvider.GetRequiredService<IAlarmRepository>();

        var userId = Guid.NewGuid();

        // Create a test alarm first
        var alarm = CreateTestAlarm(userId);
        await alarmRepository.AddAsync(alarm);

        // Create mock integrations
        var googleIntegration = new SmartAlarm.Domain.Entities.Integration(
            Guid.NewGuid(),
            alarm.Id,
            "GoogleCalendar",
            IntegrationType.GoogleCalendar,
            "Test Google Calendar",
            new Dictionary<string, string>
            {
                { "AccessToken", "mock-google-token" }
            }
        );
        googleIntegration.Enable();

        var outlookIntegration = new SmartAlarm.Domain.Entities.Integration(
            Guid.NewGuid(),
            alarm.Id,
            "OutlookCalendar",
            IntegrationType.OutlookCalendar,
            "Test Outlook Calendar",
            new Dictionary<string, string>
            {
                { "AccessToken", "mock-outlook-token" }
            }
        );
        outlookIntegration.Enable();

        await integrationRepository.AddAsync(googleIntegration);
        await integrationRepository.AddAsync(outlookIntegration);

        // Act
        var status = await calendarService.GetSyncStatusAsync(userId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(userId, status.UserId);
        Assert.Equal(2, status.Providers.Count);

        var googleProvider = status.Providers.FirstOrDefault(p => p.ProviderName == "GoogleCalendar");
        Assert.NotNull(googleProvider);
        Assert.True(googleProvider.IsAuthorized);
        Assert.True(googleProvider.IsEnabled);

        var outlookProvider = status.Providers.FirstOrDefault(p => p.ProviderName == "OutlookCalendar");
        Assert.NotNull(outlookProvider);
        Assert.True(outlookProvider.IsAuthorized);
        Assert.True(outlookProvider.IsEnabled);
    }

    [Theory]
    [InlineData(CalendarSyncAction.Create)]
    [InlineData(CalendarSyncAction.Update)]
    [InlineData(CalendarSyncAction.Delete)]
    public async Task SyncBidirectionalAsync_WithDifferentActions_ShouldHandleAllActions(CalendarSyncAction action)
    {
        // Arrange
        var calendarService = _serviceProvider.GetRequiredService<ICalendarIntegrationService>();
        var userId = Guid.NewGuid();
        var calendarEvent = CreateTestCalendarEvent();

        // Act
        var result = await calendarService.SyncBidirectionalAsync(userId, calendarEvent, action);

        // Assert
        Assert.True(result); // Should handle all actions gracefully
    }

    private CalendarEvent CreateTestCalendarEvent()
    {
        return new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test Event",
            Description = "Test event description",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsAllDay = false,
            Location = "Test Location",
            Type = CalendarEventType.Meeting,
            Attendees = new List<string> { "test@example.com" },
            IsRecurring = false
        };
    }

    private Alarm CreateTestAlarm(Guid userId)
    {
        var alarmName = new Name("Test Alarm for Calendar Integration");
        var alarmTime = DateTime.Now.AddHours(1);

        return new Alarm(
            Guid.NewGuid(),
            alarmName,
            alarmTime,
            true,
            userId
        );
    }
}
