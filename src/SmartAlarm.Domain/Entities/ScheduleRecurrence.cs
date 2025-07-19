namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Enum para representar o tipo de recorrência de um agendamento de alarme.
    /// </summary>
    public enum ScheduleRecurrence
    {
        Once = 0,
        Daily = 1,
        Weekly = 2,
        Weekdays = 3,
        Weekends = 4
    }
}
