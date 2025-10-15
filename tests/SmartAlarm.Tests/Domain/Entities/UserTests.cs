using SmartAlarm.Domain.Abstractions;
using System;
using Xunit;
using FluentAssertions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Tests.Domain.Entities
{
    public class UserTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateUser()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = new Name("João Silva");
            var email = new Email("joao@example.com");

            // Act
            var user = new User(id, name, email, true);

            // Assert
            user.Id.Should().Be(id);
            user.Name.Should().Be(name);
            user.Email.Should().Be(email);
            user.IsActive.Should().BeTrue();
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            user.LastLoginAt.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithStringParameters_ShouldCreateUser()
        {
            // Arrange
            var id = Guid.NewGuid();
            var nameStr = "João Silva";
            var emailStr = "joao@example.com";

            // Act
            var user = new User(id, nameStr, emailStr, true);

            // Assert
            user.Id.Should().Be(id);
            user.Name.Value.Should().Be(nameStr);
            user.Email.Address.Should().Be(emailStr);
        }

        [Fact]
        public void Constructor_WithEmptyId_ShouldGenerateNewId()
        {
            // Arrange
            var name = new Name("João Silva");
            var email = new Email("joao@example.com");

            // Act
            var user = new User(Guid.Empty, name, email);

            // Assert
            user.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var email = new Email("joao@example.com");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new User(id, (Name)null, email));
        }

        [Fact]
        public void Constructor_WithNullEmail_ShouldThrowArgumentNullException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = new Name("João Silva");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new User(id, name, (Email)null));
        }

        [Fact]
        public void Activate_ShouldSetIsActiveToTrue()
        {
            // Arrange
            var user = CreateValidUser();
            user.Deactivate();

            // Act
            user.Activate();

            // Assert
            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            // Arrange
            var user = CreateValidUser();

            // Act
            user.Deactivate();

            // Assert
            user.IsActive.Should().BeFalse();
        }

        [Fact]
        public void UpdateName_WithValidName_ShouldUpdateName()
        {
            // Arrange
            var user = CreateValidUser();
            var newName = new Name("Maria Silva");

            // Act
            user.UpdateName(newName);

            // Assert
            user.Name.Should().Be(newName);
        }

        [Fact]
        public void UpdateName_WithNullName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var user = CreateValidUser();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => user.UpdateName(null));
        }

        [Fact]
        public void UpdateEmail_WithValidEmail_ShouldUpdateEmail()
        {
            // Arrange
            var user = CreateValidUser();
            var newEmail = new Email("maria@example.com");

            // Act
            user.UpdateEmail(newEmail);

            // Assert
            user.Email.Should().Be(newEmail);
        }

        [Fact]
        public void UpdateEmail_WithNullEmail_ShouldThrowArgumentNullException()
        {
            // Arrange
            var user = CreateValidUser();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => user.UpdateEmail(null));
        }

        [Fact]
        public void RecordLogin_ShouldUpdateLastLoginAt()
        {
            // Arrange
            var user = CreateValidUser();

            // Act
            user.RecordLogin();

            // Assert
            user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        private static User CreateValidUser()
        {
            return new User(
                Guid.NewGuid(),
                new Name("João Silva"),
                new Email("joao@example.com"),
                true
            );
        }
    }
}