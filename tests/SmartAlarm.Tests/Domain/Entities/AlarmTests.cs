using System;
using System.Collections.Generic;
using FluentAssertions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;
using Xunit;
using IntegrationEntity = SmartAlarm.Domain.Entities.Integration;

namespace SmartAlarm.Tests.Domain.Entities
{
    public class AlarmTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateAlarm()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = new Name("Alarme Matinal");
            var time = DateTime.Today.AddHours(7);
            var userId = Guid.NewGuid();

            // Act
            var alarm = new Alarm(id, name, time, true, userId);

            // Assert
            alarm.Id.Should().Be(id);
            alarm.Name.Should().Be(name);
            alarm.Time.Should().Be(time);
            alarm.Enabled.Should().BeTrue();
            alarm.UserId.Should().Be(userId);
            alarm.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            alarm.LastTriggeredAt.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithEmptyId_ShouldGenerateNewId()
        {
            // Arrange
            var name = new Name("Alarme Matinal");
            var time = DateTime.Today.AddHours(7);
            var userId = Guid.NewGuid();

            // Act
            var alarm = new Alarm(Guid.Empty, name, time, true, userId);

            // Assert
            alarm.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Enable_ShouldSetEnabledToTrue()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            alarm.Disable();

            // Act
            alarm.Enable();

            // Assert
            alarm.Enabled.Should().BeTrue();
        }

        [Fact]
        public void Disable_ShouldSetEnabledToFalse()
        {
            // Arrange
            var alarm = CreateValidAlarm();

            // Act
            alarm.Disable();

            // Assert
            alarm.Enabled.Should().BeFalse();
        }

        [Fact]
        public void UpdateName_ShouldUpdateName()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var newName = new Name("Alarme Noturno");

            // Act
            alarm.UpdateName(newName);

            // Assert
            alarm.Name.Should().Be(newName);
        }

        [Fact]
        public void UpdateTime_ShouldUpdateTime()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var newTime = DateTime.Today.AddHours(22);

            // Act
            alarm.UpdateTime(newTime);

            // Assert
            alarm.Time.Should().Be(newTime);
        }

        [Fact]
        public void AddRoutine_ShouldAddRoutine()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var routine = new Routine(Guid.NewGuid(), new Name("Rotina 1"), alarm.Id);

            // Act
            alarm.AddRoutine(routine);

            // Assert
            alarm.Routines.Should().Contain(routine);
        }

        [Fact]
        public void AddRoutine_WithDuplicate_ShouldThrow()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var routine = new Routine(Guid.NewGuid(), new Name("Rotina 1"), alarm.Id);
            alarm.AddRoutine(routine);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => alarm.AddRoutine(routine));
        }

        [Fact]
        public void RemoveRoutine_ShouldRemoveRoutine()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var routine = new Routine(Guid.NewGuid(), new Name("Rotina 1"), alarm.Id);
            alarm.AddRoutine(routine);

            // Act
            alarm.RemoveRoutine(routine.Id);

            // Assert
            alarm.Routines.Should().NotContain(routine);
        }

        [Fact]
        public void AddIntegration_ShouldAddIntegration()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var integration = new IntegrationEntity(Guid.NewGuid(), new Name("Int 1"), "provider", "{}", alarm.Id);

            // Act
            alarm.AddIntegration(integration);

            // Assert
            alarm.Integrations.Should().Contain(integration);
        }

        [Fact]
        public void AddIntegration_WithDuplicate_ShouldThrow()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var integration = new IntegrationEntity(Guid.NewGuid(), new Name("Int 1"), "provider", "{}", alarm.Id);
            alarm.AddIntegration(integration);

            // Act & Assert
            Action act = () => alarm.AddIntegration(integration);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RemoveIntegration_ShouldRemoveIntegration()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var integration = new IntegrationEntity(Guid.NewGuid(), new Name("Int 1"), "provider", "{}", alarm.Id);
            alarm.AddIntegration(integration);

            // Act
            alarm.RemoveIntegration(integration.Id);

            // Assert
            alarm.Integrations.Should().NotContain(integration);
        }

        [Fact]
        public void AddSchedule_ShouldAddSchedule()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var schedule = new Schedule(Guid.NewGuid(), new TimeOnly(7, 0), ScheduleRecurrence.Daily, DaysOfWeek.All, alarm.Id);

            // Act
            alarm.AddSchedule(schedule);

            // Assert
            alarm.Schedules.Should().Contain(schedule);
        }

        [Fact]
        public void AddSchedule_WithWrongAlarmId_ShouldThrow()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var schedule = new Schedule(Guid.NewGuid(), new TimeOnly(7, 0), ScheduleRecurrence.Daily, DaysOfWeek.All, Guid.NewGuid());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => alarm.AddSchedule(schedule));
        }

        [Fact]
        public void AddSchedule_WithDuplicate_ShouldThrow()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var schedule = new Schedule(Guid.NewGuid(), new TimeOnly(7, 0), ScheduleRecurrence.Daily, DaysOfWeek.All, alarm.Id);
            alarm.AddSchedule(schedule);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => alarm.AddSchedule(schedule));
        }

        [Fact]
        public void RemoveSchedule_ShouldRemoveSchedule()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var schedule = new Schedule(Guid.NewGuid(), new TimeOnly(7, 0), ScheduleRecurrence.Daily, DaysOfWeek.All, alarm.Id);
            alarm.AddSchedule(schedule);

            // Act
            alarm.RemoveSchedule(schedule.Id);

            // Assert
            alarm.Schedules.Should().NotContain(schedule);
        }

        [Fact]
        public void RecordTriggered_ShouldSetLastTriggeredAt()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            alarm.Enable();

            // Act
            alarm.RecordTriggered();

            // Assert
            alarm.LastTriggeredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void RecordTriggered_WhenDisabled_ShouldThrow()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            alarm.Disable();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => alarm.RecordTriggered());
        }

        private static Alarm CreateValidAlarm()
        {
            return new Alarm(Guid.NewGuid(), new Name("Alarme Matinal"), DateTime.Today.AddHours(7), true, Guid.NewGuid());
        }
    }
}
