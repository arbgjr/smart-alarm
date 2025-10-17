using MediatR;
using SmartAlarm.IntegrationService.Infrastructure.RateLimiting;

namespace SmartAlarm.IntegrationService.Application.Queries
{
    /// <summary>
    /// Query para obter estatísticas de rate limiting
    /// </summary>
    public record GetRateLimitingStatisticsQuery(
        string? Provider = null
    ) : IRequest<GetRateLimitingStatisticsResponse>;

    /// <summary>
    /// Response das estatísticas de rate limiting
    /// </summary>
    public record GetRateLimitingStatisticsResponse(
        RateLimitStatistics Statistics,
        IEnumerable<ProviderRateLimitInfo> ProviderDetails,
        DateTime RetrievedAt
    );

    /// <summary>
    /// Informações de rate limit por provedor
    /// </summary>
    public record ProviderRateLimitInfo(
        string Provider,
        RateLimitConfiguration Configuration,
        int CurrentRequests,
        int RemainingRequests,
        TimeSpan WindowResetTime,
        bool IsThrottled
    );
}
