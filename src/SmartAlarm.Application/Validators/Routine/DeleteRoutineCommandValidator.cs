using FluentValidation;
using SmartAlarm.Application.Commands.Routine;

namespace SmartAlarm.Application.Validators.Routine
{
    public class DeleteRoutineCommandValidator : AbstractValidator<DeleteRoutineCommand>
    {
        public DeleteRoutineCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
