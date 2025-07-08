using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Application.DTOs.Auth;

/// <summary>
/// DTO para iniciar registro FIDO2
/// </summary>
public record StartFido2RegistrationRequest(
    [Required] string UserEmail,
    [Required] string DisplayName
);

/// <summary>
/// DTO para completar registro FIDO2
/// </summary>
public record CompleteFido2RegistrationRequest(
    [Required] string UserEmail,
    [Required] Fido2AttestationResponseDto AttestationResponse,
    [Required] string SessionData
);

/// <summary>
/// DTO para iniciar autenticação FIDO2
/// </summary>
public record StartFido2AuthenticationRequest(
    [Required] string UserEmail
);

/// <summary>
/// DTO para completar autenticação FIDO2
/// </summary>
public record CompleteFido2AuthenticationRequest(
    [Required] string UserEmail,
    [Required] Fido2AssertionResponseDto AssertionResponse,
    [Required] string SessionData
);

/// <summary>
/// DTO para resposta de atestação FIDO2
/// Abstração dos tipos específicos da biblioteca
/// </summary>
public record Fido2AttestationResponseDto(
    string Id,
    string RawId,
    string Type,
    Fido2AuthenticatorAttestationResponseDto Response
);

/// <summary>
/// DTO para resposta de asserção FIDO2
/// Abstração dos tipos específicos da biblioteca
/// </summary>
public record Fido2AssertionResponseDto(
    string Id,
    string RawId,
    string Type,
    Fido2AuthenticatorAssertionResponseDto Response
);

/// <summary>
/// DTO para opções de criação de credencial FIDO2
/// Abstração dos tipos específicos da biblioteca
/// </summary>
public record Fido2CredentialCreateOptionsDto(
    string Challenge,
    Fido2RelyingPartyDto Rp,
    Fido2UserDto User,
    Fido2PublicKeyCredentialParametersDto[] PubKeyCredParams,
    int Timeout,
    string SessionData
);

/// <summary>
/// DTO para opções de asserção FIDO2
/// Abstração dos tipos específicos da biblioteca
/// </summary>
public record Fido2AssertionOptionsDto(
    string Challenge,
    int Timeout,
    string RpId,
    Fido2PublicKeyCredentialDescriptorDto[]? AllowCredentials,
    string SessionData
);

/// <summary>
/// DTO para resposta do autenticador (atestação)
/// </summary>
public record Fido2AuthenticatorAttestationResponseDto(
    string AttestationObject,
    string ClientDataJSON
);

/// <summary>
/// DTO para resposta do autenticador (asserção)
/// </summary>
public record Fido2AuthenticatorAssertionResponseDto(
    string AuthenticatorData,
    string ClientDataJSON,
    string Signature,
    string? UserHandle
);

/// <summary>
/// DTO para Relying Party
/// </summary>
public record Fido2RelyingPartyDto(
    string Id,
    string Name
);

/// <summary>
/// DTO para usuário FIDO2
/// </summary>
public record Fido2UserDto(
    string Id,
    string Name,
    string DisplayName
);

/// <summary>
/// DTO para parâmetros de chave pública
/// </summary>
public record Fido2PublicKeyCredentialParametersDto(
    string Type,
    int Alg
);

/// <summary>
/// DTO para descritor de credencial de chave pública
/// </summary>
public record Fido2PublicKeyCredentialDescriptorDto(
    string Type,
    string Id,
    string[]? Transports
);

/// <summary>
/// DTO para response de credenciais do usuário
/// </summary>
public record UserCredentialDto(
    Guid Id,
    string CredentialId,
    string? DeviceName,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    bool IsActive
);

/// <summary>
/// <summary>
/// DTO para remover credencial
/// </summary>
public record RemoveCredentialDto(
    [Required] Guid UserId,
    [Required] Guid CredentialId
);

/// <summary>
/// DTO para resposta de início de registro FIDO2
/// </summary>
public record Fido2RegisterStartResponseDto(
    string CredentialCreateOptions,
    string SessionData,
    bool Success,
    string? Error = null
);

/// <summary>
/// DTO para resposta de início de autenticação FIDO2
/// </summary>
public record Fido2AuthStartResponseDto(
    string AssertionOptions,
    string SessionData,
    bool Success,
    string? Error = null
);

/// <summary>
/// DTO para iniciar registro FIDO2
/// </summary>
public record Fido2RegisterStartDto(
    [Required] Guid UserId,
    string? DisplayName = null,
    string? DeviceName = null
);

/// <summary>
/// DTO para completar registro FIDO2
/// </summary>
public record Fido2RegisterCompleteDto(
    [Required] Guid UserId,
    [Required] string Response,
    string? DeviceName = null
);

/// <summary>
/// DTO para iniciar autenticação FIDO2
/// </summary>
public record Fido2AuthStartDto(
    Guid? UserId = null,
    string? Email = null
);

/// <summary>
/// DTO para completar autenticação FIDO2
/// </summary>
public record Fido2AuthCompleteDto(
    [Required] string Response
);
