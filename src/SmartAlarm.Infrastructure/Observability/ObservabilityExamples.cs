using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Observability
{
    /// <summary>
    /// Exemplos de uso dos serviços de observabilidade (tracing e métricas).
    /// </summary>
    public class ObservabilityExamples
    {
        private readonly ITracingService _tracing;
        private readonly IMetricsService _metrics;
        private readonly ILogger<ObservabilityExamples> _logger;

        public ObservabilityExamples(ITracingService tracing, IMetricsService metrics, ILogger<ObservabilityExamples> logger)
        {
            _tracing = tracing;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task DoWorkAsync()
        {
            await _tracing.TraceAsync("DoWork", "Iniciando operação...");
            _logger.LogInformation("Operação em andamento...");
            await _metrics.IncrementAsync("DoWork_Counter");
            await _metrics.RecordAsync("DoWork_Duration", 123.45);
        }
    }
}
