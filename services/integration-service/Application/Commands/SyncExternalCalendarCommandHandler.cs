using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.IntegrationService.Application.Exceptions;
using SmartAlarm.IntegrationService.Application.Services;
using FluentValidation;
using System.Diagnostics;
using System.Text.Json;
using System.Net;
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
        private readonly ICalendarRetryService _retryService;
        private readonly SyncExternalCalendarCommandValidator _validator;

        public SyncExternalCalendarCommandHandler(
            IUserRepository userRepository,
            IAlarmRepository alarmRepository,
            ILogger<SyncExternalCalendarCommandHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            IHttpClientFactory httpClientFactory,
            ICalendarRetryService retryService)
        {
            _userRepository = userRepository;
            _alarmRepository = alarmRepository;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _httpClientFactory = httpClientFactory;
            _retryService = retryService;
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
                var fetchResult = await FetchExternalCalendarEvents(
                    request.Provider, 
                    request.AccessToken, 
                    syncFromDate, 
                    syncToDate, 
                    cancellationToken);

                // Verificar se houve falha na busca
                if (!fetchResult.IsSuccess)
                {
                    var error = fetchResult.Error!;
                    _logger.LogError("Falha na sincronização de calendário {Provider} para usuário {UserId}: {ErrorMessage}. " +
                                   "É retryável: {IsRetryable}",
                        request.Provider, request.UserId, error.Message, error.IsRetryable);

                    _meter.IncrementErrorCount("command_handler", "sync_calendar", "fetch_failed");

                    // Se for um erro retryável, incluir na resposta como warning
                    // Se for um erro permanente, lançar exceção
                    if (error.IsRetryable)
                    {
                        var fetchWarnings = new List<string> 
                        { 
                            $"Falha temporária na sincronização: {error.Message}. Tente novamente mais tarde." 
                        };

                        return new SyncExternalCalendarResponse(
                            UserId: request.UserId,
                            Provider: request.Provider,
                            EventsProcessed: 0,
                            AlarmsCreated: 0,
                            AlarmsUpdated: 0,
                            AlarmsSkipped: 0,
                            SyncedAt: DateTime.UtcNow,
                            NextSyncSuggested: DateTime.UtcNow.AddMinutes(30), // Retry em 30 minutos
                            Warnings: fetchWarnings
                        );
                    }
                    else
                    {
                        throw new ExternalCalendarPermanentException(
                            request.Provider,
                            $"Falha permanente na sincronização: {error.Message}",
                            innerException: error.OriginalException);
                    }
                }

                var externalEvents = fetchResult.Events;
                
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
        /// Busca eventos do calendário externo via API com tratamento robusto de erros
        /// </summary>
        private async Task<CalendarFetchResult> FetchExternalCalendarEvents(
            string provider, 
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("SyncExternalCalendarCommandHandler.FetchExternalCalendarEvents");
            activity?.SetTag("provider", provider);
            
            try
            {
                var events = await _retryService.ExecuteWithRetryAsync(
                    async (ct) =>
                    {
                        return provider.ToLowerInvariant() switch
                        {
                            "google" => await FetchGoogleCalendarEvents(accessToken, fromDate, toDate, ct),
                            "outlook" => await FetchOutlookCalendarEvents(accessToken, fromDate, toDate, ct),
                            "apple" => await FetchAppleCalendarEvents(accessToken, fromDate, toDate, ct),
                            "caldav" => await FetchCalDAVEvents(accessToken, fromDate, toDate, ct),
                            _ => throw new ExternalCalendarPermanentException(
                                provider, 
                                $"Provedor {provider} não é suportado")
                        };
                    },
                    $"FetchCalendarEvents-{provider}",
                    provider,
                    cancellationToken: cancellationToken);

                activity?.SetTag("events_fetched", events.Count.ToString());
                return CalendarFetchResult.Success(events);
            }
            catch (ExternalCalendarIntegrationException ex)
            {
                var error = new CalendarFetchError(
                    provider,
                    ex.GetType().Name,
                    ex.Message,
                    ex.IsRetryable,
                    DateTime.UtcNow,
                    ex,
                    ex.CalendarId);

                activity?.SetTag("error", ex.Message);
                activity?.SetTag("is_retryable", ex.IsRetryable.ToString());
                
                return CalendarFetchResult.Failure(error);
            }
            catch (Exception ex)
            {
                var error = new CalendarFetchError(
                    provider,
                    "UnexpectedError",
                    ex.Message,
                    false, // Erros não esperados não são retryáveis por padrão
                    DateTime.UtcNow,
                    ex);

                activity?.SetTag("error", ex.Message);
                activity?.SetTag("is_retryable", "false");
                
                return CalendarFetchResult.Failure(error);
            }
        }

        /// <summary>
        /// Integração real com Google Calendar API com tratamento adequado de erros
        /// </summary>
        private async Task<List<ExternalCalendarEvent>> FetchGoogleCalendarEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching Google Calendar events from {FromDate} to {ToDate}", fromDate, toDate);
            
            try
            {
                // Implementação real com Google Calendar API v3
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
                
                var events = await request.ExecuteAsync(cancellationToken);
                
                var calendarEvents = events.Items.Select(e => new ExternalCalendarEvent(
                    e.Id,
                    e.Summary ?? "Sem título",
                    e.Start.DateTimeDateTimeOffset?.DateTime ?? DateTime.Parse(e.Start.Date),
                    e.End.DateTimeDateTimeOffset?.DateTime ?? DateTime.Parse(e.End.Date),
                    e.Location ?? "",
                    e.Description ?? ""
                )).ToList();

                _logger.LogInformation("Successfully synced {EventCount} events from Google Calendar", calendarEvents.Count);
                return calendarEvents;
            }
            catch (Google.GoogleApiException googleEx)
            {
                // Mapear códigos de erro específicos do Google
                var isRetryable = googleEx.HttpStatusCode switch
                {
                    HttpStatusCode.TooManyRequests => true,
                    HttpStatusCode.InternalServerError => true,
                    HttpStatusCode.BadGateway => true,
                    HttpStatusCode.ServiceUnavailable => true,
                    HttpStatusCode.GatewayTimeout => true,
                    HttpStatusCode.Unauthorized => false, // Token inválido não deve ser retryado
                    HttpStatusCode.Forbidden => false,
                    _ => false
                };

                if (isRetryable)
                {
                    throw new ExternalCalendarTemporaryException(
                        "google",
                        $"Google Calendar API temporariamente indisponível: {googleEx.Message}",
                        innerException: googleEx);
                }
                else
                {
                    throw new ExternalCalendarPermanentException(
                        "google",
                        $"Erro permanente do Google Calendar: {googleEx.Message}",
                        innerException: googleEx);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new ExternalCalendarTemporaryException(
                    "google",
                    "Timeout na chamada da API do Google Calendar",
                    innerException: ex);
            }
            catch (HttpRequestException httpEx)
            {
                // Problemas de rede são geralmente temporários
                throw new ExternalCalendarTemporaryException(
                    "google",
                    $"Erro de conectividade com Google Calendar: {httpEx.Message}",
                    innerException: httpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao buscar eventos do Google Calendar");
                throw new ExternalCalendarPermanentException(
                    "google",
                    $"Erro inesperado: {ex.Message}",
                    innerException: ex);
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
        /// Integração real com Microsoft Graph API (Outlook Calendar) com retry policies
        /// </summary>
        /// <summary>
        /// Integração real com Microsoft Graph API (Outlook Calendar) com tratamento adequado de erros
        /// </summary>
        private async Task<List<ExternalCalendarEvent>> FetchOutlookCalendarEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching Outlook Calendar events from {FromDate} to {ToDate}", fromDate, toDate);
            
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("MicrosoftGraph");
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var filterQuery = $"start/dateTime ge '{fromDate:O}' and end/dateTime le '{toDate:O}'";
                var url = $"https://graph.microsoft.com/v1.0/me/events?$filter={Uri.EscapeDataString(filterQuery)}&$top=250";

                _logger.LogDebug("Calling Microsoft Graph: {Url}", url);

                var response = await httpClient.GetAsync(url, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                    // Mapear códigos de erro específicos do Microsoft Graph
                    var isRetryable = response.StatusCode switch
                    {
                        HttpStatusCode.TooManyRequests => true,
                        HttpStatusCode.InternalServerError => true,
                        HttpStatusCode.BadGateway => true,
                        HttpStatusCode.ServiceUnavailable => true,
                        HttpStatusCode.GatewayTimeout => true,
                        HttpStatusCode.Unauthorized => false, // Token inválido não deve ser retryado
                        HttpStatusCode.Forbidden => false,
                        _ => false
                    };

                    if (isRetryable)
                    {
                        throw new ExternalCalendarTemporaryException(
                            "outlook",
                            $"Microsoft Graph API temporariamente indisponível (HTTP {response.StatusCode}): {errorContent}");
                    }
                    else
                    {
                        throw new ExternalCalendarPermanentException(
                            "outlook",
                            $"Erro permanente do Microsoft Graph (HTTP {response.StatusCode}): {errorContent}");
                    }
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
                        
                        // Parse start/end times from Outlook format
                        var startDateTime = DateTime.UtcNow;
                        var endDateTime = DateTime.UtcNow.AddHours(1);
                        
                        if (eventElement.TryGetProperty("start", out var startElement) &&
                            startElement.TryGetProperty("dateTime", out var startDateElement))
                        {
                            DateTime.TryParse(startDateElement.GetString(), out startDateTime);
                        }
                        
                        if (eventElement.TryGetProperty("end", out var endElement) &&
                            endElement.TryGetProperty("dateTime", out var endDateElement))
                        {
                            DateTime.TryParse(endDateElement.GetString(), out endDateTime);
                        }
                        
                        var location = "";
                        if (eventElement.TryGetProperty("location", out var locationElement) &&
                            locationElement.TryGetProperty("displayName", out var locationNameElement))
                        {
                            location = locationNameElement.GetString() ?? "";
                        }
                        
                        var description = "";
                        if (eventElement.TryGetProperty("bodyPreview", out var bodyElement))
                        {
                            description = bodyElement.GetString() ?? "";
                        }

                        events.Add(new ExternalCalendarEvent(id, subject, startDateTime, endDateTime, location, description));
                    }
                }

                _logger.LogInformation("Successfully synced {EventCount} events from Outlook Calendar", events.Count);
                return events;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new ExternalCalendarTemporaryException(
                    "outlook",
                    "Timeout na chamada da API do Microsoft Graph",
                    innerException: ex);
            }
            catch (HttpRequestException httpEx)
            {
                // Problemas de rede são geralmente temporários
                throw new ExternalCalendarTemporaryException(
                    "outlook",
                    $"Erro de conectividade com Microsoft Graph: {httpEx.Message}",
                    innerException: httpEx);
            }
            catch (JsonException jsonEx)
            {
                // Problemas de parsing podem indicar mudança na API
                throw new ExternalCalendarPermanentException(
                    "outlook",
                    $"Erro ao interpretar resposta do Microsoft Graph: {jsonEx.Message}",
                    innerException: jsonEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao buscar eventos do Outlook Calendar");
                throw new ExternalCalendarPermanentException(
                    "outlook",
                    $"Erro inesperado: {ex.Message}",
                    innerException: ex);
            }
        }

        private async Task<List<ExternalCalendarEvent>> FetchFromMicrosoftGraphAsync(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            var retryCount = 0;
            const int maxRetries = 3;
            
            while (retryCount <= maxRetries)
            {
                try
                {
                    using var httpClient = _httpClientFactory.CreateClient("MicrosoftGraph");
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    var filterQuery = $"start/dateTime ge '{fromDate:O}' and end/dateTime le '{toDate:O}'";
                    var url = $"https://graph.microsoft.com/v1.0/me/events?$filter={Uri.EscapeDataString(filterQuery)}&$top=250";

                    _logger.LogDebug("Calling Microsoft Graph: {Url}", url);

                    var response = await httpClient.GetAsync(url, cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        
                        // Check for retryable HTTP status codes
                        if (IsRetryableHttpStatusCode(response.StatusCode) && retryCount < maxRetries)
                        {
                            retryCount++;
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                            
                            _logger.LogWarning("Microsoft Graph API call failed with status {StatusCode} (attempt {RetryCount}/{MaxRetries}). Retrying in {Delay}s: {Error}", 
                                response.StatusCode, retryCount, maxRetries, delay.TotalSeconds, errorContent);
                            
                            await Task.Delay(delay, cancellationToken);
                            continue;
                        }
                        
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
                catch (Exception ex) when (retryCount < maxRetries && IsRetryableError(ex))
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    
                    _logger.LogWarning(ex, "Microsoft Graph API call failed (attempt {RetryCount}/{MaxRetries}). Retrying in {Delay}s", 
                        retryCount, maxRetries, delay.TotalSeconds);
                    
                    await Task.Delay(delay, cancellationToken);
                }
            }
            
            throw new ExternalServiceException("Microsoft Graph", "MAX_RETRIES_EXCEEDED", "Microsoft Graph API failed after all retry attempts");
        }

        /// <summary>
        /// Determina se um status HTTP é elegível para retry
        /// </summary>
        private bool IsRetryableHttpStatusCode(System.Net.HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.TooManyRequests => true, // 429
                System.Net.HttpStatusCode.InternalServerError => true, // 500
                System.Net.HttpStatusCode.BadGateway => true, // 502
                System.Net.HttpStatusCode.ServiceUnavailable => true, // 503
                System.Net.HttpStatusCode.GatewayTimeout => true, // 504
                _ => false
            };
        }

        /// <summary>
        /// Integração real com Apple Calendar via CloudKit API
        /// <summary>
        /// Integração com Apple Calendar - Em desenvolvimento
        /// </summary>
        private Task<List<ExternalCalendarEvent>> FetchAppleCalendarEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            _logger.LogWarning("Apple Calendar integration não está completamente implementada");
            
            // Por enquanto, retornar uma exceção informativa ao invés de falha silenciosa
            throw new ExternalCalendarPermanentException(
                "apple",
                "Integração com Apple Calendar ainda não está implementada. " +
                "Esta funcionalidade está em desenvolvimento e será disponibilizada em uma versão futura.");
        }

        private async Task<List<ExternalCalendarEvent>> FetchFromAppleCloudKitAsync(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("AppleCloudKit");
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Apple CloudKit Web Services API para EventKit
                var queryData = new
                {
                    query = new
                    {
                        recordType = "CalendarEvent",
                        filterBy = new[]
                        {
                            new {
                                fieldName = "startDate",
                                fieldValue = new { value = fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ"), type = "TIMESTAMP" },
                                comparator = "GREATER_THAN_OR_EQUALS"
                            },
                            new {
                                fieldName = "endDate", 
                                fieldValue = new { value = toDate.ToString("yyyy-MM-ddTHH:mm:ssZ"), type = "TIMESTAMP" },
                                comparator = "LESS_THAN_OR_EQUALS"
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(queryData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _logger.LogDebug("Calling Apple CloudKit API with query: {Query}", json);

                var response = await httpClient.PostAsync("https://api.apple-cloudkit.com/database/1/_defaultZone/records/query", content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Apple CloudKit API failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                    return new List<ExternalCalendarEvent>();
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(jsonContent);
                var events = new List<ExternalCalendarEvent>();

                if (document.RootElement.TryGetProperty("records", out var recordsElement))
                {
                    foreach (var recordElement in recordsElement.EnumerateArray())
                    {
                        var fields = recordElement.GetProperty("fields");
                        
                        var recordName = recordElement.GetProperty("recordName").GetString() ?? "";
                        var title = fields.TryGetProperty("title", out var titleElement) 
                            ? titleElement.GetProperty("value").GetString() ?? "Sem título"
                            : "Evento sem título";
                        
                        var startTime = fields.TryGetProperty("startDate", out var startElement)
                            ? DateTime.Parse(startElement.GetProperty("value").GetString() ?? DateTime.Now.ToString())
                            : DateTime.Now;
                        
                        var endTime = fields.TryGetProperty("endDate", out var endElement)
                            ? DateTime.Parse(endElement.GetProperty("value").GetString() ?? DateTime.Now.ToString())
                            : startTime.AddHours(1);
                        
                        var location = fields.TryGetProperty("location", out var locationElement)
                            ? locationElement.GetProperty("value").GetString() ?? ""
                            : "";
                        
                        var notes = fields.TryGetProperty("notes", out var notesElement)
                            ? notesElement.GetProperty("value").GetString() ?? ""
                            : "";

                        events.Add(new ExternalCalendarEvent(recordName, title, startTime, endTime, location, notes));
                    }
                }

                _logger.LogInformation("Retrieved {EventCount} events from Apple CloudKit", events.Count);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Apple CloudKit API");
                throw new ExternalServiceException("Apple CloudKit API call failed", ex);
            }
        }

        /// <summary>
        /// Integração real com servidores CalDAV (RFC 4791)
        /// </summary>
        /// <summary>
        /// Integração com CalDAV - Em desenvolvimento
        /// </summary>
        private Task<List<ExternalCalendarEvent>> FetchCalDAVEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            _logger.LogWarning("CalDAV integration não está completamente implementada");
            
            // Por enquanto, retornar uma exceção informativa ao invés de falha silenciosa
            throw new ExternalCalendarPermanentException(
                "caldav",
                "Integração com CalDAV ainda não está implementada. " +
                "Esta funcionalidade está em desenvolvimento e será disponibilizada em uma versão futura.");
        }

        private async Task<List<ExternalCalendarEvent>> FetchFromCalDAVServerAsync(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("CalDAV");
                
                // CalDAV usa Basic Auth ou Bearer token dependendo do servidor
                if (accessToken.Contains(":"))
                {
                    // Basic Auth para servidores como Nextcloud, ownCloud
                    var authBytes = System.Text.Encoding.ASCII.GetBytes(accessToken);
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
                }
                else
                {
                    // Bearer token para servidores modernos
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }

                // CalDAV REPORT query para buscar eventos em intervalo específico
                var calendarQuery = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<C:calendar-query xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav"">
  <D:prop>
    <D:getetag/>
    <C:calendar-data/>
  </D:prop>
  <C:filter>
    <C:comp-filter name=""VCALENDAR"">
      <C:comp-filter name=""VEVENT"">
        <C:time-range start=""{fromDate:yyyyMMddTHHmmssZ}"" end=""{toDate:yyyyMMddTHHmmssZ}""/>
      </C:comp-filter>
    </C:comp-filter>
  </C:filter>
</C:calendar-query>";

                var content = new StringContent(calendarQuery, System.Text.Encoding.UTF8, "application/xml");
                
                _logger.LogDebug("Sending CalDAV REPORT query to calendar server");

                // Assumindo que o endpoint CalDAV está configurado via appsettings
                var calDAVEndpoint = httpClient.BaseAddress ?? new Uri("https://calendar.example.com/remote.php/dav/calendars/username/personal/");
                
                var request = new HttpRequestMessage(HttpMethod.Post, calDAVEndpoint)
                {
                    Content = content
                };
                request.Headers.Add("Depth", "1");
                request.Method = new HttpMethod("REPORT");

                var response = await httpClient.SendAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("CalDAV REPORT failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                    return new List<ExternalCalendarEvent>();
                }

                var xmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var events = ParseCalDAVResponse(xmlContent);

                _logger.LogInformation("Retrieved {EventCount} events from CalDAV server", events.Count);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CalDAV server");
                throw new ExternalServiceException("CalDAV", "CALDAV_REQUEST_ERROR", "CalDAV server request failed", ex);
            }
        }

        /// <summary>
        /// Analisa a resposta XML do CalDAV e extrai eventos
        /// </summary>
        private List<ExternalCalendarEvent> ParseCalDAVResponse(string xmlContent)
        {
            var events = new List<ExternalCalendarEvent>();

            try
            {
                using var xmlReader = System.Xml.XmlReader.Create(new StringReader(xmlContent));
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                var namespaceManager = new System.Xml.XmlNamespaceManager(xmlDoc.NameTable);
                namespaceManager.AddNamespace("D", "DAV:");
                namespaceManager.AddNamespace("C", "urn:ietf:params:xml:ns:caldav");

                var responseNodes = xmlDoc.SelectNodes("//D:response", namespaceManager);
                if (responseNodes == null) return events;

                foreach (System.Xml.XmlNode responseNode in responseNodes)
                {
                    var calendarDataNode = responseNode.SelectSingleNode(".//C:calendar-data", namespaceManager);
                    if (calendarDataNode?.InnerText == null) continue;

                    var icalData = calendarDataNode.InnerText;
                    var parsedEvent = ParseICalendarEvent(icalData);
                    if (parsedEvent != null)
                    {
                        events.Add(parsedEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CalDAV XML response");
            }

            return events;
        }

        /// <summary>
        /// Analisa dados iCalendar (RFC 5545) e extrai evento
        /// </summary>
        private ExternalCalendarEvent? ParseICalendarEvent(string icalData)
        {
            try
            {
                var lines = icalData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                string? uid = null, summary = null, location = null, description = null;
                DateTime? dtStart = null, dtEnd = null;

                foreach (var line in lines)
                {
                    var cleanLine = line.Trim();
                    
                    if (cleanLine.StartsWith("UID:"))
                        uid = cleanLine.Substring(4);
                    else if (cleanLine.StartsWith("SUMMARY:"))
                        summary = cleanLine.Substring(8);
                    else if (cleanLine.StartsWith("LOCATION:"))
                        location = cleanLine.Substring(9);
                    else if (cleanLine.StartsWith("DESCRIPTION:"))
                        description = cleanLine.Substring(12);
                    else if (cleanLine.StartsWith("DTSTART"))
                    {
                        var dateValue = ExtractDateTimeFromICalLine(cleanLine);
                        if (dateValue.HasValue) dtStart = dateValue.Value;
                    }
                    else if (cleanLine.StartsWith("DTEND"))
                    {
                        var dateValue = ExtractDateTimeFromICalLine(cleanLine);
                        if (dateValue.HasValue) dtEnd = dateValue.Value;
                    }
                }

                if (uid != null && summary != null && dtStart.HasValue)
                {
                    return new ExternalCalendarEvent(
                        uid, 
                        summary, 
                        dtStart.Value, 
                        dtEnd, 
                        location ?? "", 
                        description ?? ""
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing individual iCalendar event");
            }

            return null;
        }

        /// <summary>
        /// Extrai DateTime de linha iCalendar (DTSTART/DTEND)
        /// </summary>
        private DateTime? ExtractDateTimeFromICalLine(string icalLine)
        {
            try
            {
                var colonIndex = icalLine.IndexOf(':');
                if (colonIndex == -1) return null;

                var dateTimeString = icalLine.Substring(colonIndex + 1);
                
                // Formato típico: 20240720T140000Z ou 20240720T140000
                if (dateTimeString.Length >= 15)
                {
                    var year = int.Parse(dateTimeString.Substring(0, 4));
                    var month = int.Parse(dateTimeString.Substring(4, 2));
                    var day = int.Parse(dateTimeString.Substring(6, 2));
                    var hour = int.Parse(dateTimeString.Substring(9, 2));
                    var minute = int.Parse(dateTimeString.Substring(11, 2));
                    var second = int.Parse(dateTimeString.Substring(13, 2));

                    var dateTime = new DateTime(year, month, day, hour, minute, second);
                    
                    // Se termina com Z, é UTC
                    return dateTimeString.EndsWith('Z') ? 
                        DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : 
                        dateTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing iCalendar date/time: {DateTimeString}", icalLine);
            }

            return null;
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
        /// Determina se uma exceção é elegível para retry
        /// </summary>
        private bool IsRetryableError(Exception ex)
        {
            return ex switch
            {
                HttpRequestException httpEx => true, // Network issues
                TaskCanceledException timeoutEx when !timeoutEx.CancellationToken.IsCancellationRequested => true, // Timeout
                Google.GoogleApiException googleEx => googleEx.HttpStatusCode switch
                {
                    System.Net.HttpStatusCode.TooManyRequests => true, // Rate limiting
                    System.Net.HttpStatusCode.InternalServerError => true, // 500
                    System.Net.HttpStatusCode.BadGateway => true, // 502
                    System.Net.HttpStatusCode.ServiceUnavailable => true, // 503
                    System.Net.HttpStatusCode.GatewayTimeout => true, // 504
                    _ => false
                },
                Microsoft.Graph.ServiceException msGraphEx when msGraphEx.Message.Contains("429") => true, // Rate limiting
                Microsoft.Graph.ServiceException msGraphEx when msGraphEx.Message.Contains("500") => true, // Internal server error
                Microsoft.Graph.ServiceException msGraphEx when msGraphEx.Message.Contains("502") => true, // Bad gateway
                Microsoft.Graph.ServiceException msGraphEx when msGraphEx.Message.Contains("503") => true, // Service unavailable
                Microsoft.Graph.ServiceException msGraphEx when msGraphEx.Message.Contains("504") => true, // Gateway timeout
                _ => false
            };
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
