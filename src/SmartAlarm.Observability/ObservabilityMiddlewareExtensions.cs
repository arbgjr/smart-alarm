using Microsoft.AspNetCore.Builder;

namespace SmartAlarm.Observability
{
    /// <summary>
    /// Extensões para registrar o middleware de observabilidade no pipeline.
    /// </summary>
    public static class ObservabilityMiddlewareExtensions
    {
        public static IApplicationBuilder UseObservability(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ObservabilityMiddleware>();
        }
    }
}
