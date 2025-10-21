using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Application.Routines.Handlers;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using Xunit;

namespace SmartAlarm.Application.Tests.Routines.Commands;

[Trait("Category", "Unit")]
public class UpdateRoutineCommandHandlerTests
{
    private readonly Mock<IRoutineRepository> _routineRepositoryMock;
    private readonly Mock<IAlarmRepository> _alarmRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UpdateRoutineCommandHandler>> _loggerMock;
    private readonly UpdateRoutineCommandHandler _handler;

    public UpdateRoutineCommandHandlerTests()
    {
        _routineRepositoryMock = new Mock<IRoutineRepository>();
        _alarmRepositoryMock = new Mock<IAlarmRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UpdateRoutineCommandHandler>>();

        _handler = new UpdateRoutineCommandHandler(
            _routineRepositoryMock.Object,
            _alarmRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateRoutineAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var routineId = Guid.NewGuid();
        var command = new UpdateRoutineCommand(routineId, "Updated Name", "Updated Desc", userId, new List<Guid>());

        var existingRoutine = Routine.Create("Old Name", "Old Desc", userId);
        _routineRepositoryMock.Setup(r => r.GetByIdAsync(routineId)).ReturnsAsync(existingRoutine);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal("Updated Name", existingRoutine.Name);
        Assert.Equal("Updated Desc", existingRoutine.Description);

        _routineRepositoryMock.Verify(r => r.Update(existingRoutine), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoutineNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var command = new UpdateRoutineCommand(Guid.NewGuid(), "Name", "Desc", Guid.NewGuid(), new List<Guid>());

        _routineRepositoryMock.Setup(r => r.GetByIdAsync(command.Id)).ReturnsAsync((Routine?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var routineId = Guid.NewGuid();
        var command = new UpdateRoutineCommand(routineId, "Name", "Desc", attackerId, new List<Guid>());

        var existingRoutine = Routine.Create("Some Routine", "Some Desc", ownerId);
        _routineRepositoryMock.Setup(r => r.GetByIdAsync(routineId)).ReturnsAsync(existingRoutine);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidAlarmIds_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var routineId = Guid.NewGuid();
        var invalidAlarmId = Guid.NewGuid();
        var command = new UpdateRoutineCommand(routineId, "Name", "Desc", userId, new List<Guid> { invalidAlarmId });

        var existingRoutine = Routine.Create("Old Name", "Old Desc", userId);
        _routineRepositoryMock.Setup(r => r.GetByIdAsync(routineId)).ReturnsAsync(existingRoutine);

        _alarmRepositoryMock.Setup(r => r.AreAlarmsExistForUserAsync(userId, command.AlarmIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Um ou mais IDs de alarme fornecidos não existem ou não pertencem ao usuário.", exception.Message);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

```

*Nota: Assim como no handler de criação, esta implementação de teste pressupõe que o `UpdateRoutineCommandHandler` será criado para passar nestes cenários.*

### Destaques da Implementação dos Testes:

*   **Cenário de Sucesso:** O teste `Handle_WithValidData...` garante que, com dados válidos, a entidade `Routine` é corretamente atualizada em memória, o método `Update` do repositório é chamado e as alterações são salvas.
*   **Cenário de Rotina Não Encontrada:** O teste `Handle_WhenRoutineNotFound...` valida que o sistema lança uma `KeyNotFoundException` se a rotina a ser atualizada não existe, prevenindo erros inesperados.
*   **Cenário de Segurança (Propriedade):** O teste `Handle_WhenUserIsNotOwner...` é crucial. Ele garante que um usuário não possa modificar uma rotina que não lhe pertence, lançando uma `UnauthorizedAccessException`.
*   **Cenário de Validação de Negócio:** O teste `Handle_WithInvalidAlarmIds...` valida que a lógica de negócio para verificar a existência e propriedade dos alarmes associados também é aplicada durante a atualização.
*   **Verificação de Efeitos Colaterais:** Em todos os cenários de falha, os testes verificam (`Times.Never`) que o `SaveChangesAsync` não é chamado, garantindo que nenhuma alteração indesejada seja persistida no banco de dados.

Com estes testes, a lógica de atualização de rotinas fica robusta, segura e alinhada com as regras de negócio do sistema.

<!--
[PROMPT_SUGGESTION]Mostre-me a implementação do `UpdateRoutineCommandHandler` que passaria nesses testes unitários.[/PROMPT_SUGGESTION]
[PROMPT_SUGGESTION]Agora, crie os testes unitários para o `DeleteRoutineCommandHandler`.[/PROMPT_SUGGESTION]
-->
