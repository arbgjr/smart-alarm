using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Routine;
using SmartAlarm.Application.DTOs.Routine;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Routine
{
    public class CreateRoutineHandler : IRequestHandler<CreateRoutineCommand, Guid>
    {
        private readonly IRoutineRepository _routineRepository;
        private readonly ILogger<CreateRoutineHandler> _logger;

        public CreateRoutineHandler(IRoutineRepository routineRepository, ILogger<CreateRoutineHandler> logger)
        {
            _routineRepository = routineRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateRoutineCommand request, CancellationToken cancellationToken)
        {
            var routine = new Domain.Entities.Routine(
                Guid.NewGuid(),
                new SmartAlarm.Domain.ValueObjects.Name(request.Routine.Name),
                request.Routine.UserId,
                new System.Collections.Generic.List<string>()
            );
            await _routineRepository.AddAsync(routine);
            _logger.LogInformation("Routine created: {RoutineId}", routine.Id);
            return routine.Id;
        }
    }
}
