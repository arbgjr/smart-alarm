using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Integration;
using SmartAlarm.Domain.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Integration
{
    public class DeleteIntegrationHandler : IRequestHandler<DeleteIntegrationCommand, bool>
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly ILogger<DeleteIntegrationHandler> _logger;

        public DeleteIntegrationHandler(IIntegrationRepository integrationRepository, ILogger<DeleteIntegrationHandler> logger)
        {
            _integrationRepository = integrationRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteIntegrationCommand request, CancellationToken cancellationToken)
        {
            var integration = await _integrationRepository.GetByIdAsync(request.Id);
            if (integration == null) return false;
            await _integrationRepository.DeleteAsync(request.Id);
            _logger.LogInformation("Integration deleted: {IntegrationId}", request.Id);
            return true;
        }
    }
}
