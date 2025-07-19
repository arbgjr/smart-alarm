using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Observability
{
    /// <summary>
    /// Implementação real de ITracingService usando OpenTelemetry.
    /// Gera traces reais distribuídos para produção.
    /// </summary>
    public class OpenTelemetryTracingService : ITracingService
    {
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly ILogger<OpenTelemetryTracingService> _logger;

        public OpenTelemetryTracingService(
            SmartAlarmActivitySource activitySource,
            ILogger<OpenTelemetryTracingService> logger)
        {
            _activitySource = activitySource;
            _logger = logger;
        }

        public Task TraceAsync(string operation, string message)
        {
            try
            {
                using var activity = _activitySource.StartActivity($"Trace.{operation}");
                
                if (activity != null)
                {
                    // Adicionar tags/contexto ao trace
                    activity.SetTag("operation", operation);
                    activity.SetTag("message", message);
                    activity.SetTag("source", "TracingService");
                    activity.SetTag("timestamp", DateTimeOffset.UtcNow.ToString("O"));
                    
                    // Log estruturado com correlation context
                    _logger.LogInformation(
                        "[OpenTelemetryTracing] Operation: {Operation}, Message: {Message}, TraceId: {TraceId}, SpanId: {SpanId}",
                        operation, message, activity.TraceId, activity.SpanId);
                }
                else
                {
                    // Fallback se não houver listeners OpenTelemetry configurados
                    _logger.LogInformation(
                        "[OpenTelemetryTracing] No active listeners - Operation: {Operation}, Message: {Message}",
                        operation, message);
                }
            }
            catch (System.Exception ex)
            {
                // Error handling gracioso - não quebrar o fluxo principal
                _logger.LogError(ex, 
                    "Failed to create OpenTelemetry trace for operation {Operation}: {ErrorMessage}", 
                    operation, ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
