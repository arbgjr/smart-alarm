using FluentValidation;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Validator para comando de processamento distribuído de alarmes
    /// </summary>
    public class ProcessDistributedAlarmCommandValidator : AbstractValidator<ProcessDistributedAlarmCommand>
    {
        public ProcessDistributedAlarmCommandValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty()
                .WithMessage("AlarmId é obrigatório");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId é obrigatório");

            RuleFor(x => x.TriggerType)
                .NotEmpty()
                .Must(type => new[] { "scheduled", "manual", "api", "test" }.Contains(type))
                .WithMessage("TriggerType deve ser: scheduled, manual, api ou test");

            RuleFor(x => x.Delay)
                .Must(delay => !delay.HasValue || delay.Value >= TimeSpan.Zero)
                .WithMessage("Delay deve ser maior ou igual a zero");
        }
    }
}
