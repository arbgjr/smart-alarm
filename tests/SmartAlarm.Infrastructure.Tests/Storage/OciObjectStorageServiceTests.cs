using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using SmartAlarm.Infrastructure.Storage;

namespace SmartAlarm.Infrastructure.Tests.Storage
{
    public class OciObjectStorageServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<OciObjectStorageService>> _mockLogger;
        private readonly OciObjectStorageService _service;

        public OciObjectStorageServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<OciObjectStorageService>>();

            // Setup configuration values
            SetupConfiguration("OCI:ObjectStorage:Namespace", "test-namespace");
            SetupConfiguration("OCI:ObjectStorage:BucketName", "test-bucket");
            SetupConfiguration("OCI:ObjectStorage:Region", "us-ashburn-1");

            _service = new OciObjectStorageService(_mockConfiguration.Object, _mockLogger.Object);
        }

        private void SetupConfiguration(string key, string value)
        {
            _mockConfiguration.Setup(x => x[key]).Returns(value);
        }

        [Fact]
        public async Task UploadAsync_ValidPath_ShouldCompleteSuccessfully()
        {
            // Arrange
            var path = "test/file.txt";
            using var content = new System.IO.MemoryStream();

            // Act
            var exception = await Record.ExceptionAsync(() => _service.UploadAsync(path, content));

            // Assert
            Assert.Null(exception);
            VerifyLogCalled("Uploading file to OCI Object Storage");
            VerifyLogCalled("Successfully uploaded");
        }

        [Fact]
        public async Task DownloadAsync_ValidPath_ShouldReturnStream()
        {
            // Arrange
            var path = "test/file.txt";

            // Act
            var result = await _service.DownloadAsync(path);

            // Assert
            Assert.NotNull(result);
            VerifyLogCalled("Downloading file from OCI Object Storage");
            VerifyLogCalled("Successfully downloaded");
        }

        [Fact]
        public async Task DeleteAsync_ValidPath_ShouldCompleteSuccessfully()
        {
            // Arrange
            var path = "test/file.txt";

            // Act
            var exception = await Record.ExceptionAsync(() => _service.DeleteAsync(path));

            // Assert
            Assert.Null(exception);
            VerifyLogCalled("Deleting file from OCI Object Storage");
            VerifyLogCalled("Successfully deleted");
        }

        [Fact]
        public void Constructor_MissingNamespace_ShouldThrowException()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["OCI:ObjectStorage:Namespace"]).Returns((string?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new OciObjectStorageService(_mockConfiguration.Object, _mockLogger.Object));
            
            Assert.Contains("Namespace não configurado", exception.Message);
        }

        [Fact]
        public void Constructor_MissingBucketName_ShouldThrowException()
        {
            // Arrange
            SetupConfiguration("OCI:ObjectStorage:Namespace", "test-namespace");
            _mockConfiguration.Setup(x => x["OCI:ObjectStorage:BucketName"]).Returns((string?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new OciObjectStorageService(_mockConfiguration.Object, _mockLogger.Object));
            
            Assert.Contains("BucketName não configurado", exception.Message);
        }

        private void VerifyLogCalled(string message)
        {
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
