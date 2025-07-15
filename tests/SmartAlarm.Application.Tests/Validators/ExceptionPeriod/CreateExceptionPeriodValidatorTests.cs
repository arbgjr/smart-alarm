using FluentAssertions;
using FluentValidation;
using Moq;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Application.Validators.ExceptionPeriod;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using Xunit;

namespace SmartAlarm.Application.Tests.Validators.ExceptionPeriod;

public class CreateExceptionPeriodValidatorTests
{
    private readonly Mock<IExceptionPeriodRepository> _repositoryMock;
    private readonly CreateExceptionPeriodValidator _validator;
    private readonly Guid _testUserId = Guid.NewGuid();

    public CreateExceptionPeriodValidatorTests()
    {
        _repositoryMock = new Mock<IExceptionPeriodRepository>();
        _validator = new CreateExceptionPeriodValidator(_repositoryMock.Object);
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_NameIsEmpty()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_NameIsTooLong()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = new string('A', 201), // Maior que 200 caracteres
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_StartDateIsInPast()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Valid Name",
            StartDate = DateTime.Today.AddDays(-1), // No passado
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.StartDate));
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_EndDateIsBeforeStartDate()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Valid Name",
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(1), // Antes da data de início
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.EndDate));
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_DescriptionIsTooLong()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Valid Name",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId,
            Description = new string('A', 1001) // Maior que 1000 caracteres
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Description));
    }

    [Fact]
    public async Task Validator_Should_HaveError_When_UserIdIsEmpty()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Valid Name",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = Guid.Empty
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UserId));
    }

    [Fact]
    public async Task Validator_Should_NotHaveError_When_AllFieldsAreValid()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Valid Period",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId,
            Description = "Valid description"
        };

        // Mock repository para não ter sobreposição
        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Domain.Entities.ExceptionPeriod>());

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validator_Should_NotHaveError_When_DescriptionIsNull()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Valid Period",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId,
            Description = null // Null é permitido
        };

        // Mock repository para não ter sobreposição
        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Domain.Entities.ExceptionPeriod>());

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ExceptionPeriodType.Vacation)]
    [InlineData(ExceptionPeriodType.Travel)]
    [InlineData(ExceptionPeriodType.Holiday)]
    [InlineData(ExceptionPeriodType.Maintenance)]
    public async Task Validator_Should_AcceptValidTypes(ExceptionPeriodType type)
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Valid Period",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = type,
            UserId = _testUserId
        };

        // Mock repository para não ter sobreposição
        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Domain.Entities.ExceptionPeriod>());

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(command.Type));
    }
}
