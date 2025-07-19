using MediatR;
using SmartAlarm.Application.DTOs;

namespace SmartAlarm.Application.Commands
{
    /// <summary>
    /// Comando para criar um novo alarme.
    /// </summary>
    public class CreateAlarmCommand : IRequest<AlarmResponseDto>
    {
        public CreateAlarmDto Alarm { get; }
        public CreateAlarmCommand(CreateAlarmDto alarm)
        {
            Alarm = alarm;
        }
    }
}
