using MediatR;
using SmartAlarm.AlarmService.Infrastructure.DistributedProcessing;
using SmartAlarm.AlarmService.Infrastructure.Queues;
using SmartAlarm.AlarmService.Application.Models;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Handler para processamento distribuído de alarmes
    /// </summary>
    public class ProcessDistributedAlarmCommandHandler : IRequestHandler<ProcessDistributedAlarmCommand, ProcessDistributedAlarmResponse>
    {
        private readonly IDistributedAlarmProcessor _distributedProcessor;
        private readonly IAlarmQueue _alarmQueue;
        private readonly IValidator<ProcessDistributedAlarmCommand> _validator;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<ProcessDistributedAlarmCommandHandler> _logger;

        public ProcessDistributedAlarmCommandHandler(
            IDistributedAlarmProcessor distributedProcessor,
            IAlarmQueue alarmQueue,
            IValidator<ProcessDistributedAlarmCommand> validator,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<ProcessDistributedAlarmCommandHandler> logger)
        {
            _distributedProcessor = distributedProcessor;
            _alarmQueue = alarmQueue;
            _validator = validator;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<ProcessDistributedAlarmResponse> Handle(ProcessDistributedAlarmCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("ProcessDistributedAlarmCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("alarm.id", request.AlarmId.ToString());
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("trigger.type", request.TriggerType);
                activity?.SetTag("use.queue", request.UseQueue.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando processamento distribuído do alarme {AlarmId} - UseQueue: {UseQueue} - CorrelationId: {CorrelationId}",
                    request.AlarmId, request.UseQueue, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);
                    _meter.IncrementErrorCount("command", "process_distributed_alarm", "validation");

                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para processamento distribuído: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);

                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                AlarmProcessingResult result;

                if (request.UseQueue)
                {
                    // Processar via fila
                    result = await ProcessViaQueueAsync(request, cancellationToken);
                }
                else
                {
                    // Processar diretamente
                    result = await ProcessDirectlyAsync(request, cancellationToken);
                }

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "process_distributed_alarm", "success", "200");

                var response = new ProcessDistributedAlarmResponse(
                    request.AlarmId,
                    request.UserId,
                    result.Success,
                    result.Message,
                    result.ProcessedAt,
                    result.ProcessingDuration,
                    result.ActionsExecuted,
                    result.Notifications,
                    result.Metrics
                );

                _logger.LogInformation("Processamento distribuído do alarme {AlarmId} concluído - Sucesso: {Success} em {Duration}ms - CorrelationId: {CorrelationId}",
                    request.AlarmId, result.Success, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return response;
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "process_distributed_alarm", "validation_error", "400");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "process_distributed_alarm", "error", "500");
                _meter.IncrementErrorCount("command", "process_distributed_alarm", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro inesperado no processamento distribuído do alarme {AlarmId} - CorrelationId: {CorrelationId}",
                    request.AlarmId, _correlationContext.CorrelationId);

                throw;
            }
        }

        private async Task<AlarmProcessingResult> ProcessViaQueueAsync(
            ProcessDistributedAlarmCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processando alarme {AlarmId} via fila", request.AlarmId);

            // Criar item para a fila
            var queueItem = new AlarmQueueItem(
                Id: Guid.NewGuid().ToString(),
                AlarmId: request.AlarmId,
                UserId: request.UserId,
                TriggerType: request.TriggerType,
                Priority: AlarmPriority.Normal,
                ScheduledTime: DateTime.UtcNow,
                RetryCount: 0,
                Metadata: new Dictionary<string, object>
                {
                    ["correlation_id"] = _correlationContext.CorrelationId,
                    ["processing_node"] = Environment.MachineName,
                    ["enqueued_at"] = DateTime.UtcNow
                }
            );

            // Enfileirar com delay se especificado
            var jobId = await _alarmQueue.EnqueueAlarmAsync(queueItem, request.Delay, cancellationToken);

            // Retornar resultado indicando que foi enfileirado
            return new AlarmProcessingResult(
                request.AlarmId,
                request.UserId,
                true,
                $"Alarme enfileirado com sucesso - JobId: {jobId}",
                DateTime.UtcNow,
                TimeSpan.Zero,
                new[] { "enqueued" },
                new[] { "queue_notification" },
                new AlarmProcessingMetrics(
                    QueueTime: TimeSpan.Zero,
                    ExecutionTime: TimeSpan.Zero,
                    RetryCount: 0,
                    ProcessingNode: Environment.MachineName,
                    ComponentsInvolved: new[] { "AlarmQueue", "Hangfire" }
                )
            );
        }

        private async Task<AlarmProcessingResult> ProcessDirectlyAsync(
            ProcessDistributedAlarmCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processando alarme {AlarmId} diretamente", request.AlarmId);

            // Processar diretamente usando o processador distribuído
            return await _distributedProcessor.ProcessAlarmAsync(
                request.AlarmId,
                request.UserId,
                request.TriggerType,
                cancellationToken);
        }
    }
}
