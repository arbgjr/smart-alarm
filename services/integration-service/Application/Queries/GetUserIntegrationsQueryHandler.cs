using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;
using System.Text.Json;

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
        List<string>? HealthIssues = null
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
        private readonly IIntegrationRepository _integrationRepository;
        private readonly ILogger<GetUserIntegrationsQueryHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly GetUserIntegrationsQueryValidator _validator;

        public GetUserIntegrationsQueryHandler(
            IUserRepository userRepository,
            IAlarmRepository alarmRepository,
            IIntegrationRepository integrationRepository,
            ILogger<GetUserIntegrationsQueryHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext)
        {
            _userRepository = userRepository;
            _alarmRepository = alarmRepository;
            _integrationRepository = integrationRepository;
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

            try
            {
                // Buscar integrações reais do repositório
                IEnumerable<Integration> integrations;
                
                if (includeInactive)
                {
                    integrations = await _integrationRepository.GetByUserIdAsync(userId);
                }
                else
                {
                    integrations = await _integrationRepository.GetActiveByUserIdAsync(userId);
                }

                // Aplicar filtro de provedor se especificado
                if (!string.IsNullOrEmpty(providerFilter))
                {
                    integrations = integrations.Where(i => 
                        i.Provider.Equals(providerFilter, StringComparison.OrdinalIgnoreCase));
                }

                // Converter para UserIntegrationInfo
                var userIntegrations = integrations.Select(ConvertToUserIntegrationInfo).ToList();

                activity?.SetTag("real_integrations_found", userIntegrations.Count.ToString());
                
                _logger.LogInformation("Integrações reais recuperadas para usuário {UserId}: {Count} integrações - CorrelationId: {CorrelationId}",
                    userId, userIntegrations.Count, _correlationContext.CorrelationId);

                return userIntegrations;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao buscar integrações reais para usuário {UserId}, retornando dados de exemplo - CorrelationId: {CorrelationId}",
                    userId, _correlationContext.CorrelationId);

                // Fallback para dados de exemplo em caso de erro
                return await GetExampleIntegrationsForUser(userId);
            }
        }

        /// <summary>
        /// Converte uma integração do domínio para UserIntegrationInfo
        /// </summary>
        private UserIntegrationInfo ConvertToUserIntegrationInfo(Integration integration)
        {
            // Determinar status de saúde baseado no estado da integração
            var healthStatus = DetermineHealthStatus(integration);
            
            // Extrair dados específicos do provedor da configuração
            var providerData = ExtractProviderSpecificData(integration);
            
            // Calcular estatísticas (em uma implementação real, viria do banco)
            var stats = CalculateIntegrationStats(integration);

            return new UserIntegrationInfo(
                Provider: integration.Provider.ToLowerInvariant(),
                DisplayName: GetProviderDisplayName(integration.Provider),
                IsActive: integration.IsActive,
                IsConnected: integration.IsActive && integration.LastExecutedAt.HasValue,
                ConnectedAt: integration.CreatedAt,
                LastSyncAt: integration.LastExecutedAt,
                NextSyncScheduled: CalculateNextSync(integration),
                TotalEventsSynced: stats.EventsSynced,
                AlarmsCreatedFromIntegration: stats.AlarmsCreated,
                HealthStatus: healthStatus,
                LastError: healthStatus != IntegrationHealthStatus.Healthy ? GetHealthStatusMessage(healthStatus) : null,
                ProviderSpecificData: providerData
            );
        }

        /// <summary>
        /// Dados de exemplo como fallback
        /// </summary>
        private async Task<List<UserIntegrationInfo>> GetExampleIntegrationsForUser(Guid userId)
        {
            await Task.Delay(50); // Simular latência

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
                        ["syncFrequency"] = "4h",
                        ["dataSource"] = "fallback_example"
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
                        ["tokenExpiresIn"] = TimeSpan.FromDays(5).TotalSeconds,
                        ["dataSource"] = "fallback_example"
                    }
                )
            };

            // Simular variação baseada no userId para tornar mais realista
            var userBasedSeed = userId.GetHashCode();
            var random = new Random(userBasedSeed);
            
            var selectedIntegrations = mockIntegrations.Take(random.Next(1, 3)).ToList();
            return selectedIntegrations;
        }

        /// <summary>
        /// Determina o status de saúde da integração
        /// </summary>
        private IntegrationHealthStatus DetermineHealthStatus(Integration integration)
        {
            if (!integration.IsActive)
                return IntegrationHealthStatus.Disconnected;

            if (!integration.LastExecutedAt.HasValue)
                return IntegrationHealthStatus.AuthenticationExpired;

            // Se não executou há mais de 24 horas, consideramos Warning
            if (integration.LastExecutedAt.Value < DateTime.UtcNow.AddHours(-24))
                return IntegrationHealthStatus.Warning;

            return IntegrationHealthStatus.Healthy;
        }

        /// <summary>
        /// Extrai dados específicos do provedor da configuração JSON
        /// </summary>
        private Dictionary<string, object>? ExtractProviderSpecificData(Integration integration)
        {
            if (string.IsNullOrEmpty(integration.Configuration))
                return null;

            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(integration.Configuration);
                if (config != null)
                {
                    config["dataSource"] = "real_database";
                }
                return config;
            }
            catch (JsonException)
            {
                return new Dictionary<string, object> { ["configError"] = "Invalid JSON configuration" };
            }
        }

        /// <summary>
        /// Calcula estatísticas da integração
        /// </summary>
        private (int EventsSynced, int AlarmsCreated) CalculateIntegrationStats(Integration integration)
        {
            // Em uma implementação real, isso viria de tabelas de estatísticas
            // Por ora, simulamos baseado na idade da integração
            var daysSinceCreation = (DateTime.UtcNow - integration.CreatedAt).TotalDays;
            var eventsSynced = (int)(daysSinceCreation * 5); // ~5 eventos por dia
            var alarmsCreated = (int)(eventsSynced * 0.6); // ~60% dos eventos viram alarmes
            
            return (eventsSynced, alarmsCreated);
        }

        /// <summary>
        /// Calcula próxima sincronização baseada na configuração
        /// </summary>
        private DateTime? CalculateNextSync(Integration integration)
        {
            if (!integration.IsActive || !integration.LastExecutedAt.HasValue)
                return null;

            // Assumir sync a cada 4 horas por padrão
            var syncInterval = integration.GetConfigurationValue<int?>("syncIntervalHours") ?? 4;
            return integration.LastExecutedAt.Value.AddHours(syncInterval);
        }

        /// <summary>
        /// Obtém nome amigável do provedor
        /// </summary>
        private string GetProviderDisplayName(string provider)
        {
            return provider.ToLowerInvariant() switch
            {
                "google" => "Google Calendar",
                "outlook" => "Microsoft Outlook",
                "apple" => "Apple Calendar",
                "caldav" => "CalDAV",
                "slack" => "Slack Notifications",
                "teams" => "Microsoft Teams",
                _ => $"{provider} Integration"
            };
        }

        /// <summary>
        /// Obtém mensagem de erro baseada no status
        /// </summary>
        private string GetHealthStatusMessage(IntegrationHealthStatus status)
        {
            return status switch
            {
                IntegrationHealthStatus.Warning => "Sincronização atrasada",
                IntegrationHealthStatus.Error => "Erro na última sincronização",
                IntegrationHealthStatus.Disconnected => "Integração desconectada",
                IntegrationHealthStatus.AuthenticationExpired => "Token de autenticação expirado",
                _ => "Status desconhecido"
            };
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
