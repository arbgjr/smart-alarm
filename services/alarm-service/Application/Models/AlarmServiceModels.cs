using SmartAlarm.AlarmService.Infrastructure.DistributedProcessing;
using SmartAlarm.AlarmService.Infrastructure.Queues;

namespace SmartAlarm.AlarmService.Application.Models
{
    /// <summary>
    /// Response para processamento distribuído de alarme
    /// </summary>
    public record ProcessDistributedAlarmResponse(
        Guid AlarmId,
        Guid UserId,
        bool Success,
        string Message,
        DateTime ProcessedAt,
        TimeSpan ProcessingDuration,
        IEnumerable<string> ActionsExecuted,
        IEnumerable<string> Notifications,
        AlarmProcessingMetrics Metrics
    );

    /// <summary>
    /// Response para estatísticas da fila
    /// </summary>
    public record GetQueueStatisticsResponse(
        string QueueName,
        QueueStatistics Statistics,
        IEnumerable<AlarmQueueItem> QueuedAlarms,
        QueueHealthStatus HealthStatus,
        IEnumerable<string> Recommendations,
        DateTime RetrievedAt
    );

    /// <summary>
    /// Status de saúde da fila
    /// </summary>
    public record QueueHealthStatus(
        string Status, // "healthy", "warning", "degraded", "critical"
        double Score, // 0-100
        DateTime LastChecked,
        IEnumerable<string> Issues
    );

    /// <summary>
    /// Response para alarmes em processamento ativo
    /// </summary>
    public record GetActiveProcessingAlarmsResponse(
        IEnumerable<AlarmProcessingInfo> ActiveAlarms,
        int TotalCount,
        DateTime RetrievedAt,
        ProcessingStatistics Statistics
    );

    /// <summary>
    /// Estatísticas de processamento
    /// </summary>
    public record ProcessingStatistics(
        int ScheduledCount,
        int ProcessingCount,
        int RetryingCount,
        double AverageProcessingTimeSeconds,
        string MostActiveNode
    );

    /// <summary>
    /// Response para disparo de alarme
    /// </summary>
    public record TriggerAlarmResponse(
        Guid AlarmId,
        Guid UserId,
        bool Success,
        string Message,
        DateTime TriggeredAt,
        IEnumerable<string> ActionsExecuted,
        IEnumerable<string> Notifications
    );

    /// <summary>
    /// Response para atualização de status de alarme
    /// </summary>
    public record UpdateAlarmStatusResponse(
        Guid AlarmId,
        bool IsActive,
        string Message,
        DateTime UpdatedAt,
        string? NextScheduledTime
    );

    /// <summary>
    /// Response para listagem de alarmes do usuário
    /// </summary>
    public record ListUserAlarmsResponse(
        Guid UserId,
        IEnumerable<AlarmSummary> Alarms,
        int TotalCount,
        int Page,
        int PageSize,
        DateTime RetrievedAt
    );

    /// <summary>
    /// Resumo de alarme para listagem
    /// </summary>
    public record AlarmSummary(
        Guid Id,
        string Name,
        TimeSpan Time,
        bool Enabled,
        DateTime CreatedAt,
        DateTime? LastTriggeredAt,
        string Status, // "active", "inactive", "processing", "failed"
        IEnumerable<string> DaysOfWeek
    );

    /// <summary>
    /// Response para busca de alarme por ID
    /// </summary>
    public record GetAlarmByIdResponse(
        Guid Id,
        Guid UserId,
        string Name,
        TimeSpan Time,
        bool Enabled,
        string? Description,
        DateTime CreatedAt,
        DateTime? LastTriggeredAt,
        IEnumerable<AlarmScheduleInfo> Schedules,
        AlarmStatistics Statistics
    );

    /// <summary>
    /// Informações de agendamento do alarme
    /// </summary>
    public record AlarmScheduleInfo(
        Guid Id,
        bool IsActive,
        IEnumerable<string> DaysOfWeek,
        DateTime? StartDate,
        DateTime? EndDate
    );

    /// <summary>
    /// Estatísticas do alarme
    /// </summary>
    public record AlarmStatistics(
        int TotalTriggers,
        int SuccessfulTriggers,
        int FailedTriggers,
        DateTime? LastSuccessfulTrigger,
        DateTime? LastFailedTrigger,
        double SuccessRate
    );
}
