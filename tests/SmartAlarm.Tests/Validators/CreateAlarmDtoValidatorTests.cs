using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Validators;
using Xunit;

namespace SmartAlarm.Tests.Validators
{
    /// <summary>
    /// Testes para o validador de criação de alarme.
    /// </summary>
    public class CreateAlarmDtoValidatorTests
    {
        private readonly CreateAlarmDtoValidator _validator;

        public CreateAlarmDtoValidatorTests()
        {
            _validator = new CreateAlarmDtoValidator();
        }

        [Fact]
        public void Should_BeValid_When_AllFieldsAreCorrect()
        {
            // Arrange
            var dto = new CreateAlarmDto
            {
                Name = "Test Alarm",
                Time = DateTime.Now.AddDays(1),
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_BeInvalid_When_NameIsEmptyOrWhitespace(string name)
        {
            // Arrange
            var dto = new CreateAlarmDto
            {
                Name = name,
                Time = DateTime.Now.AddDays(1),
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAlarmDto.Name));
        }

        [Fact]
        public void Should_BeInvalid_When_NameIsTooLong()
        {
            // Arrange
            var dto = new CreateAlarmDto
            {
                Name = new string('a', 101), // 101 characters
                Time = DateTime.Now.AddDays(1),
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAlarmDto.Name));
        }

        [Fact]
        public void Should_BeInvalid_When_TimeIsInPast()
        {
            // Arrange
            var dto = new CreateAlarmDto
            {
                Name = "Test Alarm",
                Time = DateTime.Now.AddDays(-1), // Past date
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAlarmDto.Time));
        }

        [Fact]
        public void Should_BeInvalid_When_UserIdIsEmpty()
        {
            // Arrange
            var dto = new CreateAlarmDto
            {
                Name = "Test Alarm",
                Time = DateTime.Now.AddDays(1),
                UserId = Guid.Empty
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAlarmDto.UserId));
        }

        [Fact]
        public void Should_ContainCorrectErrorMessages()
        {
            // Arrange
            var dto = new CreateAlarmDto
            {
                Name = "",
                Time = DateTime.Now.AddDays(-1),
                UserId = Guid.Empty
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 3); // At least name, time, and userId errors

            var nameError = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(CreateAlarmDto.Name));
            var timeError = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(CreateAlarmDto.Time));
            var userIdError = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(CreateAlarmDto.UserId));

            Assert.NotNull(nameError);
            Assert.NotNull(timeError);
            Assert.NotNull(userIdError);

            // Verify error messages are using centralized keys
            Assert.Equal("Validation.Required.AlarmName", nameError.ErrorMessage);
            Assert.Equal("Validation.Range.FutureDateTime", timeError.ErrorMessage);
            Assert.Equal("Validation.Required.UserId", userIdError.ErrorMessage);
        }
    }
}