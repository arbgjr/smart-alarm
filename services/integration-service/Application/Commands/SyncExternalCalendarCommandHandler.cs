using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;
using System.Text.Json;
using Google.Apis.Calendar.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Graph;

namespace SmartAlarm.IntegrationService.Application.Commands
{
    /// <summary>
    /// Command para sincronizar calendário externo do usuário
    /// </summary>
    public record SyncExternalCalendarCommand(
        Guid UserId,
        string Provider, // "google", "outlook", "apple"
        string AccessToken,
        DateTime? SyncFromDate = null,
        DateTime? SyncToDate = null,
        bool ForceFullSync = false
    ) : IRequest<SyncExternalCalendarResponse>;

    /// <summary>
    /// Response da sincronização de calendário externo
    /// </summary>
    public record SyncExternalCalendarResponse(
        Guid UserId,
        string Provider,
        int EventsProcessed,
        int AlarmsCreated,
        int AlarmsUpdated,
        int AlarmsSkipped,
        DateTime SyncedAt,
        DateTime? NextSyncSuggested,
        List<string>? Warnings = null,
        List<CalendarEventInfo>? ProcessedEvents = null
    );

    /// <summary>
    /// Informações de evento do calendário processado
    /// </summary>
    public record CalendarEventInfo(
        string ExternalId,
        string Title,
        DateTime StartTime,
        DateTime? EndTime,
        string Location,
        bool AlarmCreated,
        string ProcessingStatus
    );

    /// <summary>
    /// Validator para o comando de sincronização de calendário
    /// </summary>
    public class SyncExternalCalendarCommandValidator : AbstractValidator<SyncExternalCalendarCommand>
    {
        public SyncExternalCalendarCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("ID do usuário é obrigatório para sincronização do calendário");

            RuleFor(x => x.Provider)
                .NotEmpty()
                .Must(provider => new[] { "google", "outlook", "apple", "caldav" }.Contains(provider.ToLowerInvariant()))
                .WithMessage("Provedor deve ser um dos suportados: Google, Outlook, Apple ou CalDAV");

            RuleFor(x => x.AccessToken)
                .NotEmpty()
                .MinimumLength(10)
                .WithMessage("Token de acesso válido é obrigatório para sincronização");

            RuleFor(x => x.SyncFromDate)
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .When(x => x.SyncFromDate.HasValue)
                .WithMessage("Data de início da sincronização não pode ser no futuro distante");

            RuleFor(x => x.SyncToDate)
                .GreaterThan(x => x.SyncFromDate)
                .When(x => x.SyncFromDate.HasValue && x.SyncToDate.HasValue)
                .WithMessage("Data final deve ser posterior à data inicial");

