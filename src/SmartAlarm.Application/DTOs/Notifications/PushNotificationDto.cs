namespace SmartAlarm.Application.DTOs.Notifications;

public class PushNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Image { get; set; }
    public string? Badge { get; set; }
    public string? Sound { get; set; } = "default";
    public Dictionary<string, string> Data { get; set; } = new();
    public string? ClickAction { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public int Priority { get; set; } = 1; // 1 = Low, 2 = Normal, 3 = High
    public int TimeToLive { get; set; } = 3600; // TTL in seconds
}
