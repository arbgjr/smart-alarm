using SmartAlarm.IntegrationService.Application.Commands;
using System;
using System.Collections.Generic;
using Xunit;

namespace SmartAlarm.Tests.IntegrationService.Commands
{
    public class CalendarFetchResultTests
    {
        [Fact]
        public void Success_WithEvents_ReturnsSuccessfulResult()
        {
            // Arrange
            var events = new List<ExternalCalendarEvent>
            {
                new ExternalCalendarEvent("1", "Event 1", DateTime.Now, DateTime.Now.AddHours(1), "Location", "Description"),
                new ExternalCalendarEvent("2", "Event 2", DateTime.Now.AddHours(2), DateTime.Now.AddHours(3), "Location 2", "Description 2")
            };
            var retryAttempts = 2;

            // Act
            var result = CalendarFetchResult.Success(events, retryAttempts);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(events, result.Events);
            Assert.Equal(2, result.Events.Count);
            Assert.Null(result.Error);
            Assert.Equal(retryAttempts, result.RetryAttempts);
        }

        [Fact]
        public void Success_WithEmptyEvents_ReturnsSuccessfulResult()
        {
            // Arrange
            var events = new List<ExternalCalendarEvent>();

            // Act
            var result = CalendarFetchResult.Success(events);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(events, result.Events);
            Assert.Empty(result.Events);
            Assert.Null(result.Error);
            Assert.Equal(0, result.RetryAttempts);
        }

        [Fact]
        public void Failure_WithError_ReturnsFailedResult()
        {
            // Arrange
            var error = new CalendarFetchError(
                "google",
                "API_ERROR",
                "API temporarily unavailable",
                true,
                DateTime.UtcNow,
                new Exception("Original exception"),
                "primary"
            );
            var retryAttempts = 3;

            // Act
            var result = CalendarFetchResult.Failure(error, retryAttempts);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Empty(result.Events);
            Assert.Equal(error, result.Error);
            Assert.Equal(retryAttempts, result.RetryAttempts);
        }

        [Fact]
        public void CalendarFetchError_WithAllProperties_SetsCorrectly()
        {
            // Arrange
            var provider = "outlook";
            var errorCode = "TIMEOUT";
            var message = "Request timeout occurred";
            var isRetryable = true;
            var occurredAt = DateTime.UtcNow;
            var originalException = new TimeoutException("Network timeout");
            var calendarId = "work-calendar";

            // Act
            var error = new CalendarFetchError(
                provider,
                errorCode,
                message,
                isRetryable,
                occurredAt,
                originalException,
                calendarId
            );

            // Assert
            Assert.Equal(provider, error.Provider);
            Assert.Equal(errorCode, error.ErrorCode);
            Assert.Equal(message, error.Message);
            Assert.Equal(isRetryable, error.IsRetryable);
            Assert.Equal(occurredAt, error.OccurredAt);
            Assert.Equal(originalException, error.OriginalException);
            Assert.Equal(calendarId, error.CalendarId);
        }

        [Fact]
        public void CalendarFetchError_WithMinimalProperties_SetsCorrectly()
        {
            // Arrange
            var provider = "apple";
            var errorCode = "NOT_IMPLEMENTED";
            var message = "Apple Calendar integration not implemented";
            var isRetryable = false;
            var occurredAt = DateTime.UtcNow;

            // Act
            var error = new CalendarFetchError(
                provider,
                errorCode,
                message,
                isRetryable,
                occurredAt
            );

            // Assert
            Assert.Equal(provider, error.Provider);
            Assert.Equal(errorCode, error.ErrorCode);
            Assert.Equal(message, error.Message);
            Assert.Equal(isRetryable, error.IsRetryable);
            Assert.Equal(occurredAt, error.OccurredAt);
            Assert.Null(error.OriginalException);
            Assert.Null(error.CalendarId);
        }
    }
}
