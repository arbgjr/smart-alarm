using SmartAlarm.Domain.Entities;

namespace SmartAlarm.AlarmService.Infrastructure.DistributedProcessing
{
    /// <summary>
    /// Interface para processamento distribuído de alarmes
    /// </summary>
    public interface IDistributedAlarmProcessor
    {
        /// <summary>
        /// Processa um alarme de forma distribuída
        /// </summary>
        Task<AlarmProcessingResult> ProcessAlarmAsync(
            Guid alarmId,
            Guid userId,
            string triggerType = "scheduled",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Agenda processamento de alarme para execução futura
        /// </summary>
        Task<string> ScheduleAlarmProcessingAsync(
            Guid alarmId,
            DateTime scheduledTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela processamento agendado de alarme
        /// </summary>
        Task<bool> CancelScheduledAlarmAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém status de processamento de alarme
        /// </summary>
        Task<AlarmProcessingStatus> GetProcessingStatusAsync(
            Guid alarmId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista alarmes em processamento
        /// </summary>
        Task<IEnumerable<AlarmProcessingInfo>> GetActiveProcessingAlarmsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Resultado do processamento de alarme
    /// </summary>
    public record AlarmProcessingResult(
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
    /// Status de processamento de alarme
    /// </summary>
    public enum AlarmProcessingStatus
    {
        Scheduled,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Retrying
    }

    /// <summary>
    /// Informações de processamento de alarme
    /// </summary>
    public record AlarmProcessingInfo(
        Guid AlarmId,
        Guid UserId,
        AlarmProcessingStatus Status,
        DateTime ScheduledTime,
        DateTime? StartedAt,
        DateTime? CompletedAt,
        int RetryCount,
        string? LastError
    );

    /// <summary>
    /// Métricas de processamento
    /// </summary>
    public record AlarmProcessingMetrics(
        TimeSpan QueueTime,
        TimeSpan ExecutionTime,
        int RetryCount,
        string ProcessingNode,
        IEnumerable<string> ComponentsInvolved
    );
}
