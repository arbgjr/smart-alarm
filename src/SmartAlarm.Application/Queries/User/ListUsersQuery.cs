using MediatR;
using SmartAlarm.Application.DTOs.User;
using System.Collections.Generic;

namespace SmartAlarm.Application.Queries.User
{
    public class ListUsersQuery : IRequest<List<UserResponseDto>>
    {
    }
}
