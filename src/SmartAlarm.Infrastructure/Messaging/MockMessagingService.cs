using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Messaging
{
    // IMPLEMENTAÇÃO MOCK/STUB
    // Esta classe é destinada exclusivamente para ambientes de desenvolvimento e testes.
    // Não utilizar em produção. A implementação real será ativada por configuração.
    /// <summary>
    /// Implementação mock de IMessagingService para desenvolvimento e testes.
    /// </summary>
    public class MockMessagingService : IMessagingService
    {
        private readonly ILogger<MockMessagingService> _logger;

        public MockMessagingService(ILogger<MockMessagingService> logger)
        {
            _logger = logger;
        }

        public Task PublishEventAsync(string topic, string message)
        {
            _logger.LogInformation("[MockMessaging] Evento publicado no tópico {Topic}: {Message}", topic, message);
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(string topic, Func<string, Task> handler)
        {
            _logger.LogInformation("[MockMessaging] Subscrito ao tópico {Topic}", topic);
            // Não faz nada em mock
            return Task.CompletedTask;
        }
    }
}
