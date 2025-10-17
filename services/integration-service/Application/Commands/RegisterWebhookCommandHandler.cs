using MediatR;
using SmartAlarm.IntegrationService.Infrastructure.Webhooks;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.IntegrationService.Application.Commands
{
    /// <summary>
    /// Handler para registro de webhook
    /// </summary>
    public class RegisterWebhookCommandHandler : IRequestHandler<RegisterWebhookCommand, RegisterWebhookResponse>
    {
        private readonly IWebhookManager _webhookManager;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<RegisterWebhookCommandHandler> _logger;

        public RegisterWebhookCommandHandler(
            IWebhookManager webhookManager,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<RegisterWebhookCommandHandler> logger)
        {
            _webhookManager = webhookManager;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<RegisterWebhookResponse> Handle(RegisterWebhookCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("RegisterWebhookCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("webhook.provider", request.Provider);
                activity?.SetTag("webhook.event_type", request.EventType);
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Registrando webhook para usuário {UserId} - Provider: {Provider}, EventType: {EventType} - CorrelationId: {CorrelationId}",
                    request.UserId, request.Provider, request.EventType, _correlationContext.CorrelationId);

                // Criar request para o webhook manager
                var webhookRequest = new WebhookRegistrationRequest(
                    UserId: request.UserId,
                    Provider: request.Provider,
                    EventType: request.EventType,
                    CallbackUrl: request.CallbackUrl,
                    Configuration: request.Configuration,
                    ExpirationTime: request.ExpirationTime,
                    SecretKey: null // Será gerado automaticamente
                );

                // Registrar webhook
                var webhook = await _webhookManager.RegisterWebhookAsync(webhookRequest, cancellationToken);

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "register_webhook", "success", "201");

                var response = new RegisterWebhookResponse(
                    WebhookId: webhook.Id,
                    UserId: webhook.UserId,
                    Provider: webhook.Provider,
                    EventType: webhook.EventType,
                    CallbackUrl: webhook.CallbackUrl,
                    Status: webhook.Status,
                    CreatedAt: webhook.CreatedAt,
                    ExpiresAt: webhook.ExpiresAt,
                    SecretKey: webhook.SecretKey
                );

                _logger.LogInformation("Webhook {WebhookId} registrado com sucesso para usuário {UserId} em {Duration}ms - CorrelationId: {CorrelationId}",
                    webhook.Id, request.UserId, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "register_webhook", "error", "500");
                _meter.IncrementErrorCount("command", "register_webhook", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro inesperado ao registrar webhook para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);

                throw;
            }
        }
    }
}
