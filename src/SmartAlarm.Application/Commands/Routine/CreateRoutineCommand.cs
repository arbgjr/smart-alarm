using MediatR;
using SmartAlarm.Application.DTOs.Routine;
using System;

namespace SmartAlarm.Application.Commands.Routine
{
    public class CreateRoutineCommand : IRequest<Guid>
    {
        public CreateRoutineDto Routine { get; set; }

        public CreateRoutineCommand(CreateRoutineDto routine)
        {
            Routine = routine;
        }
    }
}
