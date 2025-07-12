using System;
using FluentAssertions;
using SmartAlarm.Domain.Entities;
using Xunit;

namespace SmartAlarm.Tests.Domain.Entities
{
    public class HolidayTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateHoliday()
        {
            // Arrange
            var date = new DateTime(2025, 12, 25);
            var description = "Natal";

            // Act
            var holiday = new Holiday(date, description);

            // Assert
            holiday.Id.Should().NotBe(Guid.Empty);
            holiday.Date.Should().Be(date.Date);
            holiday.Description.Should().Be(description);
            holiday.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Constructor_WithDateTime_ShouldRemoveTimeComponent()
        {
            // Arrange
            var dateTime = new DateTime(2025, 12, 25, 14, 30, 45);
            var description = "Natal";

            // Act
            var holiday = new Holiday(dateTime, description);

            // Assert
            holiday.Date.Should().Be(new DateTime(2025, 12, 25));
            holiday.Date.TimeOfDay.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void Constructor_WithSpecificId_ShouldUseProvidedId()
        {
            // Arrange
            var id = Guid.NewGuid();
            var date = new DateTime(2025, 12, 25);
            var description = "Natal";

            // Act
            var holiday = new Holiday(id, date, description);

            // Assert
            holiday.Id.Should().Be(id);
            holiday.Date.Should().Be(date.Date);
            holiday.Description.Should().Be(description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidDescription_ShouldThrowArgumentException(string invalidDescription)
        {
            // Arrange
            var date = new DateTime(2025, 12, 25);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Holiday(date, invalidDescription));
            exception.ParamName.Should().Be("description");
            exception.Message.Should().Contain("Description é obrigatória");
        }

        [Fact]
        public void Constructor_WithDescriptionTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var date = new DateTime(2025, 12, 25);
            var longDescription = new string('a', 101); // 101 characters

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Holiday(date, longDescription));
            exception.ParamName.Should().Be("description");
            exception.Message.Should().Contain("não pode ter mais de 100 caracteres");
        }

