using Microsoft.AspNetCore.Mvc;
using MediatR;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.IntegrationService.Application.Commands;
using SmartAlarm.IntegrationService.Application.Queries;
using System.Diagnostics;

namespace SmartAlarm.IntegrationService.Controllers
{
    /// <summary>
    /// Controller para integrações externas (calendários, notificações, etc.)
    /// </summary>
    [ApiController]
    [Route("api/v1/integrations")]
    [Produces("application/json")]
    public class IntegrationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<IntegrationsController> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public IntegrationsController(
            IMediator mediator,
            ILogger<IntegrationsController> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            IHttpClientFactory httpClientFactory)
        {
            _mediator = mediator;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Sincroniza calendário externo do usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="provider">Provedor do calendário (google, outlook, apple, caldav)</param>
        /// <param name="accessToken">Token de acesso para o calendário</param>
        /// <param name="startDate">Data de início da sincronização (opcional)</param>
        /// <param name="endDate">Data de fim da sincronização (opcional)</param>
        /// <param name="forceFullSync">Forçar sincronização completa</param>
        /// <returns>Resultado da sincronização</returns>
        [HttpPost("calendar/sync")]
        public async Task<IActionResult> SyncExternalCalendar(
            [FromQuery] Guid userId,
            [FromQuery] string provider,
            [FromHeader(Name = "Authorization")] string accessToken,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool forceFullSync = false)
        {
            using var activity = _activitySource.StartActivity("IntegrationsController.SyncExternalCalendar");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", userId.ToString());
                activity?.SetTag("provider", provider.ToLowerInvariant());
                activity?.SetTag("operation", "sync_external_calendar");
                activity?.SetTag("force_full_sync", forceFullSync.ToString());

                _logger.LogInformation("Iniciando sincronização de calendário {Provider} para usuário {UserId} - CorrelationId: {CorrelationId}",
                    provider, userId, _correlationContext.CorrelationId);

                // Extrair token do header Authorization (formato: "Bearer <token>")
                var token = accessToken?.Replace("Bearer ", "").Trim();
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { error = "Token de acesso é obrigatório no header Authorization" });
                }

