using SmartAlarm.Domain.Abstractions;
using SmartAlarm.IntegrationService.Application.Exceptions;
using System;
using Xunit;

namespace SmartAlarm.Tests.IntegrationService.Exceptions
{
    public class ExternalCalendarIntegrationExceptionTests
    {
        [Fact]
        public void ExternalCalendarIntegrationException_WithBasicParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var provider = "google";
            var message = "Test error message";
            var isRetryable = true;

            // Act
            var exception = new ExternalCalendarIntegrationException(provider, message, isRetryable);

            // Assert
            Assert.Equal(provider, exception.Provider);
            Assert.Equal(isRetryable, exception.IsRetryable);
            Assert.Null(exception.CalendarId);
            Assert.Contains(provider, exception.Message);
            Assert.Contains(message, exception.Message);
        }

        [Fact]
        public void ExternalCalendarIntegrationException_WithAllParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var provider = "outlook";
            var message = "Test error message";
            var isRetryable = false;
            var calendarId = "calendar-123";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new ExternalCalendarIntegrationException(
                provider, message, isRetryable, calendarId, innerException);

            // Assert
            Assert.Equal(provider, exception.Provider);
            Assert.Equal(isRetryable, exception.IsRetryable);
            Assert.Equal(calendarId, exception.CalendarId);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Contains(provider, exception.Message);
            Assert.Contains(message, exception.Message);
        }

        [Fact]
        public void ExternalCalendarTemporaryException_IsRetryableIsTrue()
        {
            // Arrange
            var provider = "apple";
            var message = "Temporary error";

            // Act
            var exception = new ExternalCalendarTemporaryException(provider, message);

            // Assert
            Assert.True(exception.IsRetryable);
            Assert.Equal(provider, exception.Provider);
            Assert.Contains(message, exception.Message);
        }

        [Fact]
        public void ExternalCalendarPermanentException_IsRetryableIsFalse()
        {
            // Arrange
            var provider = "caldav";
            var message = "Permanent error";

            // Act
            var exception = new ExternalCalendarPermanentException(provider, message);

            // Assert
            Assert.False(exception.IsRetryable);
            Assert.Equal(provider, exception.Provider);
            Assert.Contains(message, exception.Message);
        }

        [Fact]
        public void ExternalCalendarTemporaryException_WithCalendarIdAndInnerException_SetsPropertiesCorrectly()
        {
            // Arrange
            var provider = "google";
            var message = "Temporary API error";
            var calendarId = "primary";
            var innerException = new TimeoutException("Request timeout");

            // Act
            var exception = new ExternalCalendarTemporaryException(provider, message, calendarId, innerException);

            // Assert
            Assert.True(exception.IsRetryable);
            Assert.Equal(provider, exception.Provider);
            Assert.Equal(calendarId, exception.CalendarId);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Contains(message, exception.Message);
        }

        [Fact]
        public void ExternalCalendarPermanentException_WithCalendarIdAndInnerException_SetsPropertiesCorrectly()
        {
            // Arrange
            var provider = "outlook";
            var message = "Authentication failed";
            var calendarId = "work-calendar";
            var innerException = new UnauthorizedAccessException("Invalid token");

            // Act
            var exception = new ExternalCalendarPermanentException(provider, message, calendarId, innerException);

            // Assert
            Assert.False(exception.IsRetryable);
            Assert.Equal(provider, exception.Provider);
            Assert.Equal(calendarId, exception.CalendarId);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Contains(message, exception.Message);
        }
    }
}
