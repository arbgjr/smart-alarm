using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Enums;
using System.Text.Json;

namespace SmartAlarm.Infrastructure.Services.External;

/// <summary>
/// Google Calendar integration service implementation
/// </summary>
public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IIntegrationRepository _integrationRepository;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;

    public GoogleCalendarService(
        IIntegrationRepository integrationRepository,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleCalendarService> logger)
    {
        _integrationRepository = integrationRepository;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<CalendarEvent>> GetEventsAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var integration = await GetGoogleCalendarIntegrationAsync(userId);
            if (integration == null || string.IsNullOrEmpty(integration.AccessToken))
            {
                _logger.LogWarning("No valid Google Calendar integration found for user {UserId}", userId);
                return new List<CalendarEvent>();
            }

            var events = new List<CalendarEvent>();

            // Get events from primary calendar
            var primaryEvents = await GetCalendarEventsAsync(integration.AccessToken, "primary", startDate, endDate, cancellationToken);
            events.AddRange(primaryEvents);

            _logger.LogInformation("Retrieved {Count} events from Google Calendar for user {UserId}", events.Count, userId);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Google Calendar events for user {UserId}", userId);
            return new List<CalendarEvent>();
        }
    }

    public async Task<bool> HasVacationOrDayOffAsync(Guid userId, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await GetEventsAsync(userId, date.Date, date.Date.AddDays(1), cancellationToken);

            return events.Any(e =>
                e.Type == CalendarEventType.Vacation ||
                e.Type == CalendarEventType.DayOff ||
                e.Type == CalendarEventType.Holiday ||
                (e.IsAllDay && IsVacationKeyword(e.Title)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check vacation/day off for user {UserId} on {Date}", userId, date);
            return false;
        }
    }

    public async Task<CalendarEvent?> GetNextEventAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var endDate = now.AddDays(7); // Look ahead 7 days

            var events = await GetEventsAsync(userId, now, endDate, cancellationToken);

            return events
                .Where(e => e.StartTime > now)
                .OrderBy(e => e.StartTime)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get next event for user {UserId}", userId);
            return null;
        }
    }

    public async Task<int> SyncCalendarAsync(Guid userId, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-7); // Sync last 7 days
            var endDate = DateTime.UtcNow.AddDays(30); // Sync next 30 days

            var events = await GetCalendarEventsAsync(accessToken, "primary", startDate, endDate, cancellationToken);

            // Here you would typically store the events in your database
            // For now, we'll just return the count
            _logger.LogInformation("Synced {Count} events from Google Calendar for user {UserId}", events.Count, userId);

            return events.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Google Calendar for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> IsAuthorizedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var integration = await GetGoogleCalendarIntegrationAsync(userId);
            return integration != null &&
                   integration.IsEnabled &&
                   !string.IsNullOrEmpty(integration.AccessToken) &&
                   integration.TokenExpiresAt > DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Google Calendar authorization for user {UserId}", userId);
            return false;
        }
    }

    private async Task<Domain.Entities.Integration?> GetGoogleCalendarIntegrationAsync(Guid userId)
    {
        var integrations = await _integrationRepository.GetByUserIdAsync(userId);
        return integrations.FirstOrDefault(i => i.Type == IntegrationType.GoogleCalendar);
    }

    private async Task<List<CalendarEvent>> GetCalendarEventsAsync(string accessToken, string calendarId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        try
        {
            var timeMin = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var timeMax = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var url = $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events" +
                     $"?timeMin={timeMin}&timeMax={timeMax}&singleEvents=true&orderBy=startTime";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Calendar API returned {StatusCode}: {Content}",
                    response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
                return new List<CalendarEvent>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var googleResponse = JsonSerializer.Deserialize<GoogleCalendarResponse>(content);

            if (googleResponse?.Items == null)
                return new List<CalendarEvent>();

            return googleResponse.Items.Select(ConvertToCalendarEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events from Google Calendar API");
            return new List<CalendarEvent>();
        }
    }

    private static CalendarEvent ConvertToCalendarEvent(GoogleCalendarEvent googleEvent)
    {
        var startTime = ParseGoogleDateTime(googleEvent.Start);
        var endTime = ParseGoogleDateTime(googleEvent.End);
        var isAllDay = googleEvent.Start?.Date != null;

        return new CalendarEvent
        {
            Id = googleEvent.Id ?? Guid.NewGuid().ToString(),
            Title = googleEvent.Summary ?? "No Title",
            Description = googleEvent.Description,
            StartTime = startTime,
            EndTime = endTime,
            IsAllDay = isAllDay,
            Location = googleEvent.Location,
            Type = DetermineEventType(googleEvent.Summary, googleEvent.Description),
            Attendees = googleEvent.Attendees?.Select(a => a.Email ?? string.Empty).ToList() ?? new List<string>(),
            IsRecurring = !string.IsNullOrEmpty(googleEvent.RecurringEventId)
        };
    }

    private static DateTime ParseGoogleDateTime(GoogleDateTime? googleDateTime)
    {
        if (googleDateTime?.DateTime != null)
        {
            return DateTime.Parse(googleDateTime.DateTime);
        }

        if (googleDateTime?.Date != null)
        {
            return DateTime.Parse(googleDateTime.Date);
        }

        return DateTime.MinValue;
    }

    private static CalendarEventType DetermineEventType(string? title, string? description)
    {
        var text = $"{title} {description}".ToLowerInvariant();

        if (IsVacationKeyword(text))
            return CalendarEventType.Vacation;

        if (text.Contains("meeting") || text.Contains("call") || text.Contains("conference"))
            return CalendarEventType.Meeting;

        if (text.Contains("work") || text.Contains("office"))
            return CalendarEventType.Work;

        if (text.Contains("personal") || text.Contains("family"))
            return CalendarEventType.Personal;

        return CalendarEventType.Regular;
    }

    private static bool IsVacationKeyword(string text)
    {
        var vacationKeywords = new[] { "vacation", "holiday", "off", "leave", "pto", "sick", "day off", "férias", "folga" };
        return vacationKeywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    // Google Calendar API response models
    private class GoogleCalendarResponse
    {
        public List<GoogleCalendarEvent>? Items { get; set; }
    }

    private class GoogleCalendarEvent
    {
        public string? Id { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public GoogleDateTime? Start { get; set; }
        public GoogleDateTime? End { get; set; }
        public List<GoogleAttendee>? Attendees { get; set; }
        public string? RecurringEventId { get; set; }
    }

    private class GoogleDateTime
    {
        public string? DateTime { get; set; }
        public string? Date { get; set; }
        public string? TimeZone { get; set; }
    }

    private class GoogleAttendee
    {
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
    }
}
