using FluentValidation;
using SmartAlarm.Application.Commands.Routine;

namespace SmartAlarm.Application.Validators.Routine
{
    public class CreateRoutineCommandValidator : AbstractValidator<CreateRoutineCommand>
    {
        public CreateRoutineCommandValidator()
        {
            RuleFor(x => x.Routine.Name)
                .NotEmpty().WithMessage("Nome da rotina é obrigatório.")
                .MinimumLength(3).WithMessage("Nome deve ter pelo menos 3 caracteres.");
            RuleFor(x => x.Routine.UserId)
                .NotEmpty().WithMessage("Usuário é obrigatório.");
        }
    }
}
