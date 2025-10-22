using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Routines.Handlers;

public class CreateRoutineCommandHandler : IRequestHandler<CreateRoutineCommand, RoutineDto>
{
    private readonly IRoutineRepository _routineRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateRoutineCommandHandler(IRoutineRepository routineRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _routineRepository = routineRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<RoutineDto> Handle(CreateRoutineCommand request, CancellationToken cancellationToken)
    {
        var routine = new Routine(
            Guid.NewGuid(),
            request.Name,
            request.AlarmId,
            request.Actions
        );

        if (!request.IsActive)
            routine.Deactivate();

        await _routineRepository.AddAsync(routine);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<RoutineDto>(routine);
    }
}
