using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using SmartAlarm.Application.Common;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Routines.Queries;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Routines.Handlers;

public class ListUserRoutinesQueryHandler : IRequestHandler<ListUserRoutinesQuery, PaginatedList<RoutineDto>>
{
    private readonly IRoutineRepository _routineRepository;
    private readonly IMapper _mapper;

    public ListUserRoutinesQueryHandler(IRoutineRepository routineRepository, IMapper mapper)
    {
        _routineRepository = routineRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedList<RoutineDto>> Handle(ListUserRoutinesQuery request, CancellationToken cancellationToken)
    {
        // Obter o IQueryable do repositório
        var queryable = _routineRepository.GetByUserIdQueryable(request.UserId);

        // Mapear para DTO usando ProjectTo para otimizar a consulta SQL
        var dtoQueryable = queryable
            .OrderByDescending(r => r.CreatedAt) // Ordenação padrão
            .ProjectTo<RoutineDto>(_mapper.ConfigurationProvider);

        // Criar a lista paginada
        return await PaginatedList<RoutineDto>.CreateAsync(dtoQueryable, request.PageNumber, request.PageSize);
    }
}
