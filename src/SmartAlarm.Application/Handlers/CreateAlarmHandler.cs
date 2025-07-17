using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler para criação de alarme.
    /// </summary>
    public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponseDto>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<CreateAlarmHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public CreateAlarmHandler(
            IAlarmRepository alarmRepository, 
            ILogger<CreateAlarmHandler> logger,
            SmartAlarmMeter meter,
            BusinessMetrics businessMetrics,
            ICorrelationContext correlationContext)
        {
            _alarmRepository = alarmRepository;
            _logger = logger;
            _meter = meter;
            _businessMetrics = businessMetrics;
            _correlationContext = correlationContext;
        }

        public async Task<AlarmResponseDto> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogInformation(LogTemplates.CommandStarted, 
                nameof(CreateAlarmCommand), 
                correlationId, 
                request.Alarm.UserId);

            using var activity = SmartAlarmTracing.ActivitySource.StartActivity("CreateAlarmHandler.Handle");
            activity?.SetTag("user.id", request.Alarm.UserId.ToString());
            activity?.SetTag("alarm.name", request.Alarm.Name);
            activity?.SetTag("correlation.id", correlationId);

            try
            {
                // Após validação, Name e Time não podem ser nulos
                var alarm = new Alarm(Guid.NewGuid(), request.Alarm.Name!, request.Alarm.Time!.Value, true, request.Alarm.UserId);
                await _alarmRepository.AddAsync(alarm);
                
                activity?.SetTag("alarm.id", alarm.Id.ToString());
                activity?.SetStatus(ActivityStatusCode.Ok);
                
                // Métricas técnicas
                _meter.IncrementAlarmCount("standard", request.Alarm.UserId.ToString());
                _meter.RecordAlarmCreationDuration(stopwatch.ElapsedMilliseconds, "standard", true);
                
                // Métricas de negócio
                _businessMetrics.RecordAlarmProcessingTime(stopwatch.ElapsedMilliseconds, "standard", "creation");
                
                _logger.LogInformation(LogTemplates.CommandCompleted, 
                    nameof(CreateAlarmCommand), 
                    correlationId, 
                    stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "AlarmCreated",
                    new { AlarmId = alarm.Id, UserId = request.Alarm.UserId, AlarmName = request.Alarm.Name },
                    correlationId);

                var dto = new AlarmResponseDto
                {
                    Id = alarm.Id,
                    Name = alarm.Name.ToString(),
                    Time = alarm.Time,
                    Enabled = alarm.Enabled,
                    UserId = alarm.UserId,
                    CanTriggerNow = alarm.ShouldTriggerNow()
                };
                
                return dto;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("POST", "/alarms", "AlarmCreationError");
                
                _logger.LogError(LogTemplates.CommandFailed,
                    nameof(CreateAlarmCommand),
                    correlationId,
                    ex.Message,
                    stopwatch.ElapsedMilliseconds);
                
                throw;
            }
        }
    }
}
