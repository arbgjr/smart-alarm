using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Observability
{
    // IMPLEMENTAÇÃO MOCK/STUB
    // Esta classe é destinada exclusivamente para ambientes de desenvolvimento e testes.
    // Não utilizar em produção. A implementação real será ativada por configuração.
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
