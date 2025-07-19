using FluentValidation;
using SmartAlarm.Application.Commands;

namespace SmartAlarm.Application.Validators
{
    /// <summary>
    /// Validador FluentValidation para atualização de alarme.
    /// </summary>
    public class UpdateAlarmCommandValidator : AbstractValidator<UpdateAlarmCommand>
    {
        public UpdateAlarmCommandValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty().WithMessage("Validation.Required.AlarmId");

            RuleFor(x => x.Alarm)
                .NotNull().WithMessage("Validation.Required.AlarmData")
                .SetValidator(new CreateAlarmDtoValidator());
        }
    }
}