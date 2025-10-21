using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Application.Routines.Handlers;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using Xunit;

namespace SmartAlarm.Application.Tests.Routines.Commands;

[Trait("Category", "Unit")]
public class BulkUpdateRoutinesCommandHandlerTests
{
    private readonly Mock<IRoutineRepository> _routineRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<BulkUpdateRoutinesCommandHandler>> _loggerMock;
    private readonly BulkUpdateRoutinesCommandHandler _handler;

    public BulkUpdateRoutinesCommandHandlerTests()
    {
        _routineRepositoryMock = new Mock<IRoutineRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<BulkUpdateRoutinesCommandHandler>>();

        _handler = new BulkUpdateRoutinesCommandHandler(
            _routineRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithEnableAction_ShouldEnableAllRoutinesAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var routineIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var routines = routineIds.Select(id => Routine.Create("Test", "Desc", userId)).ToList();
        routines.ForEach(r => r.Disable()); // Start as disabled

        _routineRepositoryMock.Setup(r => r.GetByIdsAndUserIdAsync(routineIds, userId)).ReturnsAsync(routines);

        var command = new BulkUpdateRoutinesCommand(userId, routineIds, BulkRoutineAction.Enable);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.All(routines, r => Assert.True(r.IsEnabled));
        _routineRepositoryMock.Verify(r => r.Update(It.IsAny<Routine>()), Times.Exactly(routines.Count));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDisableAction_ShouldDisableAllRoutinesAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var routineIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var routines = routineIds.Select(id => Routine.Create("Test", "Desc", userId)).ToList();
        // Start as enabled

        _routineRepositoryMock.Setup(r => r.GetByIdsAndUserIdAsync(routineIds, userId)).ReturnsAsync(routines);

        var command = new BulkUpdateRoutinesCommand(userId, routineIds, BulkRoutineAction.Disable);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.All(routines, r => Assert.False(r.IsEnabled));
        _routineRepositoryMock.Verify(r => r.Update(It.IsAny<Routine>()), Times.Exactly(routines.Count));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDeleteAction_ShouldRemoveAllRoutinesAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var routineIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var routines = routineIds.Select(id => Routine.Create("Test", "Desc", userId)).ToList();

        _routineRepositoryMock.Setup(r => r.GetByIdsAndUserIdAsync(routineIds, userId)).ReturnsAsync(routines);

        var command = new BulkUpdateRoutinesCommand(userId, routineIds, BulkRoutineAction.Delete);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _routineRepositoryMock.Verify(r => r.Remove(It.IsAny<Routine>()), Times.Exactly(routines.Count));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoRoutinesFound_ShouldDoNothingAndReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var routineIds = new List<Guid> { Guid.NewGuid() };
        _routineRepositoryMock.Setup(r => r.GetByIdsAndUserIdAsync(routineIds, userId)).ReturnsAsync(new List<Routine>());

        var command = new BulkUpdateRoutinesCommand(userId, routineIds, BulkRoutineAction.Delete);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _routineRepositoryMock.Verify(r => r.Remove(It.IsAny<Routine>()), Times.Never);
        _routineRepositoryMock.Verify(r => r.Update(It.IsAny<Routine>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
