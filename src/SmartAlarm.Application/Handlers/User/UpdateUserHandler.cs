using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.User;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.User
{
    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserHandler> _logger;

        public UpdateUserHandler(IUserRepository userRepository, ILogger<UpdateUserHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null) return false;
            user.UpdateName(new Name(request.Name));
            user.UpdateEmail(new Email(request.Email));
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("User updated: {UserId}", user.Id);
            return true;
        }
    }
}
