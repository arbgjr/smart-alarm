namespace SmartAlarm.IntegrationService.Infrastructure.Webhooks
{
    /// <summary>
    /// Interface para gerenciamento de webhooks
    /// </summary>
    public interface IWebhookManager
    {
        /// <summary>
        /// Registra um novo webhook
        /// </summary>
        Task<WebhookRegistration> RegisterWebhookAsync(
            WebhookRegistrationRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove um webhook
        /// </summary>
        Task<bool> UnregisterWebhookAsync(
            Guid webhookId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista webhooks ativos
        /// </summary>
        Task<IEnumerable<WebhookRegistration>> GetActiveWebhooksAsync(
            Guid? userId = null,
            string? provider = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processa webhook recebido
        /// </summary>
        Task<WebhookProcessingResult> ProcessIncomingWebhookAsync(
            string webhookId,
            string payload,
            Dictionary<string, string> headers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida assinatura do webhook
        /// </summary>
        Task<bool> ValidateWebhookSignatureAsync(
            string webhookId,
            string payload,
            string signature,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém estatísticas de webhooks
        /// </summary>
        Task<WebhookStatistics> GetWebhookStatisticsAsync(
            Guid? userId = null,
            TimeSpan? timeWindow = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reativa webhook após falha
        /// </summary>
        Task<bool> ReactivateWebhookAsync(
            Guid webhookId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Solicitação de registro de webhook
    /// </summary>
    public record WebhookRegistrationRequest(
        Guid UserId,
        string Provider,
        string EventType,
        string CallbackUrl,
        Dictionary<string, string>? Configuration = null,
        TimeSpan? ExpirationTime = null,
        string? SecretKey = null
    );

    /// <summary>
    /// Registro de webhook
    /// </summary>
    public record WebhookRegistration(
        Guid Id,
        Guid UserId,
        string Provider,
        string EventType,
        string CallbackUrl,
        WebhookStatus Status,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        DateTime? LastTriggeredAt,
        int TriggerCount,
        Dictionary<string, string> Configuration,
        string? SecretKey = null
    );

    /// <summary>
    /// Status do webhook
    /// </summary>
    public enum WebhookStatus
    {
        Active,
        Inactive,
        Failed,
        Expired,
        Suspended
    }

    /// <summary>
    /// Resultado do processamento de webhook
    /// </summary>
    public record WebhookProcessingResult(
        Guid WebhookId,
        bool Success,
        string Message,
        DateTime ProcessedAt,
        TimeSpan ProcessingDuration,
        IEnumerable<string> ActionsTriggered,
        Dictionary<string, object>? Metadata = null
    );

    /// <summary>
    /// Estatísticas de webhooks
    /// </summary>
    public record WebhookStatistics(
        int TotalWebhooks,
        int ActiveWebhooks,
        int FailedWebhooks,
        int TotalTriggers,
        double AverageProcessingTime,
        DateTime LastActivity,
        Dictionary<string, int> TriggersByProvider,
        Dictionary<string, int> TriggersByEventType
    );
}
