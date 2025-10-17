using MediatR;
using SmartAlarm.IntegrationService.Infrastructure.Webhooks;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using System.Diagnostics;

namespace SmartAlarm.IntegrationService.Application.Commands
{
    /// <summary>
    /// Handler para processamento de webhook
    /// </summary>
    public class ProcessWebhookCommandHandler : IRequestHandler<ProcessWebhookCommand, ProcessWebhookResponse>
    {
        private readonly IWebhookManager _webhookManager;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<ProcessWebhookCommandHandler> _logger;

        public ProcessWebhookCommandHandler(
            IWebhookManager webhookManager,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<ProcessWebhookCommandHandler> logger)
        {
            _webhookManager = webhookManager;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<ProcessWebhookResponse> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("ProcessWebhookCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("webhook.id", request.WebhookId);
                activity?.SetTag("payload.size", request.Payload.Length.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Processando webhook {WebhookId} - Payload size: {PayloadSize} - CorrelationId: {CorrelationId}",
                    request.WebhookId, request.Payload.Length, _correlationContext.CorrelationId);

                // Processar webhook
                var result = await _webhookManager.ProcessIncomingWebhookAsync(
                    request.WebhookId,
                    request.Payload,
                    request.Headers,
                    cancellationToken);

                stopwatch.Stop();

                // Métricas baseadas no resultado
                var status = result.Success ? "success" : "failed";
                var statusCode = result.Success ? "200" : "400";
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "process_webhook", status, statusCode);

                var response = new ProcessWebhookResponse(
                    WebhookId: result.WebhookId,
                    Success: result.Success,
                    Message: result.Message,
                    ProcessedAt: result.ProcessedAt,
                    ProcessingDuration: result.ProcessingDuration,
                    ActionsTriggered: result.ActionsTriggered
                );

                _logger.LogInformation("Webhook {WebhookId} processado - Success: {Success}, Ações: {ActionsCount} em {Duration}ms - CorrelationId: {CorrelationId}",
                    request.WebhookId, result.Success, result.ActionsTriggered.Count(), stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "process_webhook", "error", "500");
                _meter.IncrementErrorCount("command", "process_webhook", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro inesperado ao processar webhook {WebhookId} - CorrelationId: {CorrelationId}",
                    request.WebhookId, _correlationContext.CorrelationId);

                throw;
            }
        }
    }
}
