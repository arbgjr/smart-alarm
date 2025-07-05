using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.User;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.User
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CreateUserHandler> _logger;

        public CreateUserHandler(IUserRepository userRepository, ILogger<CreateUserHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var user = new Domain.Entities.User(
                Guid.NewGuid(),
                new Name(request.User.Name),
                new Email(request.User.Email)
            );
            await _userRepository.AddAsync(user);
            _logger.LogInformation("User created: {UserId}", user.Id);
            return user.Id;
        }
    }
}
