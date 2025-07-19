using MediatR;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Application.Commands.Auth;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Handlers.Auth;

public class Fido2AuthStartCommandHandler : IRequestHandler<Fido2AuthStartCommand, Fido2AuthStartResponseDto>
{
    private readonly IFido2Service _fido2Service;
    private readonly IUserRepository _userRepository;

    public Fido2AuthStartCommandHandler(IFido2Service fido2Service, IUserRepository userRepository)
    {
        _fido2Service = fido2Service;
        _userRepository = userRepository;
    }

    public async Task<Fido2AuthStartResponseDto> Handle(Fido2AuthStartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            SmartAlarm.Domain.Entities.User? user = null;

            if (request.UserId.HasValue)
            {
                user = await _userRepository.GetByIdAsync(request.UserId.Value);
            }
            else if (!string.IsNullOrEmpty(request.Email))
            {
                user = await _userRepository.GetByEmailAsync(request.Email);
            }
            
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var options = await _fido2Service.CreateAuthenticationRequestAsync(user.Email.ToString());
            return new Fido2AuthStartResponseDto(
                options,
                Guid.NewGuid().ToString(), // Session data
                true
            );
        }
        catch (KeyNotFoundException)
        {
            throw; // Propagar para o controller tratar com HTTP 404
        }
        catch (Exception ex)
        {
            return new Fido2AuthStartResponseDto(
                string.Empty,
                string.Empty,
                false,
                ex.Message
            );
        }
    }
}

public class Fido2RegisterStartCommandHandler : IRequestHandler<Fido2RegisterStartCommand, Fido2RegisterStartResponseDto>
{
    private readonly IFido2Service _fido2Service;
    private readonly IUserRepository _userRepository;

    public Fido2RegisterStartCommandHandler(IFido2Service fido2Service, IUserRepository userRepository)
    {
        _fido2Service = fido2Service;
        _userRepository = userRepository;
    }

    public async Task<Fido2RegisterStartResponseDto> Handle(Fido2RegisterStartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new Fido2RegisterStartResponseDto(
                    string.Empty,
                    string.Empty,
                    false,
                    "User not found"
                );
            }

            var options = await _fido2Service.CreateCredentialRequestAsync(user, request.DisplayName);
            return new Fido2RegisterStartResponseDto(
                options,
                Guid.NewGuid().ToString(), // Session data
                true
            );
        }
        catch (Exception ex)
        {
            return new Fido2RegisterStartResponseDto(
                string.Empty,
                string.Empty,
                false,
                ex.Message
            );
        }
    }
}