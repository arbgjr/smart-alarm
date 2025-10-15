using MediatR;
using SmartAlarm.AlarmService.Infrastructure.DistributedProcessing;
using SmartAlarm.AlarmService.Infrastructure.Queues;
using SmartAlarm.AlarmService.Infrastructure.Metrics;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Command para processar alarme usando sistema distribuído
    /// </summary>
    public record ProcessDistributedAlarmCommand(
        Guid AlarmId,
        Guid UserId,
        string TriggerType = "scheduled",
        bool UseQueue = true,
        TimeSpan? Delay = null
    ) : IRequest<ProcessDistributedAlarmResponse>;

    /// <summary>
    /// Response do processamento distribuído
    /// </summary>
    public record ProcessDistributedAlarmResponse(
        Guid AlarmId,
        Guid UserId,
        bool Success,
        string Message,
        string? JobId = null,
        AlarmProcessingResult? ProcessingResult = null,
        DateTime ProcessedAt = default
    );

    /// <summary>
    /// Validator para comando de processamento distribuído
    /// </summary>
    public class ProcessDistributedAlarmCommandValidator : AbstractValidator<ProcessDistributedAlarmCommand>
    {
        public ProcessDistributedAlarmCommandValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty()
                .WithMessage("AlarmId é obrigatório");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId é obrigatório");

            RuleFor(x => x.TriggerType)
                .NotEmpty()
                .Must(type => new[] { "scheduled", "manual", "retry", "test" }.Contains(type))
                .WithMessage("TriggerType deve ser: scheduled, manual, retry ou test");

            RuleFor(x => x.Delay)
                .Must(delay => !delay.HasValue || delay.Value >= TimeSpan.Zero)
                .WithMessage("Delay deve ser positivo");
        }
    }

    /// <summary>
    /// Handler para processamento distribuído de alarmes
    /// </summary>
    public class ProcessDistributedAlarmCommandHandler : IRequestHandler<ProcessDistributedAlarmCommand, ProcessDistributedAlarmResponse>
    {
        private readonly IDistributedAlarmProcessor _distributedProcessor;
        private readonly IAlarmQueue _alarmQueue;
        private readonly AlarmServiceMetrics _metrics;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly ICorrelationContext _correlationContext;
        private readonly IValidator<ProcessDistributedAlarmCommand> _validator;
        private readonly ILogger<ProcessDistributedAlarmCommandHandler> _logger;

        public ProcessDistributedAlarmCommandHandler(
            IDistributedAlarmProcessor distributedProcessor,
            IAlarmQueue alarmQueue,
            AlarmServiceMetrics metrics,
            SmartAlarmActivitySource activitySource,
            ICorrelationContext correlationContext,
            IValidator<ProcessDistributedAlarmCommand> validator,
            ILogger<ProcessDistributedAlarmCommandHandler> logger)
        {
            _distributedProcessor = distributedProcessor;
            _alarmQueue = alarmQueue;
            _metrics = metrics;
            _activitySource = activitySource;
            _correlationContext = correlationContext;
            _validator = validator;
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

                _logger.LogInformation("Iniciando processamento distribuído do alarme {AlarmId} - Tipo: {TriggerType}, UseQueue: {UseQueue} - CorrelationId: {CorrelationId}",
                    request.AlarmId, request.TriggerType, request.UseQueue, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);

                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para processamento distribuído: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);

                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                if (request.UseQueue)
                {
                    // Processar via fila para alta disponibilidade
                    var result = await ProcessViaQueueAsync(request, cancellationToken);

                    stopwatch.Stop();
                    _metrics.RecordAlarmProcessed(
                        request.TriggerType,
                        request.UserId.ToString(),
                        stopwatch.Elapsed.TotalSeconds,
                        new Dictionary<string, object> { ["processing_method"] = "queue" });

                    return result;
                }
                else
                {
                    // Processar diretamente
                    var result = await ProcessDirectlyAsync(request, cancellationToken);

                    stopwatch.Stop();
                    _metrics.RecordAlarmProcessed(
                        request.TriggerType,
                        request.UserId.ToString(),
                        stopwatch.Elapsed.TotalSeconds,
                        new Dictionary<string, object> { ["processing_method"] = "direct" });

                    return result;
                }
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _metrics.RecordAlarmFailure(
                    request.TriggerType,
                    request.UserId.ToString(),
                    "validation_error",
                    stopwatch.Elapsed.TotalSeconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.RecordAlarmFailure(
                    request.TriggerType,
                    request.UserId.ToString(),
                    ex.GetType().Name,
                    stopwatch.Elapsed.TotalSeconds);

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro no processamento distribuído do alarme {AlarmId} - CorrelationId: {CorrelationId}",
                    request.AlarmId, _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <summary>
        /// Processa alarme via fila para alta disponibilidade
        /// </summary>
        private async Task<ProcessDistributedAlarmResponse> ProcessViaQueueAsync(
            ProcessDistributedAlarmCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processando alarme {AlarmId} via fila com delay {Delay}",
                    request.AlarmId, request.Delay?.ToString() ?? "none");

                // Criar item da fila
                var queueItem = new AlarmQueueItem(
                    Id: Guid.NewGuid().ToString(),
                    AlarmId: request.AlarmId,
                    UserId: request.UserId,
                    TriggerType: request.TriggerType,
                    ScheduledTime: DateTime.UtcNow.Add(request.Delay ?? TimeSpan.Zero),
                    EnqueuedAt: DateTime.UtcNow,
                    RetryCount: 0,
                    Metadata: new Dictionary<string, object>
                    {
                        ["correlation_id"] = _correlationContext.CorrelationId,
                        ["processing_node"] = Environment.MachineName,
                        ["enqueued_by"] = "ProcessDistributedAlarmCommand"
                    }
                );

                // Enfileirar para processamento
                var jobId = await _alarmQueue.EnqueueAlarmAsync(queueItem, request.Delay, cancellationToken);

                // Atualizar métricas de fila
                _metrics.UpdateQueueSize(1, "alarm-processing");

                _logger.LogInformation("Alarme {AlarmId} enfileirado com sucesso - JobId: {JobId}",
                    request.AlarmId, jobId);

                return new ProcessDistributedAlarmResponse(
                    request.AlarmId,
                    request.UserId,
                    true,
                    $"Alarme enfileirado para processamento com JobId: {jobId}",
                    jobId,
                    null,
                    DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar alarme {AlarmId} via fila", request.AlarmId);

                return new ProcessDistributedAlarmResponse(
                    request.AlarmId,
                    request.UserId,
                    false,
                    $"Erro ao enfileirar alarme: {ex.Message}",
                    null,
                    null,
                    DateTime.UtcNow
                );
            }
        }

        /// <summary>
        /// Processa alarme diretamente sem fila
        /// </summary>
        private async Task<ProcessDistributedAlarmResponse> ProcessDirectlyAsync(
            ProcessDistributedAlarmCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processando alarme {AlarmId} diretamente",
                    request.AlarmId);

                // Aplicar delay se especificado
                if (request.Delay.HasValue && request.Delay.Value > TimeSpan.Zero)
                {
                    _logger.LogDebug("Aplicando delay de {Delay} antes do processamento", request.Delay.Value);
                    await Task.Delay(request.Delay.Value, cancellationToken);
                }

                // Processar usando o processador distribuído
                var processingResult = await _distributedProcessor.ProcessAlarmAsync(
                    request.AlarmId,
                    request.UserId,
                    request.TriggerType,
                    cancellationToken);

                _logger.LogInformation("Alarme {AlarmId} processado diretamente - Sucesso: {Success}",
                    request.AlarmId, processingResult.Success);

                return new ProcessDistributedAlarmResponse(
                    request.AlarmId,
                    request.UserId,
                    processingResult.Success,
                    processingResult.Message,
                    null,
                    processingResult,
                    DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar alarme {AlarmId} diretamente", request.AlarmId);

                return new ProcessDistributedAlarmResponse(
                    request.AlarmId,
                    request.UserId,
                    false,
                    $"Erro no processamento direto: {ex.Message}",
                    null,
                    null,
                    DateTime.UtcNow
                );
            }
        }
    }
}
