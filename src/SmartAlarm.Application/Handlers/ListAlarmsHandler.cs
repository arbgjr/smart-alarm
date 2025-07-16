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

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler para listagem de alarmes de um usuário.
    /// </summary>
    public class ListAlarmsHandler : IRequestHandler<ListAlarmsQuery, IList<AlarmResponseDto>>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<ListAlarmsHandler> _logger;

        public ListAlarmsHandler(IAlarmRepository alarmRepository, ILogger<ListAlarmsHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _logger = logger;
        }

        public async Task<IList<AlarmResponseDto>> Handle(ListAlarmsQuery request, CancellationToken cancellationToken)
        {
            using var activity = SmartAlarmTracing.ActivitySource.StartActivity("ListAlarmsHandler.Handle");
            activity?.SetTag("user.id", request.UserId.ToString());
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
            _logger.LogInformation("{Count} alarmes retornados para o usuário {UserId}", result.Count, request.UserId);
            activity?.SetTag("alarms.count", result.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            SmartAlarmMetrics.AlarmsListedCounter.Add(1);
            return result;
        }
    }
}
