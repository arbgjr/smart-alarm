using System;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.DTOs
{
    /// <summary>
    /// DTO de resposta para um alarme criado ou consultado.
    /// </summary>
    public class AlarmResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public bool Enabled { get; set; }
        public Guid UserId { get; set; }

        /// <summary>
        /// Construtor que mapeia a entidade de domínio Alarm para o DTO.
        /// </summary>
        /// <param name="alarm">Entidade Alarm</param>
        public AlarmResponseDto(Alarm alarm)
        {
            if (alarm == null) throw new ArgumentNullException(nameof(alarm));
            Id = alarm.Id;
            Name = alarm.Name.ToString();
            Time = alarm.Time;
            Enabled = alarm.Enabled;
            UserId = alarm.UserId;
        }

        // Construtor padrão para serialização/deserialização
        public AlarmResponseDto() { }
    }
}
