using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Integration;
using SmartAlarm.Domain.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Integration
{
    public class ToggleIntegrationHandler : IRequestHandler<ToggleIntegrationCommand, bool>
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly ILogger<ToggleIntegrationHandler> _logger;

        public ToggleIntegrationHandler(IIntegrationRepository integrationRepository, ILogger<ToggleIntegrationHandler> logger)
        {
            _integrationRepository = integrationRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(ToggleIntegrationCommand request, CancellationToken cancellationToken)
        {
            var integration = await _integrationRepository.GetByIdAsync(request.Id);
            if (integration == null) return false;
            if (request.Enable)
                integration.Activate();
            else
                integration.Deactivate();
            await _integrationRepository.UpdateAsync(integration);
            _logger.LogInformation("Integration toggled: {IntegrationId} - Enabled: {Enabled}", integration.Id, request.Enable);
            return true;
        }
    }
}
