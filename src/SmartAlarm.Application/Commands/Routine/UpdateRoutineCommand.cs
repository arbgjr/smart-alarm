using MediatR;
using SmartAlarm.Application.DTOs.Routine;
using System;

namespace SmartAlarm.Application.Commands.Routine
{
    public class UpdateRoutineCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public UpdateRoutineCommand(Guid id, string name, List<string> actions)
        {
            Id = id;
            Name = name;
            Actions = actions;
        }
    }
}
