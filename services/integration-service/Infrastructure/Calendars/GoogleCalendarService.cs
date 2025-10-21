using SmartAlarm.IntegrationService.Infrastructure.RateLimiting;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using System.Text.Json;
using System.Diagnostics;

namespace SmartAlarm.IntegrationService.Infrastructure.Calendars
{
    /// <summary>
    /// Serviço de integração com Google Calendar
    /// </summary>
    public class GoogleCalendarService : ICalendarIntegrationService
    {
        public string Provider => "google";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRateLimiter _rateLimiter;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<GoogleCalendarService> _logger;

        public GoogleCalendarService(
            IHttpClientFactory httpClientFactory,
            IRateLimiter rateLimiter,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<GoogleCalendarService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _rateLimiter = rateLimiter;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<CalendarSyncResult> SyncEventsAsync(
            CalendarSyncRequest request,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GoogleCalendarService.SyncEvents");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("provider", Provider);
                activity?.SetTag("force_full_sync", request.ForceFullSync.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando sincronização Google Calendar para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);

                // Verificar rate limit
                var rateLimitKey = $"sync:{request.UserId}";
                var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(rateLimitKey, Provider, cancellationToken);

                if (!rateLimitResult.IsAllowed)
                {
                    _logger.LogWarning("Rate limit excedido para sincronização Google Calendar - UserId: {UserId} - Reason: {Reason}",
                        request.UserId, rateLimitResult.ReasonDenied);

                    throw new InvalidOperationException($"Rate limit excedido: {rateLimitResult.ReasonDenied}");
                }

                // Validar token de acesso
                var isValidToken = await ValidateAccessTokenAsync(request.AccessToken, cancellationToken);
                if (!isValidToken)
                {
                    throw new UnauthorizedAccessException("Token de acesso inválido para Google Calendar");
                }

                // Obter eventos do Google Calendar
                var events = await GetEventsFromGoogleAsync(request, cancellationToken);

                // Processar eventos
                var processedEvents = await ProcessEventsAsync(events, request.UserId, cancellationToken);

                // Registrar requisição no rate limiter
                await _rateLimiter.RecordRequestAsync(rateLimitKey, Provider, true, stopwatch.Elapsed, cancellationToken);

                stopwatch.Stop();

                // Métricas
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "calendar_sync", Provider, "success");

                var result = new CalendarSyncResult(
                    UserId: request.UserId,
                    Provider: Provider,
                    EventsProcessed: processedEvents.EventsProcessed,
                    EventsCreated: processedEvents.EventsCreated,
                    EventsUpdated: processedEvents.EventsUpdated,
                    EventsDeleted: processedEvents.EventsDeleted,
                    Errors: processedEvents.Errors,
                    SyncedAt: DateTime.UtcNow,
                    Duration: stopwatch.Elapsed
                );

                _logger.LogInformation("Sincronização Google Calendar concluída para usuário {UserId} - Eventos: {EventsProcessed} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    request.UserId, result.EventsProcessed, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Registrar falha no rate limiter
                var rateLimitKey = $"sync:{request.UserId}";
                await _rateLimiter.RecordRequestAsync(rateLimitKey, Provider, false, stopwatch.Elapsed, cancellationToken);

                // Métricas
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "calendar_sync", Provider, "failed");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro na sincronização Google Calendar para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<CalendarEvent> CreateEventAsync(
            CreateCalendarEventRequest request,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GoogleCalendarService.CreateEvent");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("provider", Provider);
                activity?.SetTag("event.title", request.Title);
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Criando evento no Google Calendar: {Title} - CorrelationId: {CorrelationId}",
                    request.Title, _correlationContext.CorrelationId);

                // Verificar rate limit
                var rateLimitKey = "create_event";
                var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(rateLimitKey, Provider, cancellationToken);

                if (!rateLimitResult.IsAllowed)
                {
                    throw new InvalidOperationException($"Rate limit excedido: {rateLimitResult.ReasonDenied}");
                }

                // Simular criação de evento no Google Calendar
                var eventId = Guid.NewGuid().ToString();
                var calendarEvent = new CalendarEvent(
                    Id: eventId,
                    Title: request.Title,
                    Description: request.Description,
                    StartTime: request.StartTime,
                    EndTime: request.EndTime,
                    Location: request.Location,
                    Attendees: request.Attendees ?? Array.Empty<string>(),
                    CalendarId: request.CalendarId ?? "primary",
                    CreatedAt: DateTime.UtcNow,
                    UpdatedAt: DateTime.UtcNow,
                    Metadata: new Dictionary<string, object>
                    {
                        ["provider"] = Provider,
                        ["created_by"] = "SmartAlarm"
                    }
                );

                // Simular latência da API do Google
                await Task.Delay(Random.Shared.Next(100, 300), cancellationToken);

                // Registrar no rate limiter
                await _rateLimiter.RecordRequestAsync(rateLimitKey, Provider, true, stopwatch.Elapsed, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation("Evento criado no Google Calendar: {EventId} - {Title} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    eventId, request.Title, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return calendarEvent;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao criar evento no Google Calendar: {Title} - CorrelationId: {CorrelationId}",
                    request.Title, _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<CalendarEvent> UpdateEventAsync(
            UpdateCalendarEventRequest request,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GoogleCalendarService.UpdateEvent");

            try
            {
                activity?.SetTag("provider", Provider);
                activity?.SetTag("event.id", request.EventId);
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Atualizando evento no Google Calendar: {EventId} - CorrelationId: {CorrelationId}",
                    request.EventId, _correlationContext.CorrelationId);

                // Verificar rate limit
                var rateLimitKey = "update_event";
                var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(rateLimitKey, Provider, cancellationToken);

                if (!rateLimitResult.IsAllowed)
                {
                    throw new InvalidOperationException($"Rate limit excedido: {rateLimitResult.ReasonDenied}");
                }

                // Simular atualização no Google Calendar
                var updatedEvent = new CalendarEvent(
                    Id: request.EventId,
                    Title: request.Title ?? "Evento Atualizado",
                    Description: request.Description,
                    StartTime: request.StartTime ?? DateTime.UtcNow,
                    EndTime: request.EndTime ?? DateTime.UtcNow.AddHours(1),
                    Location: request.Location,
                    Attendees: request.Attendees ?? Array.Empty<string>(),
                    CalendarId: request.CalendarId ?? "primary",
                    CreatedAt: DateTime.UtcNow.AddDays(-1), // Simular criação anterior
                    UpdatedAt: DateTime.UtcNow,
                    Metadata: new Dictionary<string, object>
                    {
                        ["provider"] = Provider,
                        ["updated_by"] = "SmartAlarm"
                    }
                );

                // Simular latência da API
                await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);

                _logger.LogInformation("Evento atualizado no Google Calendar: {EventId} - CorrelationId: {CorrelationId}",
                    request.EventId, _correlationContext.CorrelationId);

                return updatedEvent;
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao atualizar evento no Google Calendar: {EventId} - CorrelationId: {CorrelationId}",
                    request.EventId, _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteEventAsync(
            string eventId,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GoogleCalendarService.DeleteEvent");

            try
            {
                activity?.SetTag("provider", Provider);
                activity?.SetTag("event.id", eventId);
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Removendo evento do Google Calendar: {EventId} - CorrelationId: {CorrelationId}",
                    eventId, _correlationContext.CorrelationId);

                // Verificar rate limit
                var rateLimitKey = "delete_event";
                var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(rateLimitKey, Provider, cancellationToken);

                if (!rateLimitResult.IsAllowed)
                {
                    throw new InvalidOperationException($"Rate limit excedido: {rateLimitResult.ReasonDenied}");
                }

                // Simular remoção no Google Calendar
                await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);

                _logger.LogInformation("Evento removido do Google Calendar: {EventId} - CorrelationId: {CorrelationId}",
                    eventId, _correlationContext.CorrelationId);

                return true;
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao remover evento do Google Calendar: {EventId} - CorrelationId: {CorrelationId}",
                    eventId, _correlationContext.CorrelationId);

                return false;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CalendarEvent>> GetEventsAsync(
            GetCalendarEventsRequest request,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("GoogleCalendarService.GetEvents");

            try
            {
                activity?.SetTag("provider", Provider);
                activity?.SetTag("date_range", $"{request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}");
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Obtendo eventos do Google Calendar: {StartDate} a {EndDate} - CorrelationId: {CorrelationId}",
                    request.StartDate, request.EndDate, _correlationContext.CorrelationId);

                // Verificar rate limit
                var rateLimitKey = "get_events";
                var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(rateLimitKey, Provider, cancellationToken);

                if (!rateLimitResult.IsAllowed)
                {
                    throw new InvalidOperationException($"Rate limit excedido: {rateLimitResult.ReasonDenied}");
                }

                // Simular obtenção de eventos do Google Calendar
                var events = GenerateSampleEvents(request.StartDate, request.EndDate, request.MaxResults);

                // Simular latência da API
                await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);

                _logger.LogInformation("Eventos obtidos do Google Calendar: {EventCount} - CorrelationId: {CorrelationId}",
                    events.Count(), _correlationContext.CorrelationId);

                return events;
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao obter eventos do Google Calendar - CorrelationId: {CorrelationId}",
                    _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ValidateAccessTokenAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Validando token de acesso do Google Calendar");

                // Simular validação do token
                if (string.IsNullOrEmpty(accessToken) || accessToken.Length < 10)
                {
                    return false;
                }

                // Simular chamada para Google OAuth2 API
                await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);

                // Simular 95% de tokens válidos
                var isValid = Random.Shared.NextDouble() > 0.05;

                _logger.LogDebug("Token de acesso do Google Calendar validado: {IsValid}", isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar token de acesso do Google Calendar");
                return false;
            }
        }

