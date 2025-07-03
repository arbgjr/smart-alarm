using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartAlarm.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para representar configurações de tempo e timezone.
    /// </summary>
    public class TimeConfiguration
    {
        public TimeOnly Time { get; }
        public string TimeZone { get; }

        public TimeConfiguration(TimeOnly time, string timeZone = "UTC")
        {
            if (string.IsNullOrWhiteSpace(timeZone))
                throw new ArgumentException("TimeZone é obrigatório.", nameof(timeZone));

            // Validate that the timezone is valid
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                throw new ArgumentException($"TimeZone '{timeZone}' é inválido.", nameof(timeZone));
            }

            Time = time;
            TimeZone = timeZone;
        }

        public DateTime GetDateTimeForToday()
        {
            var today = DateTime.Today;
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            var dateTime = today.Add(Time.ToTimeSpan());
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZoneInfo);
        }

        public override string ToString() => $"{Time} ({TimeZone})";
        public override bool Equals(object obj) => obj is TimeConfiguration other && 
                                                   Time == other.Time && 
                                                   TimeZone == other.TimeZone;
        public override int GetHashCode() => HashCode.Combine(Time, TimeZone);
    }
}