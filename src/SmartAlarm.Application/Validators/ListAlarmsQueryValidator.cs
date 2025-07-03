using FluentValidation;
using SmartAlarm.Application.Queries;

namespace SmartAlarm.Application.Validators
{
    /// <summary>
    /// Validador FluentValidation para listagem de alarmes.
    /// </summary>
    public class ListAlarmsQueryValidator : AbstractValidator<ListAlarmsQuery>
    {
        public ListAlarmsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Validation.Required.UserId");
        }
    }
}