using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Routines.Handlers;

public class DeactivateRoutineCommandHandler : IRequestHandler<DeactivateRoutineCommand, bool>
{
    private readonly IRoutineRepository _routineRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateRoutineCommandHandler(IRoutineRepository routineRepository, IUnitOfWork unitOfWork)
    {
        _routineRepository = routineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeactivateRoutineCommand request, CancellationToken cancellationToken)
    {
        var routine = await _routineRepository.GetByIdAsync(request.Id);
        if (routine == null)
            return false;

        routine.Deactivate();

        await _routineRepository.UpdateAsync(routine);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
