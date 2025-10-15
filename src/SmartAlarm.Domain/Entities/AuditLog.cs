namespace SmartAlarm.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string IpAddress { get; private set; }
    public string UserAgent { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? CorrelationId { get; private set; }
    public AuditLogLevel Level { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private AuditLog()
    {
        Metadata = new Dictionary<string, object>();
        Action = string.Empty;
        EntityType = string.Empty;
        IpAddress = string.Empty;
        UserAgent = string.Empty;
    }

    public static AuditLog Create(
        string action,
        string entityType,
        string? entityId = null,
        Guid? userId = null,
        string? oldValues = null,
        string? newValues = null,
        string ipAddress = "",
        string userAgent = "",
        string? correlationId = null,
        AuditLogLevel level = AuditLogLevel.Info,
        Dictionary<string, object>? metadata = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId,
            Level = level,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    public static AuditLog UserLogin(Guid userId, string ipAddress, string userAgent, string? correlationId = null)
    {
        return Create("USER_LOGIN", "User", userId.ToString(), userId,
            ipAddress: ipAddress, userAgent: userAgent, correlationId: correlationId, level: AuditLogLevel.Info);
    }

    public static AuditLog UserLogout(Guid userId, string ipAddress, string userAgent, string? correlationId = null)
    {
        return Create("USER_LOGOUT", "User", userId.ToString(), userId,
            ipAddress: ipAddress, userAgent: userAgent, correlationId: correlationId, level: AuditLogLevel.Info);
    }

    public static AuditLog AlarmCreated(Guid alarmId, Guid userId, string newValues, string ipAddress, string userAgent, string? correlationId = null)
    {
        return Create("ALARM_CREATED", "Alarm", alarmId.ToString(), userId,
            newValues: newValues, ipAddress: ipAddress, userAgent: userAgent, correlationId: correlationId, level: AuditLogLevel.Info);
    }

    public static AuditLog AlarmUpdated(Guid alarmId, Guid userId, string oldValues, string newValues, string ipAddress, string userAgent, string? correlationId = null)
    {
        return Create("ALARM_UPDATED", "Alarm", alarmId.ToString(), userId,
            oldValues: oldValues, newValues: newValues, ipAddress: ipAddress, userAgent: userAgent, correlationId: correlationId, level: AuditLogLevel.Info);
    }

    public static AuditLog AlarmDeleted(Guid alarmId, Guid userId, string oldValues, string ipAddress, string userAgent, string? correlationId = null)
    {
        return Create("ALARM_DELETED", "Alarm", alarmId.ToString(), userId,
            oldValues: oldValues, ipAddress: ipAddress, userAgent: userAgent, correlationId: correlationId, level: AuditLogLevel.Warning);
    }

    public static AuditLog SecurityEvent(string action, Guid? userId, string details, string ipAddress, string userAgent, string? correlationId = null)
    {
        return Create(action, "Security", null, userId,
            newValues: details, ipAddress: ipAddress, userAgent: userAgent, correlationId: correlationId, level: AuditLogLevel.Warning);
    }

    public static AuditLog SystemEvent(string action, string details, string? correlationId = null)
    {
        return Create(action, "System", null, null,
            newValues: details, correlationId: correlationId, level: AuditLogLevel.Info);
    }
}

public enum AuditLogLevel
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
