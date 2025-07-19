using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Application.DTOs.Auth;

/// <summary>
/// DTO para request de login tradicional
/// </summary>
public class LoginRequestDto
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Indica se deve lembrar do login (refresh token de longa duração)
    /// </summary>
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// DTO para request de registro
/// </summary>
public class RegisterRequestDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MinLength(2, ErrorMessage = "Nome deve ter pelo menos 2 caracteres")]
    [MaxLength(100, ErrorMessage = "Nome não pode ter mais de 100 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(8, ErrorMessage = "Senha deve ter pelo menos 8 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Senha deve conter pelo menos: 1 letra minúscula, 1 maiúscula, 1 número e 1 caractere especial")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
    [Compare(nameof(Password), ErrorMessage = "Senhas não coincidem")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO para response de autenticação
/// </summary>
public class AuthResponseDto
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ExpiresIn => (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds;
    public UserDto? User { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; }
}

/// <summary>
/// DTO simplificado de usuário para responses
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}

/// <summary>
/// DTO para request de refresh token
/// </summary>
public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "Refresh token é obrigatório")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// DTO para request de logout
/// </summary>
public class LogoutRequestDto
{
    [Required(ErrorMessage = "Token é obrigatório")]
    public string Token { get; set; } = string.Empty;
}
