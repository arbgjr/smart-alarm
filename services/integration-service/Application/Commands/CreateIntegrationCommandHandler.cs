using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;
using System.Text.Json;

namespace SmartAlarm.IntegrationService.Application.Commands
{
    /// <summary>
    /// Validator para o comando de criação de integração
    /// </summary>
    public class CreateIntegrationCommandValidator : AbstractValidator<CreateIntegrationCommand>
    {
        public CreateIntegrationCommandValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty()
                .WithMessage("AlarmId é obrigatório");

            RuleFor(x => x.Provider)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Provider é obrigatório e deve ter no máximo 100 caracteres");

            RuleFor(x => x.Configuration)
                .NotNull()
                .WithMessage("Configuration não pode ser nulo");

            RuleFor(x => x.Features)
                .NotNull()
                .WithMessage("Features não pode ser nulo");
        }
    }

    /// <summary>
    /// Handler para criar uma nova integração para um alarme específico
    /// </summary>
    public class CreateIntegrationCommandHandler : IRequestHandler<CreateIntegrationCommand, CreateIntegrationResponse>
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<CreateIntegrationCommandHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly CreateIntegrationCommandValidator _validator;

        public CreateIntegrationCommandHandler(
            IIntegrationRepository integrationRepository,
            IAlarmRepository alarmRepository,
            ILogger<CreateIntegrationCommandHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext)
        {
            _integrationRepository = integrationRepository;
            _alarmRepository = alarmRepository;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _validator = new CreateIntegrationCommandValidator();
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
                activity?.SetTag("operation", "create_integration");
                
                _logger.LogInformation("Criando integração para alarme {AlarmId} com provedor {Provider} - CorrelationId: {CorrelationId}",
                    request.AlarmId, request.Provider, _correlationContext.CorrelationId);

                // Verificar se o alarme existe
                var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId);
                if (alarm == null)
                {
                    _meter.IncrementErrorCount("command_handler", "create_integration", "alarm_not_found");
                    throw new InvalidOperationException($"Alarme {request.AlarmId} não encontrado");
                }

                // Verificar se já existe uma integração ativa para este alarme com o mesmo provedor
                var existingIntegrations = await _integrationRepository.GetByAlarmIdAsync(request.AlarmId);
                var existingProviderIntegration = existingIntegrations.FirstOrDefault(i => 
                    i.Provider.Equals(request.Provider, StringComparison.OrdinalIgnoreCase) && i.IsActive);

                if (existingProviderIntegration != null)
                {
                    _logger.LogWarning("Integração já existe para alarme {AlarmId} com provedor {Provider}", 
                        request.AlarmId, request.Provider);
                    
                    // Retornar integração existente
                    return MapToResponse(existingProviderIntegration, false);
                }

                // Gerar nome da integração baseado no provedor
                var integrationName = GenerateIntegrationName(request.Provider, alarm.Name.ToString());

                // Serializar configuração
                var configurationJson = JsonSerializer.Serialize(request.Configuration);

                // Criar nova integração
                var integration = new Integration(
                    Guid.NewGuid(),
                    new Name(integrationName),
                    request.Provider,
                    configurationJson,
                    request.AlarmId
                );

                await _integrationRepository.AddAsync(integration);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_integration", "success", "created");

                _logger.LogInformation("Integração criada com sucesso: {IntegrationId} para alarme {AlarmId} em {Duration}ms - CorrelationId: {CorrelationId}",
                    integration.Id, request.AlarmId, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return MapToResponse(integration, true);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_integration", "error", "exception");
                _meter.IncrementErrorCount("command_handler", "create_integration", "processing_error");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro ao criar integração para alarme {AlarmId} com provedor {Provider} - CorrelationId: {CorrelationId}",
                    request.AlarmId, request.Provider, _correlationContext.CorrelationId);

                throw;
            }
        }

        private static string GenerateIntegrationName(string provider, string alarmName)
        {
            return provider.ToLowerInvariant() switch
            {
                "google" => $"Google Calendar - {alarmName}",
                "outlook" => $"Microsoft Outlook - {alarmName}",
                "slack" => $"Slack Notifications - {alarmName}",
                "teams" => $"Microsoft Teams - {alarmName}",
                "webhook" => $"Webhook Integration - {alarmName}",
                _ => $"{provider} Integration - {alarmName}"
            };
        }

        private static CreateIntegrationResponse MapToResponse(Integration integration, bool isNewlyCreated)
        {
            // Parse configuration back to dictionary
            var configurationDict = new Dictionary<string, string>();
            try
            {
                if (!string.IsNullOrWhiteSpace(integration.Configuration))
                {
                    configurationDict = JsonSerializer.Deserialize<Dictionary<string, string>>(integration.Configuration) 
                                       ?? new Dictionary<string, string>();
                }
            }
            catch (JsonException)
            {
                // Se não conseguir deserializar, manter vazio
                configurationDict = new Dictionary<string, string>();
            }

            // Determinar se requer autenticação baseado no provedor
            bool requiresAuth = integration.Provider.ToLowerInvariant() switch
            {
                "google" => true,
                "outlook" => true,
                "slack" => true,
                "teams" => true,
                "webhook" => false,
                _ => true
            };

            // Gerar URL de autenticação se necessário
            string? authUrl = requiresAuth ? GenerateAuthUrl(integration.Provider, integration.Id) : null;

            // Determinar features suportadas pelo provedor
            string[] supportedFeatures = integration.Provider.ToLowerInvariant() switch
            {
                "google" => new[] { "calendar_sync", "notifications", "reminders" },
                "outlook" => new[] { "calendar_sync", "email_notifications", "teams_integration" },
                "slack" => new[] { "channel_notifications", "direct_messages", "status_updates" },
                "teams" => new[] { "chat_notifications", "meeting_integration", "status_updates" },
                "webhook" => new[] { "http_notifications", "custom_payloads" },
                _ => new[] { "basic_notifications" }
            };

            return new CreateIntegrationResponse
            {
                Id = integration.Id,
                AlarmId = integration.AlarmId,
                Provider = integration.Provider,
                Configuration = configurationDict,
                Status = isNewlyCreated ? "Created" : "AlreadyExists",
                CreatedAt = integration.CreatedAt,
                AuthRequired = requiresAuth,
                AuthUrl = authUrl,
                IsActive = integration.IsActive,
                Features = supportedFeatures
            };
        }

        private static string? GenerateAuthUrl(string provider, Guid integrationId)
        {
            // Em um ambiente real, essas URLs seriam configuradas via appsettings
            return provider.ToLowerInvariant() switch
            {
                "google" => $"https://accounts.google.com/oauth2/auth?client_id={{client_id}}&state={integrationId}",
                "outlook" => $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={{client_id}}&state={integrationId}",
                "slack" => $"https://slack.com/oauth/v2/authorize?client_id={{client_id}}&state={integrationId}",
                "teams" => $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={{client_id}}&state={integrationId}",
                _ => null
            };
        }
    }
}
