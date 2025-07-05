using MediatR;
using SmartAlarm.Application.DTOs.User;
using SmartAlarm.Application.Queries.User;
using SmartAlarm.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.User
{
    public class ListUsersHandler : IRequestHandler<ListUsersQuery, List<UserResponseDto>>
    {
        private readonly IUserRepository _userRepository;

        public ListUsersHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserResponseDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                Name = u.Name.ToString(),
                Email = u.Email.ToString(),
                IsActive = u.IsActive
            }).ToList();
        }
    }
}
