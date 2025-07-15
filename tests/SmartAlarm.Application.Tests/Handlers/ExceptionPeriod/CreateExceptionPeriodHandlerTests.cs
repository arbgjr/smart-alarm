using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Application.Handlers.ExceptionPeriod;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using Xunit;

namespace SmartAlarm.Application.Tests.Handlers.ExceptionPeriod;

public class CreateExceptionPeriodHandlerTests
{
    private readonly Mock<IExceptionPeriodRepository> _repositoryMock;
    private readonly Mock<ILogger<CreateExceptionPeriodHandler>> _loggerMock;
    private readonly CreateExceptionPeriodHandler _handler;
    private readonly Guid _testUserId = Guid.NewGuid();

    public CreateExceptionPeriodHandlerTests()
    {
        _repositoryMock = new Mock<IExceptionPeriodRepository>();
        _loggerMock = new Mock<ILogger<CreateExceptionPeriodHandler>>();
        _handler = new CreateExceptionPeriodHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateExceptionPeriod_When_ValidCommand()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Férias de Verão",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(10),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId,
            Description = "Férias em dezembro"
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.StartDate.Should().Be(command.StartDate);
        result.EndDate.Should().Be(command.EndDate);
        result.Type.Should().Be(command.Type);
        result.UserId.Should().Be(command.UserId);
        result.Description.Should().Be(command.Description);
        result.IsActive.Should().BeTrue();
        result.Id.Should().NotBeEmpty();

        _repositoryMock.Verify(x => x.AddAsync(It.Is<Domain.Entities.ExceptionPeriod>(
            p => p.Name == command.Name &&
                 p.StartDate == command.StartDate &&
                 p.EndDate == command.EndDate &&
                 p.Type == command.Type &&
                 p.UserId == command.UserId &&
                 p.Description == command.Description
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CreateExceptionPeriod_When_DescriptionIsNull()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Viagem de Trabalho",
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(7),
            Type = ExceptionPeriodType.Travel,
            UserId = _testUserId,
            Description = null
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().BeEmpty(); // Convertido para string vazia
        
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_LogInformation_When_CreatingExceptionPeriod()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Manutenção Sistema",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(2), // Data fim deve ser posterior à data início
            Type = ExceptionPeriodType.Maintenance,
            UserId = _testUserId
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating exception period")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("created successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_LogError_When_RepositoryThrows()
    {
        // Arrange
        var command = new CreateExceptionPeriodCommand
        {
            Name = "Test Period",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(2),
            Type = ExceptionPeriodType.Custom,
            UserId = _testUserId
        };

        var exception = new InvalidOperationException("Database error");
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating exception period")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
