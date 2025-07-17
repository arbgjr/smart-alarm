using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Queries
{
    /// <summary>
    /// Query para buscar um alarme por ID
    /// </summary>
    public record GetAlarmByIdQuery(
        Guid AlarmId
    ) : IRequest<GetAlarmByIdResponse>;

    /// <summary>
    /// Response da busca de alarme por ID
    /// </summary>
    public record GetAlarmByIdResponse(
        Guid AlarmId,
        string Name,
        DateTime Time,
        bool Enabled,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid UserId,
        string? Description
    );

    /// <summary>
    /// Validator para query de busca por ID
    /// </summary>
    public class GetAlarmByIdQueryValidator : AbstractValidator<GetAlarmByIdQuery>
    {
        public GetAlarmByIdQueryValidator()
        {
            RuleFor(x => x.AlarmId)
                .NotEmpty()
                .WithMessage("AlarmId é obrigatório");
        }
    }

    /// <summary>
    /// Handler para buscar alarme por ID
    /// </summary>
    public class GetAlarmByIdQueryHandler : IRequestHandler<GetAlarmByIdQuery, GetAlarmByIdResponse>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IValidator<GetAlarmByIdQuery> _validator;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<GetAlarmByIdQueryHandler> _logger;

        public GetAlarmByIdQueryHandler(
            IAlarmRepository alarmRepository,
            IValidator<GetAlarmByIdQuery> validator,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<GetAlarmByIdQueryHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _validator = validator;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<GetAlarmByIdResponse> Handle(GetAlarmByIdQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("GetAlarmByIdQueryHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("alarm.id", request.AlarmId.ToString());
                activity?.SetTag("operation", "get_alarm_by_id");
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando busca de alarme por ID {AlarmId} - CorrelationId: {CorrelationId}",
                    request.AlarmId, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);
                    _meter.IncrementErrorCount("query", "get_alarm_by_id", "validation");
                    
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para busca de alarme: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);
                    
                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Buscar alarme no repositório
                var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId);
                
                if (alarm == null)
                {
                    activity?.SetTag("alarm.found", false);
                    _meter.IncrementErrorCount("query", "get_alarm_by_id", "not_found");
                    
                    _logger.LogWarning("Alarme {AlarmId} não encontrado - CorrelationId: {CorrelationId}",
                        request.AlarmId, _correlationContext.CorrelationId);
                    
                    throw new InvalidOperationException($"Alarme {request.AlarmId} não encontrado");
                }

                activity?.SetTag("alarm.found", true);
                activity?.SetTag("alarm.name", alarm.Name.Value);
                activity?.SetTag("alarm.enabled", alarm.Enabled.ToString());
                activity?.SetTag("user.id", alarm.UserId.ToString());

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_alarm_by_id", "success", "200");

                // Log de sucesso
                _logger.LogInformation("Alarme {AlarmId} encontrado com sucesso - Nome: '{AlarmName}', Enabled: {Enabled} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    alarm.Id, alarm.Name.Value, alarm.Enabled, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                // Retornar response
                return new GetAlarmByIdResponse(
                    alarm.Id,
                    alarm.Name.Value,
                    alarm.Time,
                    alarm.Enabled,
                    alarm.CreatedAt,
                    alarm.UpdatedAt,
                    alarm.UserId,
                    alarm.Description
                );
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_alarm_by_id", "validation_error", "400");
                throw;
            }
            catch (InvalidOperationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_alarm_by_id", "not_found", "404");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_alarm_by_id", "error", "500");
                _meter.IncrementErrorCount("query", "get_alarm_by_id", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro inesperado ao buscar alarme {AlarmId} - CorrelationId: {CorrelationId}",
                    request.AlarmId, _correlationContext.CorrelationId);
                
                throw;
            }
        }
    }
}
