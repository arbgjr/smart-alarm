using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Queries;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler for GetAlarmByIdQuery.
    /// </summary>
    public class GetAlarmByIdHandler : IRequestHandler<GetAlarmByIdQuery, AlarmResponseDto?>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<GetAlarmByIdHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public GetAlarmByIdHandler(
            IAlarmRepository alarmRepository, 
            ILogger<GetAlarmByIdHandler> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _alarmRepository = alarmRepository;
            _logger = logger;
            _meter = meter;
            _correlationContext = correlationContext;
            _activitySource = activitySource;
        }

        public async Task<AlarmResponseDto?> Handle(GetAlarmByIdQuery request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogDebug(LogTemplates.QueryStarted, 
                nameof(GetAlarmByIdQuery), 
                new { AlarmId = request.Id });

            using var activity = _activitySource.StartActivity("GetAlarmByIdHandler.Handle");
            activity?.SetTag("alarm.id", request.Id.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "GetAlarmById");
            activity?.SetTag("handler", "GetAlarmByIdHandler");
            
            try
            {
                var alarm = await _alarmRepository.GetByIdAsync(request.Id);
                
                if (alarm == null)
                {
                    stopwatch.Stop();
                    _meter.IncrementErrorCount("QUERY", "Alarms", "NotFound");
                    
                    _logger.LogDebug(LogTemplates.QueryCompleted, 
                        nameof(GetAlarmByIdQuery), 
                        stopwatch.ElapsedMilliseconds, 
                        0);
                        
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    activity?.SetTag("record.found", false);
                    return null;
                }

                // Considera exceções e status de disparo
                var canTrigger = alarm.ShouldTriggerNow();

                stopwatch.Stop();
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetAlarmById", "Alarms");
                
                _logger.LogDebug(LogTemplates.QueryCompleted, 
                    nameof(GetAlarmByIdQuery), 
                    stopwatch.ElapsedMilliseconds, 
                    1);

                activity?.SetTag("alarm.name", alarm.Name);
                activity?.SetTag("alarm.user_id", alarm.UserId.ToString());
                activity?.SetTag("record.found", true);
                activity?.SetStatus(ActivityStatusCode.Ok);
                
                var dto = new AlarmResponseDto
                {
                    Id = alarm.Id,
                    Name = alarm.Name.ToString(),
                    Time = alarm.Time,
                    Enabled = alarm.Enabled,
                    UserId = alarm.UserId,
                    CanTriggerNow = canTrigger
                };
                
                return dto;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.IncrementErrorCount("QUERY", "Alarms", "QueryError");
                
                _logger.LogError(LogTemplates.QueryFailed, 
                    nameof(GetAlarmByIdQuery), 
                    stopwatch.ElapsedMilliseconds, 
                    ex.Message);
                    
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }
}
