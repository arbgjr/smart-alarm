using SmartAlarm.Domain.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Application.Handlers.ExceptionPeriod;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using Xunit;

namespace SmartAlarm.Application.Tests.Handlers.ExceptionPeriod;

public class DeleteExceptionPeriodHandlerTests
{
    private readonly Mock<IExceptionPeriodRepository> _repositoryMock;
    private readonly Mock<ILogger<DeleteExceptionPeriodHandler>> _loggerMock;
    private readonly DeleteExceptionPeriodHandler _handler;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPeriodId = Guid.NewGuid();

    public DeleteExceptionPeriodHandlerTests()
    {
        _repositoryMock = new Mock<IExceptionPeriodRepository>();
        _loggerMock = new Mock<ILogger<DeleteExceptionPeriodHandler>>();
        _handler = new DeleteExceptionPeriodHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_DeleteExceptionPeriod_When_ValidCommand()
    {
        // Arrange
        var existingPeriod = new Domain.Entities.ExceptionPeriod(
            _testPeriodId,
            "Test Period",
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation,
            _testUserId,
            "Description");

        var command = new DeleteExceptionPeriodCommand(_testPeriodId, _testUserId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        _repositoryMock
            .Setup(x => x.DeleteAsync(_testPeriodId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _repositoryMock.Verify(x => x.GetByIdAsync(_testPeriodId), Times.Once);
        _repositoryMock.Verify(x => x.DeleteAsync(_testPeriodId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_When_PeriodNotFound()
    {
        // Arrange
        var command = new DeleteExceptionPeriodCommand(_testPeriodId, _testUserId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync((Domain.Entities.ExceptionPeriod?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _repositoryMock.Verify(x => x.GetByIdAsync(_testPeriodId), Times.Once);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserNotOwner()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var existingPeriod = new Domain.Entities.ExceptionPeriod(
            _testPeriodId,
            "Test Period",
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation,
            otherUserId, // Diferente do usuário da requisição
            "Description");

        var command = new DeleteExceptionPeriodCommand(_testPeriodId, _testUserId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("não autorizado");

        _repositoryMock.Verify(x => x.GetByIdAsync(_testPeriodId), Times.Once);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_LogInformation_When_DeletingExceptionPeriod()
    {
        // Arrange
        var existingPeriod = new Domain.Entities.ExceptionPeriod(
            _testPeriodId,
            "Test Period",
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation,
            _testUserId,
            "Description");

        var command = new DeleteExceptionPeriodCommand(_testPeriodId, _testUserId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        _repositoryMock
            .Setup(x => x.DeleteAsync(_testPeriodId))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleting exception period")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("deleted successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_LogWarning_When_PeriodNotFound()
    {
        // Arrange
        var command = new DeleteExceptionPeriodCommand(_testPeriodId, _testUserId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync((Domain.Entities.ExceptionPeriod?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_LogError_When_RepositoryThrows()
    {
        // Arrange
        var existingPeriod = new Domain.Entities.ExceptionPeriod(
            _testPeriodId,
            "Test Period",
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation,
            _testUserId,
            "Description");

        var command = new DeleteExceptionPeriodCommand(_testPeriodId, _testUserId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        var exception = new InvalidOperationException("Database error");
        _repositoryMock
            .Setup(x => x.DeleteAsync(_testPeriodId))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error deleting exception period")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
