using System;

namespace SmartAlarm.Application.DTOs.Integration
{
    public class UpdateIntegrationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Configuration { get; set; } = string.Empty;
    }
}
