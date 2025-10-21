using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
public class CreateRoutineCommandHandlerTests
{
    private readonly Mock<IRoutineRepository> _routineRepositoryMock;
    private readonly Mock<IAlarmRepository> _alarmRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<CreateRoutineCommandHandler>> _loggerMock;
    private readonly CreateRoutineCommandHandler _handler;

    public CreateRoutineCommandHandlerTests()
    {
        _routineRepositoryMock = new Mock<IRoutineRepository>();
        _alarmRepositoryMock = new Mock<IAlarmRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<CreateRoutineCommandHandler>>();

        _handler = new CreateRoutineCommandHandler(
            _routineRepositoryMock.Object,
            _alarmRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateRoutineAndReturnDto()
    {
        // Arrange
        var command = new CreateRoutineCommand(
            "Rotina Matinal",
            "Acordar e se preparar para o dia",
            Guid.NewGuid(),
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        );

        // Simula que todos os alarmes fornecidos existem
        _alarmRepositoryMock.Setup(r => r.AreAlarmsExistForUserAsync(command.UserId, It.IsAny<ICollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var createdRoutine = new Routine(); // O objeto real seria criado dentro do handler
        var routineDto = new RoutineDto { Id = Guid.NewGuid(), Name = command.Name };

        _mapperMock.Setup(m => m.Map<RoutineDto>(It.IsAny<Routine>()))
            .Returns(routineDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Name, result.Name);

        // Verifica se o repositório foi chamado para adicionar a nova rotina
        _routineRepositoryMock.Verify(r => r.AddAsync(It.Is<Routine>(
                rt => rt.Name == command.Name &&
                      rt.UserId == command.UserId &&
                      rt.Alarms.Count == command.AlarmIds.Count)),
            Times.Once);

        // Verifica se as alterações foram salvas
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSomeAlarmIdDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new CreateRoutineCommand(
            "Rotina com Alarme Inválido",
            "Descrição",
            Guid.NewGuid(),
            new List<Guid> { Guid.NewGuid() } // ID de um alarme que não existe
        );

        // Simula que o alarme fornecido não existe para o usuário
        _alarmRepositoryMock.Setup(r => r.AreAlarmsExistForUserAsync(command.UserId, It.IsAny<ICollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Um ou mais IDs de alarme fornecidos não existem ou não pertencem ao usuário.", exception.Message);

        // Garante que nenhuma rotina foi adicionada e nenhuma alteração foi salva
        _routineRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Routine>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

*Nota: Para que este teste funcione, a implementação do `CreateRoutineCommandHandler` precisa ser ajustada para usar as dependências injetadas (`IAlarmRepository`, `IMapper`, etc.) conforme mostrado nos testes.*

### Destaques da Implementação dos Testes:

*   **AAA Pattern (Arrange, Act, Assert):** Cada teste segue claramente a estrutura de organizar, executar e verificar, tornando-os fáceis de ler e entender.
*   **Isolamento Total:** O `CreateRoutineCommandHandler` é testado sem nenhuma dependência real. Todas as interações com o banco de dados (`IRoutineRepository`, `IAlarmRepository`, `IUnitOfWork`) e mapeamento (`IMapper`) são simuladas.
*   **Cenário de Sucesso:** O primeiro teste (`Handle_WithValidData...`) valida o "caminho feliz", garantindo que, com dados válidos, a rotina é criada, salva e o DTO correto é retornado.
*   **Cenário de Falha de Negócio:** O segundo teste (`Handle_WhenSomeAlarmIdDoesNotExist...`) valida uma regra de negócio crucial: não se pode criar uma rotina com alarmes que não existem ou não pertencem ao usuário. Ele garante que, nesse caso, uma exceção é lançada e, mais importante, que nenhuma alteração é persistida no banco de dados.
*   **Verificação de Interações (`Verify`):** Os testes não se limitam a verificar o resultado final. Eles usam `Moq.Verify` para garantir que os métodos corretos das dependências foram chamados o número esperado de vezes, confirmando que o fluxo interno do handler está correto.

Com estes testes unitários, você pode refatorar ou estender o `CreateRoutineCommandHandler` com a confiança de que a lógica principal está protegida contra regressões.

<!--
[PROMPT_SUGGESTION]Mostre-me a implementação do `CreateRoutineCommandHandler` que passaria nesses testes unitários.[/PROMPT_SUGGESTION]
[PROMPT_SUGGESTION]Agora, crie os testes unitários para o `UpdateRoutineCommandHandler`.[/PROMPT_SUGGESTION]
-->
