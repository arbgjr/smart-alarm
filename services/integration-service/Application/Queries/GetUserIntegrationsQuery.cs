using MediatR;

namespace SmartAlarm.IntegrationService.Application.Queries
{
    /// <summary>
    /// Query para obter integrações do usuário
    /// </summary>
    public record GetUserIntegrationsQuery(
        Guid UserId,
        string? ProviderFilter = null,
        bool IncludeInactive = false,
        bool IncludeStatistics = true
    ) : IRequest<GetUserIntegrationsResponse>;

    /// <summary>
    /// Response das integrações do usuário
    /// </summary>
    public record GetUserIntegrationsResponse(
        Guid UserId,
        IEnumerable<IntegrationSummary> Integrations,
        int TotalCount,
        DateTime RetrievedAt
    );

    /// <summary>
    /// Resumo de integração
    /// </summary>
    public record IntegrationSummary(
        Guid Id,
        string Provider,
        string Type,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? LastSyncAt,
        IntegrationStatistics? Statistics = null
    );

    /// <summary>
    /// Estatísticas de integração
    /// </summary>
    public record IntegrationStatistics(
        int TotalSyncs,
        int SuccessfulSyncs,
        int FailedSyncs,
        DateTime? LastSuccessfulSync,
        DateTime? LastFailedSync,
        double SuccessRate
    );
}
