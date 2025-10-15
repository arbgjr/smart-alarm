using System.Collections.Concurrent;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;

namespace SmartAlarm.IntegrationService.Infrastructure.RateLimiting
{
    /// <summary>
    /// Implementação de rate limiter com sliding window
    /// </summary>
    public class RateLimiter : IRateLimiter
    {
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<RateLimiter> _logger;
        private readonly IConfiguration _configuration;

        // Cache para janelas de rate limiting (em produção, usar Redis)
        private readonly ConcurrentDictionary<string, SlidingWindow> _windows = new();
        private readonly ConcurrentDictionary<string, RateLimitConfiguration> _configurations = new();
        private readonly Timer _cleanupTimer;

        public RateLimiter(
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<RateLimiter> logger,
            IConfiguration configuration)
        {
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
            _configuration = configuration;

            // Inicializar configurações padrão
            InitializeDefaultConfigurations();

            // Timer para limpeza de janelas expiradas
            _cleanupTimer = new Timer(CleanupExpiredWindows, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <inheritdoc />
        public async Task<RateLimitResult> CheckRateLimitAsync(
            string key,
            string provider,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity("RateLimiter.CheckRateLimit");

            try
            {
                activity?.SetTag("rate_limit.key", key);
                activity?.SetTag("rate_limit.provider", provider);
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                var config = GetConfiguration(provider);
                var windowKey = $"{provider}:{key}";

                // Obter ou criar janela deslizante
                var window = _windows.GetOrAdd(windowKey, _ => new SlidingWindow(config.WindowDuration));

                // Verificar rate limit
                var now = DateTime.UtcNow;
                var requestsInWindow = window.CountRequestsInWindow(now);

                activity?.SetTag("rate_limit.requests_in_window", requestsInWindow.ToString());
                activity?.SetTag("rate_limit.limit", config.RequestsPerWindow.ToString());

                if (requestsInWindow >= config.RequestsPerWindow)
                {
                    var resetTime = window.GetWindowResetTime(now);

                    _meter.IncrementRateLimitBlocked(provider, key);

                    _logger.LogWarning("Rate limit excedido para {Provider}:{Key} - Requests: {Requests}/{Limit} - Reset em: {ResetTime} - CorrelationId: {CorrelationId}",
                        provider, key, requestsInWindow, config.RequestsPerWindow, resetTime, _correlationContext.CorrelationId);

                    await Task.CompletedTask;
                    return new RateLimitResult(
                        IsAllowed: false,
                        RequestsRemaining: 0,
                        ResetTime: resetTime,
                        ReasonDenied: $"Rate limit excedido: {requestsInWindow}/{config.RequestsPerWindow} requests"
                    );
                }

                // Verificar burst limit se configurado
                if (config.BurstLimit > 0)
                {
                    var burstRequests = window.CountRequestsInWindow(now, config.BurstWindow);
                    if (burstRequests >= config.BurstLimit)
                    {
                        _meter.IncrementRateLimitBlocked(provider, key);

                        _logger.LogWarning("Burst limit excedido para {Provider}:{Key} - Burst: {Burst}/{Limit} - CorrelationId: {CorrelationId}",
                            provider, key, burstRequests, config.BurstLimit, _correlationContext.CorrelationId);

                        await Task.CompletedTask;
                        return new RateLimitResult(
                            IsAllowed: false,
                            RequestsRemaining: 0,
                            ResetTime: TimeSpan.FromSeconds(config.BurstWindow.TotalSeconds),
                            ReasonDenied: $"Burst limit excedido: {burstRequests}/{config.BurstLimit} requests"
                        );
                    }
                }

                var remaining = Math.Max(0, config.RequestsPerWindow - requestsInWindow - 1);
                var resetTime = window.GetWindowResetTime(now);

                _logger.LogDebug("Rate limit OK para {Provider}:{Key} - Remaining: {Remaining} - CorrelationId: {CorrelationId}",
                    provider, key, remaining, _correlationContext.CorrelationId);

                await Task.CompletedTask;
                return new RateLimitResult(
                    IsAllowed: true,
                    RequestsRemaining: remaining,
                    ResetTime: resetTime
                );
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao verificar rate limit para {Provider}:{Key} - CorrelationId: {CorrelationId}",
                    provider, key, _correlationContext.CorrelationId);

                // Em caso de erro, permitir a requisição (fail-open)
                return new RateLimitResult(true, 100, TimeSpan.FromMinutes(1), "Error in rate limiter - allowing request");
            }
        }

        /// <inheritdoc />
        public async Task RecordRequestAsync(
            string key,
            string provider,
            bool success,
            TimeSpan duration,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var windowKey = $"{provider}:{key}";
                var window = _windows.GetOrAdd(windowKey, _ => new SlidingWindow(GetConfiguration(provider).WindowDuration));

                // Registrar requisição
                window.RecordRequest(DateTime.UtcNow, success, duration);

                // Métricas
                _meter.RecordRateLimitRequest(provider, key, success ? "success" : "failed", duration.TotalMilliseconds);

                _logger.LogDebug("Requisição registrada para {Provider}:{Key} - Success: {Success}, Duration: {Duration}ms",
                    provider, key, success, duration.TotalMilliseconds);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar requisição para {Provider}:{Key}", provider, key);
            }
        }

