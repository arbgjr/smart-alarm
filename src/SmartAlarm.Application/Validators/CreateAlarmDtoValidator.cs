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
                .NotEmpty().WithMessage("Nome do alarme é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter até 100 caracteres.");
            RuleFor(x => x.Time)
                .NotEmpty().WithMessage("Horário do alarme é obrigatório.");
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Usuário é obrigatório.");
        }
    }
}
