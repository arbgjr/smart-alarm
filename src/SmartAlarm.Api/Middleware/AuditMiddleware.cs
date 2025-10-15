using SmartAlarm.Api.Services;
using SmartAlarm.Application.Abstractions;
using System.Security.Claims;
using System.Text;

namespace SmartAlarm.Api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip audit for certain paths
        if (ShouldSkipAudit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var startTime = DateTime.UtcNow;
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Log the audit information
            await LogAuditAsync(context, startTime, endTime, duration);

            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Log the audit information for failed requests
            await LogAuditAsync(context, startTime, endTime, duration, ex);

            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogAuditAsync(HttpContext context, DateTime startTime, DateTime endTime, TimeSpan duration, Exception? exception = null)
    {
        try
        {
            var auditService = context.RequestServices.GetService<IAuditService>();
            if (auditService == null)
                return;

            var userId = GetUserId(context);
            var action = $"{context.Request.Method} {context.Request.Path}";
            var details = new
            {
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                QueryString = context.Request.QueryString.Value,
                StatusCode = context.Response.StatusCode,
                Duration = duration.TotalMilliseconds,
                StartTime = startTime,
                EndTime = endTime,
                Exception = exception?.Message,
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                Referer = context.Request.Headers["Referer"].ToString()
            };

            if (IsSecurityRelevantRequest(context) || exception != null)
            {
                await auditService.LogSecurityEventAsync(action, userId, System.Text.Json.JsonSerializer.Serialize(details));
            }
            else if (IsUserAction(context))
            {
                await auditService.LogUserActionAsync(action, userId ?? Guid.Empty, "HttpRequest", null, null, details);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit information for request {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
    }

    private Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    private bool ShouldSkipAudit(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant();

        // Skip health checks, metrics, and static files
        return pathValue?.StartsWith("/health") == true ||
               pathValue?.StartsWith("/metrics") == true ||
               pathValue?.StartsWith("/swagger") == true ||
               pathValue?.StartsWith("/hangfire") == true ||
               pathValue?.Contains("/hubs/") == true ||
               pathValue?.EndsWith(".js") == true ||
               pathValue?.EndsWith(".css") == true ||
               pathValue?.EndsWith(".ico") == true ||
               pathValue?.EndsWith(".png") == true ||
               pathValue?.EndsWith(".jpg") == true ||
               pathValue?.EndsWith(".gif") == true;
    }

    private bool IsSecurityRelevantRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        return path?.Contains("/auth/") == true ||
               path?.Contains("/login") == true ||
               path?.Contains("/logout") == true ||
               path?.Contains("/register") == true ||
               context.Response.StatusCode == 401 ||
               context.Response.StatusCode == 403 ||
               context.Response.StatusCode >= 500;
    }

    private bool IsUserAction(HttpContext context)
    {
        // Log user actions for API endpoints (not static files or system endpoints)
        var path = context.Request.Path.Value?.ToLowerInvariant();
        return path?.StartsWith("/api/") == true &&
               context.Request.Method != "GET"; // Only log non-GET requests to reduce noise
    }
}
