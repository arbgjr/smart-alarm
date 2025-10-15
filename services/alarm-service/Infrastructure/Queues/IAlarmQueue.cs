namespace SmartAlarm.AlarmService.Infrastructure.Queues
{
    /// <summary>
    /// Interface para sistema de filas de alarmes com alta disponibilidade
    /// </summary>
    public interface IAlarmQueue
    {
        /// <summary>
        /// Enfileira um alarme para processamento
        /// </summary>
        Task<string> EnqueueAlarmAsync(
            AlarmQueueItem item,
            TimeSpan? delay = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Desenfileira próximo alarme para processamento
        /// </summary>
        Task<AlarmQueueItem?> DequeueAlarmAsync(
            string queueName = "default",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirma processamento bem-sucedido de um alarme
        /// </summary>
        Task AcknowledgeAsync(
            string messageId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Rejeita processamento de um alarme (para retry)
        /// </summary>
        Task RejectAsync(
            string messageId,
            bool requeue = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém estatísticas da fila
        /// </summary>
        Task<QueueStatistics> GetQueueStatisticsAsync(
            string queueName = "default",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista alarmes na fila
        /// </summary>
        Task<IEnumerable<AlarmQueueItem>> ListQueuedAlarmsAsync(
            string queueName = "default",
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Purga fila de alarmes
        /// </summary>
        Task<int> PurgeQueueAsync(
            string queueName = "default",
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Item da fila de alarmes
    /// </summary>
    public record AlarmQueueItem(
        string Id,
        Guid AlarmId,
        Guid UserId,
        string TriggerType,
        DateTime ScheduledTime,
        DateTime EnqueuedAt,
        int RetryCount = 0,
        Dictionary<string, object>? Metadata = null
    );

    /// <summary>
    /// Estatísticas da fila
    /// </summary>
    public record QueueStatistics(
        string QueueName,
        int TotalMessages,
        int PendingMessages,
        int ProcessingMessages,
        int FailedMessages,
        DateTime LastActivity,
        TimeSpan AverageProcessingTime,
        double ThroughputPerMinute
    );

    /// <summary>
    /// Configurações da fila
    /// </summary>
    public record QueueConfiguration(
        string Name,
        int MaxRetries = 3,
        TimeSpan RetryDelay = default,
        TimeSpan MessageTtl = default,
        bool EnableDeadLetter = true,
        string? DeadLetterQueue = null
    );
}
