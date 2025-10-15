using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Services;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration;

public class CalendarIntegrationServiceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IGoogleCalendarService> _mockGoogleCalendarService;
    private readonly Mock<IOutlookCalendarService> _mockOutlookCalendarService;
    private readonly Mock<IIntegrationRepository> _mockIntegrationRepository;
    private readonly CalendarIntegrationService _calendarIntegrationService;

    public CalendarIntegrationServiceIntegrationTests()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add observability
        services.AddSingleton<SmartAlarmMeter>();
        services.AddSingleton<SmartAlarmActivitySource>();

        // Create mocks
        _mockGoogleCalendarService = new Mock<IGoogleCalendarService>();
        _mockOutlookCalendarService = new Mock<IOutlookCalendarService>();
        _mockIntegrationRepository = new Mock<IIntegrationRepository>();

        // Register mocks
        services.AddSingleton(_mockGoogleCalendarService.Object);
        services.AddSingleton(_mockOutlookCalendarService.Object);
        services.AddSingleton(_mockIntegrationRepository.Object);

        // Register service under test
        services.AddScoped<CalendarIntegrationService>();

        _serviceProvider = services.BuildServiceProvider();
        _calendarIntegrationService = _serviceProvider.GetRequiredService<CalendarIntegrationService>();
    }

    [Fact]
    public async Task GetEventsFromAllProvidersAsync_WithBothProvidersAuthorized_ShouldReturnMergedEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(7);

        var googleEvents = new List<CalendarEvent>
        {
            new CalendarEvent
            {
                Id = "google_1",
                Title = "Google Meeting",
                StartTime = startDate.AddHours(9),
                EndTime = startDate.AddHours(10),
                Description = "Google Calendar Event"
            },
            new CalendarEvent
            {
                Id = "google_2",
                Title = "Duplicate Event",
                StartTime = startDate.AddHours(14),
                EndTime = startDate.AddHours(15),
                Description = "This event exists in both calendars"
            }
        };

        var outlookEvents = new List<CalendarEvent>
        {
            new CalendarEvent
            {
                Id = "outlook_1",
                Title = "Outlook Meeting",
                StartTime = startDate.AddHours(11),
                EndTime = startDate.AddHours(12),
                Description = "Outlook Calendar Event"
            },
            new CalendarEvent
            {
                Id = "outlook_2",
                Title = "Duplicate Event", // Same title, time as Google event
                StartTime = startDate.AddHours(14),
                EndTime = startDate.AddHours(15),
                Description = "This event exists in both calendars"
            }
        };

        _mockGoogleCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockOutlookCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockGoogleCalendarService
            .Setup(x => x.GetEventsAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleEvents);

        _mockOutlookCalendarService
            .Setup(x => x.GetEventsAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(outlookEvents);

        // Act
        var result = await _calendarIntegrationService.GetEventsFromAllProvidersAsync(userId, startDate, endDate);

        // Assert
        result.Should().HaveCount(3); // 4 events - 1 duplicate = 3 unique events
        result.Should().Contain(e => e.Title == "Google Meeting");
        result.Should().Contain(e => e.Title == "Outlook Meeting");
        result.Should().Contain(e => e.Title == "Duplicate Event");
        result.Should().BeInAscendingOrder(e => e.StartTime);

        _mockGoogleCalendarService.Verify(
            x => x.GetEventsAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockOutlookCalendarService.Verify(
            x => x.GetEventsAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEventsFromAllProvidersAsync_WithNoAuthorizedProviders_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(7);

        _mockGoogleCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockOutlookCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _calendarIntegrationService.GetEventsFromAllProvidersAsync(userId, startDate, endDate);

        // Assert
        result.Should().BeEmpty();

        _mockGoogleCalendarService.Verify(
            x => x.GetEventsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockOutlookCalendarService.Verify(
            x => x.GetEventsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HasVacationOrDayOffAsync_WithVacationInGoogleCalendar_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;

        _mockGoogleCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockOutlookCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockGoogleCalendarService
            .Setup(x => x.HasVacationOrDayOffAsync(userId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockOutlookCalendarService
            .Setup(x => x.HasVacationOrDayOffAsync(userId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _calendarIntegrationService.HasVacationOrDayOffAsync(userId, date);

        // Assert
        result.Should().BeTrue();

        _mockGoogleCalendarService.Verify(
            x => x.HasVacationOrDayOffAsync(userId, date, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockOutlookCalendarService.Verify(
            x => x.HasVacationOrDayOffAsync(userId, date, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetNextEventAsync_WithEventsInBothProviders_ShouldReturnEarliest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var googleNextEvent = new CalendarEvent
        {
            Id = "google_next",
            Title = "Google Next Event",
            StartTime = now.AddHours(2),
            EndTime = now.AddHours(3)
        };

        var outlookNextEvent = new CalendarEvent
        {
            Id = "outlook_next",
            Title = "Outlook Next Event",
            StartTime = now.AddHours(1), // Earlier than Google event
            EndTime = now.AddHours(2)
        };

        _mockGoogleCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockOutlookCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockGoogleCalendarService
            .Setup(x => x.GetNextEventAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleNextEvent);

        _mockOutlookCalendarService
            .Setup(x => x.GetNextEventAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(outlookNextEvent);

        // Act
        var result = await _calendarIntegrationService.GetNextEventAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("outlook_next");
        result.Title.Should().Be("Outlook Next Event");
        result.StartTime.Should().Be(now.AddHours(1));
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WithMultipleIntegrations_ShouldSyncAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var googleIntegration = new Integration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "GoogleCalendar",
            IsEnabled = true,
            AccessToken = "google_token_123",
            LastSyncTime = DateTime.UtcNow.AddHours(-1)
        };

        var outlookIntegration = new Integration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "OutlookCalendar",
            IsEnabled = true,
            AccessToken = "outlook_token_456",
            LastSyncTime = DateTime.UtcNow.AddHours(-2)
        };

        var integrations = new List<Integration> { googleIntegration, outlookIntegration };

        _mockIntegrationRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(integrations);

        _mockGoogleCalendarService
            .Setup(x => x.SyncCalendarAsync(userId, "google_token_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5); // 5 events synced

        _mockOutlookCalendarService
            .Setup(x => x.SyncCalendarAsync(userId, "outlook_token_456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // 3 events synced

        _mockIntegrationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Integration>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _calendarIntegrationService.SyncAllCalendarsAsync(userId);

        // Assert
        result.Should().Be(8); // 5 + 3 events synced

        _mockGoogleCalendarService.Verify(
            x => x.SyncCalendarAsync(userId, "google_token_123", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockOutlookCalendarService.Verify(
            x => x.SyncCalendarAsync(userId, "outlook_token_456", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockIntegrationRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Integration>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WithDisabledIntegration_ShouldSkipDisabled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var enabledIntegration = new Integration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "GoogleCalendar",
            IsEnabled = true,
            AccessToken = "google_token_123"
        };

        var disabledIntegration = new Integration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "OutlookCalendar",
            IsEnabled = false,
            AccessToken = "outlook_token_456"
        };

        var integrations = new List<Integration> { enabledIntegration, disabledIntegration };

        _mockIntegrationRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(integrations);

        _mockGoogleCalendarService
            .Setup(x => x.SyncCalendarAsync(userId, "google_token_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _mockIntegrationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Integration>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _calendarIntegrationService.SyncAllCalendarsAsync(userId);

        // Assert
        result.Should().Be(5); // Only enabled integration synced

        _mockGoogleCalendarService.Verify(
            x => x.SyncCalendarAsync(userId, "google_token_123", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockOutlookCalendarService.Verify(
            x => x.SyncCalendarAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockIntegrationRepository.Verify(
            x => x.UpdateAsync(enabledIntegration),
            Times.Once);
    }

    [Fact]
    public async Task GetAuthorizedProvidersAsync_ShouldReturnOnlyAuthorizedProviders()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockGoogleCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockOutlookCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _calendarIntegrationService.GetAuthorizedProvidersAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("GoogleCalendar");
        result.Should().NotContain("OutlookCalendar");
    }

    [Fact]
    public async Task SyncBidirectionalAsync_WithCreateAction_ShouldCreateInOutlook()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var calendarEvent = new CalendarEvent
        {
            Id = "new_event",
            Title = "New Meeting",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Description = "Test meeting"
        };

        _mockGoogleCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockOutlookCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockOutlookCalendarService
            .Setup(x => x.CreateEventAsync(userId, calendarEvent, It.IsAny<CancellationToken>()))
            .ReturnsAsync("created_event_id");

        // Act
        var result = await _calendarIntegrationService.SyncBidirectionalAsync(
            userId, calendarEvent, CalendarSyncAction.Create);

        // Assert
        result.Should().BeTrue();

        _mockOutlookCalendarService.Verify(
            x => x.CreateEventAsync(userId, calendarEvent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSyncStatusAsync_WithIntegrations_ShouldReturnCompleteStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lastSyncTime = DateTime.UtcNow.AddHours(-1);

        var googleIntegration = new Integration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "GoogleCalendar",
            IsEnabled = true,
            AccessToken = "google_token",
            LastSyncTime = lastSyncTime
        };

        var outlookIntegration = new Integration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "OutlookCalendar",
            IsEnabled = true,
            AccessToken = "outlook_token",
            LastSyncTime = lastSyncTime.AddMinutes(-30)
        };

        var integrations = new List<Integration> { googleIntegration, outlookIntegration };

        _mockIntegrationRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(integrations);

        var googleEvents = new List<CalendarEvent>
        {
            new CalendarEvent { Id = "g1", Title = "Google Event 1", StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) }
        };

        var outlookEvents = new List<CalendarEvent>
        {
            new CalendarEvent { Id = "o1", Title = "Outlook Event 1", StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) },
            new CalendarEvent { Id = "o2", Title = "Outlook Event 2", StartTime = DateTime.UtcNow.AddHours(2), EndTime = DateTime.UtcNow.AddHours(3) }
        };

        _mockGoogleCalendarService
            .Setup(x => x.GetEventsAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleEvents);

        _mockOutlookCalendarService
            .Setup(x => x.GetEventsAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outlookEvents);

        // Act
        var result = await _calendarIntegrationService.GetSyncStatusAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.LastSyncTime.Should().Be(lastSyncTime); // Most recent sync time
        result.TotalEvents.Should().Be(3); // 1 Google + 2 Outlook
        result.Providers.Should().HaveCount(2);
        result.HasErrors.Should().BeFalse();

        var googleProvider = result.Providers.First(p => p.ProviderName == "GoogleCalendar");
        googleProvider.IsAuthorized.Should().BeTrue();
        googleProvider.IsEnabled.Should().BeTrue();
        googleProvider.EventCount.Should().Be(1);
        googleProvider.LastSyncTime.Should().Be(lastSyncTime);

        var outlookProvider = result.Providers.First(p => p.ProviderName == "OutlookCalendar");
        outlookProvider.IsAuthorized.Should().BeTrue();
        outlookProvider.IsEnabled.Should().BeTrue();
        outlookProvider.EventCount.Should().Be(2);
        outlookProvider.LastSyncTime.Should().Be(lastSyncTime.AddMinutes(-30));
    }

    [Fact]
    public async Task GetEventsFromAllProvidersAsync_WithProviderException_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(7);

        _mockGoogleCalendarService
            .Setup(x => x.IsAuthorizedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockGoogleCalendarService
            .Setup(x => x.GetEventsAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Google Calendar API error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _calendarIntegrationService.GetEventsFromAllProvidersAsync(userId, startDate, endDate));

        exception.Message.Should().Be("Google Calendar API error");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