            RuleFor(x => x.SyncToDate)
                .LessThanOrEqualTo(DateTime.UtcNow.AddYears(2))
                .When(x => x.SyncToDate.HasValue)
                .WithMessage("Data final da sincronização não pode exceder 2 anos no futuro");
        }
    }

    /// <summary>
    /// Handler para sincronização de calendário externo
    /// </summary>
    public class SyncExternalCalendarCommandHandler : IRequestHandler<SyncExternalCalendarCommand, SyncExternalCalendarResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<SyncExternalCalendarCommandHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SyncExternalCalendarCommandValidator _validator;

        public SyncExternalCalendarCommandHandler(
            IUserRepository userRepository,
            IAlarmRepository alarmRepository,
            ILogger<SyncExternalCalendarCommandHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            IHttpClientFactory httpClientFactory)
        {
            _userRepository = userRepository;
            _alarmRepository = alarmRepository;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _httpClientFactory = httpClientFactory;
            _validator = new SyncExternalCalendarCommandValidator();
        }

        public async Task<SyncExternalCalendarResponse> Handle(SyncExternalCalendarCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("SyncExternalCalendarCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ValidationException($"Validação falhou para sincronização de calendário: {errors}");
                }

                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("provider", request.Provider.ToLowerInvariant());
                activity?.SetTag("force_full_sync", request.ForceFullSync.ToString());
                
                _logger.LogInformation("Iniciando sincronização de calendário externo para usuário {UserId} com provedor {Provider} - CorrelationId: {CorrelationId}",
                    request.UserId, request.Provider, _correlationContext.CorrelationId);

                // Verificar se usuário existe
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    _meter.IncrementErrorCount("command_handler", "sync_calendar", "user_not_found");
                    throw new InvalidOperationException($"Usuário {request.UserId} não encontrado");
                }

                // Determinar intervalo de sincronização
                var syncFromDate = request.SyncFromDate ?? DateTime.UtcNow.Date;
                var syncToDate = request.SyncToDate ?? DateTime.UtcNow.Date.AddDays(30);

                activity?.SetTag("sync_from_date", syncFromDate.ToString("yyyy-MM-dd"));
                activity?.SetTag("sync_to_date", syncToDate.ToString("yyyy-MM-dd"));

                // Buscar eventos do calendário externo
                var externalEvents = await FetchExternalCalendarEvents(
                    request.Provider, 
                    request.AccessToken, 
                    syncFromDate, 
                    syncToDate, 
                    cancellationToken);

                _logger.LogInformation("Encontrados {EventCount} eventos no calendário {Provider} para usuário {UserId}",
                    externalEvents.Count, request.Provider, request.UserId);

                // Processar eventos e criar/atualizar alarmes
                var processedEvents = new List<CalendarEventInfo>();
                var warnings = new List<string>();
                int alarmsCreated = 0, alarmsUpdated = 0, alarmsSkipped = 0;

                foreach (var calendarEvent in externalEvents)
                {
                    try
                    {
                        var processResult = await ProcessCalendarEvent(calendarEvent, request.UserId, request.ForceFullSync, cancellationToken);
                        processedEvents.Add(processResult.EventInfo);

                        switch (processResult.Action)
                        {
                            case "created":
                                alarmsCreated++;
                                break;
                            case "updated":
                                alarmsUpdated++;
                                break;
                            case "skipped":
                                alarmsSkipped++;
                                break;
                        }

                        if (!string.IsNullOrEmpty(processResult.Warning))
                        {
                            warnings.Add(processResult.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao processar evento {EventId} do calendário {Provider}",
                            calendarEvent.Id, request.Provider);
                        warnings.Add($"Erro ao processar evento '{calendarEvent.Title}': {ex.Message}");
                        alarmsSkipped++;
                    }
                }

                // Calcular próxima sincronização sugerida
                var nextSyncSuggested = CalculateNextSyncTime(request.Provider, externalEvents.Count, alarmsCreated + alarmsUpdated);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "sync_external_calendar", "success", "completed");

                var response = new SyncExternalCalendarResponse(
                    UserId: request.UserId,
                    Provider: request.Provider,
                    EventsProcessed: externalEvents.Count,
                    AlarmsCreated: alarmsCreated,
                    AlarmsUpdated: alarmsUpdated,
                    AlarmsSkipped: alarmsSkipped,
                    SyncedAt: DateTime.UtcNow,
                    NextSyncSuggested: nextSyncSuggested,
                    Warnings: warnings.Any() ? warnings : null,
                    ProcessedEvents: processedEvents
                );

                _logger.LogInformation("Sincronização de calendário concluída para usuário {UserId}: {EventsProcessed} eventos, {AlarmsCreated} alarmes criados, {AlarmsUpdated} atualizados - CorrelationId: {CorrelationId}",
                    request.UserId, externalEvents.Count, alarmsCreated, alarmsUpdated, _correlationContext.CorrelationId);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "sync_external_calendar", "error", "exception");
                _meter.IncrementErrorCount("command_handler", "sync_calendar", "processing_error");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro na sincronização de calendário para usuário {UserId} com provedor {Provider} - CorrelationId: {CorrelationId}",
                    request.UserId, request.Provider, _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <summary>
        /// Busca eventos do calendário externo via API
        /// </summary>
        private async Task<List<ExternalCalendarEvent>> FetchExternalCalendarEvents(
            string provider, 
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("SyncExternalCalendarCommandHandler.FetchExternalCalendarEvents");
            
            // Simulação de integração com diferentes provedores
            // Na implementação real, cada provedor teria sua própria API
            var events = new List<ExternalCalendarEvent>();

            switch (provider.ToLowerInvariant())
            {
                case "google":
                    events = await FetchGoogleCalendarEvents(accessToken, fromDate, toDate, cancellationToken);
                    break;
                case "outlook":
                    events = await FetchOutlookCalendarEvents(accessToken, fromDate, toDate, cancellationToken);
                    break;
                case "apple":
                    events = await FetchAppleCalendarEvents(accessToken, fromDate, toDate, cancellationToken);
                    break;
                case "caldav":
                    events = await FetchCalDAVEvents(accessToken, fromDate, toDate, cancellationToken);
                    break;
                default:
                    throw new NotSupportedException($"Provedor {provider} não é suportado");
            }

            activity?.SetTag("events_fetched", events.Count.ToString());
            return events;
        }

        /// <summary>
        /// Integração real com Google Calendar API
        /// </summary>
        private async Task<List<ExternalCalendarEvent>> FetchGoogleCalendarEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching Google Calendar events from {FromDate} to {ToDate}", fromDate, toDate);
                
                // Implementação real com Google Calendar API
                var credential = GoogleCredential.FromAccessToken(accessToken);
                var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "SmartAlarm Integration Service"
                });
                
                var request = service.Events.List("primary");
                request.TimeMinDateTimeOffset = fromDate;
                request.TimeMaxDateTimeOffset = toDate;
                request.SingleEvents = true;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                request.MaxResults = 250;
                
                var events = await request.ExecuteAsync();
                
                return events.Items.Select(e => new ExternalCalendarEvent(
                    e.Id,
                    e.Summary ?? "Sem título",
                    e.Start.DateTimeDateTimeOffset?.DateTime ?? DateTime.Parse(e.Start.Date),
                    e.End.DateTimeDateTimeOffset?.DateTime ?? DateTime.Parse(e.End.Date),
                    e.Location ?? "",
                    e.Description ?? ""
                )).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Google Calendar events");
                return new List<ExternalCalendarEvent>(); // Return empty list on error
            }
        }

        private async Task<List<ExternalCalendarEvent>> FetchFromGoogleCalendarAsync(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var timeMin = fromDate.ToString("yyyy-MM-ddTHH:mm:ssK");
                var timeMax = toDate.ToString("yyyy-MM-ddTHH:mm:ssK");
                var url = $"https://www.googleapis.com/calendar/v3/calendars/primary/events" +
                         $"?timeMin={Uri.EscapeDataString(timeMin)}" +
                         $"&timeMax={Uri.EscapeDataString(timeMax)}" +
                         $"&singleEvents=true&orderBy=startTime&maxResults=250";

                _logger.LogDebug("Calling Google Calendar API: {Url}", url);

                var response = await httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Google Calendar API failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                    return new List<ExternalCalendarEvent>();
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(jsonContent);
                var events = new List<ExternalCalendarEvent>();

                if (document.RootElement.TryGetProperty("items", out var itemsElement))
                {
                    foreach (var eventElement in itemsElement.EnumerateArray())
                    {
                        var id = eventElement.GetProperty("id").GetString() ?? "";
                        var summary = eventElement.GetProperty("summary").GetString() ?? "Sem título";
                        
                        var start = eventElement.GetProperty("start");
                        var startDateTime = DateTime.Parse(
                            start.TryGetProperty("dateTime", out var startDateTimeElement) 
                                ? startDateTimeElement.GetString() ?? DateTime.Now.ToString()
                                : start.GetProperty("date").GetString() ?? DateTime.Now.ToString());
                        
                        var end = eventElement.GetProperty("end");
                        var endDateTime = DateTime.Parse(
                            end.TryGetProperty("dateTime", out var endDateTimeElement) 
                                ? endDateTimeElement.GetString() ?? DateTime.Now.ToString()
                                : end.GetProperty("date").GetString() ?? DateTime.Now.ToString());
                        
                        var location = eventElement.TryGetProperty("location", out var locationElement)
                            ? locationElement.GetString() ?? ""
                            : "";
                        
                        var description = eventElement.TryGetProperty("description", out var descriptionElement)
                            ? descriptionElement.GetString() ?? ""
                            : "";

                        events.Add(new ExternalCalendarEvent(id, summary, startDateTime, endDateTime, location, description));
                    }
                }

                _logger.LogInformation("Retrieved {EventCount} events from Google Calendar", events.Count);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Google Calendar API");
                return new List<ExternalCalendarEvent>();
            }
        }

        /// <summary>
        /// Integração real com Microsoft Graph API (Outlook Calendar)
        /// </summary>
        private async Task<List<ExternalCalendarEvent>> FetchOutlookCalendarEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching Outlook Calendar events from {FromDate} to {ToDate}", fromDate, toDate);
                
                // Implementação real estruturada para Microsoft Graph
                return await FetchFromMicrosoftGraphAsync(accessToken, fromDate, toDate, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Outlook Calendar events");
                return new List<ExternalCalendarEvent>(); // Return empty list on error
            }
        }

        private async Task<List<ExternalCalendarEvent>> FetchFromMicrosoftGraphAsync(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var filterQuery = $"start/dateTime ge '{fromDate:O}' and end/dateTime le '{toDate:O}'";
                var url = $"https://graph.microsoft.com/v1.0/me/events?$filter={Uri.EscapeDataString(filterQuery)}&$top=250";

                _logger.LogDebug("Calling Microsoft Graph: {Url}", url);

                var response = await httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Microsoft Graph API failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                    return new List<ExternalCalendarEvent>();
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(jsonContent);
                var events = new List<ExternalCalendarEvent>();

                if (document.RootElement.TryGetProperty("value", out var valueElement))
                {
                    foreach (var eventElement in valueElement.EnumerateArray())
                    {
                        var id = eventElement.GetProperty("id").GetString() ?? "";
                        var subject = eventElement.GetProperty("subject").GetString() ?? "Sem título";
                        
                        var start = eventElement.GetProperty("start");
                        var startDateTime = DateTime.Parse(start.GetProperty("dateTime").GetString() ?? DateTime.Now.ToString());
                        
                        var end = eventElement.GetProperty("end");
                        var endDateTime = DateTime.Parse(end.GetProperty("dateTime").GetString() ?? DateTime.Now.ToString());
                        
                        var location = "";
                        if (eventElement.TryGetProperty("location", out var locationElement) &&
                            locationElement.TryGetProperty("displayName", out var displayNameElement))
                        {
                            location = displayNameElement.GetString() ?? "";
                        }
                        
                        var description = "";
                        if (eventElement.TryGetProperty("body", out var bodyElement) &&
                            bodyElement.TryGetProperty("content", out var contentElement))
                        {
                            description = contentElement.GetString() ?? "";
                        }

                        events.Add(new ExternalCalendarEvent(id, subject, startDateTime, endDateTime, location, description));
                    }
                }

                _logger.LogInformation("Retrieved {EventCount} events from Microsoft Graph", events.Count);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Microsoft Graph API");
                return new List<ExternalCalendarEvent>();
            }
        }

        /// <summary>
        /// Simulação de busca no Apple Calendar
        /// </summary>
        private async Task<List<ExternalCalendarEvent>> FetchAppleCalendarEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            await Task.Delay(120, cancellationToken);

            var mockEvents = new List<ExternalCalendarEvent>
            {
                new("apple_event_1", "Aniversário", fromDate.AddDays(3), fromDate.AddDays(3).AddHours(2), "Casa", "Celebração familiar")
            };

            return mockEvents.Where(e => e.StartTime >= fromDate && e.StartTime <= toDate).ToList();
        }

        /// <summary>
        /// Simulação de busca via CalDAV
        /// </summary>
        private async Task<List<ExternalCalendarEvent>> FetchCalDAVEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken);

            var mockEvents = new List<ExternalCalendarEvent>
            {
                new("caldav_event_1", "Backup de sistemas", fromDate.AddHours(2), fromDate.AddHours(3), "Datacenter", "Manutenção programada")
            };

            return mockEvents.Where(e => e.StartTime >= fromDate && e.StartTime <= toDate).ToList();
        }

        /// <summary>
        /// Processa um evento do calendário e cria/atualiza alarme correspondente
        /// </summary>
        private async Task<EventProcessResult> ProcessCalendarEvent(
            ExternalCalendarEvent calendarEvent, 
            Guid userId, 
            bool forceUpdate, 
            CancellationToken cancellationToken)
        {
            // Verificar se já existe um alarme para este evento
            var existingAlarms = await _alarmRepository.GetByUserIdAsync(userId);
            var existingAlarm = existingAlarms.FirstOrDefault(a => 
                a.Name.ToString().Contains(calendarEvent.Id) ||
                (a.Name.ToString() == calendarEvent.Title && Math.Abs((a.Time - calendarEvent.StartTime).TotalMinutes) < 60));

            string action;
            bool alarmCreated = false;
            string? warning = null;

            if (existingAlarm != null && !forceUpdate)
            {
                action = "skipped";
                warning = $"Alarme já existe para evento '{calendarEvent.Title}'";
            }
            else if (existingAlarm != null && forceUpdate)
            {
                // Atualizar alarme existente
                existingAlarm.UpdateName(new SmartAlarm.Domain.ValueObjects.Name(calendarEvent.Title));
                existingAlarm.UpdateTime(calendarEvent.StartTime.AddMinutes(-15)); // 15 min antes do evento

                await _alarmRepository.UpdateAsync(existingAlarm);
                action = "updated";
                alarmCreated = true;
            }
            else
            {
                // Criar novo alarme
                var newAlarm = new Alarm(
                    id: Guid.NewGuid(),
                    name: calendarEvent.Title,
                    time: calendarEvent.StartTime.AddMinutes(-15), // 15 min antes do evento
                    enabled: true,
                    userId: userId
                );

                await _alarmRepository.AddAsync(newAlarm);
                action = "created";
                alarmCreated = true;
            }

            return new EventProcessResult(
                new CalendarEventInfo(
                    calendarEvent.Id,
                    calendarEvent.Title,
                    calendarEvent.StartTime,
                    calendarEvent.EndTime,
                    calendarEvent.Location ?? "",
                    alarmCreated,
                    action
                ),
                action,
                warning
            );
        }

        /// <summary>
        /// Calcula o próximo horário sugerido para sincronização
        /// </summary>
        private DateTime CalculateNextSyncTime(string provider, int eventsProcessed, int alarmsChanged)
        {
            var baseInterval = provider.ToLowerInvariant() switch
            {
                "google" => TimeSpan.FromHours(4),   // Google tem boa API, sincronizar mais frequentemente
                "outlook" => TimeSpan.FromHours(6), // Outlook moderado
                "apple" => TimeSpan.FromHours(8),   // Apple menos frequente
                "caldav" => TimeSpan.FromHours(12), // CalDAV mais conservador
                _ => TimeSpan.FromHours(6)
            };

            // Ajustar baseado na atividade
            if (eventsProcessed > 10 || alarmsChanged > 5)
            {
                baseInterval = TimeSpan.FromTicks(baseInterval.Ticks / 2); // Mais frequente se muito ativo
            }
            else if (eventsProcessed == 0)
            {
                baseInterval = TimeSpan.FromTicks(baseInterval.Ticks * 2); // Menos frequente se sem atividade
            }

            return DateTime.UtcNow.Add(baseInterval);
        }

        /// <summary>
        /// Resultado do processamento de um evento de calendário
        /// </summary>
        internal record EventProcessResult(CalendarEventInfo EventInfo, string Action, string? Warning = null);
    }

    /// <summary>
    /// Representa um evento de calendário externo
    /// </summary>
    public record ExternalCalendarEvent(
        string Id,
        string Title,
        DateTime StartTime,
        DateTime? EndTime,
        string Location,
        string Description
    );
}
