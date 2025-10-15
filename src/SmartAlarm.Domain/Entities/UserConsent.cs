using SmartAlarm.Domain.Enums;

namespace SmartAlarm.Domain.Entities;

/// <summary>
/// Entidade para consentimentos do usuário (LGPD/GDPR)
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

    // Navigation properties
    public User User { get; set; } = null!;
}
