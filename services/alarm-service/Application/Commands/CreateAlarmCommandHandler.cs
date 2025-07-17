using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Command para criar um novo alarme
    /// </summary>
    public record CreateAlarmCommand(
        Guid UserId,
        string Name,
        DateTime Time,
        bool Enabled = true,
        string? Description = null
    ) : IRequest<CreateAlarmResponse>;

    /// <summary>
    /// Response do comando de criação de alarme
    /// </summary>
    public record CreateAlarmResponse(
        Guid AlarmId,
        string Name,
        DateTime Time,
        bool Enabled,
        DateTime CreatedAt,
        Guid UserId
    );

    /// <summary>
    /// Validator para o comando de criação de alarme
    /// </summary>
    public class CreateAlarmCommandValidator : AbstractValidator<CreateAlarmCommand>
    {
        public CreateAlarmCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId é obrigatório");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Nome do alarme é obrigatório")
                .MaximumLength(100)
                .WithMessage("Nome não pode exceder 100 caracteres");

            RuleFor(x => x.Time)
                .NotEmpty()
                .WithMessage("Horário é obrigatório");
        }
    }

    /// <summary>
    /// Handler para processar comando de criação de alarme
    /// </summary>
    public class CreateAlarmCommandHandler : IRequestHandler<CreateAlarmCommand, CreateAlarmResponse>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<CreateAlarmCommand> _validator;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<CreateAlarmCommandHandler> _logger;

        public CreateAlarmCommandHandler(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IValidator<CreateAlarmCommand> validator,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<CreateAlarmCommandHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _userRepository = userRepository;
            _validator = validator;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<CreateAlarmResponse> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("CreateAlarmCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("alarm.name", request.Name);
                activity?.SetTag("operation", "create_alarm");
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando criação de alarme '{AlarmName}' para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.Name, request.UserId, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);
                    _meter.IncrementErrorCount("command", "create_alarm", "validation");
                    
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para criação de alarme: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);
                    
                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Verificar se usuário existe
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    activity?.SetTag("user.found", false);
                    _meter.IncrementErrorCount("command", "create_alarm", "user_not_found");
                    
                    _logger.LogWarning("Usuário {UserId} não encontrado para criação de alarme - CorrelationId: {CorrelationId}",
                        request.UserId, _correlationContext.CorrelationId);
                    
                    throw new InvalidOperationException($"Usuário {request.UserId} não encontrado");
                }

                activity?.SetTag("user.found", true);
                activity?.SetTag("user.email", user.Email.Address);

                // Criar entidade do alarme usando o constructor correto
                var alarm = new Alarm(
                    id: Guid.NewGuid(),
                    name: request.Name,
                    time: request.Time,
                    enabled: request.Enabled,
                    userId: request.UserId
                );

                activity?.SetTag("alarm.id", alarm.Id.ToString());
                activity?.SetTag("alarm.enabled", alarm.Enabled.ToString());

                // Persistir no banco
                await _alarmRepository.AddAsync(alarm);

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_alarm", "success", "201");
                _meter.IncrementAlarmCount("scheduled", request.UserId.ToString());

                // Log de sucesso
                _logger.LogInformation("Alarme '{AlarmName}' criado com sucesso - ID: {AlarmId}, Enabled: {Enabled} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    alarm.Name.Value, alarm.Id, alarm.Enabled, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                // Retornar response
                return new CreateAlarmResponse(
                    alarm.Id,
                    alarm.Name.Value,
                    alarm.Time,
                    alarm.Enabled,
                    alarm.CreatedAt,
                    alarm.UserId
                );
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_alarm", "validation_error", "400");
                throw;
            }
            catch (InvalidOperationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_alarm", "business_error", "409");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_alarm", "error", "500");
                _meter.IncrementErrorCount("command", "create_alarm", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro inesperado ao criar alarme '{AlarmName}' para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.Name, request.UserId, _correlationContext.CorrelationId);
                
                throw;
            }
        }
    }
}
