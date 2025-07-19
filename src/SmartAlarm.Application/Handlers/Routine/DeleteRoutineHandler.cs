using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Routine;
using SmartAlarm.Domain.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Routine
{
    public class DeleteRoutineHandler : IRequestHandler<DeleteRoutineCommand, bool>
    {
        private readonly IRoutineRepository _routineRepository;
        private readonly ILogger<DeleteRoutineHandler> _logger;

        public DeleteRoutineHandler(IRoutineRepository routineRepository, ILogger<DeleteRoutineHandler> logger)
        {
            _routineRepository = routineRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteRoutineCommand request, CancellationToken cancellationToken)
        {
            var routine = await _routineRepository.GetByIdAsync(request.Id);
            if (routine == null) return false;
            await _routineRepository.DeleteAsync(request.Id);
            _logger.LogInformation("Routine deleted: {RoutineId}", request.Id);
            return true;
        }
    }
}
