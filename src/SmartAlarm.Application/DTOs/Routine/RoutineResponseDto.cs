using System;
using System.Collections.Generic;

namespace SmartAlarm.Application.DTOs.Routine
{
    public class RoutineResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid AlarmId { get; set; }
        public List<string> Actions { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
