using MediatR;
using System;

namespace SmartAlarm.Application.Commands.Integration
{
    public class DeleteIntegrationCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public DeleteIntegrationCommand(Guid id)
        {
            Id = id;
        }
    }
}
