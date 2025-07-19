using System;

namespace SmartAlarm.Application.DTOs.Integration
{
    public class CreateIntegrationDto
    {
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Configuration { get; set; } = string.Empty;
        public Guid AlarmId { get; set; }
    }
}
