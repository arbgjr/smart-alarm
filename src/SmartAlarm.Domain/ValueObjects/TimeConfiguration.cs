using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartAlarm.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para representar configuraÃ§Ãµes de tempo e timezone.
    /// </summary>
    public class TimeConfiguration
    {
        public TimeOnly Time { get; }
        public string TimeZone { get; }

        public TimeConfiguration(TimeOnly time, string timeZone = "UTC")
        {
            if (string.IsNullOrWhiteSpace(timeZone))
                throw new ArgumentException("TimeZone Ã© obrigatÃ³rio.", nameof(timeZone));

            // Validate that the timezone is valid
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                throw new ArgumentException($"TimeZone '{timeZone}' Ã© invÃ¡lido.", nameof(timeZone));
            }

            Time = time;
            TimeZone = timeZone;
        }

        public DateTime GetDateTimeForToday()
        {
            // Cria DateTime com Kind=Unspecified para evitar erro de conversÃ£o
            var today = DateTime.Today;
            var dateTime = new DateTime(today.Year, today.Month, today.Day, Time.Hour, Time.Minute, Time.Second, DateTimeKind.Unspecified);
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZoneInfo);
        }

        public override string ToString() => $"{Time} ({TimeZone})";
        public override bool Equals(object? obj) => obj is TimeConfiguration other &&
                                                   Time == other.Time &&
                                                   TimeZone == other.TimeZone;
        public override int GetHashCode() => HashCode.Combine(Time, TimeZone);
    }
}
