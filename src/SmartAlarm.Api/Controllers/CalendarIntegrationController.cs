using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAlarm.Api.Services;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.Services.External;

namespace SmartAlarm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarIntegrationController : ControllerBase
{
    private readonly ICalendarIntegrationService _calendarIntegrationService;
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly IOutlookCalendarService _outlookCalendarService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CalendarIntegrationController> _logger;

    public CalendarIntegrationController(
        ICalendarIntegrationService calendarIntegrationService,
        IGoogleCalendarService googleCalendarService,
        IOutlookCalendarService outlookCalendarService,
        ICurrentUserService currentUserService,
        ILogger<CalendarIntegrationController> logger)
    {
        _calendarIntegrationService = calendarIntegrationService;
        _googleCalendarService = googleCalendarService;
        _outlookCalendarService = outlookCalendarService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var start = startDate ?? DateTime.UtcNow;
            var end = endDate ?? DateTime.UtcNow.AddDays(30);

            var events = await _calendarIntegrationService.GetEventsFromAllProvidersAsync(userGuid, start, end);

            var result = events.Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.StartTime,
                e.EndTime,
                e.IsAllDay,
                e.Location,
                e.Type,
                e.Attendees,
                e.IsRecurring
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get calendar events");
            return StatusCode(500, new { error = "Failed to retrieve calendar events" });
        }
    }

    [HttpGet("next-event")]
    public async Task<IActionResult> GetNextEvent()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var nextEvent = await _calendarIntegrationService.GetNextEventAsync(userGuid);

            if (nextEvent == null)
            {
                return Ok(new { hasEvent = false });
            }

            return Ok(new
            {
                hasEvent = true,
                @event = new
                {
                    nextEvent.Id,
                    nextEvent.Title,
                    nextEvent.Description,
                    nextEvent.StartTime,
                    nextEvent.EndTime,
                    nextEvent.IsAllDay,
                    nextEvent.Location,
                    nextEvent.Type
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get next calendar event");
            return StatusCode(500, new { error = "Failed to retrieve next calendar event" });
        }
    }

    [HttpGet("vacation-check")]
    public async Task<IActionResult> CheckVacationOrDayOff([FromQuery] DateTime date)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var hasVacation = await _calendarIntegrationService.HasVacationOrDayOffAsync(userGuid, date);

            return Ok(new
            {
                date = date.ToString("yyyy-MM-dd"),
                hasVacationOrDayOff = hasVacation
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check vacation/day off for date {Date}", date);
            return StatusCode(500, new { error = "Failed to check vacation/day off status" });
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncAllCalendars()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var syncedCount = await _calendarIntegrationService.SyncAllCalendarsAsync(userGuid);

            _logger.LogInformation("Synced {Count} events for user {UserId}", syncedCount, userId);
            return Ok(new
            {
                success = true,
                syncedEvents = syncedCount,
                message = $"Successfully synced {syncedCount} events from all calendar providers"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync calendars");
            return StatusCode(500, new { error = "Failed to sync calendars" });
        }
    }

    [HttpGet("providers")]
    public async Task<IActionResult> GetAuthorizedProviders()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var providers = await _calendarIntegrationService.GetAuthorizedProvidersAsync(userGuid);

            return Ok(new
            {
                authorizedProviders = providers,
                count = providers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get authorized providers");
            return StatusCode(500, new { error = "Failed to retrieve authorized providers" });
        }
    }

    [HttpGet("sync-status")]
    public async Task<IActionResult> GetSyncStatus()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var status = await _calendarIntegrationService.GetSyncStatusAsync(userGuid);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sync status");
            return StatusCode(500, new { error = "Failed to retrieve sync status" });
        }
    }

    [HttpPost("google/sync")]
    public async Task<IActionResult> SyncGoogleCalendar([FromBody] SyncCalendarRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var syncedCount = await _googleCalendarService.SyncCalendarAsync(userGuid, request.AccessToken);

            _logger.LogInformation("Synced {Count} Google Calendar events for user {UserId}", syncedCount, userId);
            return Ok(new
            {
                success = true,
                provider = "GoogleCalendar",
                syncedEvents = syncedCount,
                message = $"Successfully synced {syncedCount} events from Google Calendar"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Google Calendar");
            return StatusCode(500, new { error = "Failed to sync Google Calendar" });
        }
    }

    [HttpPost("outlook/sync")]
    public async Task<IActionResult> SyncOutlookCalendar([FromBody] SyncCalendarRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var syncedCount = await _outlookCalendarService.SyncCalendarAsync(userGuid, request.AccessToken);

            _logger.LogInformation("Synced {Count} Outlook Calendar events for user {UserId}", syncedCount, userId);
            return Ok(new
            {
                success = true,
                provider = "OutlookCalendar",
                syncedEvents = syncedCount,
                message = $"Successfully synced {syncedCount} events from Outlook Calendar"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Outlook Calendar");
            return StatusCode(500, new { error = "Failed to sync Outlook Calendar" });
        }
    }

    [HttpPost("create-event")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateCalendarEventRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var calendarEvent = new CalendarEvent
            {
                Title = request.Title,
                Description = request.Description,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                IsAllDay = request.IsAllDay,
                Location = request.Location,
                Type = request.Type
            };

            var success = await _calendarIntegrationService.SyncBidirectionalAsync(
                userGuid, calendarEvent, CalendarSyncAction.Create);

            if (success)
            {
                _logger.LogInformation("Created calendar event '{Title}' for user {UserId}", request.Title, userId);
                return Ok(new
                {
                    success = true,
                    message = "Event created successfully in all authorized calendars"
                });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to create event in one or more calendars" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar event");
            return StatusCode(500, new { error = "Failed to create calendar event" });
        }
    }
}

public class SyncCalendarRequest
{
    public string AccessToken { get; set; } = string.Empty;
}

public class CreateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    public CalendarEventType Type { get; set; } = CalendarEventType.Regular;
}
