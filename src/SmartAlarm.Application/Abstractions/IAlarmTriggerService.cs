using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Abstractions;

public interface IAlarmTriggerService
{
    Task ScheduleAlarmAsync(Alarm alarm, CancellationToken cancellationToken = default);
    Task CancelAlarmAsync(Guid alarmId, CancellationToken cancellationToken = default);
    Task RescheduleAlarmAsync(Alarm alarm, CancellationToken cancellationToken = default);
    Task TriggerAlarmAsync(Guid alarmId, CancellationToken cancellationToken = default);
    Task ProcessMissedAlarmsAsync(CancellationToken cancellationToken = default);
    Task EscalateMissedAlarmAsync(Guid alarmId, int escalationLevel, CancellationToken cancellationToken = default);
}
