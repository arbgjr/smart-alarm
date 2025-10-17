namespace SmartAlarm.Domain.Enums;

/// <summary>
/// Tipos de integração disponíveis no sistema
/// </summary>
public enum IntegrationType
{
    GoogleCalendar = 1,
    OutlookCalendar = 2,
    AppleCalendar = 3,
    Webhook = 4,
    Email = 5,
    SMS = 6,
    PushNotification = 7,
    Slack = 8,
    Teams = 9,
    Discord = 10
}
