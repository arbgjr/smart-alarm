using SmartAlarm.Domain.Enums;

namespace SmartAlarm.Domain.Entities;

/// <summary>
/// Entidade para logs de auditoria e compliance
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public AuditLogLevel Level { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
