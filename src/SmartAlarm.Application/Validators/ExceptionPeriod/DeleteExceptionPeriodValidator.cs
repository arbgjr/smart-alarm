using FluentValidation;
using SmartAlarm.Application.Commands.ExceptionPeriod;

namespace SmartAlarm.Application.Validators.ExceptionPeriod;

public class DeleteExceptionPeriodValidator : AbstractValidator<DeleteExceptionPeriodCommand>
{
    public DeleteExceptionPeriodValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID é obrigatório");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("ID do usuário é obrigatório");
    }
}
