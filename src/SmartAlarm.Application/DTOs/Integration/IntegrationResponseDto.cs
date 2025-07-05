using System;
using System.Collections.Generic;

namespace SmartAlarm.Application.DTOs.Integration
{
    public class IntegrationResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public Dictionary<string, string> Configuration { get; set; } = new();
        public bool IsActive { get; set; }
    }
}
