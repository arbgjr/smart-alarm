using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Handler para atualização de status de alarme (ativar/desativar)
    /// </summary>
    public class UpdateAlarmStatusCommandHandler : IRequestHandler<UpdateAlarmStatusCommand, UpdateAlarmStatusResponse>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<UpdateAlarmStatusCommand> _validator;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<UpdateAlarmStatusCommandHandler> _logger;

        public UpdateAlarmStatusCommandHandler(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IValidator<UpdateAlarmStatusCommand> validator,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<UpdateAlarmStatusCommandHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _userRepository = userRepository;
            _validator = validator;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<UpdateAlarmStatusResponse> Handle(UpdateAlarmStatusCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("UpdateAlarmStatusCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("alarm.id", request.AlarmId.ToString());
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("alarm.target_status", request.IsActive.ToString());
                activity?.SetTag("operation", "update_alarm_status");
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando atualização de status do alarme {AlarmId} para {Status} - Usuário: {UserId} - CorrelationId: {CorrelationId}",
                    request.AlarmId, request.IsActive ? "ativo" : "inativo", request.UserId, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);
                    _meter.IncrementErrorCount("command", "update_alarm_status", "validation");
                    
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para atualização de status do alarme: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);
                    
                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Verificar se usuário existe
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    activity?.SetTag("user.found", false);
                    _meter.IncrementErrorCount("command", "update_alarm_status", "user_not_found");
                    
                    _logger.LogWarning("Usuário {UserId} não encontrado para atualização de status do alarme {AlarmId} - CorrelationId: {CorrelationId}",
                        request.UserId, request.AlarmId, _correlationContext.CorrelationId);
                    
                    throw new InvalidOperationException($"Usuário {request.UserId} não encontrado");
                }

                activity?.SetTag("user.found", true);

                // Buscar alarme
                var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId);
                if (alarm == null)
                {
                    activity?.SetTag("alarm.found", false);
                    _meter.IncrementErrorCount("command", "update_alarm_status", "alarm_not_found");
                    
                    _logger.LogWarning("Alarme {AlarmId} não encontrado - CorrelationId: {CorrelationId}",
                        request.AlarmId, _correlationContext.CorrelationId);
                    
                    throw new InvalidOperationException($"Alarme {request.AlarmId} não encontrado");
                }

                // Verificar se o alarme pertence ao usuário
                if (alarm.UserId != request.UserId)
                {
                    activity?.SetTag("alarm.owner_mismatch", true);
                    _meter.IncrementErrorCount("command", "update_alarm_status", "unauthorized");
                    
                    _logger.LogWarning("Usuário {UserId} tentou modificar alarme {AlarmId} que não lhe pertence - CorrelationId: {CorrelationId}",
                        request.UserId, request.AlarmId, _correlationContext.CorrelationId);
                    
                    throw new UnauthorizedAccessException($"Alarme {request.AlarmId} não pertence ao usuário {request.UserId}");
                }

                activity?.SetTag("alarm.found", true);
                activity?.SetTag("alarm.current_status", alarm.Enabled.ToString());

                // Verificar se o status já é o desejado
                if (alarm.Enabled == request.IsActive)
                {
                    _logger.LogInformation("Alarme {AlarmId} já está {Status} - Nenhuma alteração necessária - CorrelationId: {CorrelationId}",
                        request.AlarmId, request.IsActive ? "ativo" : "inativo", _correlationContext.CorrelationId);
                    
                    var noChangeResponse = new UpdateAlarmStatusResponse(
                        request.AlarmId,
                        alarm.Enabled,
                        DateTime.UtcNow,
                        $"Alarme já estava {(alarm.Enabled ? "ativo" : "inativo")}"
                    );

                    _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "update_alarm_status", "no_change", "200");
                    return noChangeResponse;
                }

                // Atualizar status do alarme
                var updatedAlarm = new Alarm(
                    alarm.Id,
                    alarm.Name,
                    alarm.Time,
                    request.IsActive, // Novo status
                    alarm.UserId
                );

                await _alarmRepository.UpdateAsync(updatedAlarm);

                stopwatch.Stop();
                activity?.SetTag("alarm.status_updated", true);
                activity?.SetTag("alarm.new_status", request.IsActive.ToString());

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "update_alarm_status", "success", "200");
                _meter.IncrementAlarmCount(request.IsActive ? "activated" : "deactivated", request.UserId.ToString());

                // Log de sucesso
                _logger.LogInformation("Status do alarme '{AlarmName}' ({AlarmId}) atualizado para {Status} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    alarm.Name.Value, alarm.Id, request.IsActive ? "ativo" : "inativo", stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                // Retornar response
                return new UpdateAlarmStatusResponse(
                    request.AlarmId,
                    request.IsActive,
                    DateTime.UtcNow,
                    request.IsActive ? "Alarme ativado com sucesso" : "Alarme desativado com sucesso"
                );
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "update_alarm_status", "validation_error", "400");
                throw;
            }
            catch (InvalidOperationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "update_alarm_status", "business_error", "404");
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "update_alarm_status", "unauthorized", "403");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "update_alarm_status", "error", "500");
                _meter.IncrementErrorCount("command", "update_alarm_status", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro inesperado ao atualizar status do alarme {AlarmId} - CorrelationId: {CorrelationId}",
                    request.AlarmId, _correlationContext.CorrelationId);
                
                throw;
            }
        }
    }
}
