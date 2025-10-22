using System;
using System.Collections.Generic;

namespace SmartAlarm.Application.DTOs
{
    /// <summary>
    /// DTO para transferência de dados de rotina
    /// </summary>
    public class RoutineDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid AlarmId { get; set; }
        public List<string> Actions { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
