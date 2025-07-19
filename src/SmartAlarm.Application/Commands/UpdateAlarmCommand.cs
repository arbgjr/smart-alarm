using MediatR;
using SmartAlarm.Application.DTOs;
using System;

namespace SmartAlarm.Application.Commands
{
    /// <summary>
    /// Comando para atualizar um alarme existente.
    /// </summary>
    public class UpdateAlarmCommand : IRequest<AlarmResponseDto>
    {
        public Guid AlarmId { get; }
        public CreateAlarmDto Alarm { get; }
        public UpdateAlarmCommand(Guid alarmId, CreateAlarmDto alarm)
        {
            AlarmId = alarmId;
            Alarm = alarm;
        }
    }
}
