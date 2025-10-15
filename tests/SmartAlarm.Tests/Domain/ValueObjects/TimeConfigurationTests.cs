using SmartAlarm.Domain.Abstractions;
using System;
using FluentAssertions;
using SmartAlarm.Domain.ValueObjects;
using Xunit;

namespace SmartAlarm.Tests.Domain.ValueObjects
{
    public class TimeConfigurationTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateTimeConfiguration()
        {
            // Arrange
            var time = new TimeOnly(7, 0);
            var timeZone = "UTC";

            // Act
            var config = new TimeConfiguration(time, timeZone);

            // Assert
            config.Time.Should().Be(time);
            config.TimeZone.Should().Be(timeZone);
        }

        [Fact]
        public void Constructor_WithInvalidTimeZone_ShouldThrow()
        {
            // Arrange
            var time = new TimeOnly(7, 0);
            var invalidTimeZone = "INVALID_TZ";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new TimeConfiguration(time, invalidTimeZone));
        }

        [Fact]
        public void GetDateTimeForToday_ShouldReturnUtcDateTime()
        {
            // Arrange
            var time = new TimeOnly(7, 0);
            var config = new TimeConfiguration(time, "UTC");

            // Act
            var result = config.GetDateTimeForToday();

            // Assert
            result.Kind.Should().Be(DateTimeKind.Utc);
            result.Hour.Should().Be(7);
        }

        [Fact]
        public void Equals_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var config1 = new TimeConfiguration(new TimeOnly(7, 0), "UTC");
            var config2 = new TimeConfiguration(new TimeOnly(7, 0), "UTC");

            // Act
            var result = config1.Equals(config2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_WithDifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var config1 = new TimeConfiguration(new TimeOnly(7, 0), "UTC");
            var config2 = new TimeConfiguration(new TimeOnly(8, 0), "UTC");

            // Act
            var result = config1.Equals(config2);

            // Assert
            result.Should().BeFalse();
        }
    }
}
