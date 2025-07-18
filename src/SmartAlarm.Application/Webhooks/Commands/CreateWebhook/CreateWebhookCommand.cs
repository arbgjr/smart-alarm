using FluentValidation;
using MediatR;
using SmartAlarm.Application.Webhooks.Models;

namespace SmartAlarm.Application.Webhooks.Commands.CreateWebhook
{
    /// <summary>
    /// Comando para criar um novo webhook
    /// </summary>
    public class CreateWebhookCommand : IRequest<WebhookResponse>
    {
        public string Url { get; set; } = string.Empty;
        public string[] Events { get; set; } = Array.Empty<string>();
        public Guid UserId { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Validador para o comando de criação de webhook
    /// </summary>
    public class CreateWebhookCommandValidator : AbstractValidator<CreateWebhookCommand>
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

        public CreateWebhookCommandValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty()
                .WithMessage("URL é obrigatória")
                .Must(BeAValidUrl)
                .WithMessage("URL deve ser válida (HTTP ou HTTPS)")
                .MaximumLength(2048)
                .WithMessage("URL deve ter no máximo 2048 caracteres");

            RuleFor(x => x.Events)
                .NotEmpty()
                .WithMessage("Pelo menos um evento deve ser especificado")
                .Must(HaveValidEvents)
                .WithMessage($"Eventos devem ser válidos: {string.Join(", ", _validEvents)}")
                .Must(HaveUniqueEvents)
                .WithMessage("Eventos devem ser únicos");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("ID do usuário é obrigatório");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Descrição deve ter no máximo 500 caracteres");
        }

        private bool BeAValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private bool HaveValidEvents(string[] events)
        {
            if (events == null || events.Length == 0)
                return false;

            return events.All(e => _validEvents.Contains(e, StringComparer.OrdinalIgnoreCase));
        }

        private bool HaveUniqueEvents(string[] events)
        {
            if (events == null || events.Length == 0)
                return true;

            return events.Length == events.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        }
    }
}
