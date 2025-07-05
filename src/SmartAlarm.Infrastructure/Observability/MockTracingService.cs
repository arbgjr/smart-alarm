using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Observability
{
    /// <summary>
    /// Implementação mock de ITracingService para desenvolvimento e testes.
    /// </summary>
    public class MockTracingService : ITracingService
    {
        private readonly ILogger<MockTracingService> _logger;

        public MockTracingService(ILogger<MockTracingService> logger)
        {
            _logger = logger;
        }

        public Task TraceAsync(string operation, string message)
        {
            _logger.LogInformation("[MockTracing] {Operation}: {Message}", operation, message);
            return Task.CompletedTask;
        }
    }
}
