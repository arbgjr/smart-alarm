namespace SmartAlarm.Application.DTOs.Notifications;

public class NotificationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public int Priority { get; set; } = 1; // 1 = Low, 2 = Normal, 3 = High, 4 = Critical
}

public enum NotificationType
{
    Info = 1,
    Success = 2,
    Warning = 3,
    Error = 4,
    AlarmTriggered = 5,
    AlarmSnoozed = 6,
    AlarmDismissed = 7,
    SystemMaintenance = 8,
    SecurityAlert = 9
}
