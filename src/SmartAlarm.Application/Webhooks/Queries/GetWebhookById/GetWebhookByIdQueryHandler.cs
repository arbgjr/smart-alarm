using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Webhooks.Models;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Application.Webhooks.Queries.GetWebhookById
{
    /// <summary>
    /// Handler para buscar webhook por ID
    /// </summary>
    public class GetWebhookByIdQueryHandler : IRequestHandler<GetWebhookByIdQuery, WebhookResponse?>
    {
        private readonly IWebhookRepository _webhookRepository;
        private readonly ILogger<GetWebhookByIdQueryHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public GetWebhookByIdQueryHandler(
            IWebhookRepository webhookRepository,
            ILogger<GetWebhookByIdQueryHandler> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _webhookRepository = webhookRepository ?? throw new ArgumentNullException(nameof(webhookRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        }

        public async Task<WebhookResponse?> Handle(GetWebhookByIdQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("GetWebhookByIdQuery");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("webhook.id", request.Id.ToString());
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation(LogTemplates.QueryStarted, "GetWebhookByIdQuery", 
                    _correlationContext.CorrelationId, request.UserId);

                var webhook = await _webhookRepository.GetByIdAsync(request.Id);

                if (webhook == null || webhook.UserId != request.UserId)
                {
                    _logger.LogWarning("Webhook {WebhookId} not found or does not belong to user {UserId}", 
                        request.Id, request.UserId);

                    stopwatch.Stop();
                    _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                        new KeyValuePair<string, object?>("operation", "get_by_id"),
                        new KeyValuePair<string, object?>("result", "not_found"));

                    return null;
                }

                var response = MapToWebhookResponse(webhook);

                stopwatch.Stop();
                _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                    new KeyValuePair<string, object?>("operation", "get_by_id"),
                    new KeyValuePair<string, object?>("result", "found"));

                activity?.SetStatus(ActivityStatusCode.Ok);

                _logger.LogInformation(LogTemplates.QueryCompleted, "GetWebhookByIdQuery", 
                    _correlationContext.CorrelationId, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                _logger.LogError(ex, "Error executing GetWebhookByIdQuery for webhook {WebhookId}: {Message}", 
                    request.Id, ex.Message);

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
