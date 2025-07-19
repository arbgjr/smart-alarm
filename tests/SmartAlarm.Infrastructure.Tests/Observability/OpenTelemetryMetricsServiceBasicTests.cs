using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Observability;
using SmartAlarm.Observability.Metrics;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Observability
{
    public class OpenTelemetryMetricsServiceBasicTests : IDisposable
    {
        private readonly Mock<ILogger<OpenTelemetryMetricsService>> _mockLogger;
        private readonly SmartAlarmMeter _meter;
        private readonly OpenTelemetryMetricsService _service;

        public OpenTelemetryMetricsServiceBasicTests()
        {
            _mockLogger = new Mock<ILogger<OpenTelemetryMetricsService>>();
            _meter = new SmartAlarmMeter();
            _service = new OpenTelemetryMetricsService(_meter, _mockLogger.Object);
        }

        [Fact]
        public async Task IncrementAsync_WithValidMetric_ShouldCompleteSuccessfully()
        {
            // Arrange
            var metricName = "test_metric";

            // Act & Assert - Should not throw
            await _service.IncrementAsync(metricName);

            // Verify some logging occurred
            _mockLogger.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task RecordAsync_WithValidMetric_ShouldCompleteSuccessfully()
        {
            // Arrange
            var metricName = "test_duration";
            var value = 123.45;

            // Act & Assert - Should not throw
            await _service.RecordAsync(metricName, value);

            // Verify some logging occurred
            _mockLogger.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Theory]
        [InlineData("alarm_triggered")]
        [InlineData("user_registration")]
        [InlineData("authentication")]
        [InlineData("custom_metric")]
        public async Task IncrementAsync_WithDifferentMetrics_ShouldCompleteSuccessfully(string metricName)
        {
            // Act & Assert - Should not throw
            await _service.IncrementAsync(metricName);

            // Verify logging occurred
            _mockLogger.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Theory]
        [InlineData("request_duration", 123.45)]
        [InlineData("alarm_creation_duration", 234.56)]
        [InlineData("database_query_duration", 12.34)]
        [InlineData("external_service_duration", 456.78)]
        [InlineData("custom_duration", 789.12)]
        public async Task RecordAsync_WithDifferentMetrics_ShouldCompleteSuccessfully(string metricName, double value)
        {
            // Act & Assert - Should not throw
            await _service.RecordAsync(metricName, value);

            // Verify logging occurred
            _mockLogger.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task IncrementAsync_WithEmptyMetricName_ShouldCompleteSuccessfully()
        {
            // Act & Assert - Should not throw
            await _service.IncrementAsync("");
        }

        [Fact]
        public async Task RecordAsync_WithZeroValue_ShouldCompleteSuccessfully()
        {
            // Act & Assert - Should not throw
            await _service.RecordAsync("test_metric", 0.0);
        }

        [Fact]
        public async Task RecordAsync_WithNegativeValue_ShouldCompleteSuccessfully()
        {
            // Act & Assert - Should not throw
            await _service.RecordAsync("test_metric", -10.5);
        }

        [Fact]
        public async Task MultipleCalls_ShouldAllCompleteSuccessfully()
        {
            // Act
            await _service.IncrementAsync("counter1");
            await _service.IncrementAsync("counter2");
            await _service.RecordAsync("duration1", 100.5);
            await _service.RecordAsync("duration2", 200.7);

            // Assert - All calls should have logged something
            _mockLogger.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(4));
        }

        public void Dispose()
        {
            _meter?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
