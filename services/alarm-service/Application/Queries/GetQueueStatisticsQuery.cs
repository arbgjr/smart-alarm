using MediatR;
using SmartAlarm.AlarmService.Infrastructure.Queues;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Queries
{
    /// <summary>
    /// Query para obter estatísticas da fila de alarmes
    /// </summary>
    public record GetQueueStatisticsQuery(
        string QueueName = "default"
    ) : IRequest<GetQueueStatisticsResponse>;

    /// <summary>
    /// Response das estatísticas da fila
    /// </summary>
    public record GetQueueStatisticsResponse(
        QueueStatistics Statistics,
        IEnumerable<AlarmQueueItem> RecentItems,
        DateTime RetrievedAt
    );

    /// <summary>
    /// Validator para query de estatísticas da fila
    /// </summary>
    public class GetQueueStatisticsQueryValidator : AbstractValidator<GetQueueStatisticsQuery>
    {
        public GetQueueStatisticsQueryValidator()
        {
            RuleFor(x => x.QueueName)
                .NotEmpty()
                .WithMessage("QueueName é obrigatório")
                .MaximumLength(100)
                .WithMessage("QueueName deve ter no máximo 100 caracteres");
        }
    }

    /// <summary>
    /// Handler para obter estatísticas da fila
    /// </summary>
    public class GetQueueStatisticsQueryHandler : IRequestHandler<GetQueueStatisticsQuery, GetQueueStatisticsResponse>
    {
        private readonly IAlarmQueue _alarmQueue;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly ICorrelationContext _correlationContext;
        private readonly IValidator<GetQueueStatisticsQuery> _validator;
        private readonly ILogger<GetQueueStatisticsQueryHandler> _logger;

        public GetQueueStatisticsQueryHandler(
            IAlarmQueue alarmQueue,
            SmartAlarmActivitySource activitySource,
            ICorrelationContext correlationContext,
            IValidator<GetQueueStatisticsQuery> validator,
            ILogger<GetQueueStatisticsQueryHandler> logger)
        {
            _alarmQueue = alarmQueue;
            _activitySource = activitySource;
            _correlationContext = correlationContext;
            _validator = validator;
            _logger = logger;
        }

        public async Task<GetQueueStatisticsResponse> Handle(GetQueueStatisticsQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("GetQueueStatisticsQueryHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("queue.name", request.QueueName);
                activity?.SetTag("operation", "get_queue_statistics");
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Obtendo estatísticas da fila {QueueName} - CorrelationId: {CorrelationId}",
                    request.QueueName, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);

                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para estatísticas da fila: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);

                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Obter estatísticas da fila
                var statistics = await _alarmQueue.GetQueueStatisticsAsync(request.QueueName, cancellationToken);

                // Obter itens recentes da fila (últimos 10)
                var recentItems = await _alarmQueue.ListQueuedAlarmsAsync(request.QueueName, 10, cancellationToken);

                activity?.SetTag("queue.total_messages", statistics.TotalMessages.ToString());
                activity?.SetTag("queue.pending_messages", statistics.PendingMessages.ToString());
                activity?.SetTag("queue.processing_messages", statistics.ProcessingMessages.ToString());
                activity?.SetTag("queue.failed_messages", statistics.FailedMessages.ToString());

                stopwatch.Stop();

                _logger.LogInformation("Estatísticas da fila {QueueName} obtidas - Total: {Total}, Pendentes: {Pending}, Processando: {Processing}, Falhas: {Failed} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    request.QueueName, statistics.TotalMessages, statistics.PendingMessages, statistics.ProcessingMessages, statistics.FailedMessages, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return new GetQueueStatisticsResponse(
                    statistics,
                    recentItems,
                    DateTime.UtcNow
                );
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao obter estatísticas da fila {QueueName} - CorrelationId: {CorrelationId}",
                    request.QueueName, _correlationContext.CorrelationId);

                throw;
            }
        }
    }
}
