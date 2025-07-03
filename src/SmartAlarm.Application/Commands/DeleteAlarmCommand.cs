using MediatR;
using System;

namespace SmartAlarm.Application.Commands
{
    /// <summary>
    /// Comando para excluir um alarme existente.
    /// </summary>
    public class DeleteAlarmCommand : IRequest<bool>
    {
        public Guid AlarmId { get; }
        public DeleteAlarmCommand(Guid alarmId)
        {
            AlarmId = alarmId;
        }
    }
}
