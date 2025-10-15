using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Enums;
using System.Collections.Concurrent;

namespace SmartAlarm.Infrastructure.Repositories;

public class InMemoryAuditLogRepository : IAuditLogRepository
{
    private readonly ConcurrentBag<AuditLog> _auditLogs = new();

    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _auditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    public Task AddBulkAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
    {
        foreach (var auditLog in auditLogs)
        {
            _auditLogs.Add(auditLog);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _auditLogs.Where(log => log.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(log => log.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(log => log.Timestamp <= endDate.Value);

        return Task.FromResult(query.OrderByDescending(log => log.Timestamp).AsEnumerable());
    }

    public Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        var query = _auditLogs.Where(log =>
            log.EntityType == entityType &&
            log.EntityId == entityId);

        return Task.FromResult(query.OrderByDescending(log => log.Timestamp).AsEnumerable());
    }

    public Task<IEnumerable<AuditLog>> GetByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _auditLogs.Where(log => log.Action == action);

        if (startDate.HasValue)
            query = query.Where(log => log.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(log => log.Timestamp <= endDate.Value);

        return Task.FromResult(query.OrderByDescending(log => log.Timestamp).AsEnumerable());
    }

    public Task<IEnumerable<AuditLog>> GetByLevelAsync(AuditLogLevel level, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _auditLogs.Where(log => log.Level == level);

        if (startDate.HasValue)
            query = query.Where(log => log.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(log => log.Timestamp <= endDate.Value);

        return Task.FromResult(query.OrderByDescending(log => log.Timestamp).AsEnumerable());
    }

    public Task<IEnumerable<AuditLog>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var query = _auditLogs.Where(log => log.CorrelationId == correlationId);
        return Task.FromResult(query.OrderByDescending(log => log.Timestamp).AsEnumerable());
    }

    public Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        var query = _auditLogs
            .OrderByDescending(log => log.Timestamp)
            .Take(count);

        return Task.FromResult(query.AsEnumerable());
    }

    public Task<long> GetCountAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _auditLogs.AsEnumerable();

        if (startDate.HasValue)
            query = query.Where(log => log.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(log => log.Timestamp <= endDate.Value);

        return Task.FromResult((long)query.Count());
    }

    public Task DeleteOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        // Note: ConcurrentBag doesn't support removal, so this is a no-op for the in-memory implementation
        // In a real implementation, you would remove logs older than the cutoff date
        return Task.CompletedTask;
    }
}
