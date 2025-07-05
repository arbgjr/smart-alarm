using MediatR;
using SmartAlarm.Application.DTOs.User;
using System;

namespace SmartAlarm.Application.Commands.User
{
    public class CreateUserCommand : IRequest<Guid>
    {
        public CreateUserDto User { get; set; }
        public CreateUserCommand(CreateUserDto user)
        {
            User = user;
        }
    }
}
