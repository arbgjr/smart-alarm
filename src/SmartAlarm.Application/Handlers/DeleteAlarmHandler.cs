using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler para exclusão de alarme.
    /// </summary>
    public class DeleteAlarmHandler : IRequestHandler<DeleteAlarmCommand, bool>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<DeleteAlarmHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public DeleteAlarmHandler(
            IAlarmRepository alarmRepository, 
            ILogger<DeleteAlarmHandler> logger,
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

        public async Task<bool> Handle(DeleteAlarmCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogInformation(LogTemplates.CommandStarted, 
                nameof(DeleteAlarmCommand), 
                correlationId, 
                request.AlarmId);

            using var activity = SmartAlarmTracing.ActivitySource.StartActivity("DeleteAlarmHandler.Handle");
            activity?.SetTag("alarm.id", request.AlarmId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            
            try
            {
                var existing = await _alarmRepository.GetByIdAsync(request.AlarmId);
                if (existing == null)
                {
                    activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "Alarm not found");
                    _meter.IncrementErrorCount("DELETE", "/alarms", "NotFoundError");
                    
                    _logger.LogWarning(LogTemplates.EntityNotFound,
                        "Alarm",
                        request.AlarmId,
                        correlationId);
                    
                    return false;
                }
                
                await _alarmRepository.DeleteAsync(request.AlarmId);
                
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                
                // Métricas técnicas
                _meter.IncrementAlarmCount("standard", existing.UserId.ToString());
                _meter.RecordAlarmCreationDuration(stopwatch.ElapsedMilliseconds, "standard", true);
                
                // Métricas de negócio
                _businessMetrics.IncrementAlarmDeleted(existing.UserId.ToString(), "standard", "user_request");
                _businessMetrics.RecordAlarmProcessingTime(stopwatch.ElapsedMilliseconds, "standard", "deletion");
                
                _logger.LogInformation(LogTemplates.CommandCompleted, 
                    nameof(DeleteAlarmCommand), 
                    correlationId, 
                    stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "AlarmDeleted",
                    new { AlarmId = request.AlarmId, UserId = existing.UserId },
                    correlationId);

                return true;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DELETE", "/alarms", "AlarmDeletionError");
                
                _logger.LogError(LogTemplates.CommandFailed,
                    nameof(DeleteAlarmCommand),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
