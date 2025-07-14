using FluentValidation;
using SmartAlarm.Application.Commands.Holiday;

namespace SmartAlarm.Application.Validators.Holiday
{
    /// <summary>
    /// Validator para CreateHolidayCommand.
    /// </summary>
    public class CreateHolidayCommandValidator : AbstractValidator<CreateHolidayCommand>
    {
        public CreateHolidayCommandValidator()
        {
            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Descrição é obrigatória")
                .Length(2, 100)
                .WithMessage("Descrição deve ter entre 2 e 100 caracteres");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Data é obrigatória");
        }
    }
}
