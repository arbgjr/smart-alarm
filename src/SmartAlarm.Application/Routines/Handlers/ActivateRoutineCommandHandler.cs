using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Routines.Handlers;

public class ActivateRoutineCommandHandler : IRequestHandler<ActivateRoutineCommand, bool>
{
    private readonly IRoutineRepository _routineRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateRoutineCommandHandler(IRoutineRepository routineRepository, IUnitOfWork unitOfWork)
    {
        _routineRepository = routineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ActivateRoutineCommand request, CancellationToken cancellationToken)
    {
        var routine = await _routineRepository.GetByIdAsync(request.Id);
        if (routine == null)
            return false;

        routine.Activate();

        await _routineRepository.UpdateAsync(routine);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
