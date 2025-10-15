using MediatR;
using SmartAlarm.Application.DTOs.Auth;

namespace SmartAlarm.Application.Commands.Auth;

/// <summary>
/// Command para iniciar processo de autorização OAuth2
/// </summary>
public record GetOAuthAuthorizationUrlCommand(
    string Provider,
    string RedirectUri,
    string? State = null,
    IEnumerable<string>? Scopes = null) : IRequest<OAuthAuthorizationResponseDto>;

/// <summary>
/// Command para processar callback OAuth2 e realizar login/registro
/// </summary>
public record ProcessOAuthCallbackCommand(
    string Provider,
    string AuthorizationCode,
    string RedirectUri,
    string? State = null) : IRequest<OAuthLoginResponseDto>;

/// <summary>
/// Command para vincular conta externa a usuário existente
/// </summary>
public record LinkExternalAccountCommand(
    Guid UserId,
    string Provider,
    string AuthorizationCode,
    string RedirectUri,
    string? State = null) : IRequest<bool>;

/// <summary>
/// Command para desvincular conta externa
/// </summary>
public record UnlinkExternalAccountCommand(
    Guid UserId,
    string Provider) : IRequest<bool>;