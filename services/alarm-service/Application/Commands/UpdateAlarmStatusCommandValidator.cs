using FluentValidation;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Validator para comando de atualização de status de alarme
    /// </summary>
    public class UpdateAlarmStatusCommandValidator : AbstractValidator<UpdateAlarmStatusCommand>
    {
        public UpdateAlarmStatusCommandValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty()
                .WithMessage("AlarmId é obrigatório");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId é obrigatório");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Motivo não pode exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Reason));
        }
    }
}
