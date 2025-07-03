using System;

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
    }
}
