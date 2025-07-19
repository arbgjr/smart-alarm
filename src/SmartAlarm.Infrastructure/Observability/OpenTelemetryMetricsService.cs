using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartAlarm.Observability.Metrics;

namespace SmartAlarm.Infrastructure.Observability
{
    /// <summary>
    /// Implementação real de IMetricsService usando OpenTelemetry.
    /// Coleta métricas reais para monitoramento em produção.
    /// </summary>
    public class OpenTelemetryMetricsService : IMetricsService
    {
        private readonly SmartAlarmMeter _meter;
        private readonly ILogger<OpenTelemetryMetricsService> _logger;

        public OpenTelemetryMetricsService(
            SmartAlarmMeter meter,
            ILogger<OpenTelemetryMetricsService> logger)
        {
            _meter = meter;
            _logger = logger;
        }

        public Task IncrementAsync(string metricName)
        {
            try
            {
                // Mapear nomes de métricas para contadores específicos do SmartAlarmMeter
                switch (metricName.ToLowerInvariant())
                {
                    case "request":
                    case "requests":
                        _meter.IncrementRequestCount("POST", "/api/custom");
                        break;
                    
                    case "error":
                    case "errors":
                        _meter.IncrementErrorCount("POST", "/api/custom", "generic_error");
                        break;
                    
                    case "alarm":
                    case "alarms":
                        _meter.IncrementAlarmCount("custom", "system");
                        break;
                    
                    case "alarm_triggered":
                    case "alarmtriggered":
                        _meter.IncrementAlarmTriggered("custom", "system", "success");
                        break;
                    
                    case "user_registration":
                    case "userregistration":
                        _meter.IncrementUserRegistration("api");
                        break;
                    
                    case "authentication":
                    case "auth":
                        _meter.IncrementAuthenticationAttempt("success", "api");
                        break;
                    
                    case "monitoring":
                        _meter.IncrementMonitoringRequest("/api/health");
                        break;
                    
                    default:
                        // Para métricas customizadas, usar contador de request genérico
                        _meter.IncrementRequestCount("CUSTOM", $"/api/{metricName}");
                        break;
                }

                _logger.LogDebug(
                    "[OpenTelemetryMetrics] Incremented metric: {MetricName}", 
                    metricName);
            }
            catch (System.Exception ex)
            {
                // Error handling gracioso - não quebrar o fluxo principal
                _logger.LogError(ex, 
                    "Failed to increment metric {MetricName}: {ErrorMessage}", 
                    metricName, ex.Message);
            }

            return Task.CompletedTask;
        }

        public Task RecordAsync(string metricName, double value)
        {
            try
            {
                // Mapear nomes de métricas para histogramas específicos do SmartAlarmMeter
                switch (metricName.ToLowerInvariant())
                {
                    case "request_duration":
                    case "requestduration":
                        _meter.RecordRequestDuration(value, "POST", "/api/custom", "200");
                        break;
                    
                    case "alarm_creation_duration":
                    case "alarmcreationduration":
                        _meter.RecordAlarmCreationDuration(value, "custom", true);
                        break;
                    
                    case "database_query_duration":
                    case "databasequeryduration":
                        _meter.RecordDatabaseQueryDuration(value, "SELECT", "custom_table");
                        break;
                    
                    case "external_service_duration":
                    case "externalserviceduration":
                        _meter.RecordExternalServiceCallDuration(value, "custom_service", "api_call", true);
                        break;
                    
                    default:
                        // Para métricas customizadas, usar histograma de request genérico
                        _meter.RecordRequestDuration(value, "CUSTOM", $"/api/{metricName}", "200");
                        break;
                }

                _logger.LogDebug(
                    "[OpenTelemetryMetrics] Recorded metric: {MetricName} = {Value}", 
                    metricName, value);
            }
            catch (System.Exception ex)
            {
                // Error handling gracioso - não quebrar o fluxo principal
                _logger.LogError(ex, 
                    "Failed to record metric {MetricName} with value {Value}: {ErrorMessage}", 
                    metricName, value, ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
