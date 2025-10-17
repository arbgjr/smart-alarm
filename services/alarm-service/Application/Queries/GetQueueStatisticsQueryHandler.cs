using MediatR;
using SmartAlarm.AlarmService.Infrastructure.Queues;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Queries
{
    /// <summary>
    /// Handler para obter estatísticas da fila de processamento
    /// </summary>
    public class GetQueueStatisticsQueryHandler : IRequestHandler<GetQueueStatisticsQuery, GetQueueStatisticsResponse>
    {
        private readonly IAlarmQueue _alarmQueue;
        private readonly IValidator<GetQueueStatisticsQuery> _validator;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<GetQueueStatisticsQueryHandler> _logger;

        public GetQueueStatisticsQueryHandler(
            IAlarmQueue alarmQueue,
            IValidator<GetQueueStatisticsQuery> validator,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<GetQueueStatisticsQueryHandler> logger)
        {
            _alarmQueue = alarmQueue;
            _validator = validator;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
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
                    _meter.IncrementErrorCount("query", "get_queue_statistics", "validation");

                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para estatísticas da fila: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);

                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Obter estatísticas da fila
                var statistics = await _alarmQueue.GetQueueStatisticsAsync(request.QueueName, cancellationToken);

                // Obter alarmes na fila (limitado para performance)
                var queuedAlarms = await _alarmQueue.ListQueuedAlarmsAsync(request.QueueName, 50, cancellationToken);

                // Calcular métricas adicionais
                var healthStatus = CalculateQueueHealth(statistics);
                var recommendations = GenerateRecommendations(statistics);

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_queue_statistics", "success", "200");

                var response = new GetQueueStatisticsResponse(
                    QueueName: request.QueueName,
                    Statistics: statistics,
                    QueuedAlarms: queuedAlarms,
                    HealthStatus: healthStatus,
                    Recommendations: recommendations,
                    RetrievedAt: DateTime.UtcNow
                );

                _logger.LogInformation("Estatísticas da fila {QueueName} obtidas com sucesso - Total: {TotalMessages}, Pendentes: {PendingMessages} em {Duration}ms - CorrelationId: {CorrelationId}",
                    request.QueueName, statistics.TotalMessages, statistics.PendingMessages, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return response;
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_queue_statistics", "validation_error", "400");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_queue_statistics", "error", "500");
                _meter.IncrementErrorCount("query", "get_queue_statistics", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro inesperado ao obter estatísticas da fila {QueueName} - CorrelationId: {CorrelationId}",
                    request.QueueName, _correlationContext.CorrelationId);

                throw;
            }
        }

        private static QueueHealthStatus CalculateQueueHealth(QueueStatistics statistics)
        {
            // Calcular saúde da fila baseado em métricas
            var healthScore = 100.0;

            // Penalizar por mensagens pendentes em excesso
            if (statistics.PendingMessages > 1000)
                healthScore -= 30;
            else if (statistics.PendingMessages > 500)
                healthScore -= 15;
            else if (statistics.PendingMessages > 100)
                healthScore -= 5;

            // Penalizar por mensagens falhando
            if (statistics.FailedMessages > 50)
                healthScore -= 25;
            else if (statistics.FailedMessages > 20)
                healthScore -= 10;
            else if (statistics.FailedMessages > 5)
                healthScore -= 5;

            // Penalizar por tempo de processamento alto
            if (statistics.AverageProcessingTime.TotalSeconds > 60)
                healthScore -= 20;
            else if (statistics.AverageProcessingTime.TotalSeconds > 30)
                healthScore -= 10;

            // Penalizar por baixo throughput
            if (statistics.ThroughputPerMinute < 10)
                healthScore -= 15;

            var status = healthScore switch
            {
                >= 90 => "healthy",
                >= 70 => "warning",
                >= 50 => "degraded",
                _ => "critical"
            };

            return new QueueHealthStatus(
                Status: status,
                Score: Math.Max(0, healthScore),
                LastChecked: DateTime.UtcNow,
                Issues: GenerateHealthIssues(statistics)
            );
        }

        private static IEnumerable<string> GenerateHealthIssues(QueueStatistics statistics)
        {
            var issues = new List<string>();

            if (statistics.PendingMessages > 500)
                issues.Add($"Alto número de mensagens pendentes: {statistics.PendingMessages}");

            if (statistics.FailedMessages > 20)
                issues.Add($"Muitas mensagens falhando: {statistics.FailedMessages}");

            if (statistics.AverageProcessingTime.TotalSeconds > 30)
                issues.Add($"Tempo de processamento alto: {statistics.AverageProcessingTime.TotalSeconds:F1}s");

            if (statistics.ThroughputPerMinute < 10)
                issues.Add($"Throughput baixo: {statistics.ThroughputPerMinute:F1} alarmes/min");

            if (statistics.ProcessingMessages > statistics.TotalMessages * 0.8)
                issues.Add("Muitos alarmes em processamento simultâneo");

            return issues;
        }

        private static IEnumerable<string> GenerateRecommendations(QueueStatistics statistics)
        {
            var recommendations = new List<string>();

            if (statistics.PendingMessages > 500)
            {
                recommendations.Add("Considere aumentar o número de workers para processar a fila mais rapidamente");
                recommendations.Add("Verifique se há gargalos no processamento de alarmes");
            }

            if (statistics.FailedMessages > 20)
            {
                recommendations.Add("Investigue as causas das falhas recorrentes");
                recommendations.Add("Considere ajustar a configuração de retry");
            }

            if (statistics.AverageProcessingTime.TotalSeconds > 30)
            {
                recommendations.Add("Otimize o processamento de alarmes para reduzir latência");
                recommendations.Add("Considere usar processamento assíncrono para operações demoradas");
            }

            if (statistics.ThroughputPerMinute < 10)
            {
                recommendations.Add("Aumente a capacidade de processamento");
                recommendations.Add("Verifique se há limitações de recursos (CPU, memória, rede)");
            }

            if (!recommendations.Any())
            {
                recommendations.Add("Fila operando dentro dos parâmetros normais");
            }

            return recommendations;
        }
    }
}
