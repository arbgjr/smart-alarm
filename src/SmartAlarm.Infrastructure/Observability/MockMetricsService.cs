using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Observability
{
    // IMPLEMENTAÇÃO MOCK/STUB
    // Esta classe é destinada exclusivamente para ambientes de desenvolvimento e testes.
    // Não utilizar em produção. A implementação real será ativada por configuração.
    /// <summary>
    /// Implementação mock de IMetricsService para desenvolvimento e testes.
    /// </summary>
    public class MockMetricsService : IMetricsService
    {
        private readonly ILogger<MockMetricsService> _logger;

        public MockMetricsService(ILogger<MockMetricsService> logger)
        {
            _logger = logger;
        }

        public Task IncrementAsync(string metricName)
        {
            _logger.LogInformation("[MockMetrics] Increment {MetricName}", metricName);
            return Task.CompletedTask;
        }

        public Task RecordAsync(string metricName, double value)
        {
            _logger.LogInformation("[MockMetrics] Record {MetricName}: {Value}", metricName, value);
            return Task.CompletedTask;
        }
    }
}
