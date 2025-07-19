using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.IntegrationService.Application.Exceptions;
using SmartAlarm.IntegrationService.Application.Services;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SmartAlarm.Tests.IntegrationService.Services
{
    public class CalendarRetryServiceTests
    {
        private readonly Mock<ILogger<CalendarRetryService>> _loggerMock;
        private readonly CalendarRetryService _retryService;

        public CalendarRetryServiceTests()
        {
            _loggerMock = new Mock<ILogger<CalendarRetryService>>();
            _retryService = new CalendarRetryService(_loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_SuccessOnFirstAttempt_ReturnsResultWithoutRetry()
        {
            // Arrange
            var expectedResult = "success";
            var operationCalled = false;

            // Act
            var result = await _retryService.ExecuteWithRetryAsync(
                async (ct) =>
                {
                    operationCalled = true;
                    return await Task.FromResult(expectedResult);
                },
                "TestOperation",
                "TestProvider"
            );

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.True(operationCalled);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_RetryableExceptionThenSuccess_RetriesAndSucceeds()
        {
            // Arrange
            var attemptCount = 0;
            var expectedResult = "success";

            // Act
            var result = await _retryService.ExecuteWithRetryAsync(
                async (ct) =>
                {
                    attemptCount++;
                    if (attemptCount == 1)
                    {
                        throw new ExternalCalendarTemporaryException("test", "Temporary failure");
                    }
                    return await Task.FromResult(expectedResult);
                },
                "TestOperation",
                "TestProvider",
                new RetryPolicy(MaxAttempts: 3, InitialDelay: TimeSpan.FromMilliseconds(10))
            );

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(2, attemptCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_PermanentException_DoesNotRetryAndThrows()
        {
            // Arrange
            var attemptCount = 0;
            var permanentException = new ExternalCalendarPermanentException("test", "Permanent failure");

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<ExternalCalendarPermanentException>(async () =>
            {
                await _retryService.ExecuteWithRetryAsync<string>(
                    (ct) =>
                    {
                        attemptCount++;
                        throw permanentException;
                    },
                    "TestOperation",
                    "TestProvider"
                );
            });

            Assert.Equal(1, attemptCount);
            Assert.Equal(permanentException, thrownException);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_RetryableExceptionExceedsMaxAttempts_ThrowsTemporaryException()
        {
            // Arrange
            var attemptCount = 0;
            var originalException = new ExternalCalendarTemporaryException("test", "Always failing");

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<ExternalCalendarTemporaryException>(async () =>
            {
                await _retryService.ExecuteWithRetryAsync<string>(
                    (ct) =>
                    {
                        attemptCount++;
                        throw originalException;
                    },
                    "TestOperation",
                    "TestProvider",
                    new RetryPolicy(MaxAttempts: 2, InitialDelay: TimeSpan.FromMilliseconds(10))
                );
            });

            Assert.Equal(2, attemptCount);
            Assert.IsType<ExternalCalendarTemporaryException>(thrownException);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_HttpRequestExceptionWithRetryableMessage_Retries()
        {
            // Arrange
            var attemptCount = 0;
            var expectedResult = "success";

            // Act
            var result = await _retryService.ExecuteWithRetryAsync(
                async (ct) =>
                {
                    attemptCount++;
                    if (attemptCount == 1)
                    {
                        throw new HttpRequestException("Connection timeout occurred");
                    }
                    return await Task.FromResult(expectedResult);
                },
                "TestOperation",
                "TestProvider",
                new RetryPolicy(MaxAttempts: 3, InitialDelay: TimeSpan.FromMilliseconds(10))
            );

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(2, attemptCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_TaskCanceledException_DoesNotRetry()
        {
            // Arrange
            var attemptCount = 0;
            var canceledException = new TaskCanceledException("Operation was canceled");

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await _retryService.ExecuteWithRetryAsync<string>(
                    (ct) =>
                    {
                        attemptCount++;
                        throw canceledException;
                    },
                    "TestOperation",
                    "TestProvider"
                );
            });

            Assert.Equal(1, attemptCount);
            Assert.Equal(canceledException, thrownException);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_UnknownException_DoesNotRetryAndThrows()
        {
            // Arrange
            var attemptCount = 0;
            var unknownException = new InvalidOperationException("Unknown error");

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _retryService.ExecuteWithRetryAsync<string>(
                    (ct) =>
                    {
                        attemptCount++;
                        throw unknownException;
                    },
                    "TestOperation",
                    "TestProvider"
                );
            });

            Assert.Equal(1, attemptCount);
            Assert.Equal(unknownException, thrownException);
        }

        [Fact]
        public void RetryPolicy_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var policy = new RetryPolicy();

            // Assert
            Assert.Equal(3, policy.MaxAttempts);
            Assert.Equal(TimeSpan.FromSeconds(1), policy.InitialDelay);
            Assert.Equal(2.0, policy.BackoffMultiplier);
            Assert.Equal(TimeSpan.FromSeconds(30), policy.MaxDelay);
        }

        [Fact]
        public void RetryPolicy_CustomValues_AreCorrect()
        {
            // Arrange & Act
            var policy = new RetryPolicy(
                MaxAttempts: 5,
                InitialDelay: TimeSpan.FromSeconds(2),
                BackoffMultiplier: 1.5,
                MaxDelay: TimeSpan.FromSeconds(60)
            );

            // Assert
            Assert.Equal(5, policy.MaxAttempts);
            Assert.Equal(TimeSpan.FromSeconds(2), policy.InitialDelay);
            Assert.Equal(1.5, policy.BackoffMultiplier);
            Assert.Equal(TimeSpan.FromSeconds(60), policy.MaxDelay);
        }
    }
}
