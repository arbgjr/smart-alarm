using MediatR;
using SmartAlarm.Application.DTOs.Auth;

namespace SmartAlarm.Application.Queries.Auth;

/// <summary>
/// Query para obter credenciais do usuário
/// </summary>
public record GetUserCredentialsQuery(
    Guid UserId
) : IRequest<IEnumerable<UserCredentialDto>>;

/// <summary>
/// Query para validar token
/// </summary>
public record ValidateTokenQuery(
    string Token
) : IRequest<UserDto?>;

/// <summary>
/// Query para obter usuário atual pelo token
/// </summary>
public record GetCurrentUserQuery(
    string Token
) : IRequest<UserDto?>;
