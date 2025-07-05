using MediatR;
using SmartAlarm.Application.DTOs.Routine;
using SmartAlarm.Application.Queries.Routine;
using SmartAlarm.Domain.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Routine
{
    public class GetRoutineByIdHandler : IRequestHandler<GetRoutineByIdQuery, RoutineResponseDto>
    {
        private readonly IRoutineRepository _routineRepository;

        public GetRoutineByIdHandler(IRoutineRepository routineRepository)
        {
            _routineRepository = routineRepository;
        }

        public async Task<RoutineResponseDto> Handle(GetRoutineByIdQuery request, CancellationToken cancellationToken)
        {
            var routine = await _routineRepository.GetByIdAsync(request.Id);
            if (routine == null) return null;
            return new RoutineResponseDto
            {
                Id = routine.Id,
                Name = routine.Name.ToString(),
                AlarmId = routine.AlarmId,
                Actions = routine.Actions.ToList(),
                IsActive = routine.IsActive,
                CreatedAt = routine.CreatedAt
            };
        }
    }
}
