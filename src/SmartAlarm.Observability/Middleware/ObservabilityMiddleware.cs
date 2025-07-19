using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SmartAlarm.Observability.Middleware
{
    /// <summary>
    /// Middleware para configurar observabilidade em requisições HTTP
    /// </summary>
    public class ObservabilityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ObservabilityMiddleware> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly SmartAlarmActivitySource _activitySource;

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="next">Próximo middleware</param>
        /// <param name="logger">Logger</param>
        /// <param name="meter">Meter para métricas</param>
        /// <param name="activitySource">Activity source para tracing</param>
        public ObservabilityMiddleware(
            RequestDelegate next,
            ILogger<ObservabilityMiddleware> logger,
            SmartAlarmMeter meter,
            SmartAlarmActivitySource activitySource)
        {
            _next = next;
            _logger = logger;
            _meter = meter;
            _activitySource = activitySource;
        }

        /// <summary>
        /// Executa o middleware
        /// </summary>
        /// <param name="context">Contexto HTTP</param>
        /// <param name="correlationContext">Contexto de correlação</param>
        /// <returns>Task</returns>
        public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
        {
            var stopwatch = Stopwatch.StartNew();
            var path = context.Request.Path.Value ?? "unknown";
            var method = context.Request.Method;

            // Configurar contexto de correlação
            ConfigureCorrelationContext(context, correlationContext);

            // Configurar propriedades de log
            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationContext.CorrelationId,
                ["RequestPath"] = path,
                ["RequestMethod"] = method,
                ["UserId"] = correlationContext.UserId ?? "anonymous",
                ["SessionId"] = correlationContext.SessionId ?? "unknown"
            });

            // Criar activity para tracing
            using var activity = _activitySource.StartActivity("http_request");
            activity?.SetTag("http.method", method);
            activity?.SetTag("http.path", path);
            activity?.SetTag("correlation.id", correlationContext.CorrelationId);

            try
            {
                _logger.LogInformation("Iniciando requisição {Method} {Path}", method, path);

                // Incrementar contador de requisições
                _meter.IncrementRequestCount(method, path);

                await _next(context);

                stopwatch.Stop();

                // Registrar métricas de duração
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, method, path, context.Response.StatusCode.ToString());

                // Configurar tags da activity
                activity?.SetTag("http.status_code", context.Response.StatusCode);
                activity?.SetTag("http.response.size", context.Response.ContentLength);

                _logger.LogInformation(
                    "Requisição concluída {Method} {Path} - Status: {StatusCode} - Duração: {Duration}ms",
                    method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Registrar erro na activity
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);

                // Incrementar contador de erros
                _meter.IncrementErrorCount(method, path, ex.GetType().Name);

                _logger.LogError(ex,
                    "Erro na requisição {Method} {Path} - Duração: {Duration}ms",
                    method, path, stopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        /// <summary>
        /// Configura o contexto de correlação baseado na requisição HTTP
        /// </summary>
        /// <param name="context">Contexto HTTP</param>
        /// <param name="correlationContext">Contexto de correlação</param>
        private static void ConfigureCorrelationContext(HttpContext context, ICorrelationContext correlationContext)
        {
            // Tentar obter correlation ID do header
            if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationIdHeader))
            {
                correlationContext.AddContextProperty("OriginalCorrelationId", correlationIdHeader.ToString());
            }

            // Tentar obter session ID do header ou cookie
            if (context.Request.Headers.TryGetValue("X-Session-ID", out var sessionIdHeader))
            {
                correlationContext.SessionId = sessionIdHeader.ToString();
            }

            // Tentar obter user ID do header (se autenticado)
            if (context.Request.Headers.TryGetValue("X-User-ID", out var userIdHeader))
            {
                correlationContext.UserId = userIdHeader.ToString();
            }
            else if (context.User?.Identity?.IsAuthenticated == true)
            {
                correlationContext.UserId = context.User.Identity.Name;
            }

            // Adicionar correlation ID na resposta
            context.Response.Headers["X-Correlation-ID"] = correlationContext.CorrelationId;

            // Adicionar informações da requisição ao contexto
            correlationContext.AddContextProperty("RequestPath", context.Request.Path.Value ?? "unknown");
            correlationContext.AddContextProperty("RequestMethod", context.Request.Method);
            correlationContext.AddContextProperty("UserAgent", context.Request.Headers["User-Agent"].ToString() ?? "unknown");
            correlationContext.AddContextProperty("RemoteIpAddress", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        }
    }
}
