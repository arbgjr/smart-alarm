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
                .NotNull().WithMessage("Validation.Required.AlarmName")
                .NotEmpty().WithMessage("Validation.Required.AlarmName")
                .Length(1, 100).WithMessage("Validation.Length.AlarmNameMaxLength");

            RuleFor(x => x.Time)
                .NotNull().WithMessage("Validation.Required.AlarmTime")
                .Must(time => BeInTheFuture(time)).WithMessage("Validation.Range.FutureDateTime");

        }

        private bool BeInTheFuture(DateTime? time)
        {
            return time.HasValue && time.Value > DateTime.UtcNow;
        }
    }
}
