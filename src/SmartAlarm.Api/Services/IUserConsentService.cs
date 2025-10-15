using SmartAlarm.Domain.Enums;

namespace SmartAlarm.Api.Services;

/// <summary>
/// Interface para gerenciamento de consentimento do usuário (LGPD/GDPR)
/// </summary>
public interface IUserConsentService
{
    /// <summary>
    /// Registra consentimento do usuário
    /// </summary>
    Task<bool> RecordConsentAsync(Guid userId, ConsentType consentType, bool granted, string? details = null);

    /// <summary>
    /// Verifica se o usuário deu consentimento para um tipo específico
    /// </summary>
    Task<bool> HasConsentAsync(Guid userId, ConsentType consentType);

    /// <summary>
    /// Obtém todos os consentimentos de um usuário
    /// </summary>
    Task<List<UserConsent>> GetUserConsentsAsync(Guid userId);

    /// <summary>
    /// Revoga consentimento específico
    /// </summary>
    Task<bool> RevokeConsentAsync(Guid userId, ConsentType consentType, string reason);

    /// <summary>
    /// Revoga todos os consentimentos de um usuário
    /// </summary>
    Task<bool> RevokeAllConsentsAsync(Guid userId, string reason);

    /// <summary>
    /// Obtém dados pessoais de um usuário para exportação (GDPR Art. 20)
    /// </summary>
    Task<PersonalDataExport> ExportPersonalDataAsync(Guid userId);

    /// <summary>
    /// Solicita exclusão de dados pessoais (GDPR Art. 17 - Right to be forgotten)
    /// </summary>
    Task<bool> RequestDataDeletionAsync(Guid userId, string reason);

    /// <summary>
    /// Anonimiza dados de um usuário
    /// </summary>
    Task<bool> AnonymizeUserDataAsync(Guid userId, string reason);

    /// <summary>
    /// Verifica se o processamento de dados está em conformidade
    /// </summary>
    Task<ComplianceStatus> CheckComplianceStatusAsync(Guid userId);
}



/// <summary>
/// Consentimento do usuário
/// </summary>
public class UserConsent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ConsentType ConsentType { get; set; }
    public bool Granted { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string ConsentVersion { get; set; } = "1.0";
}

/// <summary>
/// Exportação de dados pessoais
/// </summary>
public class PersonalDataExport
{
    public Guid UserId { get; set; }
    public DateTime ExportedAt { get; set; }
    public string Format { get; set; } = "JSON";

    public UserProfile Profile { get; set; } = new();
    public List<AlarmData> Alarms { get; set; } = new();
    public List<UserConsent> Consents { get; set; } = new();
    public List<LoginHistory> LoginHistory { get; set; } = new();
    public DataProcessingHistory ProcessingHistory { get; set; } = new();
}

public class UserProfile
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }
}

public class AlarmData
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public bool IsActive { get; set; }
}

public class LoginHistory
{
    public DateTime LoginAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Successful { get; set; }
}

public class DataProcessingHistory
{
    public List<DataProcessingActivity> Activities { get; set; } = new();
    public DateTime FirstProcessingDate { get; set; }
    public DateTime LastProcessingDate { get; set; }
    public List<string> ProcessingPurposes { get; set; } = new();
    public List<string> DataCategories { get; set; } = new();
}

public class DataProcessingActivity
{
    public DateTime Date { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
}

/// <summary>
/// Status de compliance
/// </summary>
public class ComplianceStatus
{
    public Guid UserId { get; set; }
    public DateTime CheckedAt { get; set; }
    public bool IsCompliant { get; set; }
    public List<ComplianceIssue> Issues { get; set; } = new();
    public List<ConsentStatus> ConsentStatuses { get; set; } = new();
    public DataRetentionStatus RetentionStatus { get; set; } = new();
}

public class ComplianceIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public class ConsentStatus
{
    public ConsentType ConsentType { get; set; }
    public bool HasConsent { get; set; }
    public DateTime? LastUpdated { get; set; }
    public bool IsRequired { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DataRetentionStatus
{
    public DateTime DataCreatedAt { get; set; }
    public int RetentionPeriodDays { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public string Action { get; set; } = string.Empty;
}
