using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SmartAlarm.Observability
{
    /// <summary>
    /// Middleware para logging estruturado, tracing e métricas.
    /// </summary>
    public class ObservabilityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ObservabilityMiddleware> _logger;
        private static readonly ActivitySource ActivitySource = new ActivitySource("SmartAlarm.Observability");

        public ObservabilityMiddleware(RequestDelegate next, ILogger<ObservabilityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using var activity = ActivitySource.StartActivity($"HTTP {context.Request.Method} {context.Request.Path}");
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Request started: {Method} {Path}", context.Request.Method, context.Request.Path);
                await _next(context);
                _logger.LogInformation("Request finished: {Method} {Path} - {StatusCode} in {Elapsed}ms", context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed: {Method} {Path}", context.Request.Method, context.Request.Path);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                // Métricas customizadas podem ser adicionadas aqui (ex: Prometheus)
            }
        }
    }
}
