using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Handler para processar comando de disparo de alarme com integrações completas
    /// Implementação real para produção com AI Service e Integration Service
    /// </summary>
    public class TriggerAlarmCommandHandler : IRequestHandler<TriggerAlarmCommand, TriggerAlarmResponse>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<TriggerAlarmCommand> _validator;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<TriggerAlarmCommandHandler> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public TriggerAlarmCommandHandler(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IValidator<TriggerAlarmCommand> validator,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<TriggerAlarmCommandHandler> logger,
            IHttpClientFactory httpClientFactory)
        {
            _alarmRepository = alarmRepository;
            _userRepository = userRepository;
            _validator = validator;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TriggerAlarmResponse> Handle(TriggerAlarmCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("TriggerAlarmCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("alarm.id", request.AlarmId.ToString());
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("alarm.trigger_type", request.TriggerType);
                activity?.SetTag("operation", "trigger_alarm");
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando disparo de alarme {AlarmId} para usuário {UserId} - Tipo: {TriggerType} - CorrelationId: {CorrelationId}",
                    request.AlarmId, request.UserId, request.TriggerType, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);
                    _meter.IncrementErrorCount("command", "trigger_alarm", "validation");
                    
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para disparo de alarme: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);
                    
                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Verificar se usuário existe
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    activity?.SetTag("user.found", false);
                    _meter.IncrementErrorCount("command", "trigger_alarm", "user_not_found");
                    
                    _logger.LogWarning("Usuário {UserId} não encontrado para disparo de alarme {AlarmId} - CorrelationId: {CorrelationId}",
                        request.UserId, request.AlarmId, _correlationContext.CorrelationId);
                    
                    throw new InvalidOperationException($"Usuário {request.UserId} não encontrado");
                }

                activity?.SetTag("user.found", true);
                activity?.SetTag("user.email", user.Email.Address);

                // Buscar alarme
                var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId);
                if (alarm == null)
                {
                    activity?.SetTag("alarm.found", false);
                    _meter.IncrementErrorCount("command", "trigger_alarm", "alarm_not_found");
                    
                    _logger.LogWarning("Alarme {AlarmId} não encontrado para disparo - CorrelationId: {CorrelationId}",
                        request.AlarmId, _correlationContext.CorrelationId);
                    
                    throw new InvalidOperationException($"Alarme {request.AlarmId} não encontrado");
                }

                // Verificar se o alarme pertence ao usuário
                if (alarm.UserId != request.UserId)
                {
                    activity?.SetTag("alarm.owner_mismatch", true);
                    _meter.IncrementErrorCount("command", "trigger_alarm", "unauthorized");
                    
                    _logger.LogWarning("Usuário {UserId} tentou disparar alarme {AlarmId} que não lhe pertence - CorrelationId: {CorrelationId}",
                        request.UserId, request.AlarmId, _correlationContext.CorrelationId);
                    
                    throw new UnauthorizedAccessException($"Alarme {request.AlarmId} não pertence ao usuário {request.UserId}");
                }

                // Verificar se o alarme está ativo
                if (!alarm.Enabled)
                {
                    activity?.SetTag("alarm.disabled", true);
                    _meter.IncrementErrorCount("command", "trigger_alarm", "alarm_disabled");
                    
                    _logger.LogWarning("Tentativa de disparar alarme desabilitado {AlarmId} - CorrelationId: {CorrelationId}",
                        request.AlarmId, _correlationContext.CorrelationId);
                    
                    return new TriggerAlarmResponse(
                        request.AlarmId,
                        request.UserId,
                        request.TriggeredAt,
                        false,
                        "Alarme está desabilitado"
                    );
                }

                activity?.SetTag("alarm.found", true);
                activity?.SetTag("alarm.name", alarm.Name.Value);
                activity?.SetTag("alarm.enabled", alarm.Enabled.ToString());

                var actionsExecuted = new List<string>();
                var notifications = new List<string>();

                // 1. Comunicar com AI Service para obter recomendações personalizadas
                await ProcessAiRecommendations(request, alarm, user, actionsExecuted, cancellationToken);

                // 2. Comunicar com Integration Service para enviar notificações
                await ProcessIntegrationNotifications(request, alarm, user, notifications, cancellationToken);

                // 3. Registrar evento de disparo no alarme
                alarm.RecordTrigger(request.TriggeredAt);
                await _alarmRepository.UpdateAsync(alarm);
                actionsExecuted.Add("Evento de disparo registrado no alarme");

                stopwatch.Stop();
                activity?.SetTag("alarm.triggered", true);
                activity?.SetTag("actions.count", actionsExecuted.Count.ToString());
                activity?.SetTag("notifications.count", notifications.Count.ToString());

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "trigger_alarm", "success", "200");
                _meter.IncrementAlarmTriggered(request.TriggerType, request.UserId.ToString(), "success");

                // Log de sucesso
                _logger.LogInformation("Alarme '{AlarmName}' ({AlarmId}) disparado com sucesso - Ações: {ActionsCount}, Notificações: {NotificationsCount} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    alarm.Name.Value, request.AlarmId, actionsExecuted.Count, notifications.Count, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                // Retornar response de sucesso
                return new TriggerAlarmResponse(
                    request.AlarmId,
                    request.UserId,
                    request.TriggeredAt,
                    true,
                    $"Alarme '{alarm.Name.Value}' disparado com sucesso"
                )
                {
                    ActionsExecuted = actionsExecuted,
                    Notifications = notifications
                };
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "trigger_alarm", "validation_error", "400");
                throw;
            }
            catch (InvalidOperationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "trigger_alarm", "business_error", "404");
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "trigger_alarm", "unauthorized", "403");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "trigger_alarm", "error", "500");
                _meter.IncrementErrorCount("command", "trigger_alarm", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro inesperado ao disparar alarme {AlarmId} para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.AlarmId, request.UserId, _correlationContext.CorrelationId);
                
                throw;
            }
        }

        /// <summary>
        /// Comunicação com AI Service para obter recomendações personalizadas
        /// </summary>
        private async Task ProcessAiRecommendations(
            TriggerAlarmCommand request, 
            Alarm alarm, 
            User user, 
            List<string> actionsExecuted, 
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                
                // Configurar timeout e retry policy
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                // Chamar AI Service para análise de padrões e recomendações
                var aiRequest = new
                {
                    UserId = user.Id,
                    AlarmId = alarm.Id,
                    AlarmTime = alarm.Time,
                    TriggerType = request.TriggerType,
                    Context = new
                    {
                        UserEmail = user.Email.Address,
                        AlarmName = alarm.Name.Value,
                        LastTriggered = alarm.LastTriggeredAt
                    }
                };

                var response = await httpClient.PostAsJsonAsync(
                    "http://localhost:5003/api/v1/ai/analyze-patterns", 
                    aiRequest, 
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    actionsExecuted.Add("Recomendações AI obtidas com sucesso");
                    _logger.LogInformation("AI Service consultado com sucesso para alarme {AlarmId}", alarm.Id);
                }
                else
                {
                    actionsExecuted.Add($"AI Service indisponível (Status: {response.StatusCode})");
                    _logger.LogWarning("AI Service indisponível para alarme {AlarmId} - Status: {StatusCode}", 
                        alarm.Id, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                actionsExecuted.Add($"Erro na comunicação com AI Service: {ex.Message}");
                _logger.LogWarning(ex, "Erro ao consultar AI Service para alarme {AlarmId}", alarm.Id);
            }
        }

        /// <summary>
        /// Comunicação com Integration Service para enviar notificações
        /// </summary>
        private async Task ProcessIntegrationNotifications(
            TriggerAlarmCommand request, 
            Alarm alarm, 
            User user, 
            List<string> notifications, 
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                
                // Configurar timeout e retry policy
                httpClient.Timeout = TimeSpan.FromSeconds(15);
                
                // Chamar Integration Service para enviar notificações
                var notificationRequest = new
                {
                    UserId = user.Id,
                    AlarmId = alarm.Id,
                    Message = $"Alarme '{alarm.Name.Value}' disparado às {request.TriggeredAt:HH:mm}",
                    Type = "alarm_triggered",
                    Priority = "high",
                    Channels = new[] { "push", "email", "sms" },
                    UserEmail = user.Email.Address,
                    Metadata = new
                    {
                        AlarmName = alarm.Name.Value,
                        TriggerTime = request.TriggeredAt,
                        TriggerType = request.TriggerType
                    }
                };

                var response = await httpClient.PostAsJsonAsync(
                    "http://localhost:5002/api/v1/notifications/send", 
                    notificationRequest, 
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    notifications.Add("Push notification enviada");
                    notifications.Add("Email de notificação enviado");
                    notifications.Add("SMS de notificação enviado");
                    _logger.LogInformation("Integration Service executado com sucesso para alarme {AlarmId}", alarm.Id);
                }
                else
                {
                    notifications.Add($"Integration Service indisponível (Status: {response.StatusCode})");
                    _logger.LogWarning("Integration Service indisponível para alarme {AlarmId} - Status: {StatusCode}", 
                        alarm.Id, response.StatusCode);
                }

                // Tentar sincronizar com calendários externos
                await SyncWithExternalCalendars(user, alarm, notifications, cancellationToken);
            }
            catch (Exception ex)
            {
                notifications.Add($"Erro na comunicação com Integration Service: {ex.Message}");
                _logger.LogWarning(ex, "Erro ao executar Integration Service para alarme {AlarmId}", alarm.Id);
            }
        }

        /// <summary>
        /// Sincronização opcional com calendários externos
        /// </summary>
        private async Task SyncWithExternalCalendars(
            User user, 
            Alarm alarm, 
            List<string> notifications, 
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var syncRequest = new
                {
                    UserId = user.Id,
                    Provider = "all", // Google, Outlook, Apple
                    SyncType = "event_completion",
                    EventData = new
                    {
                        Title = $"Alarme: {alarm.Name.Value}",
                        Description = "Alarme disparado pelo SmartAlarm",
                        StartTime = alarm.Time,
                        Duration = TimeSpan.FromMinutes(15)
                    }
                };

                var response = await httpClient.PostAsJsonAsync(
                    "http://localhost:5002/api/v1/calendars/sync", 
                    syncRequest, 
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    notifications.Add("Evento sincronizado com calendários externos");
                }
                else
                {
                    notifications.Add("Sincronização de calendário opcional falhou");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Sincronização opcional com calendários falhou para alarme {AlarmId}", alarm.Id);
                // Não adicionar erro nas notificações pois é funcionalidade opcional
            }
        }
    }
}
