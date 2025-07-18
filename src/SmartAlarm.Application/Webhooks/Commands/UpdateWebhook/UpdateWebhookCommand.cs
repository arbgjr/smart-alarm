using FluentValidation;
using MediatR;
using SmartAlarm.Application.Webhooks.Models;

namespace SmartAlarm.Application.Webhooks.Commands.UpdateWebhook
{
    /// <summary>
    /// Comando para atualizar um webhook
    /// </summary>
    public class UpdateWebhookCommand : IRequest<WebhookResponse>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Url { get; set; }
        public string[]? Events { get; set; }
        public bool? IsActive { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Validador para o comando de atualização de webhook
    /// </summary>
    public class UpdateWebhookCommandValidator : AbstractValidator<UpdateWebhookCommand>
    {
        private readonly string[] _validEvents = 
        {
            "alarm.created",
            "alarm.triggered", 
            "alarm.dismissed",
            "alarm.snoozed",
            "user.created",
            "routine.executed",
            "integration.connected",
            "integration.failed"
        };

        public UpdateWebhookCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("ID do webhook é obrigatório");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("ID do usuário é obrigatório");

            RuleFor(x => x.Url)
                .Must(BeAValidUrlWhenProvided)
                .WithMessage("URL deve ser válida (HTTP ou HTTPS)")
                .MaximumLength(2048)
                .WithMessage("URL deve ter no máximo 2048 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Url));

            RuleFor(x => x.Events)
                .Must(HaveValidEventsWhenProvided)
                .WithMessage($"Eventos devem ser válidos: {string.Join(", ", _validEvents)}")
                .Must(HaveUniqueEventsWhenProvided)
                .WithMessage("Eventos devem ser únicos")
                .When(x => x.Events != null && x.Events.Length > 0);

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Descrição deve ter no máximo 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }

        private bool BeAValidUrlWhenProvided(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true; // Null/empty é válido para update parcial

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private bool HaveValidEventsWhenProvided(string[]? events)
        {
            if (events == null || events.Length == 0)
                return true; // Null/empty é válido para update parcial

            return events.All(e => _validEvents.Contains(e, StringComparer.OrdinalIgnoreCase));
        }

        private bool HaveUniqueEventsWhenProvided(string[]? events)
        {
            if (events == null || events.Length == 0)
                return true; // Null/empty é válido para update parcial

            return events.Length == events.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        }
    }
}