        /// <inheritdoc />
        public async Task<RateLimitStatistics> GetStatisticsAsync(
            string? provider = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var providerStats = new Dictionary<string, ProviderStatistics>();
                var totalBlocked = 0;
                var totalAllowed = 0;

                var relevantWindows = string.IsNullOrEmpty(provider)
                    ? _windows
                    : _windows.Where(kvp => kvp.Key.StartsWith($"{provider}:"));

                foreach (var kvp in relevantWindows)
                {
                    var parts = kvp.Key.Split(':');
                    if (parts.Length < 2) continue;

                    var windowProvider = parts[0];
                    var window = kvp.Value;
                    var stats = window.GetStatistics();

                    if (!providerStats.ContainsKey(windowProvider))
                    {
                        providerStats[windowProvider] = new ProviderStatistics(
                            Provider: windowProvider,
                            RequestsInWindow: stats.TotalRequests,
                            RequestsBlocked: stats.BlockedRequests,
                            RequestsAllowed: stats.SuccessfulRequests,
                            AverageResponseTime: stats.AverageResponseTime,
                            WindowStart: stats.WindowStart
                        );
                    }
                    else
                    {
                        var existing = providerStats[windowProvider];
                        providerStats[windowProvider] = existing with
                        {
                            RequestsInWindow = existing.RequestsInWindow + stats.TotalRequests,
                            RequestsBlocked = existing.RequestsBlocked + stats.BlockedRequests,
                            RequestsAllowed = existing.RequestsAllowed + stats.SuccessfulRequests
                        };
                    }

                    totalBlocked += stats.BlockedRequests;
                    totalAllowed += stats.SuccessfulRequests;
                }

                await Task.CompletedTask;

                return new RateLimitStatistics(
                    ProviderStats: providerStats,
                    TotalRequestsBlocked: totalBlocked,
                    TotalRequestsAllowed: totalAllowed,
                    LastReset: DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas de rate limiting");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ResetRateLimitAsync(
            string key,
            string provider,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var windowKey = $"{provider}:{key}";

                if (_windows.TryRemove(windowKey, out var window))
                {
                    _logger.LogInformation("Rate limit resetado para {Provider}:{Key}", provider, key);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao resetar rate limit para {Provider}:{Key}", provider, key);
            }
        }

        /// <inheritdoc />
        public RateLimitConfiguration GetConfiguration(string provider)
        {
            if (_configurations.TryGetValue(provider.ToLowerInvariant(), out var config))
            {
                return config;
            }

            // Configuração padrão se não encontrada
            return new RateLimitConfiguration(
                Provider: provider,
                RequestsPerWindow: 100,
                WindowDuration: TimeSpan.FromMinutes(1),
                BurstLimit: 10,
                BurstWindow: TimeSpan.FromSeconds(10)
            );
        }

        #region Métodos Privados

        private void InitializeDefaultConfigurations()
        {
            // Configurações específicas por provedor baseadas em suas limitações reais
            _configurations["google"] = new RateLimitConfiguration(
                Provider: "google",
                RequestsPerWindow: 1000, // Google Calendar API: 1000 requests/100 seconds
                WindowDuration: TimeSpan.FromSeconds(100),
                BurstLimit: 100,
                BurstWindow: TimeSpan.FromSeconds(10)
            );

            _configurations["microsoft"] = new RateLimitConfiguration(
                Provider: "microsoft",
                RequestsPerWindow: 600, // Microsoft Graph: 600 requests/minute
                WindowDuration: TimeSpan.FromMinutes(1),
                BurstLimit: 60,
                BurstWindow: TimeSpan.FromSeconds(10)
            );

            _configurations["apple"] = new RateLimitConfiguration(
                Provider: "apple",
                RequestsPerWindow: 400, // Apple CloudKit: mais restritivo
                WindowDuration: TimeSpan.FromMinutes(1),
                BurstLimit: 40,
                BurstWindow: TimeSpan.FromSeconds(10)
            );

            _configurations["caldav"] = new RateLimitConfiguration(
                Provider: "caldav",
                RequestsPerWindow: 200, // CalDAV: varia por servidor, sendo conservador
                WindowDuration: TimeSpan.FromMinutes(1),
                BurstLimit: 20,
                BurstWindow: TimeSpan.FromSeconds(10)
            );

            // Carregar configurações customizadas do appsettings.json se existirem
            LoadCustomConfigurations();
        }

        private void LoadCustomConfigurations()
        {
            try
            {
                var rateLimitSection = _configuration.GetSection("RateLimiting:Providers");

                foreach (var providerSection in rateLimitSection.GetChildren())
                {
                    var provider = providerSection.Key.ToLowerInvariant();
                    var requestsPerWindow = providerSection.GetValue<int>("RequestsPerWindow", 100);
                    var windowDurationSeconds = providerSection.GetValue<int>("WindowDurationSeconds", 60);
                    var burstLimit = providerSection.GetValue<int>("BurstLimit", 10);
                    var burstWindowSeconds = providerSection.GetValue<int>("BurstWindowSeconds", 10);

                    _configurations[provider] = new RateLimitConfiguration(
                        Provider: provider,
                        RequestsPerWindow: requestsPerWindow,
                        WindowDuration: TimeSpan.FromSeconds(windowDurationSeconds),
                        BurstLimit: burstLimit,
                        BurstWindow: TimeSpan.FromSeconds(burstWindowSeconds)
                    );

                    _logger.LogInformation("Configuração de rate limit carregada para {Provider}: {Requests}/{Window}s",
                        provider, requestsPerWindow, windowDurationSeconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao carregar configurações customizadas de rate limiting");
            }
        }

        private void CleanupExpiredWindows(object? state)
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = new List<string>();

                foreach (var kvp in _windows)
                {
                    if (kvp.Value.IsExpired(now))
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _windows.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("Limpeza de rate limiting: {ExpiredCount} janelas expiradas removidas", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na limpeza de janelas de rate limiting");
            }
        }

        #endregion

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }

    /// <summary>
    /// Implementação de sliding window para rate limiting
    /// </summary>
    internal class SlidingWindow
    {
        private readonly TimeSpan _windowDuration;
        private readonly List<RequestRecord> _requests = new();
        private readonly object _lock = new();

        public SlidingWindow(TimeSpan windowDuration)
        {
            _windowDuration = windowDuration;
        }

        public int CountRequestsInWindow(DateTime now, TimeSpan? customWindow = null)
        {
            var window = customWindow ?? _windowDuration;
            var cutoff = now.Subtract(window);

            lock (_lock)
            {
                // Remover requisições antigas
                _requests.RemoveAll(r => r.Timestamp < cutoff);
                return _requests.Count;
            }
        }

        public void RecordRequest(DateTime timestamp, bool success, TimeSpan duration)
        {
            lock (_lock)
            {
                _requests.Add(new RequestRecord(timestamp, success, duration));

                // Limitar tamanho da lista para evitar vazamento de memória
                if (_requests.Count > 10000)
                {
                    var cutoff = timestamp.Subtract(_windowDuration);
                    _requests.RemoveAll(r => r.Timestamp < cutoff);
                }
            }
        }

        public TimeSpan GetWindowResetTime(DateTime now)
        {
            lock (_lock)
            {
                if (!_requests.Any())
                {
                    return TimeSpan.Zero;
                }

                var oldestRequest = _requests.Min(r => r.Timestamp);
                var windowEnd = oldestRequest.Add(_windowDuration);
                return windowEnd > now ? windowEnd - now : TimeSpan.Zero;
            }
        }

        public bool IsExpired(DateTime now)
        {
            lock (_lock)
            {
                if (!_requests.Any())
                {
                    return true;
                }

                var newestRequest = _requests.Max(r => r.Timestamp);
                return now.Subtract(newestRequest) > _windowDuration.Add(TimeSpan.FromMinutes(5)); // 5 min grace period
            }
        }

        public WindowStatistics GetStatistics()
        {
            lock (_lock)
            {
                var totalRequests = _requests.Count;
                var successfulRequests = _requests.Count(r => r.Success);
                var blockedRequests = totalRequests - successfulRequests;
                var averageResponseTime = _requests.Any()
                    ? TimeSpan.FromMilliseconds(_requests.Average(r => r.Duration.TotalMilliseconds))
                    : TimeSpan.Zero;
                var windowStart = _requests.Any() ? _requests.Min(r => r.Timestamp) : DateTime.UtcNow;

                return new WindowStatistics(
                    TotalRequests: totalRequests,
                    SuccessfulRequests: successfulRequests,
                    BlockedRequests: blockedRequests,
                    AverageResponseTime: averageResponseTime,
                    WindowStart: windowStart
                );
            }
        }
    }

    /// <summary>
    /// Registro de requisição na janela
    /// </summary>
    internal record RequestRecord(DateTime Timestamp, bool Success, TimeSpan Duration);

    /// <summary>
    /// Estatísticas da janela
    /// </summary>
    internal record WindowStatistics(
        int TotalRequests,
        int SuccessfulRequests,
        int BlockedRequests,
        TimeSpan AverageResponseTime,
        DateTime WindowStart
    );
}
