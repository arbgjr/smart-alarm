using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Application.Handlers.ExceptionPeriod;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using Xunit;

namespace SmartAlarm.Application.Tests.Handlers.ExceptionPeriod;

public class UpdateExceptionPeriodHandlerTests
{
    private readonly Mock<IExceptionPeriodRepository> _repositoryMock;
    private readonly Mock<ILogger<UpdateExceptionPeriodHandler>> _loggerMock;
    private readonly UpdateExceptionPeriodHandler _handler;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPeriodId = Guid.NewGuid();

    public UpdateExceptionPeriodHandlerTests()
    {
        _repositoryMock = new Mock<IExceptionPeriodRepository>();
        _loggerMock = new Mock<ILogger<UpdateExceptionPeriodHandler>>();
        _handler = new UpdateExceptionPeriodHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_UpdateExceptionPeriod_When_ValidCommand()
    {
        // Arrange
        var existingPeriod = new Domain.Entities.ExceptionPeriod(
            _testPeriodId,
            "Old Name",
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation,
            _testUserId,
            "Old description");

        var command = new UpdateExceptionPeriodCommand
        {
            Id = _testPeriodId,
            Name = "Updated Name",
            StartDate = DateTime.Today.AddDays(2),
            EndDate = DateTime.Today.AddDays(8),
            Type = ExceptionPeriodType.Travel,
            UserId = _testUserId,
            Description = "Updated description",
            IsActive = true
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(_testPeriodId);
        result.Name.Should().Be(command.Name);
        result.StartDate.Should().Be(command.StartDate);
        result.EndDate.Should().Be(command.EndDate);
        result.Type.Should().Be(command.Type);
        result.Description.Should().Be(command.Description);
        result.IsActive.Should().BeTrue();

        _repositoryMock.Verify(x => x.GetByIdAsync(_testPeriodId), Times.Once);
        _repositoryMock.Verify(x => x.UpdateAsync(existingPeriod), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_DeactivatePeriod_When_IsActiveIsFalse()
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

        var command = new UpdateExceptionPeriodCommand
        {
            Id = _testPeriodId,
            Name = "Test Period",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId,
            IsActive = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsActive.Should().BeFalse();
        existingPeriod.IsActive.Should().BeFalse();

        _repositoryMock.Verify(x => x.UpdateAsync(existingPeriod), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ActivatePeriod_When_IsActiveIsTrue()
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

        // Desativar primeiro
        existingPeriod.Deactivate();

        var command = new UpdateExceptionPeriodCommand
        {
            Id = _testPeriodId,
            Name = "Test Period",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId,
            IsActive = true
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsActive.Should().BeTrue();
        existingPeriod.IsActive.Should().BeTrue();

        _repositoryMock.Verify(x => x.UpdateAsync(existingPeriod), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowKeyNotFoundException_When_PeriodNotFound()
    {
        // Arrange
        var command = new UpdateExceptionPeriodCommand
        {
            Id = _testPeriodId,
            Name = "Test Period",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync((Domain.Entities.ExceptionPeriod?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("não encontrado");

        _repositoryMock.Verify(x => x.GetByIdAsync(_testPeriodId), Times.Once);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()), Times.Never);
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

        var command = new UpdateExceptionPeriodCommand
        {
            Id = _testPeriodId,
            Name = "Updated Name",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId // Usuário diferente do dono
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("não autorizado");

        _repositoryMock.Verify(x => x.GetByIdAsync(_testPeriodId), Times.Once);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_LogInformation_When_UpdatingExceptionPeriod()
    {
        // Arrange
        var existingPeriod = new Domain.Entities.ExceptionPeriod(
            _testPeriodId,
            "Old Name",
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation,
            _testUserId,
            "Description");

        var command = new UpdateExceptionPeriodCommand
        {
            Id = _testPeriodId,
            Name = "Updated Name",
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(5),
            Type = ExceptionPeriodType.Vacation,
            UserId = _testUserId
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.ExceptionPeriod>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updating exception period")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("updated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
