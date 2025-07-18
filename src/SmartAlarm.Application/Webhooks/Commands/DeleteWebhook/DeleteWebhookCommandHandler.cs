using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Application.Webhooks.Commands.DeleteWebhook
{
    /// <summary>
    /// Handler para deleção de webhook
    /// </summary>
    public class DeleteWebhookCommandHandler : IRequestHandler<DeleteWebhookCommand, bool>
    {
        private readonly IWebhookRepository _webhookRepository;
        private readonly IValidator<DeleteWebhookCommand> _validator;
        private readonly ILogger<DeleteWebhookCommandHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public DeleteWebhookCommandHandler(
            IWebhookRepository webhookRepository,
            IValidator<DeleteWebhookCommand> validator,
            ILogger<DeleteWebhookCommandHandler> logger,
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

        public async Task<bool> Handle(DeleteWebhookCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("DeleteWebhookCommand");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("webhook.id", request.Id.ToString());
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation(LogTemplates.CommandStarted, "DeleteWebhookCommand", 
                    _correlationContext.CorrelationId, request.UserId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation failed for DeleteWebhookCommand: {@Errors}", 
                        validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ValidationException(validationResult.Errors);
                }

                // Verificar se webhook existe e pertence ao usuário
                var existingWebhook = await _webhookRepository.GetByIdAsync(request.Id);
                if (existingWebhook == null)
                {
                    _logger.LogWarning("Webhook {WebhookId} not found", request.Id);
                    return false;
                }

                if (existingWebhook.UserId != request.UserId)
                {
                    _logger.LogWarning("Webhook {WebhookId} does not belong to user {UserId}", 
                        request.Id, request.UserId);
                    throw new UnauthorizedAccessException("Acesso negado ao webhook");
                }

                // Deletar webhook
                var deleted = await _webhookRepository.DeleteAsync(request.Id);

                stopwatch.Stop();
                _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                    new KeyValuePair<string, object?>("operation", "delete"),
                    new KeyValuePair<string, object?>("user_id", request.UserId.ToString()),
                    new KeyValuePair<string, object?>("result", deleted.ToString()));

                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("deleted", deleted.ToString());

                if (deleted)
                {
                    _logger.LogInformation("Webhook {WebhookId} deleted successfully for user {UserId}", 
                        request.Id, request.UserId);
                }

                _logger.LogInformation(LogTemplates.CommandCompleted, "DeleteWebhookCommand", 
                    _correlationContext.CorrelationId, stopwatch.ElapsedMilliseconds);

                return deleted;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                _logger.LogError(ex, "Error executing DeleteWebhookCommand for webhook {WebhookId}: {Message}", 
                    request.Id, ex.Message);

                _meter.WebhookRegistrationErrors.Add(1, 
                    new KeyValuePair<string, object?>("user_id", request.UserId.ToString()),
                    new KeyValuePair<string, object?>("error_type", ex.GetType().Name));

                throw;
            }
        }
    }
}
