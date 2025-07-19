using FluentValidation;
using SmartAlarm.Application.Commands.Holiday;

namespace SmartAlarm.Application.Validators.Holiday
{
    /// <summary>
    /// Validator para DeleteHolidayCommand.
    /// </summary>
    public class DeleteHolidayCommandValidator : AbstractValidator<DeleteHolidayCommand>
    {
        public DeleteHolidayCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("ID é obrigatório");
        }
    }
}
