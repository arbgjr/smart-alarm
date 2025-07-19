using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Observability.HealthChecks
{
    /// <summary>
    /// Health check básico para o Smart Alarm
    /// </summary>
    public class SmartAlarmHealthCheck : IHealthCheck
    {
        private readonly ILogger<SmartAlarmHealthCheck> _logger;

        public SmartAlarmHealthCheck(ILogger<SmartAlarmHealthCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Executando health check básico do Smart Alarm");

                // Verificações básicas do sistema
                var healthData = new Dictionary<string, object>
                {
                    ["MachineName"] = Environment.MachineName,
                    ["ProcessorCount"] = Environment.ProcessorCount,
                    ["WorkingSet"] = Environment.WorkingSet,
                    ["Timestamp"] = DateTime.UtcNow,
                    ["Status"] = "Healthy"
                };

                _logger.LogInformation("Health check básico concluído com sucesso: {@HealthData}", healthData);

                return HealthCheckResult.Healthy("Smart Alarm está funcionando normalmente", healthData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no health check básico do Smart Alarm");
                return HealthCheckResult.Unhealthy("Falha no health check básico", ex);
            }
        }
    }
}
