using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Storage;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Storage
{
    public class MockStorageServiceTests
    {
        [Fact]
        public async Task UploadAsync_Should_LogInformation()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<MockStorageService>>();
            var service = new MockStorageService(loggerMock.Object);

            // Act
            await service.UploadAsync("/test.txt", new MemoryStream());

            // Assert
            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("/test.txt")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}

