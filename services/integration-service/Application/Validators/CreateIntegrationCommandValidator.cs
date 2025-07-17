using FluentValidation;
using SmartAlarm.IntegrationService.Application.Commands;

namespace SmartAlarm.IntegrationService.Application.Validators
{
    /// <summary>
    /// Validador para o comando de criação de integração.
    /// </summary>
    public class CreateIntegrationCommandValidator : AbstractValidator<CreateIntegrationCommand>
    {
        private static readonly string[] ValidProviders = 
        {
            "google", "microsoft", "outlook", "apple", "caldav",
            "slack", "teams", "webhook", "email", "sms", "whatsapp"
        };

        public CreateIntegrationCommandValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty()
                .WithMessage("AlarmId é obrigatório");

            RuleFor(x => x.Provider)
                .NotEmpty()
                .WithMessage("Provider é obrigatório")
                .Must(BeValidProvider)
                .WithMessage($"Provider deve ser um dos seguintes: {string.Join(", ", ValidProviders)}");

            RuleFor(x => x.Configuration)
                .NotNull()
                .WithMessage("Configuration não pode ser nula");

            When(x => x.Provider?.ToLowerInvariant() == "google", () =>
            {
                RuleFor(x => x.Configuration)
                    .Must(config => config.ContainsKey("clientId") && !string.IsNullOrEmpty(config["clientId"]))
                    .WithMessage("Google Calendar requer clientId na configuração");
            });

            When(x => x.Provider?.ToLowerInvariant() == "microsoft" || x.Provider?.ToLowerInvariant() == "outlook", () =>
            {
                RuleFor(x => x.Configuration)
                    .Must(config => config.ContainsKey("tenantId") && !string.IsNullOrEmpty(config["tenantId"]))
                    .WithMessage("Microsoft/Outlook requer tenantId na configuração");
            });

            When(x => x.Provider?.ToLowerInvariant() == "webhook", () =>
            {
                RuleFor(x => x.Configuration)
                    .Must(config => config.ContainsKey("url") && !string.IsNullOrEmpty(config["url"]))
                    .WithMessage("Webhook requer URL na configuração")
                    .Must(config => config.ContainsKey("url") && Uri.TryCreate(config["url"], UriKind.Absolute, out _))
                    .WithMessage("URL do webhook deve ser válida");
            });

            RuleFor(x => x.Features)
                .Must(features => features?.All(f => !string.IsNullOrWhiteSpace(f)) ?? true)
                .WithMessage("Features não podem conter valores vazios");
        }

        private static bool BeValidProvider(string? provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
                return false;

            return ValidProviders.Contains(provider.ToLowerInvariant());
        }
    }
}
