using FluentValidation;
using SmartAlarm.Application.Commands;

namespace SmartAlarm.Application.Validators
{
    /// <summary>
    /// Validador FluentValidation para exclusão de alarme.
    /// </summary>
    public class DeleteAlarmCommandValidator : AbstractValidator<DeleteAlarmCommand>
    {
        public DeleteAlarmCommandValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty().WithMessage("Validation.Required.AlarmId");
        }
    }
}