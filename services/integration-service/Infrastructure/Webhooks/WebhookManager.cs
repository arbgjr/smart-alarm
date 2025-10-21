using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace SmartAlarm.IntegrationService.Infrastructure.Webhooks
{
    /// <summary>
    /// Implementação do gerenciador de webhooks
    /// </summary>
    public class WebhookManager : IWebhookManager
    {
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<WebhookManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        // Cache em memória para webhooks (em produção, usar Redis)
        private readonly ConcurrentDictionary<Guid, WebhookRegistration> _webhooks = new();
        private readonly ConcurrentDictionary<string, List<Guid>> _webhooksByProvider = new();
        private readonly ConcurrentDictionary<Guid, WebhookStatistics> _statistics = new();

        public WebhookManager(
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<WebhookManager> logger,
            IHttpClientFactory httpClientFactory)
        {
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc />
        public async Task<WebhookRegistration> RegisterWebhookAsync(
            WebhookRegistrationRequest request,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("WebhookManager.RegisterWebhook");
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

                // Gerar ID único para o webhook
                var webhookId = Guid.NewGuid();

                // Gerar chave secreta se não fornecida
                var secretKey = request.SecretKey ?? GenerateSecretKey();

                // Criar registro do webhook
                var webhook = new WebhookRegistration(
                    Id: webhookId,
                    UserId: request.UserId,
                    Provider: request.Provider,
                    EventType: request.EventType,
                    CallbackUrl: request.CallbackUrl,
                    Status: WebhookStatus.Active,
                    CreatedAt: DateTime.UtcNow,
                    ExpiresAt: request.ExpirationTime.HasValue ? DateTime.UtcNow.Add(request.ExpirationTime.Value) : null,
                    LastTriggeredAt: null,
                    TriggerCount: 0,
                    Configuration: request.Configuration ?? new Dictionary<string, string>(),
                    SecretKey: secretKey
                );

                // Armazenar webhook
                _webhooks[webhookId] = webhook;

                // Indexar por provider
                if (!_webhooksByProvider.ContainsKey(request.Provider))
                {
                    _webhooksByProvider[request.Provider] = new List<Guid>();
                }
                _webhooksByProvider[request.Provider].Add(webhookId);

                // Registrar webhook no provedor externo (simulado)
                await RegisterWithExternalProviderAsync(webhook, cancellationToken);

                stopwatch.Stop();

                // Métricas
                _meter.IncrementCounter("webhook_registered", 1,
                    new KeyValuePair<string, object?>("provider", request.Provider),
                    new KeyValuePair<string, object?>("status", "success"));

                _logger.LogInformation("Webhook {WebhookId} registrado com sucesso para usuário {UserId} - Provider: {Provider} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    webhookId, request.UserId, request.Provider, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return webhook;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.IncrementCounter("webhook_registered", 1,
                    new KeyValuePair<string, object?>("provider", request.Provider),
                    new KeyValuePair<string, object?>("status", "failed"));

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao registrar webhook para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UnregisterWebhookAsync(
            Guid webhookId,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("WebhookManager.UnregisterWebhook");

            try
            {
                activity?.SetTag("webhook.id", webhookId.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Removendo webhook {WebhookId} - CorrelationId: {CorrelationId}",
                    webhookId, _correlationContext.CorrelationId);

                if (!_webhooks.TryGetValue(webhookId, out var webhook))
                {
                    _logger.LogWarning("Webhook {WebhookId} não encontrado para remoção", webhookId);
                    return false;
                }

                // Remover do provedor externo (simulado)
                await UnregisterFromExternalProviderAsync(webhook, cancellationToken);

                // Remover do cache
                _webhooks.TryRemove(webhookId, out _);

                // Remover do índice por provider
                if (_webhooksByProvider.TryGetValue(webhook.Provider, out var providerWebhooks))
                {
                    providerWebhooks.Remove(webhookId);
                }

                _logger.LogInformation("Webhook {WebhookId} removido com sucesso", webhookId);
                return true;
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao remover webhook {WebhookId} - CorrelationId: {CorrelationId}",
                    webhookId, _correlationContext.CorrelationId);

                return false;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<WebhookRegistration>> GetActiveWebhooksAsync(
            Guid? userId = null,
            string? provider = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Listando webhooks ativos - UserId: {UserId}, Provider: {Provider}",
                    userId?.ToString() ?? "all", provider ?? "all");

                var webhooks = _webhooks.Values.Where(w => w.Status == WebhookStatus.Active);

                if (userId.HasValue)
                {
                    webhooks = webhooks.Where(w => w.UserId == userId.Value);
                }

                if (!string.IsNullOrEmpty(provider))
                {
                    webhooks = webhooks.Where(w => w.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
                }

                await Task.CompletedTask;
                return webhooks.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar webhooks ativos");
                return Array.Empty<WebhookRegistration>();
            }
        }

        /// <inheritdoc />
        public async Task<WebhookProcessingResult> ProcessIncomingWebhookAsync(
            string webhookId,
            string payload,
            Dictionary<string, string> headers,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("WebhookManager.ProcessIncomingWebhook");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("webhook.id", webhookId);
                activity?.SetTag("payload.size", payload.Length.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Processando webhook recebido {WebhookId} - Payload size: {PayloadSize} - CorrelationId: {CorrelationId}",
                    webhookId, payload.Length, _correlationContext.CorrelationId);

                if (!Guid.TryParse(webhookId, out var webhookGuid) || !_webhooks.TryGetValue(webhookGuid, out var webhook))
                {
                    _logger.LogWarning("Webhook {WebhookId} não encontrado", webhookId);
                    return new WebhookProcessingResult(
                        webhookGuid, false, "Webhook não encontrado", DateTime.UtcNow, stopwatch.Elapsed, Array.Empty<string>());
                }

                // Validar assinatura se configurada
                if (headers.TryGetValue("X-Signature", out var signature) && !string.IsNullOrEmpty(webhook.SecretKey))
                {
                    var isValid = await ValidateWebhookSignatureAsync(webhookId, payload, signature, cancellationToken);
                    if (!isValid)
                    {
                        _logger.LogWarning("Assinatura inválida para webhook {WebhookId}", webhookId);
                        return new WebhookProcessingResult(
                            webhookGuid, false, "Assinatura inválida", DateTime.UtcNow, stopwatch.Elapsed, Array.Empty<string>());
                    }
                }

                // Processar payload baseado no provider
                var actionsTriggered = await ProcessWebhookPayloadAsync(webhook, payload, cancellationToken);

                // Atualizar estatísticas do webhook
                var updatedWebhook = webhook with
                {
                    LastTriggeredAt = DateTime.UtcNow,
                    TriggerCount = webhook.TriggerCount + 1
                };
                _webhooks[webhookGuid] = updatedWebhook;

                stopwatch.Stop();

                // Métricas
                _meter.IncrementCounter("webhook_processed", 1,
                    new KeyValuePair<string, object?>("provider", webhook.Provider),
                    new KeyValuePair<string, object?>("event_type", webhook.EventType),
                    new KeyValuePair<string, object?>("status", "success"));

                _logger.LogInformation("Webhook {WebhookId} processado com sucesso - Ações: {ActionsCount} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    webhookId, actionsTriggered.Count(), stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return new WebhookProcessingResult(
                    webhookGuid, true, "Processado com sucesso", DateTime.UtcNow, stopwatch.Elapsed, actionsTriggered);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.IncrementCounter("webhook_processed", 1,
                    new KeyValuePair<string, object?>("provider", "unknown"),
                    new KeyValuePair<string, object?>("event_type", "unknown"),
                    new KeyValuePair<string, object?>("status", "failed"));

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao processar webhook {WebhookId} - CorrelationId: {CorrelationId}",
                    webhookId, _correlationContext.CorrelationId);

                return new WebhookProcessingResult(
                    Guid.TryParse(webhookId, out var guid) ? guid : Guid.Empty,
                    false, $"Erro no processamento: {ex.Message}", DateTime.UtcNow, stopwatch.Elapsed, Array.Empty<string>());
            }
        }

        /// <inheritdoc />
        public async Task<bool> ValidateWebhookSignatureAsync(
            string webhookId,
            string payload,
            string signature,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(webhookId, out var webhookGuid) || !_webhooks.TryGetValue(webhookGuid, out var webhook))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(webhook.SecretKey))
                {
                    return true; // Sem validação se não há chave secreta
                }

                // Calcular HMAC-SHA256
                var expectedSignature = CalculateHmacSha256(payload, webhook.SecretKey);

                await Task.CompletedTask;
                return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar assinatura do webhook {WebhookId}", webhookId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<WebhookStatistics> GetWebhookStatisticsAsync(
            Guid? userId = null,
            TimeSpan? timeWindow = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var webhooks = _webhooks.Values.AsEnumerable();

                if (userId.HasValue)
                {
                    webhooks = webhooks.Where(w => w.UserId == userId.Value);
                }

                var webhooksList = webhooks.ToList();
                var cutoffTime = timeWindow.HasValue ? DateTime.UtcNow.Subtract(timeWindow.Value) : DateTime.MinValue;

                var totalWebhooks = webhooksList.Count;
                var activeWebhooks = webhooksList.Count(w => w.Status == WebhookStatus.Active);
                var failedWebhooks = webhooksList.Count(w => w.Status == WebhookStatus.Failed);
                var totalTriggers = webhooksList.Sum(w => w.TriggerCount);

                var triggersByProvider = webhooksList
                    .GroupBy(w => w.Provider)
                    .ToDictionary(g => g.Key, g => g.Sum(w => w.TriggerCount));

                var triggersByEventType = webhooksList
                    .GroupBy(w => w.EventType)
                    .ToDictionary(g => g.Key, g => g.Sum(w => w.TriggerCount));

                var lastActivity = webhooksList
                    .Where(w => w.LastTriggeredAt.HasValue)
                    .Max(w => w.LastTriggeredAt) ?? DateTime.MinValue;

                await Task.CompletedTask;

                return new WebhookStatistics(
                    TotalWebhooks: totalWebhooks,
                    ActiveWebhooks: activeWebhooks,
                    FailedWebhooks: failedWebhooks,
                    TotalTriggers: totalTriggers,
                    AverageProcessingTime: 150.0, // Simulado - em produção seria calculado
                    LastActivity: lastActivity,
                    TriggersByProvider: triggersByProvider,
                    TriggersByEventType: triggersByEventType
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas de webhooks");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ReactivateWebhookAsync(
            Guid webhookId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Reativando webhook {WebhookId}", webhookId);

                if (!_webhooks.TryGetValue(webhookId, out var webhook))
                {
                    return false;
                }

                var reactivatedWebhook = webhook with { Status = WebhookStatus.Active };
                _webhooks[webhookId] = reactivatedWebhook;

                // Reregistrar no provedor externo se necessário
                await RegisterWithExternalProviderAsync(reactivatedWebhook, cancellationToken);

                _logger.LogInformation("Webhook {WebhookId} reativado com sucesso", webhookId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reativar webhook {WebhookId}", webhookId);
                return false;
            }
        }

        #region Métodos Privados

        private static string GenerateSecretKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string CalculateHmacSha256(string payload, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private async Task RegisterWithExternalProviderAsync(WebhookRegistration webhook, CancellationToken cancellationToken)
        {
            try
            {
                // Simular registro no provedor externo
                _logger.LogDebug("Registrando webhook {WebhookId} no provedor {Provider}", webhook.Id, webhook.Provider);

                var httpClient = _httpClientFactory.CreateClient("ExternalIntegrations");

                // Em uma implementação real, aqui faria a chamada para o provedor específico
                await Task.Delay(100, cancellationToken); // Simular latência de rede

                _logger.LogDebug("Webhook {WebhookId} registrado no provedor {Provider}", webhook.Id, webhook.Provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar webhook {WebhookId} no provedor {Provider}", webhook.Id, webhook.Provider);
                throw;
            }
        }

        private async Task UnregisterFromExternalProviderAsync(WebhookRegistration webhook, CancellationToken cancellationToken)
        {
            try
            {
                // Simular remoção no provedor externo
                _logger.LogDebug("Removendo webhook {WebhookId} do provedor {Provider}", webhook.Id, webhook.Provider);

                await Task.Delay(50, cancellationToken); // Simular latência de rede

                _logger.LogDebug("Webhook {WebhookId} removido do provedor {Provider}", webhook.Id, webhook.Provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover webhook {WebhookId} do provedor {Provider}", webhook.Id, webhook.Provider);
                // Não relançar exceção para não bloquear a remoção local
            }
        }

        private async Task<IEnumerable<string>> ProcessWebhookPayloadAsync(
            WebhookRegistration webhook,
            string payload,
            CancellationToken cancellationToken)
        {
            var actionsTriggered = new List<string>();

            try
            {
                // Processar baseado no provider e tipo de evento
                switch (webhook.Provider.ToLowerInvariant())
                {
                    case "google":
                        actionsTriggered.AddRange(await ProcessGoogleWebhookAsync(webhook, payload, cancellationToken));
                        break;
                    case "microsoft":
                        actionsTriggered.AddRange(await ProcessMicrosoftWebhookAsync(webhook, payload, cancellationToken));
                        break;
                    case "apple":
                        actionsTriggered.AddRange(await ProcessAppleWebhookAsync(webhook, payload, cancellationToken));
                        break;
                    default:
                        actionsTriggered.Add("generic_processing");
                        break;
                }

                _logger.LogDebug("Webhook {WebhookId} processado - Ações: {Actions}",
                    webhook.Id, string.Join(", ", actionsTriggered));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar payload do webhook {WebhookId}", webhook.Id);
                actionsTriggered.Add("error_processing");
            }

            return actionsTriggered;
        }

        private async Task<IEnumerable<string>> ProcessGoogleWebhookAsync(
            WebhookRegistration webhook,
            string payload,
            CancellationToken cancellationToken)
        {
            var actions = new List<string>();

            try
            {
                // Simular processamento específico do Google
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);

                if (webhook.EventType == "calendar.event.created")
                {
                    actions.Add("sync_calendar_event");
                    actions.Add("update_alarm_schedule");
                }
                else if (webhook.EventType == "calendar.event.updated")
                {
                    actions.Add("update_calendar_event");
                    actions.Add("adjust_alarm_timing");
                }

                await Task.Delay(50, cancellationToken); // Simular processamento
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar webhook do Google");
                actions.Add("google_processing_error");
            }

            return actions;
        }

        private async Task<IEnumerable<string>> ProcessMicrosoftWebhookAsync(
            WebhookRegistration webhook,
            string payload,
            CancellationToken cancellationToken)
        {
            var actions = new List<string>();

            try
            {
                // Simular processamento específico do Microsoft
                if (webhook.EventType == "outlook.event.created")
                {
                    actions.Add("sync_outlook_event");
                    actions.Add("create_smart_alarm");
                }

                await Task.Delay(75, cancellationToken); // Simular processamento
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar webhook do Microsoft");
                actions.Add("microsoft_processing_error");
            }

            return actions;
        }

        private async Task<IEnumerable<string>> ProcessAppleWebhookAsync(
            WebhookRegistration webhook,
            string payload,
            CancellationToken cancellationToken)
        {
            var actions = new List<string>();

            try
            {
                // Simular processamento específico do Apple
                if (webhook.EventType == "calendar.event.reminder")
                {
                    actions.Add("sync_apple_reminder");
                    actions.Add("optimize_alarm_timing");
                }

                await Task.Delay(60, cancellationToken); // Simular processamento
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar webhook do Apple");
                actions.Add("apple_processing_error");
            }

            return actions;
        }

        #endregion
    }
}
