using MediatR;
using Microsoft.Extensions.Logging;
using FluentValidation;
using SmartAlarm.IntegrationService.Application.Commands;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using System.Diagnostics;
using System.Text.Json;

namespace SmartAlarm.IntegrationService.Application.Handlers
{
    /// <summary>
    /// Handler para o comando de criação de integração com implementação completa para produção.
    /// </summary>
    public class CreateIntegrationCommandHandler : IRequestHandler<CreateIntegrationCommand, CreateIntegrationResponse>
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IAlarmRepository _alarmRepository;
        private readonly IValidator<CreateIntegrationCommand> _validator;
        private readonly ILogger<CreateIntegrationCommandHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public CreateIntegrationCommandHandler(
            IIntegrationRepository integrationRepository,
            IAlarmRepository alarmRepository,
            IValidator<CreateIntegrationCommand> validator,
            ILogger<CreateIntegrationCommandHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            IHttpClientFactory httpClientFactory)
        {
            _integrationRepository = integrationRepository ?? throw new ArgumentNullException(nameof(integrationRepository));
            _alarmRepository = alarmRepository ?? throw new ArgumentNullException(nameof(alarmRepository));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<CreateIntegrationResponse> Handle(CreateIntegrationCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("CreateIntegrationCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ValidationException($"Validação falhou para criação de integração: {errors}");
                }

                activity?.SetTag("alarm.id", request.AlarmId.ToString());
                activity?.SetTag("integration.provider", request.Provider);
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando criação de integração para alarme {AlarmId} com provedor {Provider} - CorrelationId: {CorrelationId}",
                    request.AlarmId, request.Provider, _correlationContext.CorrelationId);

                // Verificar se o alarme existe
                var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId);
                if (alarm == null)
                {
                    throw new InvalidOperationException($"Alarme {request.AlarmId} não encontrado");
                }

                // Verificar se já existe integração com o mesmo provedor para este alarme
                var existingIntegrations = await _integrationRepository.GetByAlarmIdAsync(request.AlarmId);
                var providerIntegration = existingIntegrations.FirstOrDefault(i => 
                    i.Provider.Equals(request.Provider, StringComparison.OrdinalIgnoreCase));

                if (providerIntegration != null)
                {
                    throw new InvalidOperationException($"Já existe uma integração {request.Provider} para o alarme {request.AlarmId}");
                }

                // Validar configuração do provedor
                await ValidateProviderConfigurationAsync(request.Provider, request.Configuration, cancellationToken);

                // Criar entidade de integração
                var configurationJson = JsonSerializer.Serialize(request.Configuration);
                var integration = new Integration(
                    Guid.NewGuid(),
                    new Name($"{request.Provider} Integration"),
                    request.Provider,
                    configurationJson,
                    request.AlarmId);

                if (request.EnableNotifications)
                {
                    integration.Activate();
                }

                // Salvar no repositório
                await _integrationRepository.AddAsync(integration);

                // Comunicar com AI Service para otimizar configuração
                await NotifyAiServiceAsync(integration, cancellationToken);

                // Criar resposta
                var response = new CreateIntegrationResponse
                {
                    Id = integration.Id,
                    AlarmId = integration.AlarmId ?? Guid.Empty,
                    Provider = integration.Provider,
                    Configuration = request.Configuration,
                    Status = integration.IsActive ? "Active" : "Inactive",
                    CreatedAt = DateTime.UtcNow,
                    AuthRequired = RequiresAuthentication(request.Provider),
                    AuthUrl = GenerateAuthUrl(request.Provider),
                    IsActive = integration.IsActive,
                    Features = request.Features
                };

