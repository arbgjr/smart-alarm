using System;

namespace SmartAlarm.Application.DTOs.Routine
{
    public class CreateRoutineDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }
}
