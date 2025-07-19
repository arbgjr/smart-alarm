
using MediatR;
using System;
using SmartAlarm.Application.DTOs.Integration;

namespace SmartAlarm.Application.Commands.Integration
{
    public class CreateIntegrationCommand : IRequest<Guid>
    {
        public CreateIntegrationDto Integration { get; set; }
        public CreateIntegrationCommand(CreateIntegrationDto integration)
        {
            Integration = integration;
        }
    }
}
