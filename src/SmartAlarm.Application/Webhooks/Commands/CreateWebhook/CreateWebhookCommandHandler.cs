using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Application.Webhooks.Models;

namespace SmartAlarm.Application.Webhooks.Commands.CreateWebhook
{
    /// <summary>
    /// Handler para criação de webhook
    /// </summary>
    public class CreateWebhookCommandHandler : IRequestHandler<CreateWebhookCommand, WebhookResponse>
    {
        private readonly IWebhookRepository _webhookRepository;
        private readonly IValidator<CreateWebhookCommand> _validator;
        private readonly ILogger<CreateWebhookCommandHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public CreateWebhookCommandHandler(
            IWebhookRepository webhookRepository,
            IValidator<CreateWebhookCommand> validator,
            ILogger<CreateWebhookCommandHandler> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _webhookRepository = webhookRepository ?? throw new ArgumentNullException(nameof(webhookRepository));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        }

        public async Task<WebhookResponse> Handle(CreateWebhookCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("CreateWebhookCommand");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("webhook.url", request.Url);
                activity?.SetTag("webhook.events", string.Join(",", request.Events));
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation(LogTemplates.CommandStarted, "CreateWebhookCommand", 
                    _correlationContext.CorrelationId, request.UserId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation failed for CreateWebhookCommand: {@Errors}", 
                        validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ValidationException(validationResult.Errors);
                }

                // Verificar se já existe webhook com a mesma URL para o usuário
                var existingWebhooks = await _webhookRepository.GetByUserIdAsync(request.UserId);
                if (existingWebhooks.Any(w => w.Url.Equals(request.Url, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Já existe um webhook registrado para esta URL");
                }

                // Gerar secret seguro
                var secret = GenerateSecureSecret();

                // Criar webhook
                var webhook = new Webhook
                {
                    Id = Guid.NewGuid(),
                    Url = request.Url,
                    Events = request.Events,
                    Secret = secret,
                    UserId = request.UserId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    FailureCount = 0
                };

                var createdWebhook = await _webhookRepository.CreateAsync(webhook);

                var response = MapToWebhookResponse(createdWebhook);

                // Métricas
                _meter.WebhookRegistered.Add(1, 
                    new KeyValuePair<string, object?>("user_id", request.UserId.ToString()),
                    new KeyValuePair<string, object?>("event_count", request.Events.Length));

                stopwatch.Stop();
                _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                    new KeyValuePair<string, object?>("operation", "create"),
                    new KeyValuePair<string, object?>("user_id", request.UserId.ToString()));

                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("webhook.id", response.Id.ToString());

                _logger.LogInformation(LogTemplates.CommandCompleted, "CreateWebhookCommand", 
                    _correlationContext.CorrelationId, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                _logger.LogError(ex, "Error executing CreateWebhookCommand for user {UserId}: {Message}", 
                    request.UserId, ex.Message);
                
                _meter.WebhookRegistrationErrors.Add(1, 
                    new KeyValuePair<string, object?>("user_id", request.UserId.ToString()),
                    new KeyValuePair<string, object?>("error_type", ex.GetType().Name));

                throw;
            }
        }

        private static string GenerateSecureSecret()
        {
            const int secretLength = 32;
            using var rng = RandomNumberGenerator.Create();
            var secretBytes = new byte[secretLength];
            rng.GetBytes(secretBytes);
            return Convert.ToBase64String(secretBytes);
        }

        private static WebhookResponse MapToWebhookResponse(Webhook webhook)
        {
            return new WebhookResponse
            {
                Id = webhook.Id,
                Url = webhook.Url,
                Events = webhook.Events,
                Secret = webhook.Secret,
                IsActive = webhook.IsActive,
                CreatedAt = webhook.CreatedAt,
                UpdatedAt = webhook.UpdatedAt,
                FailureCount = webhook.FailureCount,
                LastDeliveryAttempt = webhook.LastDeliveryAttempt,
                LastSuccessfulDelivery = webhook.LastSuccessfulDelivery
            };
        }
    }
}
