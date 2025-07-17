using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler para exclusão de alarme.
    /// </summary>
    public class DeleteAlarmHandler : IRequestHandler<DeleteAlarmCommand, bool>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<DeleteAlarmHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public DeleteAlarmHandler(
            IAlarmRepository alarmRepository, 
            ILogger<DeleteAlarmHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            BusinessMetrics businessMetrics,
            ICorrelationContext correlationContext)
        {
            _alarmRepository = alarmRepository;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _businessMetrics = businessMetrics;
            _correlationContext = correlationContext;
        }

        public async Task<bool> Handle(DeleteAlarmCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogDebug(LogTemplates.CommandStarted, 
                nameof(DeleteAlarmCommand), 
                request.AlarmId,
                correlationId);

            using var activity = _activitySource.StartActivity("DeleteAlarmHandler.Handle");
            activity?.SetTag("alarm.id", request.AlarmId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "DeleteAlarm");
            activity?.SetTag("handler", "DeleteAlarmHandler");
            
            try
            {
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
                    
                    return false;
                }
                
                await _alarmRepository.DeleteAsync(request.AlarmId);
                
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("alarm.deleted", true);
                
                // Métricas técnicas
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "DeleteAlarm", "Success", "200");
                
                // Métricas de negócio
                _businessMetrics.IncrementAlarmDeleted(existing.UserId.ToString(), "standard", "user_request");
                _businessMetrics.RecordAlarmProcessingTime(stopwatch.ElapsedMilliseconds, "standard", "deletion");
                
                _logger.LogDebug(LogTemplates.CommandCompleted, 
                    nameof(DeleteAlarmCommand), 
                    stopwatch.ElapsedMilliseconds,
                    "Success");

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "AlarmDeleted",
                    new { AlarmId = request.AlarmId, UserId = existing.UserId },
                    correlationId);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("COMMAND", "Alarms", "DeleteError");
                
                _logger.LogError(LogTemplates.CommandFailed,
                    nameof(DeleteAlarmCommand),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
