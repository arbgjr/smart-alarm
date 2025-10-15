using MediatR;
using SmartAlarm.AlarmService.Infrastructure.DistributedProcessing;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Queries
{
    /// <summary>
    /// Query para obter alarmes em processamento ativo
    /// </summary>
    public record GetActiveProcessingAlarmsQuery() : IRequest<GetActiveProcessingAlarmsResponse>;

    /// <summary>
    /// Response dos alarmes em processamento ativo
    /// </summary>
    public record GetActiveProcessingAlarmsResponse(
        IEnumerable<AlarmProcessingInfo> ActiveAlarms,
        ProcessingSummary Summary,
        DateTime RetrievedAt
    );

    /// <summary>
    /// Resumo do processamento
    /// </summary>
    public record ProcessingSummary(
        int TotalActive,
        int Scheduled,
        int Processing,
        int Retrying,
        string ProcessingNode,
        TimeSpan AverageProcessingTime
    );

    /// <summary>
    /// Handler para obter alarmes em processamento ativo
    /// </summary>
    public class GetActiveProcessingAlarmsQueryHandler : IRequestHandler<GetActiveProcessingAlarmsQuery, GetActiveProcessingAlarmsResponse>
    {
        private readonly IDistributedAlarmProcessor _distributedProcessor;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<GetActiveProcessingAlarmsQueryHandler> _logger;

        public GetActiveProcessingAlarmsQueryHandler(
            IDistributedAlarmProcessor distributedProcessor,
            SmartAlarmActivitySource activitySource,
            ICorrelationContext correlationContext,
            ILogger<GetActiveProcessingAlarmsQueryHandler> logger)
        {
            _distributedProcessor = distributedProcessor;
            _activitySource = activitySource;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<GetActiveProcessingAlarmsResponse> Handle(GetActiveProcessingAlarmsQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("GetActiveProcessingAlarmsQueryHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("operation", "get_active_processing_alarms");
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Obtendo alarmes em processamento ativo - CorrelationId: {CorrelationId}",
                    _correlationContext.CorrelationId);

                // Obter alarmes ativos do processador distribuído
                var activeAlarms = await _distributedProcessor.GetActiveProcessingAlarmsAsync(cancellationToken);
                var alarmsList = activeAlarms.ToList();

                // Calcular resumo
                var summary = CalculateProcessingSummary(alarmsList);

                activity?.SetTag("active_alarms.count", alarmsList.Count.ToString());
                activity?.SetTag("processing.scheduled", summary.Scheduled.ToString());
                activity?.SetTag("processing.processing", summary.Processing.ToString());
                activity?.SetTag("processing.retrying", summary.Retrying.ToString());

                stopwatch.Stop();

                _logger.LogInformation("Alarmes em processamento ativo obtidos - Total: {Total}, Agendados: {Scheduled}, Processando: {Processing}, Retry: {Retrying} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    alarmsList.Count, summary.Scheduled, summary.Processing, summary.Retrying, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return new GetActiveProcessingAlarmsResponse(
                    alarmsList,
                    summary,
                    DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao obter alarmes em processamento ativo - CorrelationId: {CorrelationId}",
                    _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <summary>
        /// Calcula resumo do processamento
        /// </summary>
        private static ProcessingSummary CalculateProcessingSummary(IEnumerable<AlarmProcessingInfo> alarms)
        {
            var alarmsList = alarms.ToList();

            var scheduled = alarmsList.Count(a => a.Status == AlarmProcessingStatus.Scheduled);
            var processing = alarmsList.Count(a => a.Status == AlarmProcessingStatus.Processing);
            var retrying = alarmsList.Count(a => a.Status == AlarmProcessingStatus.Retrying);

            // Calcular tempo médio de processamento
            var completedAlarms = alarmsList.Where(a => a.StartedAt.HasValue && a.CompletedAt.HasValue);
            var averageProcessingTime = completedAlarms.Any()
                ? TimeSpan.FromMilliseconds(completedAlarms.Average(a => (a.CompletedAt!.Value - a.StartedAt!.Value).TotalMilliseconds))
                : TimeSpan.Zero;

            return new ProcessingSummary(
                TotalActive: alarmsList.Count,
                Scheduled: scheduled,
                Processing: processing,
                Retrying: retrying,
                ProcessingNode: Environment.MachineName,
                AverageProcessingTime: averageProcessingTime
            );
        }
    }
}
