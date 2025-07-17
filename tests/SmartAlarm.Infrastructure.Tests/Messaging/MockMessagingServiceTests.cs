// Mock utilizado exclusivamente para testes automatizados.
// Não representa lógica de produção.
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Messaging;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Messaging
{
    public class MockMessagingServiceTests
    {
        [Fact]
        public async Task PublishEventAsync_Should_LogInformation()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<MockMessagingService>>();
            var service = new MockMessagingService(loggerMock.Object);

            // Act
            await service.PublishEventAsync("test-topic", "mensagem");

            // Assert
            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test-topic") && v.ToString()!.Contains("mensagem")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}

