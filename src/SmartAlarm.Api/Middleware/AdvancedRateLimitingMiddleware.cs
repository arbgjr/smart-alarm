using SmartAlarm.Api.Services;
using System.Net;

namespace SmartAlarm.Api.Middleware;

/// <summary>
/// Middleware avançado de rate limiting com proteção DDoS
/// </summary>
public class AdvancedRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<AdvancedRateLimitingMiddleware> _logger;

    public AdvancedRateLimitingMiddleware(
        RequestDelegate next,
        IRateLimitingService rateLimitingService,
        ILogger<AdvancedRateLimitingMiddleware> logger)
    {
        _next = next;
        _rateLimitingService = rateLimitingService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Pular rate limiting para certos endpoints
        if (ShouldSkipRateLimit(context))
        {
            await _next(context);
            return;
        }

        var clientKey = GetClientKey(context);
        var endpoint = GetEndpointKey(context);
        var requestType = context.Request.Method;

        try
        {
            // Verificar se deve aplicar rate limiting
            var shouldLimit = await _rateLimitingService.ShouldRateLimitAsync(clientKey, endpoint, requestType);

            if (shouldLimit)
            {
                await HandleRateLimitExceeded(context, clientKey, endpoint);
                return;
            }

            // Adicionar headers informativos sobre rate limit
            await AddRateLimitHeaders(context, clientKey, endpoint);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware for {ClientKey} on {Endpoint}",
                clientKey, endpoint);

            // Em caso de erro, continuar sem rate limiting (fail-open)
            await _next(context);
        }
    }

    private bool ShouldSkipRateLimit(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        // Pular para endpoints de sistema
        return path?.StartsWith("/health") == true ||
               path?.StartsWith("/metrics") == true ||
               path?.StartsWith("/swagger") == true ||
               path?.StartsWith("/hangfire") == true ||
               path?.Contains("/hubs/") == true ||
               IsStaticFile(path);
    }

    private bool IsStaticFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var staticExtensions = new[] { ".js", ".css", ".ico", ".png", ".jpg", ".gif", ".svg", ".woff", ".woff2", ".ttf" };
        return staticExtensions.Any(ext => path.EndsWith(ext));
    }

    private string GetClientKey(HttpContext context)
    {
        // Priorizar usuário autenticado
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Usar IP como fallback
        var clientIp = GetClientIpAddress(context);
        return $"ip:{clientIp}";
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Verificar headers de proxy reverso
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetEndpointKey(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        // Normalizar path para agrupamento
        var normalizedPath = NormalizePath(path);

        return $"{method}:{normalizedPath}";
    }

    private string NormalizePath(string path)
    {
        // Remover IDs e parâmetros dinâmicos para agrupamento
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var normalizedSegments = new List<string>();

        foreach (var segment in segments)
        {
            // Substituir GUIDs por placeholder
            if (Guid.TryParse(segment, out _))
            {
                normalizedSegments.Add("{id}");
            }
            // Substituir números por placeholder
            else if (int.TryParse(segment, out _))
            {
                normalizedSegments.Add("{id}");
            }
            else
            {
                normalizedSegments.Add(segment.ToLowerInvariant());
            }
        }

        return "/" + string.Join("/", normalizedSegments);
    }

    private async Task HandleRateLimitExceeded(HttpContext context, string clientKey, string endpoint)
    {
        var rateLimitInfo = await _rateLimitingService.GetRateLimitInfoAsync(clientKey, endpoint);

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        // Adicionar headers de rate limit
        context.Response.Headers["X-RateLimit-Limit"] = rateLimitInfo.TotalRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(rateLimitInfo.ResetTime).ToUnixTimeSeconds().ToString();

        if (!string.IsNullOrEmpty(rateLimitInfo.RetryAfter))
        {
            context.Response.Headers["Retry-After"] = rateLimitInfo.RetryAfter;
        }

        var errorResponse = new
        {
            StatusCode = 429,
            Title = "Rate Limit Exceeded",
            Detail = "Too many requests. Please try again later.",
            Type = "RateLimitError",
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow,
            RateLimit = new
            {
                Limit = rateLimitInfo.TotalRequests,
                Remaining = 0,
                Reset = DateTimeOffset.UtcNow.Add(rateLimitInfo.ResetTime).ToUnixTimeSeconds(),
                RetryAfter = rateLimitInfo.RetryAfter
            }
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));

        _logger.LogWarning("Rate limit exceeded for {ClientKey} on {Endpoint}", clientKey, endpoint);
    }

    private async Task AddRateLimitHeaders(HttpContext context, string clientKey, string endpoint)
    {
        try
        {
            var rateLimitInfo = await _rateLimitingService.GetRateLimitInfoAsync(clientKey, endpoint);

            context.Response.Headers["X-RateLimit-Limit"] = rateLimitInfo.TotalRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = rateLimitInfo.RequestsRemaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(rateLimitInfo.ResetTime).ToUnixTimeSeconds().ToString();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to add rate limit headers for {ClientKey}", clientKey);
            // Não falhar a requisição por causa dos headers informativos
        }
    }
}

/// <summary>
/// Extensões para facilitar o registro do middleware
/// </summary>
public static class AdvancedRateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseAdvancedRateLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AdvancedRateLimitingMiddleware>();
    }
}
