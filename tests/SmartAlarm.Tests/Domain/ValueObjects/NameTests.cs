using System;
using Xunit;
using FluentAssertions;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Tests.Domain.ValueObjects
{
    public class NameTests
    {
        [Fact]
        public void Constructor_WithValidValue_ShouldCreateName()
        {
            // Arrange
            const string validName = "João Silva";

            // Act
            var name = new Name(validName);

            // Assert
            name.Value.Should().Be(validName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidValue_ShouldThrowArgumentException(string invalidName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Name(invalidName));
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            // Arrange
            const string nameValue = "João Silva";
            var name = new Name(nameValue);

            // Act
            var result = name.ToString();

            // Assert
            result.Should().Be(nameValue);
        }

        [Fact]
        public void Equals_WithSameName_ShouldReturnTrue()
        {
            // Arrange
            var name1 = new Name("João Silva");
            var name2 = new Name("João Silva");

            // Act
            var result = name1.Equals(name2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_WithDifferentName_ShouldReturnFalse()
        {
            // Arrange
            var name1 = new Name("João Silva");
            var name2 = new Name("Maria Silva");

            // Act
            var result = name1.Equals(name2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_WithSameName_ShouldReturnSameHashCode()
        {
            // Arrange
            var name1 = new Name("João Silva");
            var name2 = new Name("João Silva");

            // Act
            var hash1 = name1.GetHashCode();
            var hash2 = name2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }
    }
}