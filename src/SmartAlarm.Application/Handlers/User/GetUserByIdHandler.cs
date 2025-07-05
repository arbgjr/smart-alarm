using MediatR;
using SmartAlarm.Application.DTOs.User;
using SmartAlarm.Application.Queries.User;
using SmartAlarm.Domain.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.User
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserResponseDto>
    {
        private readonly IUserRepository _userRepository;

        public GetUserByIdHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserResponseDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null) return null;
            return new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name.ToString(),
                Email = user.Email.ToString(),
                IsActive = user.IsActive
            };
        }
    }
}
