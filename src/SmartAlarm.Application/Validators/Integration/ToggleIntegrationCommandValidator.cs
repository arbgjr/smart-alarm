using FluentValidation;
using SmartAlarm.Application.Commands.Integration;

namespace SmartAlarm.Application.Validators.Integration
{
    public class ToggleIntegrationCommandValidator : AbstractValidator<ToggleIntegrationCommand>
    {
        public ToggleIntegrationCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
