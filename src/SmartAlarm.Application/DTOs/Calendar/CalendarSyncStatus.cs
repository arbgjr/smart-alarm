namespace SmartAlarm.Application.DTOs.Calendar;

public class CalendarSyncStatus
{
    public Guid UserId { get; set; }
    public DateTime LastSyncTime { get; set; }
    public int TotalEvents { get; set; }
    public bool HasErrors { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ProviderSyncStatus> Providers { get; set; } = new();
}

public class ProviderSyncStatus
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public int EventCount { get; set; }
    public string? LastError { get; set; }
}

public enum CalendarSyncAction
{
    Create,
    Update,
    Delete
}
