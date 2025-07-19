using System;
using FluentAssertions;
using SmartAlarm.Domain.ValueObjects;
using Xunit;
using IntegrationEntity = SmartAlarm.Domain.Entities.Integration;

namespace SmartAlarm.Tests.Domain.Entities
{
    public class IntegrationTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateIntegration()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = new Name("Int 1");
            var provider = "provider";
            var configuration = "{\"key\":\"value\"}";
            var alarmId = Guid.NewGuid();

            // Act
            var integration = new IntegrationEntity(id, name, provider, configuration, alarmId);

            // Assert
            integration.Id.Should().Be(id);
            integration.Name.Should().Be(name);
            integration.Provider.Should().Be(provider);
            integration.Configuration.Should().Be(configuration);
            integration.AlarmId.Should().Be(alarmId);
            integration.IsActive.Should().BeTrue();
            integration.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            integration.LastExecutedAt.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithEmptyId_ShouldGenerateNewId()
        {
            // Arrange
            var name = new Name("Int 1");
            var provider = "provider";
            var configuration = "{}";
            var alarmId = Guid.NewGuid();

            // Act
            var integration = new IntegrationEntity(Guid.Empty, name, provider, configuration, alarmId);

            // Assert
            integration.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Activate_ShouldSetIsActiveToTrue()
        {
            // Arrange
            var integration = CreateValidIntegration();
            integration.Deactivate();

            // Act
            integration.Activate();

            // Assert
            integration.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            // Arrange
            var integration = CreateValidIntegration();

            // Act
            integration.Deactivate();

            // Assert
            integration.IsActive.Should().BeFalse();
        }

        [Fact]
        public void UpdateName_ShouldUpdateName()
        {
            // Arrange
            var integration = CreateValidIntegration();
            var newName = new Name("Int 2");

            // Act
            integration.UpdateName(newName);

            // Assert
            integration.Name.Should().Be(newName);
        }

        [Fact]
        public void UpdateConfiguration_ShouldUpdateConfiguration()
        {
            // Arrange
            var integration = CreateValidIntegration();
            var newConfig = "{\"key\":\"newValue\"}";

            // Act
            integration.UpdateConfiguration(newConfig);

            // Assert
            integration.Configuration.Should().Be(newConfig);
        }

        [Fact]
        public void UpdateConfiguration_WithInvalidJson_ShouldThrow()
        {
            // Arrange
            var integration = CreateValidIntegration();
            var invalidConfig = "not-json";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => integration.UpdateConfiguration(invalidConfig));
        }

        [Fact]
        public void RecordExecution_ShouldSetLastExecutedAt()
        {
            // Arrange
            var integration = CreateValidIntegration();
            integration.Activate();

            // Act
            integration.RecordExecution();

            // Assert
            integration.LastExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void RecordExecution_WhenInactive_ShouldThrow()
        {
            // Arrange
            var integration = CreateValidIntegration();
            integration.Deactivate();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => integration.RecordExecution());
        }

        private static IntegrationEntity CreateValidIntegration()
        {
            return new IntegrationEntity(Guid.NewGuid(), new Name("Int 1"), "provider", "{}", Guid.NewGuid());
        }
    }
}
