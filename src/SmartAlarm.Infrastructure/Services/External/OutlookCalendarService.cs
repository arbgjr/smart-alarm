using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Domain.Repositories;
using System.Net.Http.Headers;
using System.Text;
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
    /// Implementação do serviço de integração com Outlook Calendar (Microsoft Graph)
    /// </summary>
    public class OutlookCalendarService : IOutlookCalendarService
    {
        private readonly HttpClient _httpClient;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<OutlookCalendarService> _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private const string BaseUrl = "https://graph.microsoft.com/v1.0";
        private const string CacheKeyPrefix = "outlook";

        // Palavras-chave para detectar férias/folgas
        private static readonly string[] VacationKeywords = new[]
        {
            "vacation", "férias", "holiday", "feriado", "day off", "folga",
            "out of office", "fora do escritório", "ooo", "pto", "leave", "licença"
        };

        public OutlookCalendarService(
            HttpClient httpClient,
            IIntegrationRepository integrationRepository,
            IDistributedCache cache,
            ILogger<OutlookCalendarService> logger)
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
                        _logger.LogWarning("Retry {RetryCount} após {Timespan}s para Microsoft Graph API",
                            retryCount, timespan.TotalSeconds);
                    })
                .WrapAsync(Policy
                    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .CircuitBreakerAsync(
                        3,
                        TimeSpan.FromMinutes(1),
                        onBreak: (result, duration) =>
                        {
                            _logger.LogError("Circuit breaker aberto para Microsoft Graph API por {Duration}s",
                                duration.TotalSeconds);
                        },
                        onReset: () =>
                        {
                            _logger.LogInformation("Circuit breaker resetado para Microsoft Graph API");
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
                        _logger.LogDebug("Cache hit para eventos do Outlook: {UserId}", userId);
                        return cachedEvents;
                    }
                }

                // Obter token de acesso do usuário
                var integration = await _integrationRepository.GetByUserAndTypeAsync(userId, "OutlookCalendar");
                if (integration == null || string.IsNullOrEmpty(integration.AccessToken))
                {
                    _logger.LogWarning("Usuário {UserId} não tem integração com Outlook Calendar", userId);
                    return new List<CalendarEvent>();
                }

                // Buscar eventos da API
                var startTimeFilter = startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
                var endTimeFilter = endDate.ToString("yyyy-MM-ddTHH:mm:ss.fffK");

                var url = $"{BaseUrl}/me/events" +
                         $"?$filter=start/dateTime ge '{startTimeFilter}' and end/dateTime le '{endTimeFilter}'" +
                         $"&$orderby=start/dateTime" +
                         $"&$select=id,subject,body,start,end,isAllDay,location,attendees,recurrence";

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
                var apiResponse = JsonSerializer.Deserialize<OutlookCalendarResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var events = MapOutlookEventsToCalendarEvents(apiResponse?.Value ?? new List<OutlookCalendarEvent>());

                // Armazenar em cache por 1 hora
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(events),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    },
                    cancellationToken);

                _logger.LogInformation("Obtidos {Count} eventos do Outlook Calendar para usuário {UserId}",
                    events.Count, userId);

                return events;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker aberto para Microsoft Graph API");
                return new List<CalendarEvent>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar eventos do Outlook Calendar para usuário {UserId}", userId);
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
                var integration = await _integrationRepository.GetByUserAndTypeAsync(userId, "OutlookCalendar")
                    ?? new Domain.Entities.Integration(
                        Guid.NewGuid(),
                        userId,
                        "OutlookCalendar",
                        Domain.Enums.IntegrationType.OutlookCalendar,
                        "Outlook Calendar Integration",
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

                _logger.LogInformation("Calendário Outlook sincronizado para usuário {UserId}: {Count} eventos",
                    userId, events.Count);

                return events.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar calendário Outlook para usuário {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> IsAuthorizedAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var integration = await _integrationRepository.GetByUserAndTypeAsync(userId, "OutlookCalendar");
            return integration != null && integration.IsEnabled && !string.IsNullOrEmpty(integration.AccessToken);
        }

        public async Task<string?> CreateEventAsync(
            Guid userId,
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var integration = await _integrationRepository.GetByUserAndTypeAsync(userId, "OutlookCalendar");
                if (integration == null || string.IsNullOrEmpty(integration.AccessToken))
                {
                    _logger.LogWarning("Usuário {UserId} não tem integração com Outlook Calendar", userId);
                    return null;
                }

                var outlookEvent = new
                {
                    subject = calendarEvent.Title,
                    body = new
                    {
                        contentType = "Text",
                        content = calendarEvent.Description ?? ""
                    },
                    start = new
                    {
                        dateTime = calendarEvent.StartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                        timeZone = "UTC"
                    },
                    end = new
                    {
                        dateTime = calendarEvent.EndTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                        timeZone = "UTC"
                    },
                    isAllDay = calendarEvent.IsAllDay,
                    location = new
                    {
                        displayName = calendarEvent.Location ?? ""
                    }
                };

                var json = JsonSerializer.Serialize(outlookEvent);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/me/events")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", integration.AccessToken);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.SendAsync(request, cancellationToken));

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var createdEvent = JsonSerializer.Deserialize<OutlookCalendarEvent>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Evento criado no Outlook Calendar para usuário {UserId}: {EventId}",
                        userId, createdEvent?.Id);

                    return createdEvent?.Id;
                }

                _logger.LogWarning("Falha ao criar evento no Outlook Calendar para usuário {UserId}: {StatusCode}",
                    userId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar evento no Outlook Calendar para usuário {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateEventAsync(
            Guid userId,
            string eventId,
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var integration = await _integrationRepository.GetByUserAndTypeAsync(userId, "OutlookCalendar");
                if (integration == null || string.IsNullOrEmpty(integration.AccessToken))
                {
                    _logger.LogWarning("Usuário {UserId} não tem integração com Outlook Calendar", userId);
                    return false;
                }

                var outlookEvent = new
                {
                    subject = calendarEvent.Title,
                    body = new
                    {
                        contentType = "Text",
                        content = calendarEvent.Description ?? ""
                    },
                    start = new
                    {
                        dateTime = calendarEvent.StartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                        timeZone = "UTC"
                    },
                    end = new
                    {
                        dateTime = calendarEvent.EndTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                        timeZone = "UTC"
                    },
                    isAllDay = calendarEvent.IsAllDay,
                    location = new
                    {
                        displayName = calendarEvent.Location ?? ""
                    }
                };

                var json = JsonSerializer.Serialize(outlookEvent);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/me/events/{eventId}")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", integration.AccessToken);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.SendAsync(request, cancellationToken));

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Evento atualizado no Outlook Calendar para usuário {UserId}: {EventId}",
                        userId, eventId);
                    return true;
                }

                _logger.LogWarning("Falha ao atualizar evento no Outlook Calendar para usuário {UserId}: {StatusCode}",
                    userId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar evento no Outlook Calendar para usuário {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteEventAsync(
            Guid userId,
            string eventId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var integration = await _integrationRepository.GetByUserAndTypeAsync(userId, "OutlookCalendar");
                if (integration == null || string.IsNullOrEmpty(integration.AccessToken))
                {
                    _logger.LogWarning("Usuário {UserId} não tem integração com Outlook Calendar", userId);
                    return false;
                }

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/me/events/{eventId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", integration.AccessToken);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.SendAsync(request, cancellationToken));

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Evento removido do Outlook Calendar para usuário {UserId}: {EventId}",
                        userId, eventId);
                    return true;
                }

                _logger.LogWarning("Falha ao remover evento do Outlook Calendar para usuário {UserId}: {StatusCode}",
                    userId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover evento do Outlook Calendar para usuário {UserId}", userId);
                return false;
            }
        }

        private List<CalendarEvent> MapOutlookEventsToCalendarEvents(List<OutlookCalendarEvent> outlookEvents)
        {
            return outlookEvents.Select(oe => new CalendarEvent
            {
                Id = oe.Id ?? Guid.NewGuid().ToString(),
                Title = oe.Subject ?? "Sem título",
                Description = oe.Body?.Content,
                StartTime = ParseOutlookDateTime(oe.Start),
                EndTime = ParseOutlookDateTime(oe.End),
                IsAllDay = oe.IsAllDay,
                Location = oe.Location?.DisplayName,
                Type = DetermineEventType(oe),
                Attendees = oe.Attendees?.Select(a => a.EmailAddress?.Address ?? "").Where(email => !string.IsNullOrEmpty(email)).ToList() ?? new List<string>(),
                IsRecurring = oe.Recurrence != null,
                RecurrenceRule = oe.Recurrence?.Pattern?.Type
            }).ToList();
        }

        private DateTime ParseOutlookDateTime(OutlookDateTime? outlookDateTime)
        {
            if (outlookDateTime?.DateTime == null)
                return DateTime.MinValue;

            if (DateTime.TryParse(outlookDateTime.DateTime, out var dateTime))
                return dateTime;

            return DateTime.MinValue;
        }

        private CalendarEventType DetermineEventType(OutlookCalendarEvent outlookEvent)
        {
            var text = $"{outlookEvent.Subject} {outlookEvent.Body?.Content}".ToLowerInvariant();

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

    // Modelos para deserialização da API do Microsoft Graph
    internal class OutlookCalendarResponse
    {
        public List<OutlookCalendarEvent> Value { get; set; } = new();
    }

    internal class OutlookCalendarEvent
    {
        public string? Id { get; set; }
        public string? Subject { get; set; }
        public OutlookBody? Body { get; set; }
        public OutlookDateTime? Start { get; set; }
        public OutlookDateTime? End { get; set; }
        public bool IsAllDay { get; set; }
        public OutlookLocation? Location { get; set; }
        public List<OutlookAttendee>? Attendees { get; set; }
        public OutlookRecurrence? Recurrence { get; set; }
    }

    internal class OutlookBody
    {
        public string? ContentType { get; set; }
        public string? Content { get; set; }
    }

    internal class OutlookDateTime
    {
        public string? DateTime { get; set; }
        public string? TimeZone { get; set; }
    }

    internal class OutlookLocation
    {
        public string? DisplayName { get; set; }
    }

    internal class OutlookAttendee
    {
        public OutlookEmailAddress? EmailAddress { get; set; }
    }

    internal class OutlookEmailAddress
    {
        public string? Address { get; set; }
        public string? Name { get; set; }
    }

    internal class OutlookRecurrence
    {
        public OutlookRecurrencePattern? Pattern { get; set; }
    }

    internal class OutlookRecurrencePattern
    {
        public string? Type { get; set; }
    }
}
