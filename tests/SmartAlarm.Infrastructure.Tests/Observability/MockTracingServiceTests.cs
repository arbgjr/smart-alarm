using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Observability;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Observability
{
    public class MockTracingServiceTests
    {
        [Fact]
        public async Task TraceAsync_Should_LogInformation()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<MockTracingService>>();
            var service = new MockTracingService(loggerMock.Object);

            // Act
            await service.TraceAsync("TestOp", "Mensagem de teste");

            // Assert
            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("TestOp") && v.ToString().Contains("Mensagem de teste")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
