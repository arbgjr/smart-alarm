using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using Hangfire;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace SmartAlarm.AlarmService.Infrastructure.DistributedProcessing
{
    /// <summary>
    /// Implementação do processador distribuído de alarmes usando Hangfire
    /// </summary>
    public class DistributedAlarmProcessor : IDistributedAlarmProcessor
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<DistributedAlarmProcessor> _logger;

        // Cache em memória para status de processamento (em produção, usar Redis)
        private readonly ConcurrentDictionary<Guid, AlarmProcessingInfo> _processingCache = new();
        private readonly ConcurrentDictionary<string, Guid> _jobIdToAlarmId = new();

        public DistributedAlarmProcessor(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<DistributedAlarmProcessor> logger)
        {
            _alarmRepository = alarmRepository;
            _userRepository = userRepository;
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<AlarmProcessingResult> ProcessAlarmAsync(
            Guid alarmId,
            Guid userId,
            string triggerType = "scheduled",
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("DistributedAlarmProcessor.ProcessAlarm");
            var stopwatch = Stopwatch.StartNew();
            var queueStartTime = DateTime.UtcNow;

            try
            {
                // Activity tags
                activity?.SetTag("alarm.id", alarmId.ToString());
                activity?.SetTag("user.id", userId.ToString());
                activity?.SetTag("trigger.type", triggerType);
                activity?.SetTag("processing.node", Environment.MachineName);
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando processamento distribuído do alarme {AlarmId} para usuário {UserId} - Tipo: {TriggerType} - CorrelationId: {CorrelationId}",
                    alarmId, userId, triggerType, _correlationContext.CorrelationId);

                // Atualizar status para processando
                UpdateProcessingStatus(alarmId, userId, AlarmProcessingStatus.Processing, queueStartTime);

                // Buscar alarme
                var alarm = await _alarmRepository.GetByIdAsync(alarmId, cancellationToken);
                if (alarm == null)
                {
                    var errorMsg = $"Alarme {alarmId} não encontrado";
                    _logger.LogWarning(errorMsg);

                    UpdateProcessingStatus(alarmId, userId, AlarmProcessingStatus.Failed, queueStartTime, errorMsg);

                    return new AlarmProcessingResult(
                        alarmId, userId, false, errorMsg, DateTime.UtcNow, stopwatch.Elapsed,
                        Array.Empty<string>(), Array.Empty<string>(),
                        CreateMetrics(queueStartTime, stopwatch.Elapsed, 0)
                    );
                }

                // Verificar se alarme está ativo
                if (!alarm.Enabled)
                {
                    var errorMsg = $"Alarme {alarmId} está desabilitado";
                    _logger.LogInformation(errorMsg);

                    UpdateProcessingStatus(alarmId, userId, AlarmProcessingStatus.Completed, queueStartTime);

                    return new AlarmProcessingResult(
                        alarmId, userId, true, errorMsg, DateTime.UtcNow, stopwatch.Elapsed,
                        Array.Empty<string>(), Array.Empty<string>(),
                        CreateMetrics(queueStartTime, stopwatch.Elapsed, 0)
                    );
                }

                activity?.SetTag("alarm.name", alarm.Name);
                activity?.SetTag("alarm.enabled", alarm.Enabled.ToString());

                var executionStartTime = DateTime.UtcNow;
                var actionsExecuted = new List<string>();
                var notifications = new List<string>();

                // Simular processamento de ações do alarme
                actionsExecuted.AddRange(await ExecuteAlarmActionsAsync(alarm, triggerType, cancellationToken));

                // Simular envio de notificações
                notifications.AddRange(await SendAlarmNotificationsAsync(alarm, userId, triggerType, cancellationToken));

                // Registrar histórico do alarme
                await RecordAlarmHistoryAsync(alarm, userId, triggerType, actionsExecuted, notifications, cancellationToken);

                // Agendar próxima execução se for recorrente
                await ScheduleNextRecurrenceAsync(alarm, cancellationToken);

                stopwatch.Stop();

                // Atualizar status para concluído
                UpdateProcessingStatus(alarmId, userId, AlarmProcessingStatus.Completed, queueStartTime);

                // Métricas de sucesso
                _meter.IncrementAlarmTriggered(triggerType, userId.ToString(), "success");
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "process_alarm", triggerType, "success");

                var result = new AlarmProcessingResult(
                    alarmId, userId, true,
                    $"Alarme '{alarm.Name}' processado com sucesso",
                    DateTime.UtcNow, stopwatch.Elapsed,
                    actionsExecuted, notifications,
                    CreateMetrics(queueStartTime, stopwatch.Elapsed, 0)
                );

                _logger.LogInformation("Processamento do alarme {AlarmId} concluído com sucesso - Ações: {ActionsCount}, Notificações: {NotificationsCount} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    alarmId, actionsExecuted.Count, notifications.Count, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Atualizar status para falha
                UpdateProcessingStatus(alarmId, userId, AlarmProcessingStatus.Failed, queueStartTime, ex.Message);

                // Métricas de erro
                _meter.IncrementAlarmTriggered(triggerType, userId.ToString(), "failed");
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "process_alarm", triggerType, "error");
                _meter.IncrementErrorCount("distributed_processor", "process_alarm", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro no processamento distribuído do alarme {AlarmId} - CorrelationId: {CorrelationId}",
                    alarmId, _correlationContext.CorrelationId);

                // Implementar retry logic
                await ScheduleRetryIfNeededAsync(alarmId, userId, triggerType, ex, cancellationToken);

                return new AlarmProcessingResult(
                    alarmId, userId, false, $"Erro no processamento: {ex.Message}",
                    DateTime.UtcNow, stopwatch.Elapsed,
                    Array.Empty<string>(), Array.Empty<string>(),
                    CreateMetrics(queueStartTime, stopwatch.Elapsed, 0)
                );
            }
        }

        /// <inheritdoc />
        public async Task<string> ScheduleAlarmProcessingAsync(
            Guid alarmId,
            DateTime scheduledTime,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Agendando processamento do alarme {AlarmId} para {ScheduledTime}",
                    alarmId, scheduledTime);

                // Buscar alarme para obter userId
                var alarm = await _alarmRepository.GetByIdAsync(alarmId, cancellationToken);
                if (alarm == null)
                {
                    throw new InvalidOperationException($"Alarme {alarmId} não encontrado");
                }

                // Agendar job no Hangfire
                var jobId = _backgroundJobClient.Schedule(
                    () => ProcessAlarmAsync(alarmId, alarm.UserId, "scheduled", CancellationToken.None),
                    scheduledTime);

                // Registrar mapeamento
                _jobIdToAlarmId[jobId] = alarmId;

                // Atualizar status
                UpdateProcessingStatus(alarmId, alarm.UserId, AlarmProcessingStatus.Scheduled, scheduledTime);

                _logger.LogInformation("Alarme {AlarmId} agendado com sucesso - JobId: {JobId}", alarmId, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao agendar processamento do alarme {AlarmId}", alarmId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CancelScheduledAlarmAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Cancelando job agendado {JobId}", jobId);

                // Cancelar no Hangfire
                var cancelled = _backgroundJobClient.Delete(jobId);

                if (cancelled && _jobIdToAlarmId.TryGetValue(jobId, out var alarmId))
                {
                    // Buscar userId
                    var alarm = await _alarmRepository.GetByIdAsync(alarmId, cancellationToken);
                    if (alarm != null)
                    {
                        UpdateProcessingStatus(alarmId, alarm.UserId, AlarmProcessingStatus.Cancelled, DateTime.UtcNow);
                    }

                    _jobIdToAlarmId.TryRemove(jobId, out _);
                    _logger.LogInformation("Job {JobId} cancelado com sucesso para alarme {AlarmId}", jobId, alarmId);
                }

                return cancelled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar job {JobId}", jobId);
                return false;
            }
        }

        /// <inheritdoc />
        public Task<AlarmProcessingStatus> GetProcessingStatusAsync(
            Guid alarmId,
            CancellationToken cancellationToken = default)
        {
            if (_processingCache.TryGetValue(alarmId, out var info))
            {
                return Task.FromResult(info.Status);
            }

            return Task.FromResult(AlarmProcessingStatus.Scheduled);
        }

        /// <inheritdoc />
        public Task<IEnumerable<AlarmProcessingInfo>> GetActiveProcessingAlarmsAsync(
            CancellationToken cancellationToken = default)
        {
            var activeAlarms = _processingCache.Values
                .Where(info => info.Status == AlarmProcessingStatus.Processing ||
                              info.Status == AlarmProcessingStatus.Scheduled ||
                              info.Status == AlarmProcessingStatus.Retrying)
                .ToList();

            return Task.FromResult<IEnumerable<AlarmProcessingInfo>>(activeAlarms);
        }

        #region Métodos Privados

        private async Task<IEnumerable<string>> ExecuteAlarmActionsAsync(
            Alarm alarm,
            string triggerType,
            CancellationToken cancellationToken)
        {
            var actions = new List<string>();

            try
            {
                // Simular execução de ações do alarme
                actions.Add("sound_notification");

                if (alarm.Name.Value?.Contains("vibrate") == true)
                {
                    actions.Add("vibration");
                }

                if (alarm.Name.Value?.Contains("light") == true)
                {
                    actions.Add("light_activation");
                }

                // Simular delay de processamento
                await Task.Delay(100, cancellationToken);

                _logger.LogDebug("Ações executadas para alarme {AlarmId}: {Actions}",
                    alarm.Id, string.Join(", ", actions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar ações do alarme {AlarmId}", alarm.Id);
                actions.Add("error_in_actions");
            }

            return actions;
        }

        private async Task<IEnumerable<string>> SendAlarmNotificationsAsync(
            Alarm alarm,
            Guid userId,
            string triggerType,
            CancellationToken cancellationToken)
        {
            var notifications = new List<string>();

            try
            {
                // Simular envio de notificações
                notifications.Add("push_notification");
                notifications.Add("in_app_notification");

                if (triggerType == "manual")
                {
                    notifications.Add("manual_trigger_confirmation");
                }

                // Simular delay de envio
                await Task.Delay(50, cancellationToken);

                _logger.LogDebug("Notificações enviadas para alarme {AlarmId}: {Notifications}",
                    alarm.Id, string.Join(", ", notifications));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificações do alarme {AlarmId}", alarm.Id);
                notifications.Add("error_in_notifications");
            }

            return notifications;
        }

        private async Task RecordAlarmHistoryAsync(
            Alarm alarm,
            Guid userId,
            string triggerType,
            IEnumerable<string> actions,
            IEnumerable<string> notifications,
            CancellationToken cancellationToken)
        {
            try
            {
                // Em uma implementação real, isso salvaria no banco de dados
                _logger.LogInformation("Registrando histórico do alarme {AlarmId} - Tipo: {TriggerType}, Ações: {ActionsCount}, Notificações: {NotificationsCount}",
                    alarm.Id, triggerType, actions.Count(), notifications.Count());

                // Simular salvamento no banco
                await Task.Delay(25, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar histórico do alarme {AlarmId}", alarm.Id);
            }
        }

        private async Task ScheduleNextRecurrenceAsync(Alarm alarm, CancellationToken cancellationToken)
        {
            try
            {
                // Verificar se o alarme tem recorrência configurada
                var activeSchedules = alarm.Schedules.Where(s => s.IsActive).ToList();

                if (activeSchedules.Any())
                {
                    // Calcular próxima execução baseada nos schedules
                    var nextExecution = CalculateNextExecution(alarm, activeSchedules);

                    if (nextExecution.HasValue)
                    {
                        await ScheduleAlarmProcessingAsync(alarm.Id, nextExecution.Value, cancellationToken);

                        _logger.LogInformation("Próxima execução do alarme {AlarmId} agendada para {NextExecution}",
                            alarm.Id, nextExecution.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao agendar próxima recorrência do alarme {AlarmId}", alarm.Id);
            }
        }

        private async Task ScheduleRetryIfNeededAsync(
            Guid alarmId,
            Guid userId,
            string triggerType,
            Exception exception,
            CancellationToken cancellationToken)
        {
            try
            {
                // Obter informações de retry do cache
                if (_processingCache.TryGetValue(alarmId, out var info))
                {
                    var retryCount = info.RetryCount + 1;
                    const int maxRetries = 3;

                    if (retryCount <= maxRetries)
                    {
                        // Calcular delay exponencial: 2^retry * 30 segundos
                        var delayMinutes = Math.Pow(2, retryCount) * 0.5; // 30s, 1min, 2min
                        var retryTime = DateTime.UtcNow.AddMinutes(delayMinutes);

                        // Agendar retry
                        var jobId = _backgroundJobClient.Schedule(
                            () => ProcessAlarmAsync(alarmId, userId, $"{triggerType}_retry_{retryCount}", CancellationToken.None),
                            retryTime);

                        // Atualizar status
                        var updatedInfo = info with
                        {
                            Status = AlarmProcessingStatus.Retrying,
                            RetryCount = retryCount,
                            LastError = exception.Message
                        };
                        _processingCache[alarmId] = updatedInfo;

                        _logger.LogWarning("Agendando retry {RetryCount}/{MaxRetries} para alarme {AlarmId} em {DelayMinutes} minutos - JobId: {JobId}",
                            retryCount, maxRetries, alarmId, delayMinutes, jobId);
                    }
                    else
                    {
                        _logger.LogError("Máximo de retries ({MaxRetries}) atingido para alarme {AlarmId}. Falha permanente.",
                            maxRetries, alarmId);

                        // Marcar como falha permanente
                        var failedInfo = info with
                        {
                            Status = AlarmProcessingStatus.Failed,
                            LastError = $"Max retries exceeded: {exception.Message}"
                        };
                        _processingCache[alarmId] = failedInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao agendar retry para alarme {AlarmId}", alarmId);
            }
        }

        private DateTime? CalculateNextExecution(Alarm alarm, IEnumerable<Schedule> schedules)
        {
            // Implementação simplificada - em produção seria mais complexa
            var now = DateTime.Now;
            var today = now.Date;
            var alarmTime = today.Add(alarm.Time.TimeOfDay);

            // Se o horário de hoje já passou, agendar para amanhã
            if (alarmTime <= now)
            {
                alarmTime = alarmTime.AddDays(1);
            }

            return alarmTime;
        }

        private void UpdateProcessingStatus(
            Guid alarmId,
            Guid userId,
            AlarmProcessingStatus status,
            DateTime scheduledTime,
            string? error = null)
        {
            var now = DateTime.UtcNow;

            if (_processingCache.TryGetValue(alarmId, out var existing))
            {
                var updated = existing with
                {
                    Status = status,
                    StartedAt = status == AlarmProcessingStatus.Processing ? now : existing.StartedAt,
                    CompletedAt = status is AlarmProcessingStatus.Completed or AlarmProcessingStatus.Failed or AlarmProcessingStatus.Cancelled ? now : null,
                    LastError = error ?? existing.LastError
                };
                _processingCache[alarmId] = updated;
            }
            else
            {
                var newInfo = new AlarmProcessingInfo(
                    alarmId, userId, status, scheduledTime,
                    status == AlarmProcessingStatus.Processing ? now : null,
                    status is AlarmProcessingStatus.Completed or AlarmProcessingStatus.Failed or AlarmProcessingStatus.Cancelled ? now : null,
                    0, error
                );
                _processingCache[alarmId] = newInfo;
            }
        }

        private AlarmProcessingMetrics CreateMetrics(DateTime queueStartTime, TimeSpan executionTime, int retryCount)
        {
            var queueTime = DateTime.UtcNow - queueStartTime;

            return new AlarmProcessingMetrics(
                QueueTime: queueTime,
                ExecutionTime: executionTime,
                RetryCount: retryCount,
                ProcessingNode: Environment.MachineName,
                ComponentsInvolved: new[] { "DistributedAlarmProcessor", "Hangfire", "NotificationService" }
            );
        }

        #endregion
    }
}
