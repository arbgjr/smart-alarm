using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;
using System.Text.Json;

namespace SmartAlarm.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<AuditService> _logger;
    private readonly SmartAlarmMeter _meter;
    private readonly SmartAlarmActivitySource _activitySource;

    public AuditService(
        IAuditLogRepository auditLogRepository,
        IHttpContextAccessor httpContextAccessor,
        ICorrelationContext correlationContext,
        ILogger<AuditService> logger,
        SmartAlarmMeter meter,
        SmartAlarmActivitySource activitySource)
    {
        _auditLogRepository = auditLogRepository;
        _httpContextAccessor = httpContextAccessor;
        _correlationContext = correlationContext;
        _logger = logger;
        _meter = meter;
        _activitySource = activitySource;
    }

    public async Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("LogAudit");
        activity?.SetTag("audit.action", auditLog.Action);
        activity?.SetTag("audit.entity_type", auditLog.EntityType);
        activity?.SetTag("audit.level", auditLog.Level.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "AddAuditLog", "AuditLog");
            _meter.IncrementCounter("audit_logs_created", 1, new Dictionary<string, object>
            {
                { "action", auditLog.Action },
                { "entity_type", auditLog.EntityType },
                { "level", auditLog.Level.ToString() }
            });

            _logger.LogDebug("Audit log created: {Action} on {EntityType} by user {UserId} in {ElapsedMs}ms",
                auditLog.Action, auditLog.EntityType, auditLog.UserId, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AuditService", "LogError");

            _logger.LogError(ex, "Failed to create audit log in {ElapsedMs}ms: {Error}",
                stopwatch.ElapsedMilliseconds, ex.Message);

            // Don't throw - audit logging should not break the main operation
        }
    }

    public async Task LogUserActionAsync(string action, Guid userId, string? entityType = null, string? entityId = null,
        object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(httpContext);
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "";
            var correlationId = _correlationContext.CorrelationId;

            var oldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null;
            var newValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null;

            var auditLog = AuditLog.Create(
                action: action,
                entityType: entityType ?? "Unknown",
                entityId: entityId,
                userId: userId,
                oldValues: oldValuesJson,
                newValues: newValuesJson,
                ipAddress: ipAddress,
                userAgent: userAgent,
                correlationId: correlationId,
                level: AuditLogLevel.Info
            );

            await LogAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user action {Action} for user {UserId}", action, userId);
        }
    }

    public async Task LogSecurityEventAsync(string action, Guid? userId, string details, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(httpContext);
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "";
            var correlationId = _correlationContext.CorrelationId;

            var auditLog = AuditLog.SecurityEvent(action, userId, details, ipAddress, userAgent, correlationId);

            await LogAsync(auditLog, cancellationToken);

            // Also log security events at warning level
            _logger.LogWarning("Security event: {Action} for user {UserId} from {IpAddress} - {Details}",
                action, userId, ipAddress, details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event {Action}", action);
        }
    }

    public async Task LogSystemEventAsync(string action, string details, CancellationToken cancellationToken = default)
    {
        try
        {
            var correlationId = _correlationContext.CorrelationId;
            var auditLog = AuditLog.SystemEvent(action, details, correlationId);

            await LogAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log system event {Action}", action);
        }
    }

    public async Task<IEnumerable<AuditLog>> GetUserAuditTrailAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetUserAuditTrail");
        activity?.SetTag("user.id", userId.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var auditLogs = await _auditLogRepository.GetByUserIdAsync(userId, startDate, endDate, cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetUserAuditTrail", "AuditLog");

            _logger.LogInformation("Retrieved {Count} audit logs for user {UserId} in {ElapsedMs}ms",
                auditLogs.Count(), userId, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return auditLogs;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AuditService", "QueryError");

            _logger.LogError(ex, "Failed to get user audit trail for user {UserId} in {ElapsedMs}ms",
                userId, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetEntityAuditTrailAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetEntityAuditTrail");
        activity?.SetTag("entity.type", entityType);
        activity?.SetTag("entity.id", entityId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var auditLogs = await _auditLogRepository.GetByEntityAsync(entityType, entityId, cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetEntityAuditTrail", "AuditLog");

            _logger.LogInformation("Retrieved {Count} audit logs for {EntityType} {EntityId} in {ElapsedMs}ms",
                auditLogs.Count(), entityType, entityId, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return auditLogs;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AuditService", "QueryError");

            _logger.LogError(ex, "Failed to get entity audit trail for {EntityType} {EntityId} in {ElapsedMs}ms",
                entityType, entityId, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetSecurityEvents");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var auditLogs = await _auditLogRepository.GetByLevelAsync(AuditLogLevel.Warning, startDate, endDate, cancellationToken);
            var securityEvents = auditLogs.Where(log => log.EntityType == "Security").ToList();
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetSecurityEvents", "AuditLog");

            _logger.LogInformation("Retrieved {Count} security events in {ElapsedMs}ms",
                securityEvents.Count, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return securityEvents;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AuditService", "QueryError");

            _logger.LogError(ex, "Failed to get security events in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    public async Task CleanupOldLogsAsync(int retentionDays = 365, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("CleanupOldLogs");
        activity?.SetTag("retention.days", retentionDays.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            await _auditLogRepository.DeleteOldLogsAsync(cutoffDate, cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "CleanupOldLogs", "AuditLog");

            _logger.LogInformation("Cleaned up audit logs older than {CutoffDate} in {ElapsedMs}ms",
                cutoffDate, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AuditService", "CleanupError");

            _logger.LogError(ex, "Failed to cleanup old audit logs in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private string GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return "Unknown";

        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
