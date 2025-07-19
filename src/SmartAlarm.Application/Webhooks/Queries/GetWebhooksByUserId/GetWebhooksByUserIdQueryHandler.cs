using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Webhooks.Models;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Application.Webhooks.Queries.GetWebhooksByUserId
{
    /// <summary>
    /// Handler para buscar webhooks de um usuário
    /// </summary>
    public class GetWebhooksByUserIdQueryHandler : IRequestHandler<GetWebhooksByUserIdQuery, WebhookListResponse>
    {
        private readonly IWebhookRepository _webhookRepository;
        private readonly ILogger<GetWebhooksByUserIdQueryHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public GetWebhooksByUserIdQueryHandler(
            IWebhookRepository webhookRepository,
            ILogger<GetWebhooksByUserIdQueryHandler> logger,
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

        public async Task<WebhookListResponse> Handle(GetWebhooksByUserIdQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("GetWebhooksByUserIdQuery");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("include.inactive", request.IncludeInactive.ToString());
                activity?.SetTag("pagination.page", request.Page.ToString());
                activity?.SetTag("pagination.pageSize", request.PageSize.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation(LogTemplates.QueryStarted, "GetWebhooksByUserIdQuery", 
                    _correlationContext.CorrelationId, request.UserId);

                var allWebhooks = await _webhookRepository.GetByUserIdAsync(request.UserId);

                // Filtrar por ativo/inativo se necessário
                var filteredWebhooks = request.IncludeInactive 
                    ? allWebhooks 
                    : allWebhooks.Where(w => w.IsActive);

                var webhooksList = filteredWebhooks.ToList();
                var totalCount = webhooksList.Count;

                // Aplicar paginação
                var paginatedWebhooks = webhooksList
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(MapToWebhookResponse)
                    .ToList();

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var response = new WebhookListResponse
                {
                    Webhooks = paginatedWebhooks,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                };

                stopwatch.Stop();
                _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                    new KeyValuePair<string, object?>("operation", "get_by_user"),
                    new KeyValuePair<string, object?>("result_count", totalCount.ToString()));

                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("result.total_count", totalCount.ToString());

                _logger.LogInformation(LogTemplates.QueryCompleted, "GetWebhooksByUserIdQuery", 
                    _correlationContext.CorrelationId, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                _logger.LogError(ex, "Error executing GetWebhooksByUserIdQuery for user {UserId}: {Message}", 
                    request.UserId, ex.Message);

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
