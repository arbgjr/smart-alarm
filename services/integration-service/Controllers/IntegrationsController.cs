using Microsoft.AspNetCore.Mvc;
using MediatR;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
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
        /// Lista integrações disponíveis
        /// </summary>
        /// <returns>Lista de provedores de integração suportados</returns>
        [HttpGet("providers")]
        public async Task<IActionResult> GetAvailableProviders()
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

                // Simular validação com serviço externo
                var httpClient = _httpClientFactory.CreateClient("ExternalIntegrations");
                
                // TODO: Implementar comando real para criar integração
                // var integration = await _mediator.Send(new CreateIntegrationCommand(alarmId, request));
                
                var mockIntegration = new
                {
                    Id = Guid.NewGuid(),
                    AlarmId = alarmId,
                    Provider = request.Provider,
                    Configuration = request.Configuration,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    AuthRequired = true,
                    AuthUrl = $"https://auth.{request.Provider.ToLower()}.com/oauth/authorize?client_id=smartalarm"
                };

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_alarm_integration", "success", "201");
                
                _logger.LogInformation("Integração criada com sucesso para alarme {AlarmId} em {Duration}ms", 
                    alarmId, stopwatch.ElapsedMilliseconds);

                return Created($"/api/v1/integrations/{mockIntegration.Id}", mockIntegration);
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
        public async Task<IActionResult> SyncIntegration(Guid integrationId)
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
}
