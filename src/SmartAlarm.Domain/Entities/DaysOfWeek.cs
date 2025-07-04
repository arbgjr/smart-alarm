using System;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Enum para representar os dias da semana para agendamento de alarmes.
    /// </summary>
    [Flags]
    public enum DaysOfWeek
    {
        None = 0,
        Sunday = 1 << 0,
        Monday = 1 << 1,
        Tuesday = 1 << 2,
        Wednesday = 1 << 3,
        Thursday = 1 << 4,
        Friday = 1 << 5,
        Saturday = 1 << 6,
        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
        Weekends = Saturday | Sunday,
        All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
    }
}
