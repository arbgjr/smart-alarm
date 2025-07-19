using MediatR;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Comando para atualizar status de ativação/desativação de alarme
    /// </summary>
    public record UpdateAlarmStatusCommand : IRequest<UpdateAlarmStatusResponse>
    {
        public Guid AlarmId { get; init; }
        public bool IsActive { get; init; }
        public string? Reason { get; init; }
        public Guid UserId { get; init; }

        public UpdateAlarmStatusCommand(Guid alarmId, bool isActive, Guid userId, string? reason = null)
        {
            AlarmId = alarmId;
            IsActive = isActive;
            UserId = userId;
            Reason = reason;
        }
    }

    /// <summary>
    /// Response para comando de atualização de status
    /// </summary>
    public record UpdateAlarmStatusResponse
    {
        public Guid AlarmId { get; init; }
        public bool IsActive { get; init; }
        public DateTime UpdatedAt { get; init; }
        public string Message { get; init; }

        public UpdateAlarmStatusResponse(Guid alarmId, bool isActive, DateTime updatedAt, string message)
        {
            AlarmId = alarmId;
            IsActive = isActive;
            UpdatedAt = updatedAt;
            Message = message;
        }
    }
}
