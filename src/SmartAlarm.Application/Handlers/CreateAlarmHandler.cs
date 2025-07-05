using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler para criação de alarme.
    /// </summary>
    public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponseDto>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<CreateAlarmHandler> _logger;

        public CreateAlarmHandler(IAlarmRepository alarmRepository, ILogger<CreateAlarmHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _logger = logger;
        }

        public async Task<AlarmResponseDto> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
        {
            using var activity = SmartAlarmTracing.ActivitySource.StartActivity("CreateAlarmHandler.Handle");
            activity?.SetTag("user.id", request.Alarm.UserId.ToString());
            activity?.SetTag("alarm.name", request.Alarm.Name);

            // Após validação, Name e Time não podem ser nulos
            var alarm = new Alarm(Guid.NewGuid(), request.Alarm.Name!, request.Alarm.Time!.Value, true, request.Alarm.UserId);
            await _alarmRepository.AddAsync(alarm);
            _logger.LogInformation("Alarme criado: {AlarmId}", alarm.Id);
            activity?.SetTag("alarm.id", alarm.Id.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            SmartAlarmMetrics.AlarmsCreatedCounter.Add(1);
            return new AlarmResponseDto
            {
                Id = alarm.Id,
                Name = alarm.Name.ToString(),
                Time = alarm.Time,
                Enabled = alarm.Enabled,
                UserId = alarm.UserId
            };
        }
    }
}
