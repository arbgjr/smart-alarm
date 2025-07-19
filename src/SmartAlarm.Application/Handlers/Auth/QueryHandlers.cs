using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Application.Queries.Auth;
using System.Linq;

namespace SmartAlarm.Application.Handlers.Auth;

/// <summary>
/// Handler para obter credenciais do usuário
/// </summary>
public class GetUserCredentialsHandler : IRequestHandler<GetUserCredentialsQuery, IEnumerable<UserCredentialDto>>
{
    private readonly IFido2Service _fido2Service;
    private readonly ILogger<GetUserCredentialsHandler> _logger;

    public GetUserCredentialsHandler(
        IFido2Service fido2Service,
        ILogger<GetUserCredentialsHandler> logger)
    {
        _fido2Service = fido2Service ?? throw new ArgumentNullException(nameof(fido2Service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<UserCredentialDto>> Handle(GetUserCredentialsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting credentials for user: {UserId}", request.UserId);

            var credentials = await _fido2Service.GetUserCredentialsAsync(request.UserId);

            return credentials.Select(c => new UserCredentialDto(
                c.Id,
                c.CredentialId,
                c.DeviceName,
                c.CreatedAt,
                c.LastUsedAt,
                c.IsActive
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credentials for user: {UserId}", request.UserId);
            return Enumerable.Empty<UserCredentialDto>();
        }
    }
}

/// <summary>
/// Handler para validar token
/// </summary>
public class ValidateTokenHandler : IRequestHandler<ValidateTokenQuery, UserDto?>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ValidateTokenHandler> _logger;

    public ValidateTokenHandler(
        IJwtTokenService jwtTokenService,
        IUserRepository userRepository,
        ILogger<ValidateTokenHandler> logger)
    {
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserDto?> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Validating token");

            var isValid = await _jwtTokenService.ValidateTokenAsync(request.Token);
            if (!isValid)
            {
                _logger.LogWarning("Token validation failed");
                return null;
            }

            var userId = await _jwtTokenService.GetUserIdFromTokenAsync(request.Token);
            if (userId == null)
            {
                _logger.LogWarning("Could not extract user ID from token");
                return null;
            }

            // Buscar dados reais do usuário no banco de dados
            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User not found or inactive: {UserId}", userId);
                return null;
            }

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name.Value,
                Email = user.Email.Address,
                IsActive = user.IsActive,
                EmailVerified = user.EmailVerified,
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToArray() ?? Array.Empty<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return null;
        }
    }
}

/// <summary>
/// Handler para obter usuário atual
/// </summary>
public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetCurrentUserHandler> _logger;

    public GetCurrentUserHandler(
        IMediator mediator,
        ILogger<GetCurrentUserHandler> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting current user from token");

            // Reutilizar a validação de token
            var query = new ValidateTokenQuery(request.Token);
            return await _mediator.Send(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }
}
