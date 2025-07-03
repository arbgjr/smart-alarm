using System;
using Xunit;
using FluentAssertions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;

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
            var time = DateTime.Now.AddHours(1);
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
            alarm.Routines.Should().BeEmpty();
            alarm.Integrations.Should().BeEmpty();
            alarm.Schedules.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_WithEmptyId_ShouldGenerateNewId()
        {
            // Arrange
            var name = new Name("Alarme Teste");
            var time = DateTime.Now.AddHours(1);
            var userId = Guid.NewGuid();

            // Act
            var alarm = new Alarm(Guid.Empty, name, time, true, userId);

            // Assert
            alarm.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var time = DateTime.Now.AddHours(1);
            var userId = Guid.NewGuid();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Alarm(id, (Name)null, time, true, userId));
        }

        [Fact]
        public void Constructor_WithEmptyUserId_ShouldThrowArgumentException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = new Name("Alarme Teste");
            var time = DateTime.Now.AddHours(1);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Alarm(id, name, time, true, Guid.Empty));
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
        public void AddRoutine_WithValidRoutine_ShouldAddToCollection()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var routine = new Routine(Guid.NewGuid(), new Name("Rotina Teste"), alarm.Id);

            // Act
            alarm.AddRoutine(routine);

            // Assert
            alarm.Routines.Should().Contain(routine);
        }

        [Fact]
        public void AddRoutine_WithNullRoutine_ShouldThrowArgumentNullException()
        {
            // Arrange
            var alarm = CreateValidAlarm();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => alarm.AddRoutine(null));
        }

        [Fact]
        public void AddRoutine_WithDuplicateRoutine_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            var routine = new Routine(Guid.NewGuid(), new Name("Rotina Teste"), alarm.Id);
            alarm.AddRoutine(routine);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => alarm.AddRoutine(routine));
        }

        [Fact]
        public void RecordTriggered_WhenEnabled_ShouldUpdateLastTriggeredAt()
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
        public void RecordTriggered_WhenDisabled_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var alarm = CreateValidAlarm();
            alarm.Disable();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => alarm.RecordTriggered());
        }

        private static Alarm CreateValidAlarm()
        {
            return new Alarm(
                Guid.NewGuid(),
                new Name("Alarme Teste"),
                DateTime.Now.AddHours(1),
                true,
                Guid.NewGuid()
            );
        }
    }
}
