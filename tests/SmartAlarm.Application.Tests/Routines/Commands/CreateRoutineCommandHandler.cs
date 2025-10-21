using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Routines.Handlers;

/// <summary>
/// Handler para o comando de criação de uma nova rotina.
/// </summary>
public class CreateRoutineCommandHandler : IRequestHandler<CreateRoutineCommand, RoutineDto>
{
    private readonly IRoutineRepository _routineRepository;
    private readonly IAlarmRepository _alarmRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateRoutineCommandHandler> _logger;

    public CreateRoutineCommandHandler(
        IRoutineRepository routineRepository,
        IAlarmRepository alarmRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateRoutineCommandHandler> logger)
    {
        _routineRepository = routineRepository;
        _alarmRepository = alarmRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RoutineDto> Handle(CreateRoutineCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando criação de rotina '{RoutineName}' para o usuário {UserId}", request.Name, request.UserId);

        // Validação de negócio: Verifica se todos os alarmes fornecidos existem e pertencem ao usuário.
        // Isso corresponde ao teste 'Handle_WhenSomeAlarmIdDoesNotExist_ShouldThrowInvalidOperationException'.
        if (request.AlarmIds.Any())
        {
            var allAlarmsExist = await _alarmRepository.AreAlarmsExistForUserAsync(request.UserId, request.AlarmIds, cancellationToken);
            if (!allAlarmsExist)
            {
                _logger.LogWarning("Tentativa de criar rotina com IDs de alarme inválidos para o usuário {UserId}.", request.UserId);
                throw new InvalidOperationException("Um ou mais IDs de alarme fornecidos não existem ou não pertencem ao usuário.");
            }
        }

        // Criação da entidade de domínio.
        var routine = Routine.Create(request.Name, request.Description, request.UserId);

        // Adiciona os alarmes à rotina.
        foreach (var alarmId in request.AlarmIds)
        {
            routine.AddAlarm(alarmId);
        }

        await _routineRepository.AddAsync(routine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Rotina '{RoutineName}' (ID: {RoutineId}) criada com sucesso.", routine.Name, routine.Id);

        return _mapper.Map<RoutineDto>(routine);
    }
}