        #region Métodos Privados

        private async Task<IEnumerable<CalendarEvent>> GetEventsFromGoogleAsync(
            CalendarSyncRequest request,
            CancellationToken cancellationToken)
        {
            // Simular chamada para Google Calendar API
            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-7);
            var endDate = request.EndDate ?? DateTime.UtcNow.AddDays(30);

            var events = GenerateSampleEvents(startDate, endDate, 50);

            // Simular latência da API
            await Task.Delay(Random.Shared.Next(300, 800), cancellationToken);

            return events;
        }

        private async Task<EventProcessingResult> ProcessEventsAsync(
            IEnumerable<CalendarEvent> events,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var eventsList = events.ToList();
            var errors = new List<string>();

            try
            {
                // Simular processamento de eventos
                var created = 0;
                var updated = 0;
                var deleted = 0;

                foreach (var calendarEvent in eventsList)
                {
                    try
                    {
                        // Simular lógica de processamento
                        if (Random.Shared.NextDouble() > 0.8) // 20% novos eventos
                        {
                            created++;
                        }
                        else if (Random.Shared.NextDouble() > 0.5) // 30% eventos atualizados
                        {
                            updated++;
                        }

                        // Simular pequeno delay por evento
                        await Task.Delay(10, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Erro ao processar evento {calendarEvent.Id}: {ex.Message}");
                    }
                }

                return new EventProcessingResult(
                    EventsProcessed: eventsList.Count,
                    EventsCreated: created,
                    EventsUpdated: updated,
                    EventsDeleted: deleted,
                    Errors: errors
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento de eventos para usuário {UserId}", userId);
                errors.Add($"Erro geral no processamento: {ex.Message}");

                return new EventProcessingResult(0, 0, 0, 0, errors);
            }
        }

        private static IEnumerable<CalendarEvent> GenerateSampleEvents(DateTime startDate, DateTime endDate, int maxResults)
        {
            var events = new List<CalendarEvent>();
            var random = new Random();
            var eventCount = Math.Min(maxResults, random.Next(5, 20));

            for (int i = 0; i < eventCount; i++)
            {
                var eventStart = startDate.AddDays(random.Next(0, (int)(endDate - startDate).TotalDays))
                                          .AddHours(random.Next(8, 18))
                                          .AddMinutes(random.Next(0, 60));

                var eventEnd = eventStart.AddHours(random.Next(1, 4));

                events.Add(new CalendarEvent(
                    Id: Guid.NewGuid().ToString(),
                    Title: $"Evento Google {i + 1}",
                    Description: $"Descrição do evento {i + 1}",
                    StartTime: eventStart,
                    EndTime: eventEnd,
                    Location: random.NextDouble() > 0.5 ? "Escritório" : null,
                    Attendees: random.NextDouble() > 0.7 ? new[] { "user@example.com" } : Array.Empty<string>(),
                    CalendarId: "primary",
                    CreatedAt: DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                    UpdatedAt: DateTime.UtcNow.AddDays(-random.Next(0, 5)),
                    Metadata: new Dictionary<string, object>
                    {
                        ["provider"] = "google",
                        ["sync_source"] = "SmartAlarm"
                    }
                ));
            }

            return events;
        }

        #endregion
    }

    /// <summary>
    /// Resultado do processamento de eventos
    /// </summary>
    internal record EventProcessingResult(
        int EventsProcessed,
        int EventsCreated,
        int EventsUpdated,
        int EventsDeleted,
        IEnumerable<string> Errors
    );
}
