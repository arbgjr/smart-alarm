using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Domain.Repositories;
using Polly;
using Polly.CircuitBreaker;

namespace SmartAlarm.Infrastructure.Services.External
{
    /// <summary>
    /// Implementação do serviço de integração com Google Calendar
    /// </summary>
    public class GoogleCalendarService : IGoogleCalendarService
    {
        private readonly HttpClient _httpClient;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GoogleCalendarService> _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private const string BaseUrl = "https://www.googleapis.com/calendar/v3";
        private const string CacheKeyPrefix = "gcal";

        // Palavras-chave para detectar férias/folgas
        private static readonly string[] VacationKeywords = new[]
        {
            "vacation", "férias", "holiday", "feriado", "day off", "folga", 
            "out of office", "fora do escritório", "ooo", "pto", "leave", "licença"
        };

        public GoogleCalendarService(
            HttpClient httpClient,
            IIntegrationRepository integrationRepository,
            IDistributedCache cache,
            ILogger<GoogleCalendarService> logger)
        {
            _httpClient = httpClient;
            _integrationRepository = integrationRepository;
            _cache = cache;
            _logger = logger;

            // Configurar política de retry com circuit breaker
            _retryPolicy = Policy<HttpResponseMessage>
                .HandleResult(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                .WaitAndRetryAsync(
                    2,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} após {Timespan}s para Google Calendar API", 
                            retryCount, timespan.TotalSeconds);
                    })
                .WrapAsync(Policy
                    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .CircuitBreakerAsync(
                        3,
                        TimeSpan.FromMinutes(1),
                        onBreak: (result, duration) =>
                        {
                            _logger.LogError("Circuit breaker aberto para Google Calendar API por {Duration}s", 
                                duration.TotalSeconds);
                        },
                        onReset: () =>
                        {
                            _logger.LogInformation("Circuit breaker resetado para Google Calendar API");
                        }));
        }

        public async Task<List<CalendarEvent>> GetEventsAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar cache primeiro
                var cacheKey = $"{CacheKeyPrefix}:events:{userId}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
                
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var cachedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(cachedData);
                    if (cachedEvents != null)
                    {
                        _logger.LogDebug("Cache hit para eventos do calendário: {UserId}", userId);
                        return cachedEvents;
                    }
                }

                // Obter token de acesso do usuário
                var integration = await _integrationRepository.GetByUserAndTypeAsync(userId, "GoogleCalendar");
                if (integration == null || string.IsNullOrEmpty(integration.AccessToken))
                {
                    _logger.LogWarning("Usuário {UserId} não tem integração com Google Calendar", userId);
                    return new List<CalendarEvent>();
                }

                // Buscar eventos da API
                var url = $"{BaseUrl}/calendars/primary/events" +
                         $"?timeMin={startDate:yyyy-MM-dd'T'HH:mm:ss'Z'}" +
                         $"&timeMax={endDate:yyyy-MM-dd'T'HH:mm:ss'Z'}" +
                         $"&singleEvents=true" +
                         $"&orderBy=startTime";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", integration.AccessToken);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.SendAsync(request, cancellationToken));

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("Token expirado para usuário {UserId}", userId);
                        // Marcar integração como inválida
                        integration.Disable();
                        await _integrationRepository.UpdateAsync(integration);
                    }
                    return new List<CalendarEvent>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<GoogleCalendarResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var events = MapGoogleEventsToCalendarEvents(apiResponse?.Items ?? new List<GoogleCalendarEvent>());

                // Armazenar em cache por 1 hora
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(events),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    },
                    cancellationToken);

                _logger.LogInformation("Obtidos {Count} eventos do Google Calendar para usuário {UserId}", 
                    events.Count, userId);

                return events;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker aberto para Google Calendar API");
                return new List<CalendarEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar eventos do Google Calendar para usuário {UserId}", userId);
                return new List<CalendarEvent>();
            }
        }

        public async Task<bool> HasVacationOrDayOffAsync(
            Guid userId, 
            DateTime date, 
            CancellationToken cancellationToken = default)
        {
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1).AddSeconds(-1);

            var events = await GetEventsAsync(userId, startOfDay, endOfDay, cancellationToken);

            return events.Any(e => 
                e.Type == CalendarEventType.Vacation || 
                e.Type == CalendarEventType.DayOff ||
                e.Type == CalendarEventType.Holiday ||
                (e.IsAllDay && ContainsVacationKeywords(e.Title + " " + e.Description)));
        }

        public async Task<CalendarEvent?> GetNextEventAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var endDate = now.AddDays(7); // Buscar eventos dos próximos 7 dias

            var events = await GetEventsAsync(userId, now, endDate, cancellationToken);

            return events
                .Where(e => e.StartTime > now)
                .OrderBy(e => e.StartTime)
                .FirstOrDefault();
        }

        public async Task<int> SyncCalendarAsync(
            Guid userId, 
            string accessToken, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Atualizar ou criar integração
                var integration = await _integrationRepository.GetByUserAndTypeAsync(userId, "GoogleCalendar") 
                    ?? new Domain.Entities.Integration(
                        Guid.NewGuid(),
                        userId,
                        "GoogleCalendar",
                        "Google Calendar Integration",
                        new Dictionary<string, string>());

                integration.UpdateAccessToken(accessToken, null, DateTime.UtcNow.AddHours(1));
                integration.Enable();

                if (integration.Id == Guid.Empty)
                {
                    await _integrationRepository.AddAsync(integration);
                }
                else
                {
                    await _integrationRepository.UpdateAsync(integration);
                }

                // Sincronizar eventos dos próximos 30 dias
                var startDate = DateTime.UtcNow;
                var endDate = startDate.AddDays(30);

                var events = await GetEventsAsync(userId, startDate, endDate, cancellationToken);

                _logger.LogInformation("Calendário sincronizado para usuário {UserId}: {Count} eventos", 
                    userId, events.Count);

                return events.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar calendário para usuário {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> IsAuthorizedAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            var integration = await _integrationRepository.GetByUserAndTypeAsync(userId, "GoogleCalendar");
            return integration != null && integration.IsEnabled && !string.IsNullOrEmpty(integration.AccessToken);
        }

        private List<CalendarEvent> MapGoogleEventsToCalendarEvents(List<GoogleCalendarEvent> googleEvents)
        {
            return googleEvents.Select(ge => new CalendarEvent
            {
                Id = ge.Id ?? Guid.NewGuid().ToString(),
                Title = ge.Summary ?? "Sem título",
                Description = ge.Description,
                StartTime = ParseGoogleDateTime(ge.Start),
                EndTime = ParseGoogleDateTime(ge.End),
                IsAllDay = ge.Start?.Date != null,
                Location = ge.Location,
                Type = DetermineEventType(ge),
                Attendees = ge.Attendees?.Select(a => a.Email).ToList() ?? new List<string>(),
                IsRecurring = ge.RecurringEventId != null,
                RecurrenceRule = ge.Recurrence?.FirstOrDefault()
            }).ToList();
        }

        private DateTime ParseGoogleDateTime(GoogleDateTime? googleDateTime)
        {
            if (googleDateTime == null)
                return DateTime.MinValue;

            if (!string.IsNullOrEmpty(googleDateTime.DateTime))
                return DateTime.Parse(googleDateTime.DateTime);

            if (!string.IsNullOrEmpty(googleDateTime.Date))
                return DateTime.Parse(googleDateTime.Date);

            return DateTime.MinValue;
        }

        private CalendarEventType DetermineEventType(GoogleCalendarEvent googleEvent)
        {
            var text = $"{googleEvent.Summary} {googleEvent.Description}".ToLowerInvariant();

            if (ContainsVacationKeywords(text))
                return CalendarEventType.Vacation;

            if (text.Contains("meeting") || text.Contains("reunião") || text.Contains("call"))
                return CalendarEventType.Meeting;

            if (text.Contains("work") || text.Contains("trabalho"))
                return CalendarEventType.Work;

            if (text.Contains("personal") || text.Contains("pessoal"))
                return CalendarEventType.Personal;

            return CalendarEventType.Regular;
        }

        private bool ContainsVacationKeywords(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var lowerText = text.ToLowerInvariant();
            return VacationKeywords.Any(keyword => lowerText.Contains(keyword));
        }
    }

    // Modelos para deserialização da API do Google Calendar
    internal class GoogleCalendarResponse
    {
        public List<GoogleCalendarEvent> Items { get; set; } = new();
    }

    internal class GoogleCalendarEvent
    {
        public string? Id { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public GoogleDateTime? Start { get; set; }
        public GoogleDateTime? End { get; set; }
        public string? Location { get; set; }
        public List<GoogleAttendee>? Attendees { get; set; }
        public string? RecurringEventId { get; set; }
        public List<string>? Recurrence { get; set; }
    }

    internal class GoogleDateTime
    {
        public string? DateTime { get; set; }
        public string? Date { get; set; }
    }

    internal class GoogleAttendee
    {
        public string Email { get; set; } = string.Empty;
    }
}