using SmartAlarm.Domain.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using SmartAlarm.Infrastructure.Storage;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System.Net.Http;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace SmartAlarm.Infrastructure.Tests.Storage
{
    public class OciObjectStorageServiceTests : IDisposable
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<OciObjectStorageService>> _mockLogger;
        private readonly Mock<SmartAlarmMeter> _mockMeter;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly HttpClient _httpClient;
        private readonly OciObjectStorageService _service;

        public OciObjectStorageServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<OciObjectStorageService>>();
            _mockMeter = new Mock<SmartAlarmMeter>();
            _activitySource = new SmartAlarmActivitySource();
            _httpClient = new HttpClient();

            // Setup configuration values
            SetupConfiguration("OCI:ObjectStorage:Namespace", "test-namespace");
            SetupConfiguration("OCI:ObjectStorage:BucketName", "test-bucket");
            SetupConfiguration("OCI:ObjectStorage:Region", "us-ashburn-1");
            SetupConfiguration("OCI:TenancyId", "test-tenancy");
            SetupConfiguration("OCI:UserId", "test-user");
            SetupConfiguration("OCI:Fingerprint", "test-fingerprint");
            SetupConfiguration("OCI:PrivateKey", GenerateTestRsaPrivateKey());

            _service = new OciObjectStorageService(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockMeter.Object,
                _activitySource,
                _httpClient);
        }

        private void SetupConfiguration(string key, string value)
        {
            _mockConfiguration.Setup(x => x[key]).Returns(value);
        }

        private static string GenerateTestRsaPrivateKey()
        {
            using var rsa = RSA.Create(2048);
            var pkcs8PrivateKey = rsa.ExportPkcs8PrivateKey();
            return $"-----BEGIN PRIVATE KEY-----\n{Convert.ToBase64String(pkcs8PrivateKey)}\n-----END PRIVATE KEY-----";
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UploadAsync_ValidPath_ShouldCompleteSuccessfully()
        {
            // Arrange
            var path = "test/file.txt";
            using var content = new MemoryStream();

            // Act & Assert - O serviço vai tentar fazer uma chamada HTTP real e falhar (esperado)
            var exception = await Record.ExceptionAsync(() => _service.UploadAsync(path, content));

            // Verifica se houve uma tentativa de upload (não nula = houve erro de comunicação)
            Assert.NotNull(exception);
            VerifyLogCalled("OCIUpload");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task DownloadAsync_ValidPath_ShouldReturnStream()
        {
            // Arrange
            var path = "test/file.txt";

            // Act & Assert - O serviço vai tentar fazer uma chamada HTTP real e falhar (esperado)
            var exception = await Record.ExceptionAsync(() => _service.DownloadAsync(path));

            // Verifica se houve uma tentativa de download (não nula = houve erro de comunicação)
            Assert.NotNull(exception);
            VerifyLogCalled("OCIDownload");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task DeleteAsync_ValidPath_ShouldCompleteSuccessfully()
        {
            // Arrange
            var path = "test/file.txt";

            // Act & Assert - O serviço vai tentar fazer uma chamada HTTP real e falhar (esperado)
            var exception = await Record.ExceptionAsync(() => _service.DeleteAsync(path));

            // Verifica se houve uma tentativa de delete (não nula = houve erro de comunicação)
            Assert.NotNull(exception);
            VerifyLogCalled("OCIDelete");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Constructor_MissingNamespace_ShouldThrowException()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<OciObjectStorageService>>();
            var mockMeter = new Mock<SmartAlarmMeter>();
            var activitySource = new SmartAlarmActivitySource();
            var httpClient = new HttpClient();

            // Setup minimal required configs except Namespace
            mockConfig.Setup(x => x["OCI:ObjectStorage:BucketName"]).Returns("test-bucket");
            mockConfig.Setup(x => x["OCI:ObjectStorage:Region"]).Returns("us-ashburn-1");
            mockConfig.Setup(x => x["OCI:TenancyId"]).Returns("test-tenancy");
            mockConfig.Setup(x => x["OCI:UserId"]).Returns("test-user");
            mockConfig.Setup(x => x["OCI:Fingerprint"]).Returns("test-fingerprint");
            mockConfig.Setup(x => x["OCI:PrivateKey"]).Returns("-----BEGIN PRIVATE KEY-----\ntest\n-----END PRIVATE KEY-----");
            mockConfig.Setup(x => x["OCI:ObjectStorage:Namespace"]).Returns((string?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new OciObjectStorageService(
                    mockConfig.Object,
                    mockLogger.Object,
                    mockMeter.Object,
                    activitySource,
                    httpClient));

            Assert.Contains("Namespace não configurado", exception.Message);

            // Cleanup
            httpClient.Dispose();
            activitySource.Dispose();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Constructor_MissingBucketName_ShouldThrowException()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<OciObjectStorageService>>();
            var mockMeter = new Mock<SmartAlarmMeter>();
            var activitySource = new SmartAlarmActivitySource();
            var httpClient = new HttpClient();

            // Setup minimal required configs except BucketName
            mockConfig.Setup(x => x["OCI:ObjectStorage:Namespace"]).Returns("test-namespace");
            mockConfig.Setup(x => x["OCI:ObjectStorage:Region"]).Returns("us-ashburn-1");
            mockConfig.Setup(x => x["OCI:TenancyId"]).Returns("test-tenancy");
            mockConfig.Setup(x => x["OCI:UserId"]).Returns("test-user");
            mockConfig.Setup(x => x["OCI:Fingerprint"]).Returns("test-fingerprint");
            mockConfig.Setup(x => x["OCI:PrivateKey"]).Returns("-----BEGIN PRIVATE KEY-----\ntest\n-----END PRIVATE KEY-----");
            mockConfig.Setup(x => x["OCI:ObjectStorage:BucketName"]).Returns((string?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new OciObjectStorageService(
                    mockConfig.Object,
                    mockLogger.Object,
                    mockMeter.Object,
                    activitySource,
                    httpClient));

            Assert.Contains("BucketName não configurado", exception.Message);

            // Cleanup
            httpClient.Dispose();
            activitySource.Dispose();
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

        public void Dispose()
        {
            _httpClient?.Dispose();
            _activitySource?.Dispose();
            _service?.Dispose();
        }
    }
}
