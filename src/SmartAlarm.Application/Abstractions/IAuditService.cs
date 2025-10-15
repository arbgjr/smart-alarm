using SmartAlarm.Domain.Enums;

namespace SmartAlarm.Application.Abstractions;

/// <summary>
/// Interface para serviço de auditoria e compliance
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Registra uma ação do usuário
    /// </summary>
    Task LogUserActionAsync(string action, Guid userId, string entityType, string? entityId, object? oldValue, object? newValue);

    /// <summary>
    /// Registra um evento de segurança
    /// </summary>
    Task LogSecurityEventAsync(string eventType, Guid? userId, string details);

    /// <summary>
    /// Registra acesso a dados pessoais (LGPD/GDPR)
    /// </summary>
    Task LogDataAccessAsync(Guid userId, string dataType, string purpose, Guid? accessedUserId = null);

    /// <summary>
    /// Registra consentimento do usuário (LGPD/GDPR)
    /// </summary>
    Task LogConsentAsync(Guid userId, string consentType, bool granted, string? details = null);

    /// <summary>
    /// Registra tentativa de acesso negado
    /// </summary>
    Task LogAccessDeniedAsync(Guid? userId, string resource, string reason);

    /// <summary>
    /// Registra operação de sistema
    /// </summary>
    Task LogSystemEventAsync(string eventType, string details, string? correlationId = null);

    /// <summary>
    /// Obtém logs de auditoria com filtros
    /// </summary>
    Task<AuditLogResult> GetAuditLogsAsync(AuditLogFilter filter);

    /// <summary>
    /// Gera relatório de compliance
    /// </summary>
    Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, Guid? userId = null);

    /// <summary>
    /// Anonimiza dados de um usuário (LGPD/GDPR)
    /// </summary>
    Task AnonymizeUserDataAsync(Guid userId, string reason);
}

/// <summary>
/// Filtros para consulta de logs de auditoria
/// </summary>
public class AuditLogFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? UserId { get; set; }
    public string? EventType { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public AuditLogLevel? Level { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Resultado da consulta de logs de auditoria
/// </summary>
public class AuditLogResult
{
    public List<AuditLogEntry> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Entrada de log de auditoria
/// </summary>
public class AuditLogEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
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
}



/// <summary>
/// Relatório de compliance
/// </summary>
public class ComplianceReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? UserId { get; set; }

    public ComplianceMetrics Metrics { get; set; } = new();
    public List<DataAccessSummary> DataAccess { get; set; } = new();
    public List<ConsentSummary> Consents { get; set; } = new();
    public List<SecurityEventSummary> SecurityEvents { get; set; } = new();
}

public class ComplianceMetrics
{
    public int TotalUserActions { get; set; }
    public int DataAccessEvents { get; set; }
    public int SecurityEvents { get; set; }
    public int ConsentEvents { get; set; }
    public int AccessDeniedEvents { get; set; }
}

public class DataAccessSummary
{
    public string DataType { get; set; } = string.Empty;
    public int AccessCount { get; set; }
    public DateTime LastAccess { get; set; }
    public List<string> Purposes { get; set; } = new();
}

public class ConsentSummary
{
    public string ConsentType { get; set; } = string.Empty;
    public bool CurrentStatus { get; set; }
    public DateTime LastUpdated { get; set; }
    public int ChangeCount { get; set; }
}

public class SecurityEventSummary
{
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastOccurrence { get; set; }
    public AuditLogLevel Severity { get; set; }
}
