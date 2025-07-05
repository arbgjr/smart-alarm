using MediatR;
using System;

namespace SmartAlarm.Application.Commands.Integration
{
    public class UpdateIntegrationCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Configuration { get; set; } = string.Empty;
        public UpdateIntegrationCommand(Guid id, string name, string configuration)
        {
            Id = id;
            Name = name;
            Configuration = configuration;
        }
    }
}
