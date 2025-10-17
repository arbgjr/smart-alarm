using Microsoft.EntityFrameworkCore;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Enums;
using SmartAlarm.Domain.ValueObjects;
using System.Text.Json;

namespace SmartAlarm.Api.Services;

/// <summary>
/// Implementação do serviço de consentimento do usuário (LGPD/GDPR)
/// </summary>
public class UserConsentService : IUserConsentService
{
    private readonly SmartAlarmDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserConsentService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserConsentService(
        SmartAlarmDbContext context,
        IAuditService auditService,
        ILogger<UserConsentService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> RecordConsentAsync(Guid userId, ConsentType consentType, bool granted, string? details = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

            // Verificar se já existe consentimento para este tipo
            var existingConsent = await _context.UserConsents
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ConsentType == consentType);

            if (existingConsent != null)
            {
                // Atualizar consentimento existente
                existingConsent.Granted = granted;
                existingConsent.GrantedAt = DateTime.UtcNow;
                existingConsent.RevokedAt = granted ? null : DateTime.UtcNow;
                existingConsent.Details = details;
                existingConsent.IpAddress = ipAddress;
                existingConsent.UserAgent = userAgent;
            }
            else
            {
                // Criar novo consentimento
                var consent = new UserConsent
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ConsentType = consentType,
                    Granted = granted,
                    GrantedAt = DateTime.UtcNow,
                    RevokedAt = granted ? null : DateTime.UtcNow,
                    Details = details,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    ConsentVersion = "1.0"
                };

                _context.UserConsents.Add(consent);
            }

            await _context.SaveChangesAsync();

            // Log de auditoria
            await _auditService.LogConsentAsync(userId, consentType.ToString(), granted, details);

