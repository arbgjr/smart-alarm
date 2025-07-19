using SmartAlarm.Infrastructure.Messaging;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;

namespace SmartAlarm.Infrastructure.Tests.Integration
{
    public class RabbitMqMessagingServiceIntegrationTests
    {
        private readonly RabbitMqMessagingService _service;

        public RabbitMqMessagingServiceIntegrationTests()
        {
            // Define variáveis de ambiente necessárias para testes
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
            
            // Em Docker, a variável DOTNET_RUNNING_IN_CONTAINER já deve estar definida como "true"
            // e RABBITMQ_HOST já deve estar definido como "rabbitmq" no docker-compose
            
            // Verificar se estamos em ambiente de teste em contêiner
            bool inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            
            // Se não estiver em um contêiner (execução local), usar localhost
            if (!inContainer && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_HOST")))
            {
                Environment.SetEnvironmentVariable("RABBITMQ_HOST", "localhost");
            }
            
            // Criar serviço com logger e observability mocks
            var loggerFactory = new LoggerFactory();
            var meterMock = new Mock<SmartAlarmMeter>();
            var correlationContextMock = new Mock<ICorrelationContext>();
            var activitySourceMock = new Mock<SmartAlarmActivitySource>();
            
            _service = new RabbitMqMessagingService(
                loggerFactory.CreateLogger<RabbitMqMessagingService>(),
                meterMock.Object,
                correlationContextMock.Object,
                activitySourceMock.Object);
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
