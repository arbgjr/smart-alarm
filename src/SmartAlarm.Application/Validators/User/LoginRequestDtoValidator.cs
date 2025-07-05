using FluentValidation;

namespace SmartAlarm.Application.Validators.User
{
    public class LoginRequestDtoValidator : AbstractValidator<DTOs.User.LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("O nome de usuário é obrigatório.");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("A senha é obrigatória.");
        }
    }
}
