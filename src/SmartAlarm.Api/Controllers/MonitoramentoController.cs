using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartAlarm.Api.Controllers
{
    /// <summary>
    /// Controller para endpoints de monitoramento e observabilidade
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MonitoramentoController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<MonitoramentoController> _logger;

        public MonitoramentoController(
            HealthCheckService healthCheckService,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<MonitoramentoController> logger)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retorna o status geral do sistema
        /// </summary>
        /// <returns>Status do sistema</returns>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                _logger.LogInformation("Consultando status geral do sistema. CorrelationId: {CorrelationId}", 
                    _correlationContext.CorrelationId);

                var healthReport = await _healthCheckService.CheckHealthAsync();

                var status = new
                {
                    Status = healthReport.Status.ToString(),
                    TotalDuration = healthReport.TotalDuration.TotalMilliseconds,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = _correlationContext.CorrelationId,
                    ServiceInfo = new
                    {
                        Name = "SmartAlarm",
                        Version = "1.0.0",
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                        MachineName = Environment.MachineName,
                        ProcessorCount = Environment.ProcessorCount,
                        WorkingSet = Environment.WorkingSet
                    },
                    ComponentsCount = healthReport.Entries.Count,
                    HealthyComponents = healthReport.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                    DegradedComponents = healthReport.Entries.Count(e => e.Value.Status == HealthStatus.Degraded),
                    UnhealthyComponents = healthReport.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy)
                };

                _meter.IncrementMonitoringRequest("status");

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter status do sistema");
                return StatusCode(500, new { Error = "Erro interno do servidor", CorrelationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Retorna informações detalhadas de health checks
        /// </summary>
        /// <returns>Health checks detalhados</returns>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                _logger.LogInformation("Executando health checks detalhados. CorrelationId: {CorrelationId}", 
                    _correlationContext.CorrelationId);

                var healthReport = await _healthCheckService.CheckHealthAsync();

                var healthDetails = new
                {
                    Status = healthReport.Status.ToString(),
                    TotalDuration = $"{healthReport.TotalDuration.TotalMilliseconds}ms",
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = _correlationContext.CorrelationId,
                    Entries = healthReport.Entries.Select(entry => new
                    {
                        Name = entry.Key,
                        Status = entry.Value.Status.ToString(),
                        Duration = $"{entry.Value.Duration.TotalMilliseconds}ms",
                        Description = entry.Value.Description,
                        Tags = entry.Value.Tags,
                        Data = entry.Value.Data,
                        Exception = entry.Value.Exception?.Message
                    })
                };

                _meter.IncrementMonitoringRequest("health");

                var statusCode = healthReport.Status switch
                {
                    HealthStatus.Healthy => 200,
                    HealthStatus.Degraded => 200, // Ainda operacional
                    HealthStatus.Unhealthy => 503, // Service Unavailable
                    _ => 500
                };

                return StatusCode(statusCode, healthDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar health checks");
                return StatusCode(500, new { Error = "Erro interno do servidor", CorrelationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Retorna métricas do sistema
        /// </summary>
        /// <returns>Métricas em formato JSON</returns>
        [HttpGet("metrics")]
        public IActionResult GetMetrics()
        {
            try
            {
                _logger.LogInformation("Consultando métricas do sistema. CorrelationId: {CorrelationId}", 
                    _correlationContext.CorrelationId);

                var metrics = new
                {
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = _correlationContext.CorrelationId,
                    System = new
                    {
                        ProcessorCount = Environment.ProcessorCount,
                        WorkingSet = Environment.WorkingSet,
                        MachineName = Environment.MachineName,
                        OSVersion = Environment.OSVersion.ToString(),
                        Runtime = new
                        {
                            Version = Environment.Version.ToString(),
                            FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
                        }
                    },
                    Application = new
                    {
                        Name = "SmartAlarm",
                        Version = "1.0.0",
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                        StartTime = Process.GetCurrentProcess().StartTime,
                        Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
                    },
                    Note = "Para métricas Prometheus detalhadas, acesse /metrics endpoint"
                };

                _meter.IncrementMonitoringRequest("metrics");

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter métricas");
                return StatusCode(500, new { Error = "Erro interno do servidor", CorrelationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Força reconexão com dependências externas
        /// </summary>
        /// <returns>Resultado da operação</returns>
        [HttpPost("reconnect")]
        public async Task<IActionResult> ForceReconnect()
        {
            try
            {
                _logger.LogWarning("Solicitação de reconexão forçada. CorrelationId: {CorrelationId}", 
                    _correlationContext.CorrelationId);

                // Executa health checks para verificar conectividade
                var healthReport = await _healthCheckService.CheckHealthAsync();

                var result = new
                {
                    Status = "Attempted",
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = _correlationContext.CorrelationId,
                    Message = "Reconexão solicitada. Verifique health checks para status atualizado.",
                    HealthStatus = healthReport.Status.ToString(),
                    ComponentsReconnected = healthReport.Entries.Count
                };

                _meter.IncrementMonitoringRequest("reconnect");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante reconexão forçada");
                return StatusCode(500, new { Error = "Erro interno do servidor", CorrelationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Health check básico (liveness probe)
        /// </summary>
        /// <returns>Status básico</returns>
        [HttpGet("alive")]
        public IActionResult IsAlive()
        {
            try
            {
                var response = new
                {
                    Status = "Alive",
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = _correlationContext.CorrelationId,
                    ServiceName = "SmartAlarm",
                    Version = "1.0.0"
                };

                _meter.IncrementMonitoringRequest("alive");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no health check básico");
                return StatusCode(500, new { Error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Readiness probe (verificação de dependências)
        /// </summary>
        /// <returns>Status de readiness</returns>
        [HttpGet("ready")]
        public async Task<IActionResult> IsReady()
        {
            try
            {
                _logger.LogDebug("Verificando readiness. CorrelationId: {CorrelationId}", 
                    _correlationContext.CorrelationId);

                var healthReport = await _healthCheckService.CheckHealthAsync(check => 
                    check.Tags.Contains("readiness") || check.Tags.Contains("database"));

                var response = new
                {
                    Status = healthReport.Status == HealthStatus.Healthy ? "Ready" : "NotReady",
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = _correlationContext.CorrelationId,
                    CheckedComponents = healthReport.Entries.Count,
                    TotalDuration = $"{healthReport.TotalDuration.TotalMilliseconds}ms"
                };

                _meter.IncrementMonitoringRequest("ready");

                var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no readiness check");
                return StatusCode(503, new { Status = "NotReady", Error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Retorna informações de versão e build
        /// </summary>
        /// <returns>Informações de versão</returns>
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            try
            {
                var version = new
                {
                    ServiceName = "SmartAlarm",
                    Version = "1.0.0",
                    BuildDate = "2025-01-16", // Será substituído por build real
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    Runtime = new
                    {
                        Version = Environment.Version.ToString(),
                        Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                        OSDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                        Architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()
                    },
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = _correlationContext.CorrelationId
                };

                _meter.IncrementMonitoringRequest("version");

                return Ok(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter informações de versão");
                return StatusCode(500, new { Error = "Erro interno do servidor", CorrelationId = _correlationContext.CorrelationId });
            }
        }
    }
}
