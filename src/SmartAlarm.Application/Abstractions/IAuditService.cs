using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Abstractions;

public interface IAuditService
{
    Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task LogUserActionAsync(string action, Guid userId, string? entityType = null, string? entityId = null,
        object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default);
    Task LogSecurityEventAsync(string action, Guid? userId, string details, CancellationToken cancellationToken = default);
    Task LogSystemEventAsync(string action, string details, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetUserAuditTrailAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetEntityAuditTrailAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task CleanupOldLogsAsync(int retentionDays = 365, CancellationToken cancellationToken = default);
}
