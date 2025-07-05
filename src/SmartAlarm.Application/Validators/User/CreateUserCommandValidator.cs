using FluentValidation;
using SmartAlarm.Application.Commands.User;

namespace SmartAlarm.Application.Validators.User
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.User.Name).NotEmpty().MinimumLength(3);
            RuleFor(x => x.User.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.User.Password).NotEmpty().MinimumLength(6);
        }
    }
}
