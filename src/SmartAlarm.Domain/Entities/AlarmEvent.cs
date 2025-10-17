using System;
using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa um evento de alarme para análise de padrões
    /// </summary>
    public class AlarmEvent
    {
        public Guid Id { get; private set; }
        public Guid AlarmId { get; private set; }
        public Guid UserId { get; private set; }
        public AlarmEventType EventType { get; private set; }
        public DateTime Timestamp { get; private set; }
        public DayOfWeek DayOfWeek { get; private set; }
        public TimeOnly Time { get; private set; }
        public int? SnoozeMinutes { get; private set; }
        public string? Metadata { get; private set; }
        public string? Location { get; private set; }
        public string? DeviceInfo { get; private set; }

        // Navigation property
        public virtual Alarm Alarm { get; private set; } = null!;

        // Private constructor for EF Core
        private AlarmEvent() { }

        /// <summary>
        /// Cria um novo evento de alarme
        /// </summary>
        public AlarmEvent(
            Guid alarmId,
            Guid userId,
            AlarmEventType eventType,
            DateTime timestamp,
            int? snoozeMinutes = null,
            string? metadata = null,
            string? location = null,
            string? deviceInfo = null)
        {
            if (alarmId == Guid.Empty)
                throw new ArgumentException("AlarmId é obrigatório", nameof(alarmId));
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId é obrigatório", nameof(userId));

            Id = Guid.NewGuid();
            AlarmId = alarmId;
            UserId = userId;
            EventType = eventType;
            Timestamp = timestamp;
            DayOfWeek = timestamp.DayOfWeek;
            Time = TimeOnly.FromDateTime(timestamp);
            SnoozeMinutes = snoozeMinutes;
            Metadata = metadata;
            Location = location;
            DeviceInfo = deviceInfo;
        }

        /// <summary>
        /// Cria evento de alarme criado
        /// </summary>
        public static AlarmEvent AlarmCreated(Guid alarmId, Guid userId, DateTime timestamp, string? metadata = null)
        {
            return new AlarmEvent(alarmId, userId, AlarmEventType.Created, timestamp, null, metadata);
        }

        /// <summary>
        /// Cria evento de alarme disparado
        /// </summary>
        public static AlarmEvent AlarmTriggered(Guid alarmId, Guid userId, DateTime timestamp, string? location = null)
        {
            return new AlarmEvent(alarmId, userId, AlarmEventType.Triggered, timestamp, null, null, location);
        }

        /// <summary>
        /// Cria evento de alarme soneca
        /// </summary>
        public static AlarmEvent AlarmSnoozed(Guid alarmId, Guid userId, DateTime timestamp, int snoozeMinutes)
        {
            return new AlarmEvent(alarmId, userId, AlarmEventType.Snoozed, timestamp, snoozeMinutes);
        }

        /// <summary>
        /// Cria evento de alarme desativado
        /// </summary>
        public static AlarmEvent AlarmDisabled(Guid alarmId, Guid userId, DateTime timestamp, string? reason = null)
        {
            return new AlarmEvent(alarmId, userId, AlarmEventType.Disabled, timestamp, null, reason);
        }

        /// <summary>
        /// Cria evento de alarme ignorado
        /// </summary>
        public static AlarmEvent AlarmDismissed(Guid alarmId, Guid userId, DateTime timestamp)
        {
            return new AlarmEvent(alarmId, userId, AlarmEventType.Dismissed, timestamp);
        }

        /// <summary>
        /// Cria evento de alarme modificado
        /// </summary>
        public static AlarmEvent AlarmModified(Guid alarmId, Guid userId, DateTime timestamp, string? changes = null)
        {
            return new AlarmEvent(alarmId, userId, AlarmEventType.Modified, timestamp, null, changes);
        }

        /// <summary>
        /// Verifica se o evento ocorreu dentro de uma janela de tempo
        /// </summary>
        public bool OccurredWithin(TimeSpan window, DateTime referenceTime)
        {
            var diff = Math.Abs((Timestamp - referenceTime).TotalMinutes);
            return diff <= window.TotalMinutes;
        }

        /// <summary>
        /// Verifica se o evento é de um tipo específico
        /// </summary>
        public bool IsOfType(AlarmEventType eventType)
        {
            return EventType == eventType;
        }

        /// <summary>
        /// Verifica se o evento ocorreu em um dia da semana específico
        /// </summary>
        public bool OccurredOn(DayOfWeek dayOfWeek)
        {
            return DayOfWeek == dayOfWeek;
        }

        /// <summary>
        /// Verifica se o evento ocorreu em um horário aproximado
        /// </summary>
        public bool OccurredAround(TimeOnly targetTime, TimeSpan tolerance)
        {
            var timeDiff = Math.Abs((Time.ToTimeSpan() - targetTime.ToTimeSpan()).TotalMinutes);
            return timeDiff <= tolerance.TotalMinutes;
        }

        public override string ToString()
        {
            return $"{EventType} - {DayOfWeek} {Time} ({Timestamp:yyyy-MM-dd})";
        }
    }

    /// <summary>
    /// Tipos de eventos de alarme
    /// </summary>
    public enum AlarmEventType
    {
        [Display(Name = "Criado")]
        Created,

        [Display(Name = "Disparado")]
        Triggered,

        [Display(Name = "Soneca")]
        Snoozed,

        [Display(Name = "Desativado")]
        Disabled,

        [Display(Name = "Ignorado")]
        Dismissed,

        [Display(Name = "Modificado")]
        Modified,

        [Display(Name = "Ativado")]
        Enabled,

        [Display(Name = "Excluído")]
        Deleted,

        [Display(Name = "Escalado")]
        Escalated
    }
}
