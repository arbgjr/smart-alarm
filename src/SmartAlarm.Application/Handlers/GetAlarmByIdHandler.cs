using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Queries;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler for GetAlarmByIdQuery.
    /// </summary>
    public class GetAlarmByIdHandler : IRequestHandler<GetAlarmByIdQuery, AlarmResponseDto?>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<GetAlarmByIdHandler> _logger;

        public GetAlarmByIdHandler(IAlarmRepository alarmRepository, ILogger<GetAlarmByIdHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _logger = logger;
        }

        public async Task<AlarmResponseDto?> Handle(GetAlarmByIdQuery request, CancellationToken cancellationToken)
        {
            using var activity = SmartAlarmTracing.ActivitySource.StartActivity("GetAlarmByIdHandler.Handle");
            activity?.SetTag("alarm.id", request.Id.ToString());
            
            var alarm = await _alarmRepository.GetByIdAsync(request.Id);
            if (alarm == null)
            {
                _logger.LogWarning("Alarm not found: {AlarmId}", request.Id);
                activity?.SetStatus(ActivityStatusCode.Error, "Alarm not found");
                SmartAlarmMetrics.NotFoundErrorsCounter.Add(1);
                return null;
            }
            
            _logger.LogInformation("Alarm retrieved: {AlarmId}", alarm.Id);
            activity?.SetTag("alarm.name", alarm.Name);
            activity?.SetTag("alarm.user_id", alarm.UserId.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            SmartAlarmMetrics.AlarmsRetrievedCounter.Add(1);
            return new AlarmResponseDto(alarm);
        }
    }
}
