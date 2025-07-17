using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using FluentValidation;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler para atualização de alarme.
    /// </summary>
    public class UpdateAlarmHandler : IRequestHandler<UpdateAlarmCommand, AlarmResponseDto>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IValidator<CreateAlarmDto> _validator;
        private readonly ILogger<UpdateAlarmHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public UpdateAlarmHandler(
            IAlarmRepository alarmRepository, 
            IValidator<CreateAlarmDto> validator, 
            ILogger<UpdateAlarmHandler> logger,
            SmartAlarmMeter meter,
            BusinessMetrics businessMetrics,
            ICorrelationContext correlationContext)
        {
            _alarmRepository = alarmRepository;
            _validator = validator;
            _logger = logger;
            _meter = meter;
            _businessMetrics = businessMetrics;
            _correlationContext = correlationContext;
        }

        public async Task<AlarmResponseDto> Handle(UpdateAlarmCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogInformation(LogTemplates.CommandStarted, 
                nameof(UpdateAlarmCommand), 
                correlationId, 
                request.Alarm.UserId);

            using var activity = SmartAlarmTracing.ActivitySource.StartActivity("UpdateAlarmHandler.Handle");
            activity?.SetTag("alarm.id", request.AlarmId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            
            try
            {
                var validationResult = await _validator.ValidateAsync(request.Alarm, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "Validation failed");
                    _meter.IncrementErrorCount("PUT", "/alarms", "ValidationError");
                    
                    _logger.LogWarning(LogTemplates.ValidationFailed,
                        nameof(UpdateAlarmCommand),
                        validationResult.Errors);
                    
                    throw new ValidationException(validationResult.Errors.ToString());
                }
                
                var existing = await _alarmRepository.GetByIdAsync(request.AlarmId);
                if (existing == null)
                {
                    activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "Alarm not found");
                    _meter.IncrementErrorCount("PUT", "/alarms", "NotFoundError");
                    
                    _logger.LogWarning(LogTemplates.EntityNotFound,
                        "Alarm",
                        request.AlarmId,
                        correlationId);
                    
                    throw new SmartAlarm.Domain.Exceptions.NotFoundException("Alarm", request.AlarmId);
                }
                
                var updated = new Alarm(request.AlarmId, request.Alarm.Name!, request.Alarm.Time!.Value, existing.Enabled, request.Alarm.UserId);
                await _alarmRepository.UpdateAsync(updated);
                
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                
                // Métricas técnicas
                _meter.IncrementAlarmCount("standard", request.Alarm.UserId.ToString());
                _meter.RecordAlarmCreationDuration(stopwatch.ElapsedMilliseconds, "standard", true);
                
                // Métricas de negócio
                _businessMetrics.RecordAlarmProcessingTime(stopwatch.ElapsedMilliseconds, "standard", "update");
                
                _logger.LogInformation(LogTemplates.CommandCompleted, 
                    nameof(UpdateAlarmCommand), 
                    correlationId, 
                    stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "AlarmUpdated",
                    new { AlarmId = updated.Id, UserId = request.Alarm.UserId, AlarmName = request.Alarm.Name },
                    correlationId);

                var dto = new AlarmResponseDto
                {
                    Id = updated.Id,
                    Name = updated.Name.ToString(),
                    Time = updated.Time,
                    Enabled = updated.Enabled,
                    UserId = updated.UserId,
                    CanTriggerNow = updated.ShouldTriggerNow()
                };
                
                return dto;
            }
            catch (ValidationException)
            {
                throw; // Re-throw validation exceptions as they're already handled above
            }
            catch (SmartAlarm.Domain.Exceptions.NotFoundException)
            {
                throw; // Re-throw not found exceptions as they're already handled above
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("PUT", "/alarms", "AlarmUpdateError");
                
                _logger.LogError(LogTemplates.CommandFailed,
                    nameof(UpdateAlarmCommand),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
