using MediatR;
using SmartAlarm.Application.DTOs.User;
using System;

namespace SmartAlarm.Application.Queries.User
{
    public class GetUserByIdQuery : IRequest<UserResponseDto>
    {
        public Guid Id { get; set; }
        public GetUserByIdQuery(Guid id) => Id = id;
    }
}
