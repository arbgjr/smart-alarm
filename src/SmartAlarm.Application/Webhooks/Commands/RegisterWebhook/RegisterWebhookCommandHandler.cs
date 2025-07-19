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

namespace SmartAlarm.Application.Webhooks.Commands.RegisterWebhook
{
    /// <summary>
    /// Handler para registro de webhook
    /// </summary>
    public class RegisterWebhookCommandHandler : IRequestHandler<RegisterWebhookCommand, RegisterWebhookResponse>
    {
        private readonly IWebhookRepository _webhookRepository;
        private readonly IValidator<RegisterWebhookCommand> _validator;
        private readonly ILogger<RegisterWebhookCommandHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public RegisterWebhookCommandHandler(
            IWebhookRepository webhookRepository,
            IValidator<RegisterWebhookCommand> validator,
            ILogger<RegisterWebhookCommandHandler> logger,
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

        public async Task<RegisterWebhookResponse> Handle(RegisterWebhookCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("RegisterWebhookCommand");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("webhook.url", request.Url);
                activity?.SetTag("webhook.events", string.Join(",", request.Events));
                activity?.SetTag("user.id", request.UserId.ToString());

                _logger.LogInformation(LogTemplates.CommandStarted, "RegisterWebhookCommand", 
                    _correlationContext.CorrelationId, request.UserId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation failed for RegisterWebhookCommand: {@Errors}", 
                        validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ValidationException(validationResult.Errors);
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

                var response = new RegisterWebhookResponse
                {
                    Id = createdWebhook.Id,
                    Url = createdWebhook.Url,
                    Events = createdWebhook.Events,
                    Secret = createdWebhook.Secret,
                    CreatedAt = createdWebhook.CreatedAt
                };

                // Métricas
                _meter.WebhookRegistered.Add(1, new KeyValuePair<string, object?>("user_id", request.UserId.ToString()));

                _logger.LogInformation(LogTemplates.CommandCompleted, "RegisterWebhookCommand", 
                    _correlationContext.CorrelationId, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing RegisterWebhookCommand for user {UserId}: {Message}", 
                    request.UserId, ex.Message);
                _meter.WebhookRegistrationErrors.Add(1, new KeyValuePair<string, object?>("user_id", request.UserId.ToString()));
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds, 
                    new KeyValuePair<string, object?>("command", "RegisterWebhook"), 
                    new KeyValuePair<string, object?>("user_id", request.UserId.ToString()));
            }
        }

        /// <summary>
        /// Gera um secret seguro para o webhook usando criptografia
        /// </summary>
        private static string GenerateSecureSecret()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32]; // 256 bits
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
