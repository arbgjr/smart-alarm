using MediatR;
using SmartAlarm.Application.DTOs.Auth;

namespace SmartAlarm.Application.Commands.Auth;

/// <summary>
/// Command para login tradicional
/// </summary>
public record LoginCommand(
    string Email,
    string Password,
    bool RememberMe = false
) : IRequest<AuthResponseDto>;

/// <summary>
/// Command para registro de usuário
/// </summary>
public record RegisterCommand(
    string Name,
    string Email,
    string Password,
    string ConfirmPassword
) : IRequest<AuthResponseDto>;

/// <summary>
/// Command para refresh token
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<AuthResponseDto>;

/// <summary>
/// Command para logout
/// </summary>
public record LogoutCommand(
    string Token
) : IRequest<bool>;

/// <summary>
/// Command para iniciar registro FIDO2
/// </summary>
public record Fido2RegisterStartCommand(
    Guid UserId,
    string DisplayName,
    string? DeviceName = null
) : IRequest<Fido2RegisterStartResponseDto>;

/// <summary>
/// Command para completar registro FIDO2
/// </summary>
public record Fido2RegisterCompleteCommand(
    Guid UserId,
    object Response, // AuthenticatorAttestationRawResponse será deserializado
    string? DeviceName = null
) : IRequest<AuthResponseDto>;

/// <summary>
/// Command para iniciar autenticação FIDO2
/// </summary>
public record Fido2AuthStartCommand(
    Guid? UserId = null,
    string? Email = null
) : IRequest<Fido2AuthStartResponseDto>;

/// <summary>
/// Command para completar autenticação FIDO2
/// </summary>
public record Fido2AuthCompleteCommand(
    object Response // AuthenticatorAssertionRawResponse será deserializado
) : IRequest<AuthResponseDto>;

/// <summary>
/// Command para remover credencial FIDO2
/// </summary>
public record RemoveCredentialCommand(
    string CredentialId,
    Guid UserId
) : IRequest<bool>;
