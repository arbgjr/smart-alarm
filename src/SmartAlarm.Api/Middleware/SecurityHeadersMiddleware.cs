using Microsoft.Extensions.Options;

namespace SmartAlarm.Api.Middleware;

/// <summary>
/// Middleware para configurar headers de segurança obrigatórios
/// Implementa proteções contra OWASP Top 10 e melhores práticas de segurança
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SecurityHeadersOptions> options,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Aplicar headers de segurança antes de processar a requisição
        ApplySecurityHeaders(context);

        await _next(context);

        // Remover headers que podem vazar informações do servidor
        RemoveServerHeaders(context);
    }

    private void ApplySecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // X-Content-Type-Options: Previne MIME type sniffing
        if (!headers.ContainsKey("X-Content-Type-Options"))
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        // X-Frame-Options: Previne clickjacking
        if (!headers.ContainsKey("X-Frame-Options"))
        {
            headers["X-Frame-Options"] = _options.XFrameOptions;
        }

        // X-XSS-Protection: Proteção XSS para browsers legados
        if (!headers.ContainsKey("X-XSS-Protection"))
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }

        // Strict-Transport-Security: Força HTTPS
        if (context.Request.IsHttps && !headers.ContainsKey("Strict-Transport-Security"))
        {
            headers["Strict-Transport-Security"] = _options.StrictTransportSecurity;
        }

        // Content-Security-Policy: Previne XSS e injection attacks
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        // Referrer-Policy: Controla informações de referrer
        if (!headers.ContainsKey("Referrer-Policy"))
        {
            headers["Referrer-Policy"] = _options.ReferrerPolicy;
        }

        // Permissions-Policy: Controla APIs do browser
        if (!headers.ContainsKey("Permissions-Policy"))
        {
            headers["Permissions-Policy"] = _options.PermissionsPolicy;
        }

        // Cross-Origin-Embedder-Policy: Isolamento de origem cruzada
        if (!headers.ContainsKey("Cross-Origin-Embedder-Policy"))
        {
            headers["Cross-Origin-Embedder-Policy"] = "require-corp";
        }

        // Cross-Origin-Opener-Policy: Isolamento de janelas
        if (!headers.ContainsKey("Cross-Origin-Opener-Policy"))
        {
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
        }

        // Cross-Origin-Resource-Policy: Controle de recursos cross-origin
        if (!headers.ContainsKey("Cross-Origin-Resource-Policy"))
        {
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
        }
    }

    private void RemoveServerHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Remover headers que podem vazar informações do servidor
        var headersToRemove = new[]
        {
            "Server",
            "X-Powered-By",
            "X-AspNet-Version",
            "X-AspNetMvc-Version",
            "X-SourceFiles"
        };

        foreach (var header in headersToRemove)
        {
            headers.Remove(header);
        }
    }
}

/// <summary>
/// Opções de configuração para headers de segurança
/// </summary>
public class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    /// <summary>
    /// X-Frame-Options header value
    /// </summary>
    public string XFrameOptions { get; set; } = "DENY";

    /// <summary>
    /// Strict-Transport-Security header value
    /// </summary>
    public string StrictTransportSecurity { get; set; } = "max-age=31536000; includeSubDomains; preload";

    /// <summary>
    /// Content-Security-Policy header value
    /// </summary>
    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' wss: ws:; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    /// <summary>
    /// Referrer-Policy header value
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Permissions-Policy header value
    /// </summary>
    public string PermissionsPolicy { get; set; } =
        "camera=(), " +
        "microphone=(), " +
        "geolocation=(), " +
        "payment=(), " +
        "usb=(), " +
        "magnetometer=(), " +
        "gyroscope=(), " +
        "accelerometer=()";
}

/// <summary>
/// Extensões para facilitar o registro do middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }

    public static IServiceCollection AddSecurityHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecurityHeadersOptions>(
            configuration.GetSection(SecurityHeadersOptions.SectionName));

        return services;
    }
}
