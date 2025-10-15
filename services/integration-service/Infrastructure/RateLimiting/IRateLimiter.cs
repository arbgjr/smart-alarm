namespace SmartAlarm.IntegrationService.Infrastructure.RateLimiting
{
    /// <summary>
    /// Interface para rate limiting de APIs externas
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// Verifica se uma requisição pode ser executada
        /// </summary>
        Task<RateLimitResult> CheckRateLimitAsync(
            string key,
            string provider,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra uma requisição executada
        /// </summary>
        Task RecordRequestAsync(
            string key,
            string provider,
            bool success,
            TimeSpan duration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém estatísticas de rate limiting
        /// </summary>
        Task<RateLimitStatistics> GetStatisticsAsync(
            string? provider = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reseta rate limit para uma chave específica
        /// </summary>
        Task ResetRateLimitAsync(
            string key,
            string provider,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém configuração de rate limit para um provedor
        /// </summary>
        RateLimitConfiguration GetConfiguration(string provider);
    }

    /// <summary>
    /// Resultado da verificação de rate limit
    /// </summary>
    public record RateLimitResult(
        bool IsAllowed,
        int RequestsRemaining,
        TimeSpan ResetTime,
        string? ReasonDenied = null
    );

    /// <summary>
    /// Estatísticas de rate limiting
    /// </summary>
    public record RateLimitStatistics(
        Dictionary<string, ProviderStatistics> ProviderStats,
        int TotalRequestsBlocked,
        int TotalRequestsAllowed,
        DateTime LastReset
    );

    /// <summary>
    /// Estatísticas por provedor
    /// </summary>
    public record ProviderStatistics(
        string Provider,
        int RequestsInWindow,
        int RequestsBlocked,
        int RequestsAllowed,
        TimeSpan AverageResponseTime,
        DateTime WindowStart
    );

    /// <summary>
    /// Configuração de rate limit
    /// </summary>
    public record RateLimitConfiguration(
        string Provider,
        int RequestsPerWindow,
        TimeSpan WindowDuration,
        int BurstLimit,
        TimeSpan BurstWindow,
        bool EnableAdaptiveRateLimit = false
    );
}
