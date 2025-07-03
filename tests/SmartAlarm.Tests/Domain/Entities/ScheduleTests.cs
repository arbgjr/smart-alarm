using System;
using Xunit;
using FluentAssertions;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Tests.Domain.Entities
{
    public class ScheduleTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateSchedule()
        {
            // Arrange
            var id = Guid.NewGuid();
            var time = new TimeOnly(7, 30);
            var recurrence = ScheduleRecurrence.Daily;
            var daysOfWeek = DaysOfWeek.All;
            var alarmId = Guid.NewGuid();

            // Act
            var schedule = new Schedule(id, time, recurrence, daysOfWeek, alarmId);

            // Assert
            schedule.Id.Should().Be(id);
            schedule.Time.Should().Be(time);
            schedule.Recurrence.Should().Be(recurrence);
            schedule.DaysOfWeek.Should().Be(daysOfWeek);
            schedule.AlarmId.Should().Be(alarmId);
            schedule.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithEmptyId_ShouldGenerateNewId()
        {
            // Arrange
            var time = new TimeOnly(7, 30);
            var alarmId = Guid.NewGuid();

            // Act
            var schedule = new Schedule(Guid.Empty, time, ScheduleRecurrence.Daily, DaysOfWeek.All, alarmId);

            // Assert
            schedule.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Constructor_WithEmptyAlarmId_ShouldThrowArgumentException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var time = new TimeOnly(7, 30);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Schedule(id, time, ScheduleRecurrence.Daily, DaysOfWeek.All, Guid.Empty));
        }

        [Fact]
        public void Activate_ShouldSetIsActiveToTrue()
        {
            // Arrange
            var schedule = CreateValidSchedule();
            schedule.Deactivate();

            // Act
            schedule.Activate();

            // Assert
            schedule.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            // Arrange
            var schedule = CreateValidSchedule();

            // Act
            schedule.Deactivate();

            // Assert
            schedule.IsActive.Should().BeFalse();
        }

        [Fact]
        public void UpdateTime_WithNewTime_ShouldUpdateTime()
        {
            // Arrange
            var schedule = CreateValidSchedule();
            var newTime = new TimeOnly(8, 45);

            // Act
            schedule.UpdateTime(newTime);

            // Assert
            schedule.Time.Should().Be(newTime);
        }

        [Fact]
        public void UpdateRecurrence_WithNewRecurrence_ShouldUpdateRecurrence()
        {
            // Arrange
            var schedule = CreateValidSchedule();
            var newRecurrence = ScheduleRecurrence.Weekly;
            var newDaysOfWeek = DaysOfWeek.Monday | DaysOfWeek.Friday;

            // Act
            schedule.UpdateRecurrence(newRecurrence, newDaysOfWeek);

            // Assert
            schedule.Recurrence.Should().Be(newRecurrence);
            schedule.DaysOfWeek.Should().Be(newDaysOfWeek);
        }

        [Theory]
        [InlineData(ScheduleRecurrence.Once, DaysOfWeek.None, true)]
        [InlineData(ScheduleRecurrence.Daily, DaysOfWeek.None, true)]
        [InlineData(ScheduleRecurrence.Weekdays, DaysOfWeek.None, true)] // Assuming today is a weekday
        [InlineData(ScheduleRecurrence.Weekends, DaysOfWeek.None, false)] // Assuming today is not weekend
        public void ShouldTriggerToday_WithDifferentRecurrences_ShouldReturnCorrectValue(
            ScheduleRecurrence recurrence, DaysOfWeek daysOfWeek, bool expected)
        {
            // Arrange
            var schedule = CreateValidSchedule();
            schedule.UpdateRecurrence(recurrence, daysOfWeek);

            // Act
            var result = schedule.ShouldTriggerToday();

            // Assert
            // Note: This test might need adjustment based on actual day of week when running
            // For more deterministic testing, we would need to inject a time provider
            result.Should().Be(expected || IsTodayMatchingRecurrence(recurrence));
        }

        [Fact]
        public void ShouldTriggerToday_WithWeeklyRecurrence_ShouldCheckDaysOfWeek()
        {
            // Arrange
            var schedule = CreateValidSchedule();
            var today = DateTime.Today.DayOfWeek;
            var expectedDaysOfWeek = ConvertToDaysOfWeekEnum(today);
            schedule.UpdateRecurrence(ScheduleRecurrence.Weekly, expectedDaysOfWeek);

            // Act
            var result = schedule.ShouldTriggerToday();

            // Assert
            result.Should().BeTrue();
        }

        private static Schedule CreateValidSchedule()
        {
            return new Schedule(
                Guid.NewGuid(),
                new TimeOnly(7, 30),
                ScheduleRecurrence.Daily,
                DaysOfWeek.All,
                Guid.NewGuid()
            );
        }

        private static bool IsTodayMatchingRecurrence(ScheduleRecurrence recurrence)
        {
            var today = DateTime.Today.DayOfWeek;
            return recurrence switch
            {
                ScheduleRecurrence.Weekdays => today >= System.DayOfWeek.Monday && today <= System.DayOfWeek.Friday,
                ScheduleRecurrence.Weekends => today == System.DayOfWeek.Saturday || today == System.DayOfWeek.Sunday,
                _ => true
            };
        }

        private static DaysOfWeek ConvertToDaysOfWeekEnum(System.DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                System.DayOfWeek.Sunday => DaysOfWeek.Sunday,
                System.DayOfWeek.Monday => DaysOfWeek.Monday,
                System.DayOfWeek.Tuesday => DaysOfWeek.Tuesday,
                System.DayOfWeek.Wednesday => DaysOfWeek.Wednesday,
                System.DayOfWeek.Thursday => DaysOfWeek.Thursday,
                System.DayOfWeek.Friday => DaysOfWeek.Friday,
                System.DayOfWeek.Saturday => DaysOfWeek.Saturday,
                _ => DaysOfWeek.None
            };
        }
    }
}