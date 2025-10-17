using SmartAlarm.Application.Abstractions;
using SmartAlarm.Api.Services;
using System.Text;
using System.Text.Json;

namespace SmartAlarm.Api.Middleware;

/// <summary>
/// Middleware for automatic audit logging of user actions
/// </summary>
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

        var auditService = context.RequestServices.GetService<IAuditService>();
        var currentUserService = context.RequestServices.GetService<ICurrentUserService>();

        if (auditService == null || currentUserService == null)
        {
            await _next(context);
            return;
        }

        var startTime = DateTime.UtcNow;
        var requestBody = await CaptureRequestBodyAsync(context.Request);
        var originalResponseBodyStream = context.Response.Body;

        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            // Log successful operations
            if (context.Response.StatusCode < 400 && ShouldLogAction(context))
            {
                await LogUserActionAsync(auditService, currentUserService, context, requestBody, startTime);
            }
        }
        catch (Exception ex)
        {
            // Log failed operations
            if (ShouldLogAction(context))
            {
                await LogFailedActionAsync(auditService, currentUserService, context, requestBody, ex, startTime);
            }
            throw;
        }
        finally
        {
            // Copy response back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }

    private static bool ShouldSkipAudit(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip health checks, metrics, static files, etc.
        return pathValue.StartsWith("/health") ||
               pathValue.StartsWith("/metrics") ||
               pathValue.StartsWith("/swagger") ||
               pathValue.StartsWith("/hubs") ||
               pathValue.Contains("/css/") ||
               pathValue.Contains("/js/") ||
               pathValue.Contains("/images/") ||
               pathValue.EndsWith(".ico") ||
               pathValue.EndsWith(".png") ||
               pathValue.EndsWith(".jpg") ||
               pathValue.EndsWith(".css") ||
               pathValue.EndsWith(".js");
    }

    private static bool ShouldLogAction(HttpContext context)
    {
        // Only log state-changing operations (POST, PUT, DELETE)
        var method = context.Request.Method.ToUpperInvariant();
        return method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH";
    }

    private async Task<string?> CaptureRequestBodyAsync(HttpRequest request)
    {
        try
        {
            if (request.ContentLength == null || request.ContentLength == 0)
                return null;

            request.EnableBuffering();
            var buffer = new byte[request.ContentLength.Value];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Position = 0;

            var bodyText = Encoding.UTF8.GetString(buffer);

            // Don't log sensitive data
            if (ContainsSensitiveData(request.Path))
            {
                return "[SENSITIVE_DATA_REDACTED]";
            }

            // Limit body size for logging
            return bodyText.Length > 1000 ? bodyText[..1000] + "..." : bodyText;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture request body for audit");
            return "[CAPTURE_FAILED]";
        }
    }

    private static bool ContainsSensitiveData(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        return pathValue.Contains("/auth/") ||
               pathValue.Contains("/login") ||
               pathValue.Contains("/register") ||
               pathValue.Contains("/password") ||
               pathValue.Contains("/token");
    }

    private async Task LogUserActionAsync(
        IAuditService auditService,
        ICurrentUserService currentUserService,
        HttpContext context,
        string? requestBody,
        DateTime startTime)
    {
        try
        {
            var userId = GetUserId(currentUserService);
            if (!userId.HasValue)
                return;

            var action = DetermineAction(context);
            var entityInfo = ExtractEntityInfo(context);
            var duration = DateTime.UtcNow - startTime;

            await auditService.LogUserActionAsync(
                action: action,
                userId: userId.Value,
                entityType: entityInfo.EntityType,
                entityId: entityInfo.EntityId,
                oldValue: null, // Would need to be captured from before the operation
                newValue: requestBody
            );

            _logger.LogDebug("Logged user action: {Action} by user {UserId} on {EntityType} {EntityId} (took {Duration}ms)",
                action, userId, entityInfo.EntityType, entityInfo.EntityId, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user action");
        }
    }

    private async Task LogFailedActionAsync(
        IAuditService auditService,
        ICurrentUserService currentUserService,
        HttpContext context,
        string? requestBody,
        Exception exception,
        DateTime startTime)
    {
        try
        {
            var userId = GetUserId(currentUserService);
            var action = DetermineAction(context);
            var entityInfo = ExtractEntityInfo(context);
            var duration = DateTime.UtcNow - startTime;

            await auditService.LogSecurityEventAsync(
                eventType: "FailedOperation",
                userId: userId,
                details: JsonSerializer.Serialize(new
                {
                    Action = action,
                    EntityType = entityInfo.EntityType,
                    EntityId = entityInfo.EntityId,
                    Error = exception.Message,
                    StatusCode = context.Response.StatusCode,
                    Duration = duration.TotalMilliseconds,
                    IpAddress = GetClientIpAddress(context),
                    UserAgent = context.Request.Headers.UserAgent.ToString()
                })
            );

            _logger.LogWarning("Logged failed action: {Action} by user {UserId} on {EntityType} {EntityId} - {Error} (took {Duration}ms)",
                action, userId, entityInfo.EntityType, entityInfo.EntityId, exception.Message, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log failed action");
        }
    }

    private static Guid? GetUserId(ICurrentUserService currentUserService)
    {
        try
        {
            var userIdString = currentUserService.UserId;
            return string.IsNullOrEmpty(userIdString) ? null : Guid.Parse(userIdString);
        }
        catch
        {
            return null;
        }
    }

    private static string DetermineAction(HttpContext context)
    {
        var method = context.Request.Method.ToUpperInvariant();
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        return method switch
        {
            "POST" when path.Contains("/login") => "Login",
            "POST" when path.Contains("/logout") => "Logout",
            "POST" when path.Contains("/register") => "Register",
            "POST" => "Create",
            "PUT" => "Update",
            "PATCH" => "Update",
            "DELETE" => "Delete",
            _ => method
        };
    }

    private static (string EntityType, string? EntityId) ExtractEntityInfo(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            return ("Unknown", null);

        // Try to extract entity type and ID from path
        // Examples: /api/alarms/123, /api/users/456
        if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            var entityType = segments[1];
            var entityId = segments.Length >= 3 ? segments[2] : null;

            // Validate that entityId is actually an ID (GUID or number)
            if (entityId != null && !IsValidId(entityId))
            {
                entityId = null;
            }

            return (entityType, entityId);
        }

        return (segments[0], segments.Length > 1 ? segments[1] : null);
    }

    private static bool IsValidId(string value)
    {
        return Guid.TryParse(value, out _) || int.TryParse(value, out _) || long.TryParse(value, out _);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (in case of proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
