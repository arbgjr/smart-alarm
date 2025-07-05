using MediatR;
using SmartAlarm.Application.DTOs.Routine;
using SmartAlarm.Application.Queries.Routine;
using SmartAlarm.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Routine
{
    public class ListRoutinesHandler : IRequestHandler<ListRoutinesQuery, List<RoutineResponseDto>>
    {
        private readonly IRoutineRepository _routineRepository;

        public ListRoutinesHandler(IRoutineRepository routineRepository)
        {
            _routineRepository = routineRepository;
        }

        public async Task<List<RoutineResponseDto>> Handle(ListRoutinesQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<Domain.Entities.Routine> routines;
            if (request.AlarmId.HasValue)
                routines = await _routineRepository.GetByAlarmIdAsync(request.AlarmId.Value);
            else
                routines = await _routineRepository.GetByAlarmIdAsync(Guid.Empty); // TODO: ajustar para buscar todas se necessário

            if (request.UserId.HasValue)
                routines = routines.Where(r => r.AlarmId == request.UserId.Value); // Ajustar se houver UserId na entidade

            return routines.Select(r => new RoutineResponseDto
            {
                Id = r.Id,
                Name = r.Name.ToString(),
                AlarmId = r.AlarmId,
                Actions = r.Actions.ToList(),
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            }).ToList();
        }
    }
}
