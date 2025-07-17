using MediatR;
using System;
using System.Collections.Generic;

namespace SmartAlarm.IntegrationService.Application.Commands
{
    /// <summary>
    /// Comando para criar uma nova integração para um alarme específico.
    /// </summary>
    public class CreateIntegrationCommand : IRequest<CreateIntegrationResponse>
    {
        public Guid AlarmId { get; }
        public string Provider { get; }
        public Dictionary<string, string> Configuration { get; }
        public bool EnableNotifications { get; }
        public string[] Features { get; }

        public CreateIntegrationCommand(
            Guid alarmId, 
            string provider, 
            Dictionary<string, string> configuration, 
            bool enableNotifications = true, 
            string[]? features = null)
        {
            AlarmId = alarmId;
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Configuration = configuration ?? new Dictionary<string, string>();
            EnableNotifications = enableNotifications;
            Features = features ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Resposta do comando de criação de integração.
    /// </summary>
    public class CreateIntegrationResponse
    {
        public Guid Id { get; set; }
        public Guid AlarmId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public Dictionary<string, string> Configuration { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool AuthRequired { get; set; }
        public string? AuthUrl { get; set; }
        public bool IsActive { get; set; }
        public string[] Features { get; set; } = Array.Empty<string>();
    }
}
