using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Routines.Handlers;

public class UpdateRoutineCommandHandler : IRequestHandler<UpdateRoutineCommand, RoutineDto?>
{
    private readonly IRoutineRepository _routineRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateRoutineCommandHandler(IRoutineRepository routineRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _routineRepository = routineRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<RoutineDto?> Handle(UpdateRoutineCommand request, CancellationToken cancellationToken)
    {
        var routine = await _routineRepository.GetByIdAsync(request.Id);
        if (routine == null)
            return null;

        routine.UpdateName(new SmartAlarm.Domain.ValueObjects.Name(request.Name));

        // Update actions by clearing and adding new ones
        routine.ClearActions();
        foreach (var action in request.Actions)
        {
            routine.AddAction(action);
        }

        if (request.IsActive)
            routine.Activate();
        else
            routine.Deactivate();

        await _routineRepository.UpdateAsync(routine);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<RoutineDto>(routine);
    }
}
