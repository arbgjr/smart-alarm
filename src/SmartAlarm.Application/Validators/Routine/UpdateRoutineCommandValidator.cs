using FluentValidation;
using SmartAlarm.Application.Commands.Routine;

namespace SmartAlarm.Application.Validators.Routine
{
    public class UpdateRoutineCommandValidator : AbstractValidator<UpdateRoutineCommand>
    {
        public UpdateRoutineCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
        }
    }
}
