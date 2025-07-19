using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Routine;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Routine
{
    public class UpdateRoutineHandler : IRequestHandler<UpdateRoutineCommand, bool>
    {
        private readonly IRoutineRepository _routineRepository;
        private readonly ILogger<UpdateRoutineHandler> _logger;

        public UpdateRoutineHandler(IRoutineRepository routineRepository, ILogger<UpdateRoutineHandler> logger)
        {
            _routineRepository = routineRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateRoutineCommand request, CancellationToken cancellationToken)
        {
            var routine = await _routineRepository.GetByIdAsync(request.Id);
            if (routine == null) return false;
            routine.UpdateName(new Name(request.Name));
            routine.ClearActions();
            foreach (var action in request.Actions)
                routine.AddAction(action);
            await _routineRepository.UpdateAsync(routine);
            _logger.LogInformation("Routine updated: {RoutineId}", routine.Id);
            return true;
        }
    }
}
