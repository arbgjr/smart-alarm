using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Routines.Queries;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Routines.Handlers;

public class GetRoutineByIdQueryHandler : IRequestHandler<GetRoutineByIdQuery, RoutineDto?>
{
    private readonly IRoutineRepository _routineRepository;
    private readonly IMapper _mapper;

    public GetRoutineByIdQueryHandler(IRoutineRepository routineRepository, IMapper mapper)
    {
        _routineRepository = routineRepository;
        _mapper = mapper;
    }

    public async Task<RoutineDto?> Handle(GetRoutineByIdQuery request, CancellationToken cancellationToken)
    {
        var routine = await _routineRepository.GetByIdAsync(request.Id);
        return routine == null ? null : _mapper.Map<RoutineDto>(routine);
    }
}
