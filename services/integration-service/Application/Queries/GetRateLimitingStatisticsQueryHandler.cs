using MediatR;
using SmartAlarm.IntegrationService.Infrastructure.RateLimiting;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using System.Diagnostics;

namespace SmartAlarm.IntegrationService.Application.Queries
{
    /// <summary>
    /// Handler para obter estatísticas de rate limiting
    /// </summary>
    public class GetRateLimitingStatisticsQueryHandler : IRequestHandler<GetRateLimitingStatisticsQuery, GetRateLimitingStatisticsResponse>
    {
        private readonly IRateLimiter _rateLimiter;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<GetRateLimitingStatisticsQueryHandler> _logger;

        public GetRateLimitingStatisticsQueryHandler(
            IRateLimiter rateLimiter,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<GetRateLimitingStatisticsQueryHandler> logger)
        {
            _rateLimiter = rateLimiter;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<GetRateLimitingStatisticsResponse> Handle(GetRateLimitingStatisticsQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("GetRateLimitingStatisticsQueryHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("provider", request.Provider ?? "all");
                activity?.SetTag("operation", "get_rate_limiting_statistics");
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Obtendo estatísticas de rate limiting - Provider: {Provider} - CorrelationId: {CorrelationId}",
                    request.Provider ?? "all", _correlationContext.CorrelationId);

                // Obter estatísticas gerais
                var statistics = await _rateLimiter.GetStatisticsAsync(request.Provider, cancellationToken);

                // Obter detalhes por provedor
                var providerDetails = await GetProviderDetailsAsync(request.Provider, cancellationToken);

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_rate_limiting_statistics", "success", "200");

                var response = new GetRateLimitingStatisticsResponse(
                    Statistics: statistics,
                    ProviderDetails: providerDetails,
                    RetrievedAt: DateTime.UtcNow
                );

                _logger.LogInformation("Estatísticas de rate limiting obtidas - Providers: {ProviderCount} em {Duration}ms - CorrelationId: {CorrelationId}",
                    providerDetails.Count(), stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_rate_limiting_statistics", "error", "500");
                _meter.IncrementErrorCount("query", "get_rate_limiting_statistics", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro inesperado ao obter estatísticas de rate limiting - CorrelationId: {CorrelationId}",
                    _correlationContext.CorrelationId);

                throw;
            }
        }

        private async Task<IEnumerable<ProviderRateLimitInfo>> GetProviderDetailsAsync(string? providerFilter, CancellationToken cancellationToken)
        {
            var providers = string.IsNullOrEmpty(providerFilter)
                ? new[] { "google", "microsoft", "apple", "caldav" }
                : new[] { providerFilter };

            var providerDetails = new List<ProviderRateLimitInfo>();

            foreach (var provider in providers)
            {
                try
                {
                    var config = _rateLimiter.GetConfiguration(provider);

                    // Simular verificação de status atual (em produção, seria obtido do cache/Redis)
                    var testKey = $"test:{provider}";
                    var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(testKey, provider, cancellationToken);

                    var providerInfo = new ProviderRateLimitInfo(
                        Provider: provider,
                        Configuration: config,
                        CurrentRequests: Random.Shared.Next(0, config.RequestsPerWindow),
                        RemainingRequests: rateLimitResult.RequestsRemaining,
                        WindowResetTime: rateLimitResult.ResetTime,
                        IsThrottled: !rateLimitResult.IsAllowed
                    );

                    providerDetails.Add(providerInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao obter detalhes do provedor {Provider}", provider);
                }
            }

            return providerDetails;
        }
    }
}
