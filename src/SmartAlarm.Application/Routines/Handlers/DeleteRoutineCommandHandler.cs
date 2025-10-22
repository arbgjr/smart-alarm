using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Routines.Handlers;

public class DeleteRoutineCommandHandler : IRequestHandler<DeleteRoutineCommand, bool>
{
    private readonly IRoutineRepository _routineRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoutineCommandHandler(IRoutineRepository routineRepository, IUnitOfWork unitOfWork)
    {
        _routineRepository = routineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteRoutineCommand request, CancellationToken cancellationToken)
    {
        var routine = await _routineRepository.GetByIdAsync(request.Id);
        if (routine == null)
            return false;

        await _routineRepository.DeleteAsync(routine.Id);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
