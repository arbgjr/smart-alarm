using FluentValidation;
using MediatR;

namespace SmartAlarm.Application.Webhooks.Commands.RegisterWebhook
{
    /// <summary>
    /// Comando para registrar um novo webhook
    /// </summary>
    public class RegisterWebhookCommand : IRequest<RegisterWebhookResponse>
    {
        public string Url { get; set; } = string.Empty;
        public string[] Events { get; set; } = Array.Empty<string>();
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Resposta do comando de registro de webhook
    /// </summary>
    public class RegisterWebhookResponse
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string[] Events { get; set; } = Array.Empty<string>();
        public string Secret { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Validador para o comando de registro de webhook
    /// </summary>
    public class RegisterWebhookCommandValidator : AbstractValidator<RegisterWebhookCommand>
    {
        private readonly string[] _validEvents = 
        {
            "alarm.created",
            "alarm.triggered", 
            "alarm.dismissed",
            "alarm.snoozed",
            "user.created",
            "routine.executed"
        };

        public RegisterWebhookCommandValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty()
                .WithMessage("URL é obrigatória")
                .Must(BeAValidUrl)
                .WithMessage("URL deve ser válida (HTTP ou HTTPS)");

            RuleFor(x => x.Events)
                .NotEmpty()
                .WithMessage("Pelo menos um evento deve ser especificado")
                .Must(HaveValidEvents)
                .WithMessage($"Eventos devem ser válidos: {string.Join(", ", _validEvents)}");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("ID do usuário é obrigatório");
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

            return events.All(e => _validEvents.Contains(e));
        }
    }
}
