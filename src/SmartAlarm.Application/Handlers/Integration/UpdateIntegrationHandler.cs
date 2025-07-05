using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Integration;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Integration
{
    public class UpdateIntegrationHandler : IRequestHandler<UpdateIntegrationCommand, bool>
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly ILogger<UpdateIntegrationHandler> _logger;

        public UpdateIntegrationHandler(IIntegrationRepository integrationRepository, ILogger<UpdateIntegrationHandler> logger)
        {
            _integrationRepository = integrationRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateIntegrationCommand request, CancellationToken cancellationToken)
        {
            var integration = await _integrationRepository.GetByIdAsync(request.Id);
            if (integration == null) return false;
            integration.UpdateName(new Name(request.Name));
            integration.UpdateConfiguration(request.Configuration);
            await _integrationRepository.UpdateAsync(integration);
            _logger.LogInformation("Integration updated: {IntegrationId}", integration.Id);
            return true;
        }
    }
}
