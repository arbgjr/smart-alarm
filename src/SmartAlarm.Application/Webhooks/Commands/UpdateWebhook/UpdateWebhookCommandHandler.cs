using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Webhooks.Models;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Application.Webhooks.Commands.UpdateWebhook
{
    /// <summary>
    /// Handler para atualização de webhook
    /// </summary>
    public class UpdateWebhookCommandHandler : IRequestHandler<UpdateWebhookCommand, WebhookResponse>
    {
        private readonly IWebhookRepository _webhookRepository;
        private readonly IValidator<UpdateWebhookCommand> _validator;
        private readonly ILogger<UpdateWebhookCommandHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public UpdateWebhookCommandHandler(
            IWebhookRepository webhookRepository,
            IValidator<UpdateWebhookCommand> validator,
            ILogger<UpdateWebhookCommandHandler> logger,
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

        public async Task<WebhookResponse> Handle(UpdateWebhookCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("UpdateWebhookCommand");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("webhook.id", request.Id.ToString());
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation(LogTemplates.CommandStarted, "UpdateWebhookCommand", 
                    _correlationContext.CorrelationId, request.UserId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation failed for UpdateWebhookCommand: {@Errors}", 
                        validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ValidationException(validationResult.Errors);
                }

                // Buscar webhook existente
                var existingWebhook = await _webhookRepository.GetByIdAsync(request.Id);
                if (existingWebhook == null || existingWebhook.UserId != request.UserId)
                {
                    _logger.LogWarning("Webhook {WebhookId} not found or does not belong to user {UserId}", 
                        request.Id, request.UserId);
                    throw new InvalidOperationException("Webhook não encontrado ou acesso negado");
                }

                // Verificar se a nova URL já existe para outro webhook do usuário
                if (!string.IsNullOrEmpty(request.Url) && !request.Url.Equals(existingWebhook.Url, StringComparison.OrdinalIgnoreCase))
                {
                    var userWebhooks = await _webhookRepository.GetByUserIdAsync(request.UserId);
                    if (userWebhooks.Any(w => w.Id != request.Id && w.Url.Equals(request.Url, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("Já existe um webhook registrado para esta URL");
                    }
                }

                // Atualizar campos fornecidos
                if (!string.IsNullOrEmpty(request.Url))
                    existingWebhook.Url = request.Url;

                if (request.Events != null && request.Events.Length > 0)
                    existingWebhook.Events = request.Events;

                if (request.IsActive.HasValue)
                    existingWebhook.IsActive = request.IsActive.Value;

                existingWebhook.UpdatedAt = DateTime.UtcNow;

                var updatedWebhook = await _webhookRepository.UpdateAsync(existingWebhook);

                var response = MapToWebhookResponse(updatedWebhook);

                stopwatch.Stop();
                _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                    new KeyValuePair<string, object?>("operation", "update"),
                    new KeyValuePair<string, object?>("user_id", request.UserId.ToString()));

                activity?.SetStatus(ActivityStatusCode.Ok);

                _logger.LogInformation(LogTemplates.CommandCompleted, "UpdateWebhookCommand", 
                    _correlationContext.CorrelationId, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                _logger.LogError(ex, "Error executing UpdateWebhookCommand for webhook {WebhookId}: {Message}", 
                    request.Id, ex.Message);

                _meter.WebhookRegistrationErrors.Add(1, 
                    new KeyValuePair<string, object?>("user_id", request.UserId.ToString()),
                    new KeyValuePair<string, object?>("error_type", ex.GetType().Name));

                throw;
            }
        }

        private static WebhookResponse MapToWebhookResponse(Domain.Entities.Webhook webhook)
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
