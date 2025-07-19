using MediatR;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Comando para disparar um alarme com todas as integrações
    /// </summary>
    public record TriggerAlarmCommand : IRequest<TriggerAlarmResponse>
    {
        public Guid AlarmId { get; init; }
        public Guid UserId { get; init; }
        public DateTime TriggeredAt { get; init; }
        public string TriggerType { get; init; } = "scheduled"; // scheduled, manual, test

        public TriggerAlarmCommand(Guid alarmId, Guid userId, string triggerType = "scheduled")
        {
            AlarmId = alarmId;
            UserId = userId;
            TriggeredAt = DateTime.UtcNow;
            TriggerType = triggerType;
        }
    }

    /// <summary>
    /// Response para comando de disparo de alarme
    /// </summary>
    public record TriggerAlarmResponse
    {
        public Guid AlarmId { get; init; }
        public Guid UserId { get; init; }
        public DateTime TriggeredAt { get; init; }
        public bool Success { get; init; }
        public string Message { get; init; }
        public List<string> ActionsExecuted { get; init; } = new();
        public List<string> Notifications { get; init; } = new();

        public TriggerAlarmResponse(Guid alarmId, Guid userId, DateTime triggeredAt, bool success, string message)
        {
            AlarmId = alarmId;
            UserId = userId;
            TriggeredAt = triggeredAt;
            Success = success;
            Message = message;
        }
    }
}
