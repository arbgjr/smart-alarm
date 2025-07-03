using System;
using Xunit;
using FluentAssertions;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Tests.Domain.ValueObjects
{
    public class EmailTests
    {
        [Fact]
        public void Constructor_WithValidEmail_ShouldCreateEmail()
        {
            // Arrange
            const string validEmail = "joao@example.com";

            // Act
            var email = new Email(validEmail);

            // Assert
            email.Address.Should().Be(validEmail);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid-email")]
        [InlineData("invalid@")]
        [InlineData("@invalid.com")]
        public void Constructor_WithInvalidEmail_ShouldThrowArgumentException(string invalidEmail)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
        }

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("test.email@domain.org")]
        [InlineData("user+tag@subdomain.example.com")]
        public void Constructor_WithValidEmailFormats_ShouldCreateEmail(string validEmail)
        {
            // Act
            var email = new Email(validEmail);

            // Assert
            email.Address.Should().Be(validEmail);
        }

        [Fact]
        public void ToString_ShouldReturnAddress()
        {
            // Arrange
            const string emailAddress = "joao@example.com";
            var email = new Email(emailAddress);

            // Act
            var result = email.ToString();

            // Assert
            result.Should().Be(emailAddress);
        }

        [Fact]
        public void Equals_WithSameEmail_ShouldReturnTrue()
        {
            // Arrange
            var email1 = new Email("joao@example.com");
            var email2 = new Email("joao@example.com");

            // Act
            var result = email1.Equals(email2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_WithDifferentEmail_ShouldReturnFalse()
        {
            // Arrange
            var email1 = new Email("joao@example.com");
            var email2 = new Email("maria@example.com");

            // Act
            var result = email1.Equals(email2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_WithSameEmail_ShouldReturnSameHashCode()
        {
            // Arrange
            var email1 = new Email("joao@example.com");
            var email2 = new Email("joao@example.com");

            // Act
            var hash1 = email1.GetHashCode();
            var hash2 = email2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }
    }
}