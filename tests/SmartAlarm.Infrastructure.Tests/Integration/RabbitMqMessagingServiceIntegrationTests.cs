using SmartAlarm.Infrastructure.Messaging;
using Xunit;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Tests.Integration
{
    public class RabbitMqMessagingServiceIntegrationTests
    {
        private readonly RabbitMqMessagingService _service;

        public RabbitMqMessagingServiceIntegrationTests()
        {
            // If RabbitMqMessagingService requires a logger, provide a mock or a NullLogger
            _service = new RabbitMqMessagingService(new LoggerFactory().CreateLogger<RabbitMqMessagingService>());
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Deve_Publicar_E_Consumir_Mensagem()
        {
            // Arrange
            var topic = $"test-topic-{Guid.NewGuid()}";
            var received = string.Empty;
            var tcs = new TaskCompletionSource<string>();
            await _service.SubscribeAsync(topic, msg => { received = msg; tcs.SetResult(msg); return Task.CompletedTask; });
            var mensagem = "mensagem de integração";

            // Act
            await _service.PublishEventAsync(topic, mensagem);
            var result = await Task.WhenAny(tcs.Task, Task.Delay(5000));

            // Assert
            Assert.True(tcs.Task.IsCompleted, "Mensagem não recebida no tempo esperado");
            Assert.Equal(mensagem, received);
        }
    }
}
