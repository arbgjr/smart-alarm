using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Enums;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework;

public class EfAuditLogRepository : IAuditLogRepository
{
    private readonly SmartAlarmDbContext _context;
    private readonly ILogger<EfAuditLogRepository> _logger;
    private readonly SmartAlarmMeter _meter;
    private readonly SmartAlarmActivitySource _activitySource;
    private readonly ICorrelationContext _correlationContext;

    public EfAuditLogRepository(
        SmartAlarmDbContext context,
        ILogger<EfAuditLogRepository> logger,
        SmartAlarmMeter meter,
        SmartAlarmActivitySource activitySource,
        ICorrelationContext correlationContext)
    {
        _context = context;
        _logger = logger;
        _meter = meter;
        _activitySource = activitySource;
        _correlationContext = correlationContext;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.AddAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "AddAsync");
        activity?.SetTag("table", "AuditLogs");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "INSERT", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogDebug("Added audit log {AuditLogId} in {ElapsedMs}ms",
                auditLog.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("INSERT", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to add audit log {AuditLogId} in {ElapsedMs}ms",
                auditLog.Id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task AddBulkAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.AddBulkAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "AddBulkAsync");
        activity?.SetTag("table", "AuditLogs");

        var stopwatch = Stopwatch.StartNew();
        var auditLogList = auditLogs.ToList();
        activity?.SetTag("bulk.count", auditLogList.Count);

        try
        {
            _context.AuditLogs.AddRange(auditLogList);
            await _context.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "BULK_INSERT", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogDebug("Added {Count} audit logs in {ElapsedMs}ms",
                auditLogList.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("BULK_INSERT", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to add {Count} audit logs in {ElapsedMs}ms",
                auditLogList.Count, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.GetByUserIdAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "GetByUserIdAsync");
        activity?.SetTag("table", "AuditLogs");
        activity?.SetTag("user.id", userId.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var query = _context.AuditLogs.Where(a => a.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            var result = await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync(cancellationToken);

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "SELECT", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("result.count", result.Count);

            _logger.LogDebug("Retrieved {Count} audit logs for user {UserId} in {ElapsedMs}ms",
                result.Count, userId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SELECT", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to retrieve audit logs for user {UserId} in {ElapsedMs}ms",
                userId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.GetByEntityAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "GetByEntityAsync");
        activity?.SetTag("table", "AuditLogs");
        activity?.SetTag("entity.type", entityType);
        activity?.SetTag("entity.id", entityId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _context.AuditLogs
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync(cancellationToken);

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "SELECT", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("result.count", result.Count);

            _logger.LogDebug("Retrieved {Count} audit logs for entity {EntityType}:{EntityId} in {ElapsedMs}ms",
                result.Count, entityType, entityId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SELECT", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to retrieve audit logs for entity {EntityType}:{EntityId} in {ElapsedMs}ms",
                entityType, entityId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.GetByActionAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "GetByActionAsync");
        activity?.SetTag("table", "AuditLogs");
        activity?.SetTag("action", action);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var query = _context.AuditLogs.Where(a => a.Action == action);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            var result = await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync(cancellationToken);

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "SELECT", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("result.count", result.Count);

            _logger.LogDebug("Retrieved {Count} audit logs for action {Action} in {ElapsedMs}ms",
                result.Count, action, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SELECT", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to retrieve audit logs for action {Action} in {ElapsedMs}ms",
                action, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByLevelAsync(AuditLogLevel level, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.GetByLevelAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "GetByLevelAsync");
        activity?.SetTag("table", "AuditLogs");
        activity?.SetTag("level", level.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var query = _context.AuditLogs.Where(a => a.Level == level);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            var result = await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync(cancellationToken);

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "SELECT", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("result.count", result.Count);

            _logger.LogDebug("Retrieved {Count} audit logs for level {Level} in {ElapsedMs}ms",
                result.Count, level, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SELECT", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to retrieve audit logs for level {Level} in {ElapsedMs}ms",
                level, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.GetByCorrelationIdAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "GetByCorrelationIdAsync");
        activity?.SetTag("table", "AuditLogs");
        activity?.SetTag("search.correlation_id", correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _context.AuditLogs
                .Where(a => a.CorrelationId == correlationId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync(cancellationToken);

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "SELECT", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("result.count", result.Count);

            _logger.LogDebug("Retrieved {Count} audit logs for correlation ID {CorrelationId} in {ElapsedMs}ms",
                result.Count, correlationId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SELECT", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to retrieve audit logs for correlation ID {CorrelationId} in {ElapsedMs}ms",
                correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.GetRecentAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "GetRecentAsync");
        activity?.SetTag("table", "AuditLogs");
        activity?.SetTag("limit", count);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync(cancellationToken);

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "SELECT", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("result.count", result.Count);

            _logger.LogDebug("Retrieved {Count} recent audit logs in {ElapsedMs}ms",
                result.Count, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SELECT", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to retrieve recent audit logs in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<long> GetCountAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.GetCountAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "GetCountAsync");
        activity?.SetTag("table", "AuditLogs");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            var result = await query.CountAsync(cancellationToken);

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "COUNT", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("result.count", result);

            _logger.LogDebug("Counted {Count} audit logs in {ElapsedMs}ms",
                result, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("COUNT", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to count audit logs in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task DeleteOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EfAuditLogRepository.DeleteOldLogsAsync");
        activity?.SetTag("correlation.id", _correlationContext.CorrelationId);
        activity?.SetTag("operation", "DeleteOldLogsAsync");
        activity?.SetTag("table", "AuditLogs");
        activity?.SetTag("cutoff.date", cutoffDate.ToString("yyyy-MM-dd"));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var oldLogs = await _context.AuditLogs
                .Where(a => a.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldLogs.Any())
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync(cancellationToken);
            }

            stopwatch.Stop();
            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "DELETE", "AuditLogs");
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("deleted.count", oldLogs.Count);

            _logger.LogInformation("Deleted {Count} old audit logs (older than {CutoffDate}) in {ElapsedMs}ms",
                oldLogs.Count, cutoffDate, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("DELETE", "AuditLogs", "DatabaseError");

            _logger.LogError(ex, "Failed to delete old audit logs in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
