using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Integration;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Integration
{
    public class CreateIntegrationHandler : IRequestHandler<CreateIntegrationCommand, Guid>
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly ILogger<CreateIntegrationHandler> _logger;

        public CreateIntegrationHandler(IIntegrationRepository integrationRepository, ILogger<CreateIntegrationHandler> logger)
        {
            _integrationRepository = integrationRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateIntegrationCommand request, CancellationToken cancellationToken)
        {
            var integration = new SmartAlarm.Domain.Entities.Integration(
                Guid.NewGuid(),
                new Name(request.Integration.Name),
                request.Integration.Provider,
                request.Integration.Configuration,
                request.Integration.AlarmId
            );
            await _integrationRepository.AddAsync(integration);
            _logger.LogInformation("Integration created: {IntegrationId}", integration.Id);
            return integration.Id;
        }
    }
}
