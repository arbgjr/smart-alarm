using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler para listagem de alarmes de um usuário.
    /// </summary>
    public class ListAlarmsHandler : IRequestHandler<ListAlarmsQuery, IList<AlarmResponseDto>>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<ListAlarmsHandler> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public ListAlarmsHandler(
            IAlarmRepository alarmRepository, 
            ILogger<ListAlarmsHandler> logger,
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

        public async Task<IList<AlarmResponseDto>> Handle(ListAlarmsQuery request, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogInformation(LogTemplates.QueryStarted, 
                nameof(ListAlarmsQuery), 
                request.UserId);

            using var activity = SmartAlarmTracing.ActivitySource.StartActivity("ListAlarmsHandler.Handle");
            activity?.SetTag("user.id", request.UserId.ToString());
            activity?.SetTag("correlation.id", correlationId);
            
            try
            {
                var alarms = await _alarmRepository.GetByUserIdAsync(request.UserId);
                var result = alarms.Select(a => {
                    var dto = new AlarmResponseDto
                    {
                        Id = a.Id,
                        Name = a.Name.ToString(),
                        Time = a.Time,
                        Enabled = a.Enabled,
                        UserId = a.UserId,
                        CanTriggerNow = a.ShouldTriggerNow()
                    };
                    return dto;
                }).ToList();
                
                activity?.SetTag("alarms.count", result.Count);
                activity?.SetStatus(ActivityStatusCode.Ok);
                
                // Métricas técnicas
                _meter.IncrementRequestCount("GET", "/alarms");
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "GET", "/alarms", "200");
                
                // Métricas de negócio
                _businessMetrics.UpdateUsersActiveToday(1);
                _businessMetrics.UpdateAlarmsPendingToday(result.Count(a => a.Enabled && a.CanTriggerNow));
                
                _logger.LogInformation(LogTemplates.QueryCompleted, 
                    nameof(ListAlarmsQuery), 
                    correlationId, 
                    stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "AlarmsListed",
                    new { UserId = request.UserId, Count = result.Count, ActiveCount = result.Count(a => a.Enabled) },
                    correlationId);

                return result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("GET", "/alarms", "AlarmListError");
                
                _logger.LogError(LogTemplates.QueryFailed,
                    nameof(ListAlarmsQuery),
                    correlationId,
                    ex.Message,
                    stopwatch.ElapsedMilliseconds);
                
                throw;
            }
        }
    }
}
