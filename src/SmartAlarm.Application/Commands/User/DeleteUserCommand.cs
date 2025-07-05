using MediatR;
using System;

namespace SmartAlarm.Application.Commands.User
{
    public class DeleteUserCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public DeleteUserCommand(Guid id) => Id = id;
    }
}
