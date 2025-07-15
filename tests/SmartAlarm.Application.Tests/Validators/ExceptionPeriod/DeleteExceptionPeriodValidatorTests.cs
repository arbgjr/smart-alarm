using FluentAssertions;
using FluentValidation;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Application.Validators.ExceptionPeriod;
using Xunit;

namespace SmartAlarm.Application.Tests.Validators.ExceptionPeriod;

public class DeleteExceptionPeriodValidatorTests
{
    private readonly DeleteExceptionPeriodValidator _validator;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPeriodId = Guid.NewGuid();

    public DeleteExceptionPeriodValidatorTests()
    {
        _validator = new DeleteExceptionPeriodValidator();
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_IdIsEmpty()
    {
        // Arrange
        var command = new DeleteExceptionPeriodCommand(Guid.Empty, _testUserId);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Id));
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_UserIdIsEmpty()
    {
        // Arrange
        var command = new DeleteExceptionPeriodCommand(_testPeriodId, Guid.Empty);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UserId));
    }

    [Fact]
    public async Task Validator_Should_NotHaveErrors_When_AllFieldsAreValid()
    {
        // Arrange
        var command = new DeleteExceptionPeriodCommand(_testPeriodId, _testUserId);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_IdIsDefault()
    {
        // Arrange
        var command = new DeleteExceptionPeriodCommand(default(Guid), _testUserId);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Id));
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_UserIdIsDefault()
    {
        // Arrange
        var command = new DeleteExceptionPeriodCommand(_testPeriodId, default(Guid));

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UserId));
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")] // Guid.Empty como string
    public async Task Validator_Should_HaveError_When_IdIsEmptyGuidString(string guidString)
    {
        // Arrange
        var emptyGuid = Guid.Parse(guidString);
        var command = new DeleteExceptionPeriodCommand(emptyGuid, _testUserId);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Id));
    }

    [Fact]
    public async Task Validator_Should_AcceptValidGuids()
    {
        // Arrange - Testando vários GUIDs válidos
        var validGuids = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        foreach (var validGuid in validGuids)
        {
            var command = new DeleteExceptionPeriodCommand(validGuid, _testUserId);

            // Act
            var result = await _validator.ValidateAsync(command);

            // Assert
            result.Errors.Should().NotContain(e => e.PropertyName == nameof(command.Id));
            result.Errors.Should().NotContain(e => e.PropertyName == nameof(command.UserId));
        }
    }

    [Fact]
    public async Task Validator_Should_ValidateAllPropertiesSimultaneously()
    {
        // Arrange - Comando com ambos os campos inválidos
        var command = new DeleteExceptionPeriodCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Id));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UserId));
        result.Errors.Should().HaveCount(2);
    }
}
