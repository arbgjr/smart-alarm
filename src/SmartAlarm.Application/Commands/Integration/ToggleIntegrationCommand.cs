using MediatR;
using System;

namespace SmartAlarm.Application.Commands.Integration
{
    public class ToggleIntegrationCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public bool Enable { get; set; }
        public ToggleIntegrationCommand(Guid id, bool enable)
        {
            Id = id;
            Enable = enable;
        }
    }
}