        [Fact]
        public void Constructor_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var id = Guid.Empty;
            var date = new DateTime(2025, 12, 25);
            var description = "Natal";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Holiday(id, date, description));
            exception.ParamName.Should().Be("id");
            exception.Message.Should().Contain("Id não pode ser vazio");
        }

        [Fact]
        public void Constructor_WithWhitespaceDescription_ShouldTrimDescription()
        {
            // Arrange
            var date = new DateTime(2025, 12, 25);
            var description = "  Natal  ";

            // Act
            var holiday = new Holiday(date, description);

            // Assert
            holiday.Description.Should().Be("Natal");
        }

        #endregion

        #region UpdateDescription Tests

        [Fact]
        public void UpdateDescription_WithValidDescription_ShouldUpdateDescription()
        {
            // Arrange
            var holiday = CreateValidHoliday();
            var newDescription = "Dia de Natal";

            // Act
            holiday.UpdateDescription(newDescription);

            // Assert
            holiday.Description.Should().Be(newDescription);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDescription_WithInvalidDescription_ShouldThrowArgumentException(string invalidDescription)
        {
            // Arrange
            var holiday = CreateValidHoliday();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => holiday.UpdateDescription(invalidDescription));
            exception.ParamName.Should().Be("newDescription");
            exception.Message.Should().Contain("Description é obrigatória");
        }

        [Fact]
        public void UpdateDescription_WithDescriptionTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var holiday = CreateValidHoliday();
            var longDescription = new string('b', 101);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => holiday.UpdateDescription(longDescription));
            exception.ParamName.Should().Be("newDescription");
            exception.Message.Should().Contain("não pode ter mais de 100 caracteres");
        }

        [Fact]
        public void UpdateDescription_WithWhitespace_ShouldTrimDescription()
        {
            // Arrange
            var holiday = CreateValidHoliday();
            var description = "  Novo Ano  ";

            // Act
            holiday.UpdateDescription(description);

            // Assert
            holiday.Description.Should().Be("Novo Ano");
        }

        #endregion

        #region IsOnDate Tests

        [Fact]
        public void IsOnDate_WithSameDate_ShouldReturnTrue()
        {
            // Arrange
            var date = new DateTime(2025, 12, 25);
            var holiday = new Holiday(date, "Natal");

            // Act
            var result = holiday.IsOnDate(date);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsOnDate_WithSameDateDifferentTime_ShouldReturnTrue()
        {
            // Arrange
            var holidayDate = new DateTime(2025, 12, 25);
            var checkDate = new DateTime(2025, 12, 25, 14, 30, 0);
            var holiday = new Holiday(holidayDate, "Natal");

            // Act
            var result = holiday.IsOnDate(checkDate);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsOnDate_WithDifferentDate_ShouldReturnFalse()
        {
            // Arrange
            var holidayDate = new DateTime(2025, 12, 25);
            var checkDate = new DateTime(2025, 12, 26);
            var holiday = new Holiday(holidayDate, "Natal");

            // Act
            var result = holiday.IsOnDate(checkDate);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IsRecurring Tests

        [Fact]
        public void IsRecurring_WithYear1_ShouldReturnTrue()
        {
            // Arrange
            var date = new DateTime(1, 12, 25); // Year 1 indicates recurring
            var holiday = new Holiday(date, "Natal Anual");

            // Act
            var result = holiday.IsRecurring();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsRecurring_WithSpecificYear_ShouldReturnFalse()
        {
            // Arrange
            var date = new DateTime(2025, 12, 25);
            var holiday = new Holiday(date, "Natal 2025");

            // Act
            var result = holiday.IsRecurring();

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_WithSameId_ShouldReturnTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var holiday1 = new Holiday(id, new DateTime(2025, 12, 25), "Natal");
            var holiday2 = new Holiday(id, new DateTime(2025, 12, 26), "Outro"); // Different date/description

            // Act & Assert
            holiday1.Equals(holiday2).Should().BeTrue();
            holiday1.GetHashCode().Should().Be(holiday2.GetHashCode());
        }

        [Fact]
        public void Equals_WithDifferentId_ShouldReturnFalse()
        {
            // Arrange
            var holiday1 = new Holiday(new DateTime(2025, 12, 25), "Natal");
            var holiday2 = new Holiday(new DateTime(2025, 12, 25), "Natal");

            // Act & Assert
            holiday1.Equals(holiday2).Should().BeFalse();
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var holiday = CreateValidHoliday();

            // Act & Assert
            holiday.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            // Arrange
            var holiday = CreateValidHoliday();
            var otherObject = "not a holiday";

            // Act & Assert
            holiday.Equals(otherObject).Should().BeFalse();
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var date = new DateTime(2025, 12, 25);
            var description = "Natal";
            var holiday = new Holiday(date, description);

            // Act
            var result = holiday.ToString();

            // Assert
            result.Should().Be("Natal (25/12/2025)");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Constructor_WithMaxLengthDescription_ShouldNotThrow()
        {
            // Arrange
            var date = new DateTime(2025, 12, 25);
            var maxDescription = new string('a', 100); // Exactly 100 characters

            // Act
            var action = () => new Holiday(date, maxDescription);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void Constructor_WithLeapYearDate_ShouldWork()
        {
            // Arrange
            var leapYearDate = new DateTime(2024, 2, 29); // Leap year
            var description = "Dia extra";

            // Act
            var holiday = new Holiday(leapYearDate, description);

            // Assert
            holiday.Date.Should().Be(leapYearDate.Date);
        }

        [Fact]
        public void Constructor_WithMinMaxDateValues_ShouldWork()
        {
            // Arrange
            var minDate = DateTime.MinValue;
            var maxDate = DateTime.MaxValue;

            // Act & Assert
            var minHoliday = new Holiday(minDate, "Min Date");
            var maxHoliday = new Holiday(maxDate, "Max Date");

            minHoliday.Date.Should().Be(minDate.Date);
            maxHoliday.Date.Should().Be(maxDate.Date);
        }

        #endregion

        #region Helper Methods

        private static Holiday CreateValidHoliday()
        {
            return new Holiday(new DateTime(2025, 12, 25), "Natal");
        }

        #endregion
    }
}
