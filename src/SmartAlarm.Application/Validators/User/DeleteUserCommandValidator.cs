using FluentValidation;
using SmartAlarm.Application.Commands.User;

namespace SmartAlarm.Application.Validators.User
{
    public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
    {
        public DeleteUserCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
