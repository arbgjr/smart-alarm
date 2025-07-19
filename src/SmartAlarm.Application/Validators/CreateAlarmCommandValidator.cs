using FluentValidation;
using SmartAlarm.Application.Commands;

namespace SmartAlarm.Application.Validators
{
    /// <summary>
    /// Validador FluentValidation para comando de criação de alarme.
    /// </summary>
    public class CreateAlarmCommandValidator : AbstractValidator<CreateAlarmCommand>
    {
        public CreateAlarmCommandValidator()
        {
            RuleFor(x => x.Alarm)
                .NotNull().WithMessage("Validation.Required.AlarmData")
                .SetValidator(new CreateAlarmDtoValidator());
        }
    }
}