using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Enums;
using System.Text.Json;

namespace SmartAlarm.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de auditoria e compliance
/// </summary>
public class AuditService : IAuditService
{
    private readonly SmartAlarmDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(SmartAlarmDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogUserActionAsync(string action, Guid userId, string entityType, string? entityId, object? oldValue, object? newValue)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                EventType = "UserAction",
                Level = AuditLogLevel.Information,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                Details = $"User {userId} performed {action} on {entityType} {entityId}"
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user action: {Action} for user {UserId}", action, userId);
        }
    }

    public async Task LogSecurityEventAsync(string eventType, Guid? userId, string details)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                EventType = eventType,
                Level = AuditLogLevel.Security,
                EntityType = "Security",
                Action = eventType,
                Details = details
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Security event logged: {EventType} for user {UserId}", eventType, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {EventType} for user {UserId}", eventType, userId);
        }
    }

    public async Task LogDataAccessAsync(Guid userId, string dataType, string purpose, Guid? accessedUserId = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                EventType = "DataAccess",
                Level = AuditLogLevel.Information,
                EntityType = dataType,
                EntityId = accessedUserId?.ToString(),
                Action = "Access",
                Details = JsonSerializer.Serialize(new
                {
                    DataType = dataType,
                    Purpose = purpose,
                    AccessedUserId = accessedUserId,
                    Timestamp = DateTime.UtcNow
                })
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log data access: {DataType} for user {UserId}", dataType, userId);
        }
    }

    public async Task LogConsentAsync(Guid userId, string consentType, bool granted, string? details = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                EventType = "Consent",
                Level = AuditLogLevel.Information,
                EntityType = "UserConsent",
                EntityId = userId.ToString(),
                Action = granted ? "Grant" : "Revoke",
                Details = JsonSerializer.Serialize(new
                {
                    ConsentType = consentType,
                    Granted = granted,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                })
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log consent: {ConsentType} for user {UserId}", consentType, userId);
        }
    }

    public async Task LogAccessDeniedAsync(Guid? userId, string resource, string reason)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                EventType = "AccessDenied",
                Level = AuditLogLevel.Warning,
                EntityType = "Security",
                Action = "AccessDenied",
                Details = JsonSerializer.Serialize(new
                {
                    Resource = resource,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow
                })
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log access denied for user {UserId} on resource {Resource}", userId, resource);
        }
    }

    public async Task LogSystemEventAsync(string eventType, string details, string? correlationId = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                EventType = eventType,
                Level = AuditLogLevel.Information,
                EntityType = "System",
                Action = eventType,
                Details = details,
                CorrelationId = correlationId
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log system event: {EventType}", eventType);
        }
    }

    public async Task<AuditLogResult> GetAuditLogsAsync(AuditLogFilter filter)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            // Aplicar filtros
            if (filter.StartDate.HasValue)
                query = query.Where(x => x.Timestamp >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(x => x.Timestamp <= filter.EndDate.Value);

            if (filter.UserId.HasValue)
                query = query.Where(x => x.UserId == filter.UserId.Value);

            if (!string.IsNullOrEmpty(filter.EventType))
                query = query.Where(x => x.EventType == filter.EventType);

            if (!string.IsNullOrEmpty(filter.EntityType))
                query = query.Where(x => x.EntityType == filter.EntityType);

            if (!string.IsNullOrEmpty(filter.EntityId))
                query = query.Where(x => x.EntityId == filter.EntityId);

            if (filter.Level.HasValue)
                query = query.Where(x => x.Level == filter.Level.Value);

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new AuditLogEntry
                {
                    Id = x.Id,
                    Timestamp = x.Timestamp,
                    UserId = x.UserId,
                    UserName = x.User != null ? x.User.Name.Value : null,
                    EventType = x.EventType,
                    Level = x.Level,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    Action = x.Action,
                    OldValue = x.OldValue,
                    NewValue = x.NewValue,
                    Details = x.Details,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    CorrelationId = x.CorrelationId
                })
                .ToListAsync();

            return new AuditLogResult
            {
                Logs = logs,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs");
            return new AuditLogResult();
        }
    }

    public async Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, Guid? userId = null)
    {
        try
        {
            var query = _context.AuditLogs.Where(x => x.Timestamp >= startDate && x.Timestamp <= endDate);

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId.Value);

            var logs = await query.ToListAsync();

            var report = new ComplianceReport
            {
                GeneratedAt = DateTime.UtcNow,
                StartDate = startDate,
                EndDate = endDate,
                UserId = userId
            };

            // Métricas gerais
            report.Metrics = new ComplianceMetrics
            {
                TotalUserActions = logs.Count(x => x.EventType == "UserAction"),
                DataAccessEvents = logs.Count(x => x.EventType == "DataAccess"),
                SecurityEvents = logs.Count(x => x.Level == AuditLogLevel.Security),
                ConsentEvents = logs.Count(x => x.EventType == "Consent"),
                AccessDeniedEvents = logs.Count(x => x.EventType == "AccessDenied")
            };

            // Resumo de acesso a dados
            report.DataAccess = logs
                .Where(x => x.EventType == "DataAccess")
                .GroupBy(x => x.EntityType)
                .Select(g => new DataAccessSummary
                {
                    DataType = g.Key,
                    AccessCount = g.Count(),
                    LastAccess = g.Max(x => x.Timestamp),
                    Purposes = g.Select(x => ExtractPurposeFromDetails(x.Details)).Distinct().ToList()
                })
                .ToList();

            // Resumo de consentimentos
            report.Consents = logs
                .Where(x => x.EventType == "Consent")
                .GroupBy(x => ExtractConsentTypeFromDetails(x.Details))
                .Select(g => new ConsentSummary
                {
                    ConsentType = g.Key,
                    CurrentStatus = g.OrderByDescending(x => x.Timestamp).First().Action == "Grant",
                    LastUpdated = g.Max(x => x.Timestamp),
                    ChangeCount = g.Count()
                })
                .ToList();

            // Resumo de eventos de segurança
            report.SecurityEvents = logs
                .Where(x => x.Level == AuditLogLevel.Security)
                .GroupBy(x => x.EventType)
                .Select(g => new SecurityEventSummary
                {
                    EventType = g.Key,
                    Count = g.Count(),
                    LastOccurrence = g.Max(x => x.Timestamp),
                    Severity = g.Max(x => x.Level)
                })
                .ToList();

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report");
            return new ComplianceReport
            {
                GeneratedAt = DateTime.UtcNow,
                StartDate = startDate,
                EndDate = endDate,
                UserId = userId
            };
        }
    }

    public async Task AnonymizeUserDataAsync(Guid userId, string reason)
    {
        try
        {
            // Log da operação de anonimização
            await LogSystemEventAsync("DataAnonymization", JsonSerializer.Serialize(new
            {
                UserId = userId,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            }));

            // Anonimizar dados do usuário nos logs de auditoria
            var userLogs = await _context.AuditLogs
                .Where(x => x.UserId == userId)
                .ToListAsync();

            foreach (var log in userLogs)
            {
                log.UserId = null;
                log.Details = AnonymizeJsonData(log.Details);
                log.OldValue = AnonymizeJsonData(log.OldValue);
                log.NewValue = AnonymizeJsonData(log.NewValue);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User data anonymized for user {UserId} with reason: {Reason}", userId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to anonymize user data for user {UserId}", userId);
            throw;
        }
    }

    private string ExtractPurposeFromDetails(string details)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(details);
            return data?.GetValueOrDefault("Purpose")?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string ExtractConsentTypeFromDetails(string details)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(details);
            return data?.GetValueOrDefault("ConsentType")?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string? AnonymizeJsonData(string? jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
            return jsonData;

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
            if (data == null) return jsonData;

            // Replace sensitive data with anonymized values
            var anonymizedData = new Dictionary<string, object>();
            foreach (var kvp in data)
            {
                anonymizedData[kvp.Key] = kvp.Key.ToLowerInvariant() switch
                {
                    "userid" => "[ANONYMIZED]",
                    "email" => "[ANONYMIZED_EMAIL]",
                    "name" => "[ANONYMIZED_NAME]",
                    "phone" => "[ANONYMIZED_PHONE]",
                    "address" => "[ANONYMIZED_ADDRESS]",
                    _ => kvp.Value
                };
            }

            return JsonSerializer.Serialize(anonymizedData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to anonymize JSON data, returning original");
            return jsonData;
        }
    }

    public async Task<IEnumerable<AuditLogEntry>> GetUserAuditTrailAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.AuditLogs.Where(x => x.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(x => x.Timestamp)
                .Select(x => new AuditLogEntry
                {
                    Id = x.Id,
                    Timestamp = x.Timestamp,
                    UserId = x.UserId,
                    UserName = x.User != null ? x.User.Name.Value : null,
                    EventType = x.EventType,
                    Level = x.Level,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    Action = x.Action,
                    OldValue = x.OldValue,
                    NewValue = x.NewValue,
                    OldValues = x.OldValue, // Alternative property name
                    NewValues = x.NewValue, // Alternative property name
                    Details = x.Details,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    CorrelationId = x.CorrelationId
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user audit trail for user {UserId}", userId);
            return new List<AuditLogEntry>();
        }
    }

    public async Task<IEnumerable<AuditLogEntry>> GetEntityAuditTrailAsync(string entityType, string entityId)
    {
        try
        {
            return await _context.AuditLogs
                .Where(x => x.EntityType == entityType && x.EntityId == entityId)
                .OrderByDescending(x => x.Timestamp)
                .Select(x => new AuditLogEntry
                {
                    Id = x.Id,
                    Timestamp = x.Timestamp,
                    UserId = x.UserId,
                    UserName = x.User != null ? x.User.Name.Value : null,
                    EventType = x.EventType,
                    Level = x.Level,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    Action = x.Action,
                    OldValue = x.OldValue,
                    NewValue = x.NewValue,
                    OldValues = x.OldValue,
                    NewValues = x.NewValue,
                    Details = x.Details,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    CorrelationId = x.CorrelationId
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity audit trail for {EntityType} {EntityId}", entityType, entityId);
            return new List<AuditLogEntry>();
        }
    }

    public async Task<IEnumerable<AuditLogEntry>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.AuditLogs.Where(x => x.Level == AuditLogLevel.Security);

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(x => x.Timestamp)
                .Select(x => new AuditLogEntry
                {
                    Id = x.Id,
                    Timestamp = x.Timestamp,
                    UserId = x.UserId,
                    UserName = x.User != null ? x.User.Name.Value : null,
                    EventType = x.EventType,
                    Level = x.Level,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    Action = x.Action,
                    OldValue = x.OldValue,
                    NewValue = x.NewValue,
                    OldValues = x.OldValue,
                    NewValues = x.NewValue,
                    Details = x.Details,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    CorrelationId = x.CorrelationId
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events");
            return new List<AuditLogEntry>();
        }
    }

    public async Task CleanupOldLogsAsync(int retentionDays)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var oldLogs = await _context.AuditLogs
                .Where(x => x.Timestamp < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                await LogSystemEventAsync("AuditLogCleanup", JsonSerializer.Serialize(new
                {
                    DeletedCount = oldLogs.Count,
                    CutoffDate = cutoffDate,
                    RetentionDays = retentionDays,
                    Timestamp = DateTime.UtcNow
                }));

                _logger.LogInformation("Cleaned up {Count} old audit logs older than {CutoffDate}",
                    oldLogs.Count, cutoffDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old audit logs");
            throw;
        }
    }
}