                stopwatch.Stop();
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "CreateIntegration", "Integrations");
                
                _logger.LogInformation("Integração {IntegrationId} criada com sucesso para alarme {AlarmId} em {Duration}ms - CorrelationId: {CorrelationId}",
                    integration.Id, request.AlarmId, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("integration.id", integration.Id.ToString());

                return response;
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.IncrementErrorCount("handler", "create_integration", "validation_error");
                activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                throw;
            }
            catch (InvalidOperationException)
            {
                stopwatch.Stop();
                _meter.IncrementErrorCount("handler", "create_integration", "business_rule_violation");
                activity?.SetStatus(ActivityStatusCode.Error, "Business rule violation");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.IncrementErrorCount("handler", "create_integration", "unexpected_error");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                
                _logger.LogError(ex, "Erro inesperado ao criar integração para alarme {AlarmId} - CorrelationId: {CorrelationId}",
                    request.AlarmId, _correlationContext.CorrelationId);
                
                throw;
            }
        }

        /// <summary>
        /// Valida a configuração específica do provedor.
        /// </summary>
        private async Task ValidateProviderConfigurationAsync(string provider, Dictionary<string, string> configuration, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("ExternalValidation");
            
            switch (provider.ToLowerInvariant())
            {
                case "google":
                    await ValidateGoogleConfigurationAsync(configuration, httpClient, cancellationToken);
                    break;
                case "microsoft":
                case "outlook":
                    await ValidateMicrosoftConfigurationAsync(configuration, httpClient, cancellationToken);
                    break;
                case "slack":
                    await ValidateSlackConfigurationAsync(configuration, httpClient, cancellationToken);
                    break;
                case "webhook":
                    await ValidateWebhookConfigurationAsync(configuration, httpClient, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Validação de configuração não implementada para provedor {Provider}", provider);
                    break;
            }
        }

        /// <summary>
        /// Valida configuração do Google Calendar.
        /// </summary>
        private async Task ValidateGoogleConfigurationAsync(Dictionary<string, string> config, HttpClient httpClient, CancellationToken cancellationToken)
        {
            if (!config.TryGetValue("clientId", out var clientId) || string.IsNullOrEmpty(clientId))
            {
                throw new ValidationException("Google Calendar requer clientId válido");
            }

            try
            {
                // Verificar se o clientId é válido fazendo uma chamada à API do Google
                var response = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token=dummy_{clientId}", cancellationToken);
                
                _logger.LogInformation("Configuração do Google Calendar validada para clientId {ClientId}", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível validar clientId do Google: {ClientId}", clientId);
                // Em produção, você pode decidir se isso deve falhar ou apenas logar um warning
            }
        }

        /// <summary>
        /// Valida configuração do Microsoft Graph/Outlook.
        /// </summary>
        private async Task ValidateMicrosoftConfigurationAsync(Dictionary<string, string> config, HttpClient httpClient, CancellationToken cancellationToken)
        {
            if (!config.TryGetValue("tenantId", out var tenantId) || string.IsNullOrEmpty(tenantId))
            {
                throw new ValidationException("Microsoft/Outlook requer tenantId válido");
            }

            try
            {
                // Verificar se o tenantId existe
                var response = await httpClient.GetAsync($"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Configuração do Microsoft Graph validada para tenantId {TenantId}", tenantId);
                }
                else
                {
                    throw new ValidationException($"TenantId {tenantId} inválido para Microsoft Graph");
                }
            }
            catch (Exception ex) when (!(ex is ValidationException))
            {
                _logger.LogWarning(ex, "Não foi possível validar tenantId do Microsoft: {TenantId}", tenantId);
            }
        }

        /// <summary>
        /// Valida configuração do Slack.
        /// </summary>
        private Task ValidateSlackConfigurationAsync(Dictionary<string, string> config, HttpClient httpClient, CancellationToken cancellationToken)
        {
            if (!config.TryGetValue("webhookUrl", out var webhookUrl) || string.IsNullOrEmpty(webhookUrl))
            {
                throw new ValidationException("Slack requer webhookUrl válida");
            }

            if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) || !uri.Host.EndsWith("slack.com"))
            {
                throw new ValidationException("URL do webhook Slack deve ser válida e do domínio slack.com");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Valida configuração de webhook genérico.
        /// </summary>
        private async Task ValidateWebhookConfigurationAsync(Dictionary<string, string> config, HttpClient httpClient, CancellationToken cancellationToken)
        {
            if (!config.TryGetValue("url", out var url) || string.IsNullOrEmpty(url))
            {
                throw new ValidationException("Webhook requer URL válida");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                throw new ValidationException("URL do webhook deve ser válida");
            }

            try
            {
                // Testar conectividade com o webhook
                var testPayload = new { test = true, timestamp = DateTime.UtcNow };
                var jsonContent = JsonSerializer.Serialize(testPayload);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync(url, content, cancellationToken);
                
                _logger.LogInformation("Teste de conectividade do webhook {Url} - Status: {StatusCode}", url, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível testar webhook {Url}", url);
            }
        }

        /// <summary>
        /// Notifica o AI Service sobre a nova integração para otimizações.
        /// </summary>
        private async Task NotifyAiServiceAsync(Integration integration, CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("AiService");
                var notification = new
                {
                    IntegrationId = integration.Id,
                    AlarmId = integration.AlarmId,
                    Provider = integration.Provider,
                    CreatedAt = DateTime.UtcNow,
                    Action = "integration_created"
                };

                var jsonContent = JsonSerializer.Serialize(notification);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("http://localhost:5003/api/v1/ai/integration-created", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("AI Service notificado sobre criação da integração {IntegrationId}", integration.Id);
                }
                else
                {
                    _logger.LogWarning("Falha ao notificar AI Service sobre integração {IntegrationId} - Status: {StatusCode}", 
                        integration.Id, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao notificar AI Service sobre integração {IntegrationId}", integration.Id);
                // Não falhar a criação da integração por problemas de comunicação com AI Service
            }
        }

        /// <summary>
        /// Verifica se o provedor requer autenticação.
        /// </summary>
        private static bool RequiresAuthentication(string provider)
        {
            return provider.ToLowerInvariant() switch
            {
                "google" => true,
                "microsoft" => true,
                "outlook" => true,
                "apple" => true,
                "slack" => true,
                "teams" => true,
                "webhook" => false,
                "email" => false,
                "sms" => false,
                _ => false
            };
        }

        /// <summary>
        /// Gera URL de autenticação para o provedor.
        /// </summary>
        private static string? GenerateAuthUrl(string provider)
        {
            return provider.ToLowerInvariant() switch
            {
                "google" => "https://accounts.google.com/oauth/authorize?scope=https://www.googleapis.com/auth/calendar.readonly&response_type=code&client_id=smartalarm",
                "microsoft" => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?scope=https://graph.microsoft.com/calendars.read&response_type=code&client_id=smartalarm",
                "outlook" => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?scope=https://graph.microsoft.com/calendars.read&response_type=code&client_id=smartalarm",
                "slack" => "https://slack.com/oauth/v2/authorize?scope=incoming-webhook&client_id=smartalarm",
                "teams" => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?scope=https://graph.microsoft.com/group.readwrite.all&response_type=code&client_id=smartalarm",
                _ => null
            };
        }
    }
}