                var command = new SyncExternalCalendarCommand(userId, provider, token, startDate, endDate, forceFullSync);
                var result = await _mediator.Send(command);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "sync_external_calendar", "success", "200");

                _logger.LogInformation("Sincronização de calendário concluída para usuário {UserId}: {EventsProcessed} eventos processados em {Duration}ms",
                    userId, result.EventsProcessed, stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "sync_external_calendar", "error", "500");
                _meter.IncrementErrorCount("controller", "sync_external_calendar", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao sincronizar calendário {Provider} para usuário {UserId} - CorrelationId: {CorrelationId}",
                    provider, userId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Lista integrações ativas do usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="providerFilter">Filtro por provedor específico (opcional)</param>
        /// <param name="includeInactive">Incluir integrações inativas</param>
        /// <param name="includeStatistics">Incluir estatísticas das integrações</param>
        /// <returns>Lista de integrações do usuário</returns>
        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetUserIntegrations(
            Guid userId,
            [FromQuery] string? providerFilter = null,
            [FromQuery] bool includeInactive = false,
            [FromQuery] bool includeStatistics = true)
        {
            using var activity = _activitySource.StartActivity("IntegrationsController.GetUserIntegrations");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", userId.ToString());
                activity?.SetTag("provider_filter", providerFilter ?? "none");
                activity?.SetTag("operation", "get_user_integrations");

                _logger.LogInformation("Buscando integrações para usuário {UserId} - CorrelationId: {CorrelationId}",
                    userId, _correlationContext.CorrelationId);

                var query = new GetUserIntegrationsQuery(userId, providerFilter, includeInactive, includeStatistics);
                var result = await _mediator.Send(query);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_user_integrations", "success", "200");

                _logger.LogInformation("Integrações recuperadas para usuário {UserId} em {Duration}ms",
                    userId, stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_user_integrations", "error", "500");
                _meter.IncrementErrorCount("controller", "get_user_integrations", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao buscar integrações para usuário {UserId} - CorrelationId: {CorrelationId}",
                    userId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Lista integrações disponíveis
        /// </summary>
        /// <returns>Lista de provedores de integração suportados</returns>
        [HttpGet("providers")]
        public IActionResult GetAvailableProviders()
        {
            using var activity = _activitySource.StartActivity("IntegrationsController.GetAvailableProviders");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("operation", "get_integration_providers");

                _logger.LogInformation("Listando provedores de integração disponíveis - CorrelationId: {CorrelationId}",
                    _correlationContext.CorrelationId);

                var providers = new
                {
                    CalendarProviders = new[]
                    {
                        new { Name = "Google Calendar", Enabled = true, RequiresAuth = true },
                        new { Name = "Microsoft Outlook", Enabled = true, RequiresAuth = true },
                        new { Name = "Apple Calendar", Enabled = false, RequiresAuth = true }
                    },
                    NotificationProviders = new[]
                    {
                        new { Name = "Slack", Enabled = true, RequiresAuth = true },
                        new { Name = "Microsoft Teams", Enabled = true, RequiresAuth = true },
                        new { Name = "WhatsApp Business", Enabled = false, RequiresAuth = true }
                    },
                    Total = 6,
                    GeneratedAt = DateTime.UtcNow
                };

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_integration_providers", "success", "200");

                _logger.LogInformation("Provedores de integração listados com sucesso em {Duration}ms",
                    stopwatch.ElapsedMilliseconds);

                return Ok(providers);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_integration_providers", "error", "500");
                _meter.IncrementErrorCount("controller", "integration_providers", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao listar provedores de integração - CorrelationId: {CorrelationId}",
                    _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Cria uma nova integração para um alarme específico
        /// </summary>
        /// <param name="alarmId">ID do alarme</param>
        /// <param name="request">Dados da integração</param>
        /// <returns>Detalhes da integração criada</returns>
        [HttpPost("alarm/{alarmId:guid}")]
        public async Task<IActionResult> CreateAlarmIntegration(Guid alarmId, [FromBody] CreateIntegrationRequest request)
        {
            using var activity = _activitySource.StartActivity("IntegrationsController.CreateAlarmIntegration");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("alarm.id", alarmId.ToString());
                activity?.SetTag("integration.provider", request.Provider);
                activity?.SetTag("operation", "create_alarm_integration");

                _logger.LogInformation("Criando integração para alarme {AlarmId} com provedor {Provider} - CorrelationId: {CorrelationId}",
                    alarmId, request.Provider, _correlationContext.CorrelationId);

                // Implementação real do comando para criar integração
                var command = new CreateIntegrationCommand(
                    alarmId,
                    request.Provider,
                    request.Configuration,
                    request.EnableNotifications,
                    request.Features);

                var integration = await _mediator.Send(command);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_alarm_integration", "success", "201");

                _logger.LogInformation("Integração criada com sucesso para alarme {AlarmId} em {Duration}ms",
                    alarmId, stopwatch.ElapsedMilliseconds);

                return Created($"/api/v1/integrations/{integration.Id}", integration);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_alarm_integration", "error", "500");
                _meter.IncrementErrorCount("controller", "alarm_integration", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao criar integração para alarme {AlarmId} - CorrelationId: {CorrelationId}",
                    alarmId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Sincroniza integração com serviço externo
        /// </summary>
        /// <param name="integrationId">ID da integração</param>
        /// <returns>Status da sincronização</returns>
        [HttpPost("{integrationId:guid}/sync")]
        public IActionResult SyncIntegration(Guid integrationId)
        {
            using var activity = _activitySource.StartActivity("IntegrationsController.SyncIntegration");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("integration.id", integrationId.ToString());
                activity?.SetTag("operation", "sync_integration");

                _logger.LogInformation("Iniciando sincronização da integração {IntegrationId} - CorrelationId: {CorrelationId}",
                    integrationId, _correlationContext.CorrelationId);

                // Simular comunicação com AI Service para otimizar sincronização
                var httpClient = _httpClientFactory.CreateClient("ExternalIntegrations");

                // Exemplo de comunicação inter-serviços
                // var aiResponse = await httpClient.GetAsync($"http://localhost:5001/api/v1/ai/recommendations/{userId}");

                var syncResult = new
                {
                    IntegrationId = integrationId,
                    Status = "Success",
                    ItemsSynced = 5,
                    LastSync = DateTime.UtcNow,
                    NextSync = DateTime.UtcNow.AddHours(1),
                    Conflicts = new string[0],
                    Duration = stopwatch.ElapsedMilliseconds
                };

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "sync_integration", "success", "200");

                _logger.LogInformation("Sincronização da integração {IntegrationId} concluída em {Duration}ms",
                    integrationId, stopwatch.ElapsedMilliseconds);

                return Ok(syncResult);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "sync_integration", "error", "500");
                _meter.IncrementErrorCount("controller", "integration_sync", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro na sincronização da integração {IntegrationId} - CorrelationId: {CorrelationId}",
                    integrationId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Registra um webhook para receber notificações de eventos
        /// </summary>
        /// <param name="request">Dados do webhook</param>
        /// <returns>Detalhes do webhook registrado</returns>
        [HttpPost("webhooks")]
        public async Task<IActionResult> RegisterWebhook([FromBody] RegisterWebhookRequest request)
        {
            using var activity = _activitySource.StartActivity("IntegrationsController.RegisterWebhook");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("webhook.provider", request.Provider);
                activity?.SetTag("webhook.event_type", request.EventType);
                activity?.SetTag("operation", "register_webhook");

                _logger.LogInformation("Registrando webhook para usuário {UserId} - Provider: {Provider}, EventType: {EventType} - CorrelationId: {CorrelationId}",
                    request.UserId, request.Provider, request.EventType, _correlationContext.CorrelationId);

                var command = new RegisterWebhookCommand(
                    request.UserId,
                    request.Provider,
                    request.EventType,
                    request.CallbackUrl,
                    request.Configuration,
                    request.ExpirationHours.HasValue ? TimeSpan.FromHours(request.ExpirationHours.Value) : null);

                var result = await _mediator.Send(command);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "register_webhook", "success", "201");

                _logger.LogInformation("Webhook registrado com sucesso para usuário {UserId} - WebhookId: {WebhookId} em {Duration}ms",
                    request.UserId, result.WebhookId, stopwatch.ElapsedMilliseconds);

                return Created($"/api/v1/integrations/webhooks/{result.WebhookId}", result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "register_webhook", "error", "500");
                _meter.IncrementErrorCount("controller", "register_webhook", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao registrar webhook para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Processa webhook recebido de provedor externo
        /// </summary>
        /// <param name="webhookId">ID do webhook</param>
        /// <param name="payload">Payload do webhook</param>
        /// <returns>Resultado do processamento</returns>
        [HttpPost("webhooks/{webhookId}/process")]
        public async Task<IActionResult> ProcessWebhook(string webhookId, [FromBody] object payload)
        {
            using var activity = _activitySource.StartActivity("IntegrationsController.ProcessWebhook");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("webhook.id", webhookId);
                activity?.SetTag("operation", "process_webhook");

                _logger.LogInformation("Processando webhook {WebhookId} - CorrelationId: {CorrelationId}",
                    webhookId, _correlationContext.CorrelationId);

                // Extrair headers da requisição
                var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
                var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

                var command = new ProcessWebhookCommand(webhookId, payloadJson, headers);
                var result = await _mediator.Send(command);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "process_webhook", "success", "200");

                _logger.LogInformation("Webhook {WebhookId} processado - Success: {Success} em {Duration}ms",
                    webhookId, result.Success, stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "process_webhook", "error", "500");
                _meter.IncrementErrorCount("controller", "process_webhook", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao processar webhook {WebhookId} - CorrelationId: {CorrelationId}",
                    webhookId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Obtém estatísticas de rate limiting
        /// </summary>
        /// <param name="provider">Provedor específico (opcional)</param>
        /// <returns>Estatísticas de rate limiting</returns>
        [HttpGet("rate-limiting/statistics")]
        public async Task<IActionResult> GetRateLimitingStatistics([FromQuery] string? provider = null)
        {
            using var activity = _activitySource.StartActivity("IntegrationsController.GetRateLimitingStatistics");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("provider", provider ?? "all");
                activity?.SetTag("operation", "get_rate_limiting_statistics");

                _logger.LogInformation("Obtendo estatísticas de rate limiting - Provider: {Provider} - CorrelationId: {CorrelationId}",
                    provider ?? "all", _correlationContext.CorrelationId);

                var query = new GetRateLimitingStatisticsQuery(provider);
                var result = await _mediator.Send(query);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_rate_limiting_statistics", "success", "200");

                _logger.LogInformation("Estatísticas de rate limiting obtidas em {Duration}ms",
                    stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_rate_limiting_statistics", "error", "500");
                _meter.IncrementErrorCount("controller", "rate_limiting_statistics", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao obter estatísticas de rate limiting - CorrelationId: {CorrelationId}",
                    _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }
    }

    /// <summary>
    /// Modelo para criação de integração
    /// </summary>
    public class CreateIntegrationRequest
    {
        public string Provider { get; set; } = string.Empty;
        public Dictionary<string, string> Configuration { get; set; } = new();
        public bool EnableNotifications { get; set; } = true;
        public string[] Features { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Modelo para registro de webhook
    /// </summary>
    public class RegisterWebhookRequest
    {
        public Guid UserId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public Dictionary<string, string>? Configuration { get; set; }
        public int? ExpirationHours { get; set; }
    }
}
