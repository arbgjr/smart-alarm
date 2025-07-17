using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;
using System.Text.Json;

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
        /// Simulação de busca no Google Calendar
        /// </summary>
        private async Task<List<ExternalCalendarEvent>> FetchGoogleCalendarEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            // Simulação de eventos do Google Calendar
            // Na implementação real, faria chamada para Google Calendar API
            await Task.Delay(100, cancellationToken); // Simular latência de rede

            var mockEvents = new List<ExternalCalendarEvent>
            {
                new("google_event_1", "Reunião de trabalho", fromDate.AddHours(9), fromDate.AddHours(10), "Escritório", "Reunião importante"),
                new("google_event_2", "Consulta médica", fromDate.AddDays(1).AddHours(14), fromDate.AddDays(1).AddHours(15), "Hospital", "Checkup anual"),
                new("google_event_3", "Academia", fromDate.AddDays(2).AddHours(18), fromDate.AddDays(2).AddHours(19), "Gym", "Treino de força")
            };

            return mockEvents.Where(e => e.StartTime >= fromDate && e.StartTime <= toDate).ToList();
        }

        /// <summary>
        /// Simulação de busca no Outlook Calendar
        /// </summary>
        private async Task<List<ExternalCalendarEvent>> FetchOutlookCalendarEvents(
            string accessToken, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken)
        {
            await Task.Delay(150, cancellationToken);

            var mockEvents = new List<ExternalCalendarEvent>
            {
                new("outlook_event_1", "Apresentação do projeto", fromDate.AddHours(10), fromDate.AddHours(12), "Sala de conferências", "Projeto Q4"),
                new("outlook_event_2", "Almoço com cliente", fromDate.AddDays(1).AddHours(12), fromDate.AddDays(1).AddHours(14), "Restaurante", "Discussão de contrato")
            };

            return mockEvents.Where(e => e.StartTime >= fromDate && e.StartTime <= toDate).ToList();
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
        /// Resultado do processamento de um evento
        /// </summary>
        private record EventProcessResult(CalendarEventInfo EventInfo, string Action, string? Warning = null);
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
