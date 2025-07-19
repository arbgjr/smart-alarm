using FluentValidation;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Validator para TriggerAlarmCommand
    /// </summary>
    public class TriggerAlarmCommandValidator : AbstractValidator<TriggerAlarmCommand>
    {
        public TriggerAlarmCommandValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty()
                .WithMessage("ID do alarme é obrigatório");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("ID do usuário é obrigatório");

            RuleFor(x => x.TriggerType)
                .NotEmpty()
                .WithMessage("Tipo de disparo é obrigatório")
                .Must(BeValidTriggerType)
                .WithMessage("Tipo de disparo deve ser: scheduled, manual ou test");

            RuleFor(x => x.TriggeredAt)
                .NotEmpty()
                .WithMessage("Data/hora do disparo é obrigatória")
                .Must(BeReasonableTime)
                .WithMessage("Data/hora do disparo deve estar dentro de um intervalo razoável");
        }

        private static bool BeValidTriggerType(string triggerType)
        {
            var validTypes = new[] { "scheduled", "manual", "test" };
            return validTypes.Contains(triggerType?.ToLowerInvariant());
        }

        private static bool BeReasonableTime(DateTime triggeredAt)
        {
            var now = DateTime.UtcNow;
            var minTime = now.AddDays(-1); // Máximo 1 dia no passado
            var maxTime = now.AddMinutes(5); // Máximo 5 minutos no futuro
            
            return triggeredAt >= minTime && triggeredAt <= maxTime;
        }
    }
}
