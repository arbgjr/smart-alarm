using FluentValidation;
using SmartAlarm.Application.Commands.Auth;

namespace SmartAlarm.Application.Validators.Auth;

/// <summary>
/// Validador para comando de login
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email deve ter formato válido")
            .MaximumLength(254).WithMessage("Email não pode ter mais de 254 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .MinimumLength(6).WithMessage("Senha deve ter pelo menos 6 caracteres")
            .MaximumLength(128).WithMessage("Senha não pode ter mais de 128 caracteres");
    }
}

/// <summary>
/// Validador para comando de registro
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres")
            .MaximumLength(100).WithMessage("Nome não pode ter mais de 100 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras e espaços");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email deve ter formato válido")
            .MaximumLength(254).WithMessage("Email não pode ter mais de 254 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres")
            .MaximumLength(128).WithMessage("Senha não pode ter mais de 128 caracteres")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("Senha deve conter pelo menos: 1 letra minúscula, 1 maiúscula, 1 número e 1 caractere especial");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória")
            .Equal(x => x.Password).WithMessage("Senhas não coincidem");
    }
}

/// <summary>
/// Validador para comando de refresh token
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token é obrigatório")
            .MinimumLength(32).WithMessage("Refresh token inválido");
    }
}

/// <summary>
/// Validador para comando de logout
/// </summary>
public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token é obrigatório")
            .MinimumLength(100).WithMessage("Token inválido"); // JWT mínimo
    }
}

/// <summary>
/// Validador para comando de início de registro FIDO2
/// </summary>
public class Fido2RegisterStartCommandValidator : AbstractValidator<Fido2RegisterStartCommand>
{
    public Fido2RegisterStartCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID é obrigatório");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name é obrigatório")
            .MinimumLength(2).WithMessage("Display name deve ter pelo menos 2 caracteres")
            .MaximumLength(100).WithMessage("Display name não pode ter mais de 100 caracteres");

        RuleFor(x => x.DeviceName)
            .MaximumLength(200).WithMessage("Nome do dispositivo não pode ter mais de 200 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.DeviceName));
    }
}

/// <summary>
/// Validador para comando de conclusão de registro FIDO2
/// </summary>
public class Fido2RegisterCompleteCommandValidator : AbstractValidator<Fido2RegisterCompleteCommand>
{
    public Fido2RegisterCompleteCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID é obrigatório");

        RuleFor(x => x.Response)
            .NotNull().WithMessage("Response é obrigatória");

        RuleFor(x => x.DeviceName)
            .MaximumLength(200).WithMessage("Nome do dispositivo não pode ter mais de 200 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.DeviceName));
    }
}

/// <summary>
/// Validador para comando de conclusão de autenticação FIDO2
/// </summary>
public class Fido2AuthCompleteCommandValidator : AbstractValidator<Fido2AuthCompleteCommand>
{
    public Fido2AuthCompleteCommandValidator()
    {
        RuleFor(x => x.Response)
            .NotNull().WithMessage("Response é obrigatória");
    }
}
