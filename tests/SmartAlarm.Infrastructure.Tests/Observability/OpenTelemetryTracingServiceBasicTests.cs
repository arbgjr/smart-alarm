using SmartAlarm.Domain.Abstractions;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Observability;
using SmartAlarm.Observability.Tracing;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Observability
{
    public class OpenTelemetryTracingServiceBasicTests : IDisposable
    {
        private readonly Mock<ILogger<OpenTelemetryTracingService>> _mockLogger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly OpenTelemetryTracingService _service;

        public OpenTelemetryTracingServiceBasicTests()
        {
            _mockLogger = new Mock<ILogger<OpenTelemetryTracingService>>();
            _activitySource = new SmartAlarmActivitySource();
            _service = new OpenTelemetryTracingService(_activitySource, _mockLogger.Object);
        }

        [Fact]
        public async Task TraceAsync_WithValidOperation_ShouldCompleteSuccessfully()
        {
            // Arrange
            var operation = "test-operation";
            var message = "test message";

            // Act & Assert - Should not throw
            await _service.TraceAsync(operation, message);

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
        public async Task TraceAsync_WithEmptyOperation_ShouldCompleteSuccessfully()
        {
            // Arrange
            var operation = "";
            var message = "test message";

            // Act & Assert - Should not throw
            await _service.TraceAsync(operation, message);
        }

        [Fact]
        public async Task TraceAsync_WithNullMessage_ShouldCompleteSuccessfully()
        {
            // Arrange
            var operation = "test-operation";

            // Act & Assert - Should not throw
            await _service.TraceAsync(operation, null!);
        }

        [Theory]
        [InlineData("CreateAlarm", "Creating new alarm")]
        [InlineData("UpdateUser", "Updating user profile")]
        [InlineData("DeleteSchedule", "Removing schedule")]
        [InlineData("ProcessWebhook", "Processing incoming webhook")]
        public async Task TraceAsync_WithDifferentOperations_ShouldCompleteSuccessfully(string operation, string message)
        {
            // Act & Assert - Should not throw
            await _service.TraceAsync(operation, message);

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
        public async Task TraceAsync_MultipleCalls_ShouldAllCompleteSuccessfully()
        {
            // Act
            await _service.TraceAsync("operation1", "message1");
            await _service.TraceAsync("operation2", "message2");
            await _service.TraceAsync("operation3", "message3");

            // Assert - All calls should have logged something
            _mockLogger.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(3));
        }

        public void Dispose()
        {
            _activitySource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
