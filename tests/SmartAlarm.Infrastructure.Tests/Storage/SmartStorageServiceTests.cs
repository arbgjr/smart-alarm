using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Configuration;
using SmartAlarm.Infrastructure.Storage;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Storage
{
    public class SmartStorageServiceTests
    {
        private readonly Mock<ILogger<SmartStorageService>> _loggerMock;
        private readonly Mock<ILogger<MinioStorageService>> _minioLoggerMock;
        private readonly Mock<ILogger<MockStorageService>> _mockLoggerMock;
        private readonly Mock<IConfigurationResolver> _configResolverMock;
        private readonly Mock<SmartAlarmMeter> _meterMock;
        private readonly Mock<ICorrelationContext> _correlationContextMock;
        private readonly Mock<SmartAlarmActivitySource> _activitySourceMock;

        public SmartStorageServiceTests()
        {
            _loggerMock = new Mock<ILogger<SmartStorageService>>();
            _minioLoggerMock = new Mock<ILogger<MinioStorageService>>();
            _mockLoggerMock = new Mock<ILogger<MockStorageService>>();
            _configResolverMock = new Mock<IConfigurationResolver>();
            _meterMock = new Mock<SmartAlarmMeter>();
            _correlationContextMock = new Mock<ICorrelationContext>();
            _activitySourceMock = new Mock<SmartAlarmActivitySource>();

            // Setup configuração padrão do MinIO
            _configResolverMock.Setup(x => x.GetConfigAsync(It.Is<string>(s => s == "MINIO_ENDPOINT"), It.IsAny<CancellationToken>()))
                .ReturnsAsync("localhost");
            _configResolverMock.Setup(x => x.GetConfigAsync(It.Is<string>(s => s == "MINIO_PORT"), It.IsAny<CancellationToken>()))
                .ReturnsAsync("9000");
        }

        [Fact(DisplayName = "Construtor deve criar instância sem erro")]
        [Trait("Category", "Unit")]
        public void Constructor_Should_CreateInstance_WithoutError()
        {
            // AAA: Arrange, Act, Assert - padrão obrigatório para todos os testes.
            // Arrange & Act
            var service = CreateSmartStorageService();

            // Assert
            Assert.NotNull(service);
        }

        [Fact(DisplayName = "Upload deve usar fallback quando MinIO falha")]
        [Trait("Category", "Unit")]
        public async Task UploadAsync_Should_UseFallback_When_MinIOFails()
        {
            // Arrange
            var service = CreateSmartStorageService();
            var testPath = "/test-upload.txt";
            using var testStream = new MemoryStream();

            // Act - Como MinIO não está rodando, deve usar fallback automaticamente
            await service.UploadAsync(testPath, testStream);

            // Assert - Verifica se logou o uso do fallback
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("fallback") && v.ToString()!.Contains(testPath)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact(DisplayName = "Download deve usar fallback quando MinIO falha")]
        [Trait("Category", "Unit")]
        public async Task DownloadAsync_Should_UseFallback_When_MinIOFails()
        {
            // Arrange
            var service = CreateSmartStorageService();
            var testPath = "/test-download.txt";

            // Act
            var result = await service.DownloadAsync(testPath);

            // Assert
            Assert.NotNull(result);
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("fallback") && v.ToString()!.Contains(testPath)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact(DisplayName = "Delete deve usar fallback quando MinIO falha")]
        [Trait("Category", "Unit")]
        public async Task DeleteAsync_Should_UseFallback_When_MinIOFails()
        {
            // Arrange
            var service = CreateSmartStorageService();
            var testPath = "/test-delete.txt";

            // Act
            await service.DeleteAsync(testPath);

            // Assert
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("fallback") && v.ToString()!.Contains(testPath)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact(DisplayName = "Deve logar warning quando serviço primário falha")]
        [Trait("Category", "Unit")]
        public async Task Should_LogWarning_When_PrimaryServiceFails()
        {
            // Arrange
            var service = CreateSmartStorageService();
            var testPath = "/test-warning.txt";
            using var testStream = new MemoryStream();

            // Act
            await service.UploadAsync(testPath, testStream);

            // Assert - Verifica se logou o warning da falha do MinIO
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Primary storage service failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact(DisplayName = "Múltiplas operações devem manter estado de fallback")]
        [Trait("Category", "Unit")]
        public async Task MultipleOperations_Should_MaintainFallbackState()
        {
            // Arrange
            var service = CreateSmartStorageService();
            using var stream1 = new MemoryStream();
            using var stream2 = new MemoryStream();

            // Act
            await service.UploadAsync("/file1.txt", stream1);
            await service.UploadAsync("/file2.txt", stream2);
            await service.DeleteAsync("/file1.txt");

            // Assert - Segunda operação deve usar diretamente o fallback (sem tentar MinIO novamente)
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("fallback")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(2)); // Pelo menos nas últimas 2 operações
        }

        private SmartStorageService CreateSmartStorageService()
        {
            return new SmartStorageService(
                _loggerMock.Object,
                _minioLoggerMock.Object,
                _mockLoggerMock.Object,
                _configResolverMock.Object,
                _meterMock.Object,
                _correlationContextMock.Object,
                _activitySourceMock.Object);
        }
    }
}
