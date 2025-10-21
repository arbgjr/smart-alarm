using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Routines.Handlers;

public class BulkUpdateRoutinesCommandHandler : IRequestHandler<BulkUpdateRoutinesCommand, Unit>
{
    private readonly IRoutineRepository _routineRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkUpdateRoutinesCommandHandler> _logger;

    public BulkUpdateRoutinesCommandHandler(
        IRoutineRepository routineRepository,
        IUnitOfWork unitOfWork,
        ILogger<BulkUpdateRoutinesCommandHandler> logger)
    {
        _routineRepository = routineRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(BulkUpdateRoutinesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando ação em lote '{Action}' para {Count} rotinas do usuário {UserId}.",
            request.Action, request.RoutineIds.Count, request.UserId);

        // Buscar apenas as rotinas que pertencem ao usuário e estão na lista de IDs
        var routines = await _routineRepository.GetByIdsAndUserIdAsync(request.RoutineIds, request.UserId);

        if (!routines.Any())
        {
            _logger.LogWarning("Nenhuma rotina encontrada para a ação em lote '{Action}' para o usuário {UserId} com os IDs fornecidos.",
                request.Action, request.UserId);
            return Unit.Value; // Nenhuma rotina para processar, retorna sucesso
        }

        var processedCount = 0;
        foreach (var routine in routines)
        {
            switch (request.Action)
            {
                case BulkRoutineAction.Enable:
                    routine.Enable();
                    _routineRepository.Update(routine);
                    processedCount++;
                    break;
                case BulkRoutineAction.Disable:
                    routine.Disable();
                    _routineRepository.Update(routine);
                    processedCount++;
                    break;
                case BulkRoutineAction.Delete:
                    _routineRepository.Remove(routine);
                    processedCount++;
                    break;
                default:
                    _logger.LogWarning("Ação em lote desconhecida: {Action}.", request.Action);
                    break;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ação em lote '{Action}' concluída para {ProcessedCount} de {TotalRequested} rotinas do usuário {UserId}.",
            request.Action, processedCount, request.RoutineIds.Count, request.UserId);

        return Unit.Value;
    }
}
