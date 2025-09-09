using SmartAlarm.Domain.Abstractions;
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
        private readonly Mock<IConfigurationResolver> _configResolverMock;
        private readonly Mock<SmartAlarmMeter> _meterMock;
        private readonly Mock<ICorrelationContext> _correlationContextMock;
        private readonly Mock<SmartAlarmActivitySource> _activitySourceMock;

        public SmartStorageServiceTests()
        {
            _loggerMock = new Mock<ILogger<SmartStorageService>>();
            _minioLoggerMock = new Mock<ILogger<MinioStorageService>>();
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

        [Fact(DisplayName = "Upload deve completar mesmo quando MinIO falha")]
        [Trait("Category", "Unit")]
        public async Task UploadAsync_Should_Complete_When_MinIOFails()
        {
            // Arrange
            var service = CreateSmartStorageService();
            var testPath = "/test-upload.txt";
            using var testStream = new MemoryStream();

            // Act - Como MinIO não está rodando, circuit breaker deve lidar com falhas
            await service.UploadAsync(testPath, testStream);

            // Assert - Verifica se logou erro ou warning sobre falha de storage
            _loggerMock.Verify(
                l => l.Log(
                    It.IsIn(LogLevel.Warning, LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Storage") || v.ToString()!.Contains("failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact(DisplayName = "Download deve retornar stream quando MinIO falha")]
        [Trait("Category", "Unit")]
        public async Task DownloadAsync_Should_ReturnStream_When_MinIOFails()
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
                    It.IsIn(LogLevel.Warning, LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Storage") || v.ToString()!.Contains("failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact(DisplayName = "Delete deve completar quando MinIO falha")]
        [Trait("Category", "Unit")]
        public async Task DeleteAsync_Should_Complete_When_MinIOFails()
        {
            // Arrange
            var service = CreateSmartStorageService();
            var testPath = "/test-delete.txt";

            // Act
            await service.DeleteAsync(testPath);

            // Assert
            _loggerMock.Verify(
                l => l.Log(
                    It.IsIn(LogLevel.Warning, LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Storage") || v.ToString()!.Contains("failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact(DisplayName = "Deve logar quando circuit breaker atua")]
        [Trait("Category", "Unit")]
        public async Task Should_LogCircuitBreakerEvents()
        {
            // Arrange
            var service = CreateSmartStorageService();
            var testPath = "/test-circuit-breaker.txt";
            using var testStream = new MemoryStream();

            // Act
            await service.UploadAsync(testPath, testStream);

            // Assert - Verifica se logou eventos de circuit breaker ou falhas de storage
            _loggerMock.Verify(
                l => l.Log(
                    It.IsIn(LogLevel.Warning, LogLevel.Error, LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Storage") || v.ToString()!.Contains("circuit") || v.ToString()!.Contains("failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact(DisplayName = "Múltiplas operações devem funcionar com circuit breaker")]
        [Trait("Category", "Unit")]
        public async Task MultipleOperations_Should_WorkWithCircuitBreaker()
        {
            // Arrange
            var service = CreateSmartStorageService();
            using var stream1 = new MemoryStream();
            using var stream2 = new MemoryStream();

            // Act
            await service.UploadAsync("/file1.txt", stream1);
            await service.UploadAsync("/file2.txt", stream2);
            await service.DeleteAsync("/file1.txt");

            // Assert - Verifica que o serviço continua funcionando mesmo com falhas
            _loggerMock.Verify(
                l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(1)); // Pelo menos um log foi chamado
        }

        private SmartStorageService CreateSmartStorageService()
        {
            return new SmartStorageService(
                _loggerMock.Object,
                _minioLoggerMock.Object,
                _configResolverMock.Object,
                _meterMock.Object,
                _correlationContextMock.Object,
                _activitySourceMock.Object);
        }
    }
}
