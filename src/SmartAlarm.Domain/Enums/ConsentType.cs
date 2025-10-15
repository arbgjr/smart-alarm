namespace SmartAlarm.Domain.Enums;

/// <summary>
/// Tipos de consentimento LGPD/GDPR
/// </summary>
public enum ConsentType
{
    DataProcessing = 1,
    Marketing = 2,
    Analytics = 3,
    Cookies = 4,
    ThirdPartySharing = 5,
    LocationTracking = 6,
    PushNotifications = 7,
    EmailCommunication = 8,
    DataRetention = 9,
    AutomatedDecisionMaking = 10
}
