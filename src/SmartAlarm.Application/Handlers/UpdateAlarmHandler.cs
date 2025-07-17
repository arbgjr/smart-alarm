using System;
using System.Diagnostics;
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
using SmartAlarm.Observability.Tracing;

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
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public UpdateAlarmHandler(
            IAlarmRepository alarmRepository, 
            IValidator<CreateAlarmDto> validator, 
            ILogger<UpdateAlarmHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            BusinessMetrics businessMetrics,
            ICorrelationContext correlationContext)
        {
            _alarmRepository = alarmRepository;
            _validator = validator;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _businessMetrics = businessMetrics;
            _correlationContext = correlationContext;
        }

        public async Task<AlarmResponseDto> Handle(UpdateAlarmCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogDebug(LogTemplates.CommandStarted, 
                nameof(UpdateAlarmCommand), 
                request.Alarm.UserId,
                correlationId);

            using var activity = _activitySource.StartActivity("UpdateAlarmHandler.Handle");
            activity?.SetTag("alarm.id", request.AlarmId.ToString());
            activity?.SetTag("user.id", request.Alarm.UserId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "UpdateAlarm");
            activity?.SetTag("handler", "UpdateAlarmHandler");
            
            try
            {
                var validationResult = await _validator.ValidateAsync(request.Alarm, cancellationToken);
                if (!validationResult.IsValid)
                {
                    stopwatch.Stop();
                    activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                    _meter.IncrementErrorCount("COMMAND", "Alarms", "ValidationError");
                    
                    _logger.LogWarning(LogTemplates.ValidationFailed,
                        nameof(UpdateAlarmCommand),
                        validationResult.Errors.ToString());
                    
                    throw new ValidationException(validationResult.Errors.ToString());
                }
                
                var existing = await _alarmRepository.GetByIdAsync(request.AlarmId);
                if (existing == null)
                {
                    stopwatch.Stop();
                    activity?.SetStatus(ActivityStatusCode.Error, "Alarm not found");
                    _meter.IncrementErrorCount("COMMAND", "Alarms", "NotFound");
                    
                    _logger.LogWarning(LogTemplates.EntityNotFound,
                        "Alarm",
                        request.AlarmId,
                        correlationId);
                    
                    throw new SmartAlarm.Domain.Exceptions.NotFoundException("Alarm", request.AlarmId);
                }
                
                var updated = new Alarm(request.AlarmId, request.Alarm.Name!, request.Alarm.Time!.Value, existing.Enabled, request.Alarm.UserId);
                await _alarmRepository.UpdateAsync(updated);
                
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("alarm.updated", true);
                
                // Métricas técnicas
                _meter.IncrementAlarmCount("standard", request.Alarm.UserId.ToString());
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "UpdateAlarm", "Success", "200");
                
                // Métricas de negócio
                _businessMetrics.RecordAlarmProcessingTime(stopwatch.ElapsedMilliseconds, "standard", "update");
                
                _logger.LogDebug(LogTemplates.CommandCompleted, 
                    nameof(UpdateAlarmCommand), 
                    stopwatch.ElapsedMilliseconds,
                    "Success");

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
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("COMMAND", "Alarms", "UpdateError");
                
                _logger.LogError(LogTemplates.CommandFailed,
                    nameof(UpdateAlarmCommand),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
