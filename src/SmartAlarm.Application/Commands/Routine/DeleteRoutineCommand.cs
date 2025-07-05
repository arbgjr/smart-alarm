using MediatR;
using System;

namespace SmartAlarm.Application.Commands.Routine
{
    public class DeleteRoutineCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public DeleteRoutineCommand(Guid id) => Id = id;
    }
}
