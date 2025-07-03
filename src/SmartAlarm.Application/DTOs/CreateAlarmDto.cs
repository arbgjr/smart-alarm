using System;

namespace SmartAlarm.Application.DTOs
{
    /// <summary>
    /// DTO para criação de um novo alarme.
    /// </summary>
    public class CreateAlarmDto
    {
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public Guid UserId { get; set; }
    }
}
