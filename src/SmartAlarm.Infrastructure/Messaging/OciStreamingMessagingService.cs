using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Messaging
{
    // STUB DE INTEGRAÇÃO
    // Integração real com o serviço cloud ainda não implementada.
    // TODO: Substituir por implementação real antes do deploy em produção.
    /// <summary>
    /// Stub para integração futura com OCI Streaming (produção).
    /// </summary>
    public class OciStreamingMessagingService : IMessagingService
    {
        private readonly ILogger<OciStreamingMessagingService> _logger;
        public OciStreamingMessagingService(ILogger<OciStreamingMessagingService> logger)
        {
            _logger = logger;
        }
        public Task PublishEventAsync(string topic, string message)
        {
            _logger.LogInformation("[OCI Streaming] Evento publicado no tópico {Topic}: {Message}", topic, message);
            // TODO: Implementar integração real com OCI SDK
            return Task.CompletedTask;
        }
        public Task SubscribeAsync(string topic, Func<string, Task> handler)
        {
            _logger.LogInformation("[OCI Streaming] Subscrito ao tópico {Topic}", topic);
            // TODO: Implementar integração real com OCI SDK
            return Task.CompletedTask;
        }
    }
}
