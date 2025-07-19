using FluentValidation;
using SmartAlarm.Application.Queries;

namespace SmartAlarm.Application.Validators
{
    /// <summary>
    /// Validador FluentValidation para busca de alarme por ID.
    /// </summary>
    public class GetAlarmByIdQueryValidator : AbstractValidator<GetAlarmByIdQuery>
    {
        public GetAlarmByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Validation.Required.AlarmId");
        }
    }
}