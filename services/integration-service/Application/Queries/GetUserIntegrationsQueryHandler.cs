using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.IntegrationService.Application.Queries
{
    /// <summary>
    /// Query para listar integrações ativas do usuário
    /// </summary>
    public record GetUserIntegrationsQuery(
        Guid UserId,
        string? ProviderFilter = null,
        bool IncludeInactive = false,
        bool IncludeStatistics = true
    ) : IRequest<GetUserIntegrationsResponse>;

    /// <summary>
    /// Response com as integrações do usuário
    /// </summary>
    public record GetUserIntegrationsResponse(
        Guid UserId,
        List<UserIntegrationInfo> Integrations,
        IntegrationStatistics? Statistics = null,
        DateTime RetrievedAt = default
    );

    /// <summary>
    /// Informações de uma integração do usuário
    /// </summary>
    public record UserIntegrationInfo(
        string Provider,
        string DisplayName,
        bool IsActive,
        bool IsConnected,
        DateTime ConnectedAt,
        DateTime? LastSyncAt,
        DateTime? NextSyncScheduled,
        int TotalEventsSynced,
        int AlarmsCreatedFromIntegration,
        IntegrationHealthStatus HealthStatus,
        string? LastError = null,
        Dictionary<string, object>? ProviderSpecificData = null
    );

    /// <summary>
    /// Estatísticas gerais das integrações
    /// </summary>
    public record IntegrationStatistics(
        int TotalActiveIntegrations,
        int TotalConnectedProviders,
        int TotalEventsSynced,
        int TotalAlarmsFromIntegrations,
        DateTime? LastSuccessfulSync,
        List<string> HealthIssues = null
    );

    /// <summary>
    /// Status de saúde da integração
    /// </summary>
    public enum IntegrationHealthStatus
    {
        Healthy = 0,
        Warning = 1,
        Error = 2,
        Disconnected = 3,
        AuthenticationExpired = 4
    }

    /// <summary>
    /// Validator para a query de integrações do usuário
    /// </summary>
    public class GetUserIntegrationsQueryValidator : AbstractValidator<GetUserIntegrationsQuery>
    {
        public GetUserIntegrationsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("ID do usuário é obrigatório para buscar integrações");

            RuleFor(x => x.ProviderFilter)
                .Must(provider => string.IsNullOrEmpty(provider) || 
                               new[] { "google", "outlook", "apple", "caldav", "slack", "teams" }.Contains(provider.ToLowerInvariant()))
                .When(x => !string.IsNullOrEmpty(x.ProviderFilter))
                .WithMessage("Filtro de provedor deve ser um dos suportados: Google, Outlook, Apple, CalDAV, Slack, Teams");
        }
    }

    /// <summary>
    /// Handler para buscar integrações do usuário
    /// </summary>
    public class GetUserIntegrationsQueryHandler : IRequestHandler<GetUserIntegrationsQuery, GetUserIntegrationsResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<GetUserIntegrationsQueryHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly GetUserIntegrationsQueryValidator _validator;

        public GetUserIntegrationsQueryHandler(
            IUserRepository userRepository,
            IAlarmRepository alarmRepository,
            ILogger<GetUserIntegrationsQueryHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext)
        {
            _userRepository = userRepository;
            _alarmRepository = alarmRepository;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _validator = new GetUserIntegrationsQueryValidator();
        }

        public async Task<GetUserIntegrationsResponse> Handle(GetUserIntegrationsQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("GetUserIntegrationsQueryHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ValidationException($"Validação falhou para busca de integrações: {errors}");
                }

                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("provider_filter", request.ProviderFilter ?? "none");
                activity?.SetTag("include_inactive", request.IncludeInactive.ToString());
                
                _logger.LogInformation("Buscando integrações para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);

                // Verificar se usuário existe
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    _meter.IncrementErrorCount("query_handler", "get_user_integrations", "user_not_found");
                    throw new InvalidOperationException($"Usuário {request.UserId} não encontrado");
                }

                // Buscar integrações do usuário
                var integrations = await GetUserIntegrationsFromStorage(request.UserId, request.ProviderFilter, request.IncludeInactive);

                // Aplicar filtros se necessário
                if (!string.IsNullOrEmpty(request.ProviderFilter))
                {
                    integrations = integrations.Where(i => 
                        i.Provider.Equals(request.ProviderFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!request.IncludeInactive)
                {
                    integrations = integrations.Where(i => i.IsActive).ToList();
                }

                activity?.SetTag("integrations_found", integrations.Count.ToString());

                // Calcular estatísticas se solicitado
                IntegrationStatistics? statistics = null;
                if (request.IncludeStatistics)
                {
                    statistics = await CalculateIntegrationStatistics(request.UserId, integrations);
                }

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_user_integrations", "success", "completed");
                _meter.RecordCustomMetric("user_integrations_retrieved", integrations.Count, "user_id", request.UserId.ToString());

                var response = new GetUserIntegrationsResponse(
                    UserId: request.UserId,
                    Integrations: integrations,
                    Statistics: statistics,
                    RetrievedAt: DateTime.UtcNow
                );

                _logger.LogInformation("Integrações recuperadas para usuário {UserId}: {IntegrationCount} integrações encontradas - CorrelationId: {CorrelationId}",
                    request.UserId, integrations.Count, _correlationContext.CorrelationId);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_user_integrations", "error", "exception");
                _meter.IncrementErrorCount("query_handler", "get_user_integrations", "processing_error");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao buscar integrações para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <summary>
        /// Busca integrações do usuário do armazenamento
        /// </summary>
        private async Task<List<UserIntegrationInfo>> GetUserIntegrationsFromStorage(
            Guid userId, 
            string? providerFilter, 
            bool includeInactive)
        {
            using var activity = _activitySource.StartActivity("GetUserIntegrationsQueryHandler.GetUserIntegrationsFromStorage");

            // Em uma implementação real, isso buscaria de um repositório de integrações
            // Por enquanto, simulamos algumas integrações de exemplo
            await Task.Delay(50); // Simular latência de banco de dados

            var mockIntegrations = new List<UserIntegrationInfo>
            {
                new UserIntegrationInfo(
                    Provider: "google",
                    DisplayName: "Google Calendar",
                    IsActive: true,
                    IsConnected: true,
                    ConnectedAt: DateTime.UtcNow.AddDays(-30),
                    LastSyncAt: DateTime.UtcNow.AddHours(-2),
                    NextSyncScheduled: DateTime.UtcNow.AddHours(2),
                    TotalEventsSynced: 145,
                    AlarmsCreatedFromIntegration: 89,
                    HealthStatus: IntegrationHealthStatus.Healthy,
                    ProviderSpecificData: new Dictionary<string, object>
                    {
                        ["calendarId"] = "primary",
                        ["timeZone"] = "America/Sao_Paulo",
                        ["syncFrequency"] = "4h"
                    }
                ),
                new UserIntegrationInfo(
                    Provider: "outlook",
                    DisplayName: "Microsoft Outlook",
                    IsActive: true,
                    IsConnected: true,
                    ConnectedAt: DateTime.UtcNow.AddDays(-15),
                    LastSyncAt: DateTime.UtcNow.AddHours(-6),
                    NextSyncScheduled: DateTime.UtcNow.AddHours(6),
                    TotalEventsSynced: 67,
                    AlarmsCreatedFromIntegration: 34,
                    HealthStatus: IntegrationHealthStatus.Warning,
                    LastError: "Token expirando em 5 dias",
                    ProviderSpecificData: new Dictionary<string, object>
                    {
                        ["mailboxId"] = "user@company.com",
                        ["tokenExpiresIn"] = TimeSpan.FromDays(5).TotalSeconds
                    }
                ),
                new UserIntegrationInfo(
                    Provider: "slack",
                    DisplayName: "Slack Notifications",
                    IsActive: false,
                    IsConnected: false,
                    ConnectedAt: DateTime.UtcNow.AddDays(-60),
                    LastSyncAt: DateTime.UtcNow.AddDays(-10),
                    NextSyncScheduled: null,
                    TotalEventsSynced: 12,
                    AlarmsCreatedFromIntegration: 8,
                    HealthStatus: IntegrationHealthStatus.Disconnected,
                    LastError: "Integração desabilitada pelo usuário"
                ),
                new UserIntegrationInfo(
                    Provider: "apple",
                    DisplayName: "Apple Calendar",
                    IsActive: true,
                    IsConnected: false,
                    ConnectedAt: DateTime.UtcNow.AddDays(-5),
                    LastSyncAt: null,
                    NextSyncScheduled: null,
                    TotalEventsSynced: 0,
                    AlarmsCreatedFromIntegration: 0,
                    HealthStatus: IntegrationHealthStatus.AuthenticationExpired,
                    LastError: "Token de autenticação expirado, reconexão necessária"
                )
            };

            // Simular variação baseada no userId
            var userBasedSeed = userId.GetHashCode();
            var random = new Random(userBasedSeed);
            
            // Randomizar algumas propriedades para tornar mais realista
            foreach (var integration in mockIntegrations.Take(random.Next(1, 4)))
            {
                if (integration.IsActive && integration.IsConnected)
                {
                    // Variar os números baseado no usuário
                    var multiplier = (userBasedSeed % 3) + 1;
                    integration = integration with
                    {
                        TotalEventsSynced = integration.TotalEventsSynced * multiplier,
                        AlarmsCreatedFromIntegration = integration.AlarmsCreatedFromIntegration * multiplier
                    };
                }
            }

            activity?.SetTag("mock_integrations_generated", mockIntegrations.Count.ToString());
            return mockIntegrations;
        }

        /// <summary>
        /// Calcula estatísticas das integrações do usuário
        /// </summary>
        private async Task<IntegrationStatistics> CalculateIntegrationStatistics(
            Guid userId, 
            List<UserIntegrationInfo> integrations)
        {
            using var activity = _activitySource.StartActivity("GetUserIntegrationsQueryHandler.CalculateIntegrationStatistics");

            await Task.Delay(25); // Simular cálculo

            var activeIntegrations = integrations.Where(i => i.IsActive).ToList();
            var connectedProviders = integrations.Where(i => i.IsConnected).ToList();
            var totalEventsSynced = integrations.Sum(i => i.TotalEventsSynced);
            var totalAlarmsFromIntegrations = integrations.Sum(i => i.AlarmsCreatedFromIntegration);
            var lastSuccessfulSync = integrations
                .Where(i => i.LastSyncAt.HasValue)
                .Max(i => i.LastSyncAt);

            // Identificar problemas de saúde
            var healthIssues = new List<string>();
            foreach (var integration in integrations)
            {
                switch (integration.HealthStatus)
                {
                    case IntegrationHealthStatus.Warning:
                        healthIssues.Add($"{integration.DisplayName}: {integration.LastError ?? "Status de atenção"}");
                        break;
                    case IntegrationHealthStatus.Error:
                        healthIssues.Add($"{integration.DisplayName}: Erro - {integration.LastError ?? "Erro desconhecido"}");
                        break;
                    case IntegrationHealthStatus.Disconnected:
                        healthIssues.Add($"{integration.DisplayName}: Desconectado");
                        break;
                    case IntegrationHealthStatus.AuthenticationExpired:
                        healthIssues.Add($"{integration.DisplayName}: Autenticação expirada");
                        break;
                }
            }

            var statistics = new IntegrationStatistics(
                TotalActiveIntegrations: activeIntegrations.Count,
                TotalConnectedProviders: connectedProviders.Count,
                TotalEventsSynced: totalEventsSynced,
                TotalAlarmsFromIntegrations: totalAlarmsFromIntegrations,
                LastSuccessfulSync: lastSuccessfulSync,
                HealthIssues: healthIssues.Any() ? healthIssues : null
            );

            activity?.SetTag("active_integrations", activeIntegrations.Count.ToString());
            activity?.SetTag("connected_providers", connectedProviders.Count.ToString());
            activity?.SetTag("health_issues", healthIssues.Count.ToString());

            return statistics;
        }
    }
}
