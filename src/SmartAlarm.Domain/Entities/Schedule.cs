using System;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa a configuração de horário e recorrência de um alarme.
    /// </summary>
    public class Schedule
    {
        public Guid Id { get; private set; }
        public TimeOnly Time { get; private set; }
        public ScheduleRecurrence Recurrence { get; private set; }
        public DaysOfWeek DaysOfWeek { get; private set; }
        public bool IsActive { get; private set; }
        public Guid AlarmId { get; private set; }

        // Private constructor for EF Core
        private Schedule() { }

        public Schedule(Guid id, TimeOnly time, ScheduleRecurrence recurrence, DaysOfWeek daysOfWeek, Guid alarmId)
        {
            if (alarmId == Guid.Empty)
                throw new ArgumentException("AlarmId é obrigatório.", nameof(alarmId));

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Time = time;
            Recurrence = recurrence;
            DaysOfWeek = daysOfWeek;
            AlarmId = alarmId;
            IsActive = true;
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public void UpdateTime(TimeOnly newTime)
        {
            Time = newTime;
        }

        public void UpdateRecurrence(ScheduleRecurrence newRecurrence, DaysOfWeek daysOfWeek = DaysOfWeek.None)
        {
            Recurrence = newRecurrence;
            DaysOfWeek = daysOfWeek;
        }

        public bool ShouldTriggerToday()
        {
            var today = DateTime.Today.DayOfWeek;
            
            return Recurrence switch
            {
                ScheduleRecurrence.Once => true,
                ScheduleRecurrence.Daily => true,
                ScheduleRecurrence.Weekly => DaysOfWeek.HasFlag(ConvertToDaysOfWeekEnum(today)),
                ScheduleRecurrence.Weekdays => today >= System.DayOfWeek.Monday && today <= System.DayOfWeek.Friday,
                ScheduleRecurrence.Weekends => today == System.DayOfWeek.Saturday || today == System.DayOfWeek.Sunday,
                _ => false
            };
        }

        private static DaysOfWeek ConvertToDaysOfWeekEnum(System.DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                System.DayOfWeek.Sunday => DaysOfWeek.Sunday,
                System.DayOfWeek.Monday => DaysOfWeek.Monday,
                System.DayOfWeek.Tuesday => DaysOfWeek.Tuesday,
                System.DayOfWeek.Wednesday => DaysOfWeek.Wednesday,
                System.DayOfWeek.Thursday => DaysOfWeek.Thursday,
                System.DayOfWeek.Friday => DaysOfWeek.Friday,
                System.DayOfWeek.Saturday => DaysOfWeek.Saturday,
                _ => DaysOfWeek.None
            };
        }
    }

    /// <summary>
    /// Enum para tipos de recorrência de agenda.
    /// </summary>
    public enum ScheduleRecurrence
    {
        Once = 0,
        Daily = 1,
        Weekly = 2,
        Weekdays = 3,
        Weekends = 4
    }

    /// <summary>
    /// Enum com flags para dias da semana.
    /// </summary>
    [Flags]
    public enum DaysOfWeek
    {
        None = 0,
        Sunday = 1,
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Saturday = 64,
        All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
    }
}