            _logger.LogInformation("Consent {ConsentType} {Action} for user {UserId}",
                consentType, granted ? "granted" : "revoked", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record consent {ConsentType} for user {UserId}", consentType, userId);
            return false;
        }
    }

    public async Task<bool> HasConsentAsync(Guid userId, ConsentType consentType)
    {
        try
        {
            var consent = await _context.UserConsents
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ConsentType == consentType);

            return consent?.Granted == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check consent {ConsentType} for user {UserId}", consentType, userId);
            return false;
        }
    }

    public async Task<List<UserConsent>> GetUserConsentsAsync(Guid userId)
    {
        try
        {
            return await _context.UserConsents
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.GrantedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consents for user {UserId}", userId);
            return new List<UserConsent>();
        }
    }

    public async Task<bool> RevokeConsentAsync(Guid userId, ConsentType consentType, string reason)
    {
        return await RecordConsentAsync(userId, consentType, false, reason);
    }

    public async Task<bool> RevokeAllConsentsAsync(Guid userId, string reason)
    {
        try
        {
            var consents = await _context.UserConsents
                .Where(x => x.UserId == userId && x.Granted)
                .ToListAsync();

            foreach (var consent in consents)
            {
                await RecordConsentAsync(userId, consent.ConsentType, false, reason);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke all consents for user {UserId}", userId);
            return false;
        }
    }

    public async Task<PersonalDataExport> ExportPersonalDataAsync(Guid userId)
    {
        try
        {
            // Log de acesso a dados pessoais
            await _auditService.LogDataAccessAsync(userId, "PersonalDataExport", "GDPR Article 20 - Data Portability", userId);

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            var export = new PersonalDataExport
            {
                UserId = userId,
                ExportedAt = DateTime.UtcNow,
                Format = "JSON"
            };

            // Dados do perfil
            export.Profile = new UserProfile
            {
                Name = user.Name.Value,
                Email = user.Email.Address,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                PreferredLanguage = user.PreferredLanguage,
                TimeZone = user.TimeZone
            };

            // Dados dos alarmes
            export.Alarms = await _context.Alarms
                .Where(x => x.UserId == userId)
                .Select(x => new AlarmData
                {
                    Id = x.Id,
                    Title = x.Name.Value,
                    CreatedAt = x.CreatedAt,
                    LastModifiedAt = x.CreatedAt, // Alarm doesn't have UpdatedAt, using CreatedAt
                    IsActive = x.IsActive
                })
                .ToListAsync();

            // Consentimentos
            export.Consents = await GetUserConsentsAsync(userId);

            // Histórico de login (últimos 90 dias)
            var loginLogs = await _context.AuditLogs
                .Where(x => x.UserId == userId &&
                           x.EventType == "UserAction" &&
                           x.Action.Contains("Login") &&
                           x.Timestamp >= DateTime.UtcNow.AddDays(-90))
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();

            export.LoginHistory = loginLogs.Select(x => new LoginHistory
            {
                LoginAt = x.Timestamp,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                Successful = !x.Action.Contains("Failed")
            }).ToList();

            // Histórico de processamento de dados
            var processingLogs = await _context.AuditLogs
                .Where(x => x.UserId == userId && x.EventType == "DataAccess")
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();

            export.ProcessingHistory = new DataProcessingHistory
            {
                FirstProcessingDate = processingLogs.LastOrDefault()?.Timestamp ?? user.CreatedAt,
                LastProcessingDate = processingLogs.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                Activities = processingLogs.Select(x => new DataProcessingActivity
                {
                    Date = x.Timestamp,
                    Activity = x.Action,
                    Purpose = ExtractPurposeFromDetails(x.Details),
                    LegalBasis = "Consent"
                }).ToList(),
                ProcessingPurposes = processingLogs
                    .Select(x => ExtractPurposeFromDetails(x.Details))
                    .Distinct()
                    .ToList(),
                DataCategories = new List<string> { "Profile", "Alarms", "Usage", "Authentication" }
            };

            return export;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export personal data for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> RequestDataDeletionAsync(Guid userId, string reason)
    {
        try
        {
            // Log da solicitação de exclusão
            await _auditService.LogSystemEventAsync("DataDeletionRequest", JsonSerializer.Serialize(new
            {
                UserId = userId,
                Reason = reason,
                RequestedAt = DateTime.UtcNow,
                Status = "Pending"
            }));

            // Em um sistema real, isso iniciaria um processo de revisão
            // Por enquanto, vamos apenas marcar para exclusão
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user != null)
            {
                user.MarkedForDeletion = true;
                user.DeletionRequestedAt = DateTime.UtcNow;
                user.DeletionReason = reason;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Data deletion requested for user {UserId} with reason: {Reason}", userId, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request data deletion for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> AnonymizeUserDataAsync(Guid userId, string reason)
    {
        try
        {
            // Usar o serviço de auditoria para anonimizar
            await _auditService.AnonymizeUserDataAsync(userId, reason);

            // Anonimizar dados do usuário
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user != null)
            {
                user.UpdateName(new Name("[ANONYMIZED]"));
                user.UpdateEmail(new Email($"anonymized_{userId}@deleted.local"));
                user.IsAnonymized = true;
                user.AnonymizedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to anonymize user data for user {UserId}", userId);
            return false;
        }
    }

    public async Task<ComplianceStatus> CheckComplianceStatusAsync(Guid userId)
    {
        try
        {
            var status = new ComplianceStatus
            {
                UserId = userId,
                CheckedAt = DateTime.UtcNow,
                IsCompliant = true
            };

            // Verificar consentimentos
            var consents = await GetUserConsentsAsync(userId);
            var requiredConsents = new[] { ConsentType.DataProcessing, ConsentType.Cookies };

            foreach (var requiredConsent in requiredConsents)
            {
                var consent = consents.FirstOrDefault(x => x.ConsentType == requiredConsent);
                var hasConsent = consent?.Granted == true;

                status.ConsentStatuses.Add(new ConsentStatus
                {
                    ConsentType = requiredConsent,
                    HasConsent = hasConsent,
                    LastUpdated = consent?.GrantedAt,
                    IsRequired = true,
                    Status = hasConsent ? "Valid" : "Missing"
                });

                if (!hasConsent)
                {
                    status.IsCompliant = false;
                    status.Issues.Add(new ComplianceIssue
                    {
                        Type = "MissingConsent",
                        Description = $"Missing required consent: {requiredConsent}",
                        Severity = "High",
                        RecommendedAction = "Request user consent"
                    });
                }
            }

            // Verificar retenção de dados
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user != null)
            {
                var retentionPeriodDays = 2555; // 7 anos
                var expiresAt = user.CreatedAt.AddDays(retentionPeriodDays);
                var isExpired = DateTime.UtcNow > expiresAt;

                status.RetentionStatus = new DataRetentionStatus
                {
                    DataCreatedAt = user.CreatedAt,
                    RetentionPeriodDays = retentionPeriodDays,
                    ExpiresAt = expiresAt,
                    IsExpired = isExpired,
                    Action = isExpired ? "Delete or Anonymize" : "Retain"
                };

                if (isExpired)
                {
                    status.IsCompliant = false;
                    status.Issues.Add(new ComplianceIssue
                    {
                        Type = "DataRetentionExpired",
                        Description = "Data retention period has expired",
                        Severity = "Medium",
                        RecommendedAction = "Delete or anonymize user data"
                    });
                }
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check compliance status for user {UserId}", userId);
            return new ComplianceStatus
            {
                UserId = userId,
                CheckedAt = DateTime.UtcNow,
                IsCompliant = false,
                Issues = new List<ComplianceIssue>
                {
                    new ComplianceIssue
                    {
                        Type = "SystemError",
                        Description = "Failed to check compliance status",
                        Severity = "High",
                        RecommendedAction = "Contact system administrator"
                    }
                }
            };
        }
    }

    public void RegisterConsent(string userId, bool consentGiven)
    {
        if (Guid.TryParse(userId, out var userGuid))
        {
            RecordConsentAsync(userGuid, ConsentType.DataProcessing, consentGiven).GetAwaiter().GetResult();
        }
    }

    public bool HasConsent(string userId)
    {
        if (Guid.TryParse(userId, out var userGuid))
        {
            return HasConsentAsync(userGuid, ConsentType.DataProcessing).GetAwaiter().GetResult();
        }
        return false;
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
}
