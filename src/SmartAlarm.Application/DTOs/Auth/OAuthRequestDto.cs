using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Application.DTOs.Auth;

/// <summary>
/// DTO para iniciar processo de autorização OAuth2
/// </summary>
public class OAuthAuthorizationRequestDto
{
    [Required]
    public string Provider { get; set; } = string.Empty;
    
    [Required]
    public string RedirectUri { get; set; } = string.Empty;
    
    public string? State { get; set; }
    
    public List<string>? Scopes { get; set; }
}

/// <summary>
/// DTO para resposta de autorização OAuth2
/// </summary>
public class OAuthAuthorizationResponseDto
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

/// <summary>
/// DTO para callback OAuth2 após autorização
/// </summary>
public class OAuthCallbackRequestDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
    
    public string? State { get; set; }
    
    public string? Error { get; set; }
    
    public string? ErrorDescription { get; set; }
}

/// <summary>
/// DTO para resposta do login OAuth2
/// </summary>
public class OAuthLoginResponseDto
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string? Message { get; set; }
    public string? Provider { get; set; }
    public bool IsNewUser { get; set; }
}