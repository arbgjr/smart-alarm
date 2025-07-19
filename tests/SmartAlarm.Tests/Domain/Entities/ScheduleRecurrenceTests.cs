using System;
using SmartAlarm.Domain.Entities;
using Xunit;

namespace SmartAlarm.Tests.Domain.Entities
{
    public class ScheduleRecurrenceTests
    {
        [Theory]
        [InlineData(ScheduleRecurrence.Once, 0)]
        [InlineData(ScheduleRecurrence.Daily, 1)]
        [InlineData(ScheduleRecurrence.Weekly, 2)]
        [InlineData(ScheduleRecurrence.Weekdays, 3)]
        [InlineData(ScheduleRecurrence.Weekends, 4)]
        public void Enum_ShouldHaveExpectedValues(ScheduleRecurrence recurrence, int expectedValue)
        {
            // Assert
            Assert.Equal(expectedValue, (int)recurrence);
        }
    }
}
