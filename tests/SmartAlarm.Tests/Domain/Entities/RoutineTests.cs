using SmartAlarm.Domain.Abstractions;
using System;
using System.Collections.Generic;
using FluentAssertions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;
using Xunit;

namespace SmartAlarm.Tests.Domain.Entities
{
    public class RoutineTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateRoutine()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = new Name("Rotina Café");
            var alarmId = Guid.NewGuid();
            var actions = new List<string> { "Ligar cafeteira" };

            // Act
            var routine = new Routine(id, name, alarmId, actions);

            // Assert
            routine.Id.Should().Be(id);
            routine.Name.Should().Be(name);
            routine.AlarmId.Should().Be(alarmId);
            routine.IsActive.Should().BeTrue();
            routine.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            routine.Actions.Should().BeEquivalentTo(actions);
        }

        [Fact]
        public void Constructor_WithEmptyId_ShouldGenerateNewId()
        {
            // Arrange
            var name = new Name("Rotina Café");
            var alarmId = Guid.NewGuid();

            // Act
            var routine = new Routine(Guid.Empty, name, alarmId);

            // Assert
            routine.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Activate_ShouldSetIsActiveToTrue()
        {
            // Arrange
            var routine = CreateValidRoutine();
            routine.Deactivate();

            // Act
            routine.Activate();

            // Assert
            routine.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            // Arrange
            var routine = CreateValidRoutine();

            // Act
            routine.Deactivate();

            // Assert
            routine.IsActive.Should().BeFalse();
        }

        [Fact]
        public void UpdateName_ShouldUpdateName()
        {
            // Arrange
            var routine = CreateValidRoutine();
            var newName = new Name("Rotina Luzes");

            // Act
            routine.UpdateName(newName);

            // Assert
            routine.Name.Should().Be(newName);
        }

        [Fact]
        public void AddAction_ShouldAddAction()
        {
            // Arrange
            var routine = CreateValidRoutine();
            var action = "Ligar luz";

            // Act
            routine.AddAction(action);

            // Assert
            routine.Actions.Should().Contain(action);
        }

        [Fact]
        public void AddAction_WithDuplicate_ShouldThrow()
        {
            // Arrange
            var routine = CreateValidRoutine();
            var action = "Ligar luz";
            routine.AddAction(action);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => routine.AddAction(action));
        }

        [Fact]
        public void RemoveAction_ShouldRemoveAction()
        {
            // Arrange
            var routine = CreateValidRoutine();
            var action = "Ligar luz";
            routine.AddAction(action);

            // Act
            routine.RemoveAction(action);

            // Assert
            routine.Actions.Should().NotContain(action);
        }

        [Fact]
        public void ClearActions_ShouldRemoveAllActions()
        {
            // Arrange
            var routine = CreateValidRoutine();
            routine.AddAction("A1");
            routine.AddAction("A2");

            // Act
            routine.ClearActions();

            // Assert
            routine.Actions.Should().BeEmpty();
        }

        [Fact]
        public void HasActions_ShouldReturnTrueIfActionsExist()
        {
            // Arrange
            var routine = CreateValidRoutine();
            routine.AddAction("A1");

            // Act
            var result = routine.HasActions;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasActions_ShouldReturnFalseIfNoActions()
        {
            // Arrange
            var routine = CreateValidRoutine();

            // Act
            var result = routine.HasActions;

            // Assert
            result.Should().BeFalse();
        }

        private static Routine CreateValidRoutine()
        {
            return new Routine(Guid.NewGuid(), new Name("Rotina Café"), Guid.NewGuid());
        }
    }
}
