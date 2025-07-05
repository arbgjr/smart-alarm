using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Observability;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Observability
{
    public class MockMetricsServiceTests
    {
        [Fact]
        public async Task IncrementAsync_Should_LogInformation()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<MockMetricsService>>();
            var service = new MockMetricsService(loggerMock.Object);

            // Act
            await service.IncrementAsync("TestMetric");

            // Assert
            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("TestMetric")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RecordAsync_Should_LogInformation()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<MockMetricsService>>();
            var service = new MockMetricsService(loggerMock.Object);

            // Act
            await service.RecordAsync("TestMetric", 42.0);

            // Assert
            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("TestMetric") && v.ToString()!.Contains("42")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
