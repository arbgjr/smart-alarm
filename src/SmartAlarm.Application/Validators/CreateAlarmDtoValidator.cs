using FluentValidation;
using SmartAlarm.Application.DTOs;

namespace SmartAlarm.Application.Validators
{
    /// <summary>
    /// Validador FluentValidation para criação de alarme.
    /// </summary>
    public class CreateAlarmDtoValidator : AbstractValidator<CreateAlarmDto>
    {
        public CreateAlarmDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Validation.Required.AlarmName")
                .Length(1, 100).WithMessage("Validation.Length.AlarmNameMaxLength");

            RuleFor(x => x.Time)
                .NotEmpty().WithMessage("Validation.Required.AlarmTime")
                .GreaterThan(DateTime.Now).WithMessage("Validation.Range.FutureDateTime");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Validation.Required.UserId");
        }
    }
}
