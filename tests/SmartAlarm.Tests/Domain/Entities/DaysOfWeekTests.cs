using System;
using SmartAlarm.Domain.Entities;
using Xunit;

namespace SmartAlarm.Tests.Domain.Entities
{
    public class DaysOfWeekTests
    {
        [Fact]
        public void Flags_ShouldCombineCorrectly()
        {
            // Arrange
            var combined = DaysOfWeek.Monday | DaysOfWeek.Wednesday;

            // Assert
            Assert.True(combined.HasFlag(DaysOfWeek.Monday));
            Assert.True(combined.HasFlag(DaysOfWeek.Wednesday));
            Assert.False(combined.HasFlag(DaysOfWeek.Friday));
        }

        [Fact]
        public void Weekdays_ShouldContainAllWeekdays()
        {
            // Assert
            Assert.True(DaysOfWeek.Weekdays.HasFlag(DaysOfWeek.Monday));
            Assert.True(DaysOfWeek.Weekdays.HasFlag(DaysOfWeek.Friday));
            Assert.False(DaysOfWeek.Weekdays.HasFlag(DaysOfWeek.Sunday));
        }

        [Fact]
        public void Weekends_ShouldContainSaturdayAndSunday()
        {
            // Assert
            Assert.True(DaysOfWeek.Weekends.HasFlag(DaysOfWeek.Saturday));
            Assert.True(DaysOfWeek.Weekends.HasFlag(DaysOfWeek.Sunday));
            Assert.False(DaysOfWeek.Weekends.HasFlag(DaysOfWeek.Monday));
        }
    }
}
